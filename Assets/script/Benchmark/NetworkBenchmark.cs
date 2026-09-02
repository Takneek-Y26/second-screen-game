using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

// Sends periodic UDP ping_test messages to a target endpoint and measures round-trip
// stats (latency, jitter, packet loss). Drop this on the same object as PhoneDiscovery
// (or wire targetIP/targetPort manually) and point it at whatever is running
// BenchmarkResponder on the other end (phone app, PC, etc).
//
// Enable/disable this component (e.g. from the "Benchmark Mode" toggle) to start/stop
// the test. Read the public Stats snapshot from UI code every frame - it's updated on
// the main thread in Update(), no locking needed by callers.
public class NetworkBenchmark : MonoBehaviour
{
    public enum Protocol { UDP, TCP }

    [Header("Endpoint")]
    public Protocol protocol = Protocol.UDP;
    public string targetIP = "127.0.0.1";
    public int targetPort = 47502;

    [Header("Test parameters")]
    [Tooltip("Seconds between ping messages.")]
    public float sendInterval = 0.5f;
    [Tooltip("How long to wait for a pong before counting the ping as lost.")]
    public float timeoutSeconds = 2f;
    [Tooltip("Number of recent samples kept for min/max/avg/jitter.")]
    public int rollingWindow = 60;

    [Serializable]
    public struct Stats
    {
        public bool running;
        public int sent;
        public int received;
        public int lost;
        public float lastRttMs;
        public float minRttMs;
        public float maxRttMs;
        public float avgRttMs;
        public float jitterMs;      // mean absolute deviation between consecutive RTTs
        public float lossPercent;
    }

    public Stats CurrentStats { get; private set; }

    private UdpClient udp;
    private Thread receiveThread;
    private volatile bool running;

    private int nextMsgId = 1;
    private float sendTimer;

    // msg_id -> send time (ms since startup, via Time.realtimeSinceStartup*1000)
    private readonly ConcurrentDictionary<int, double> pending = new ConcurrentDictionary<int, double>();
    private readonly ConcurrentQueue<float> rttQueue = new ConcurrentQueue<float>();

    private readonly System.Collections.Generic.Queue<float> rttSamples = new System.Collections.Generic.Queue<float>();
    private float rttSum;
    private float lastSampleForJitter = -1f;
    private float jitterAccum;
    private int jitterCount;

    private int sentCount;
    private int receivedCount;

    void OnEnable()
    {
        if (protocol != Protocol.UDP)
        {
            Debug.LogWarning("[NetworkBenchmark] Only UDP is implemented; falling back to UDP.");
        }

        ResetStats();

        try
        {
            udp = new UdpClient(0); // any local port
            udp.Client.ReceiveTimeout = 0;
            running = true;

            receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
            receiveThread.Start();

            Debug.Log($"[NetworkBenchmark] Started, pinging {targetIP}:{targetPort}");
        }
        catch (Exception e)
        {
            Debug.LogError("[NetworkBenchmark] Failed to start: " + e.Message);
            enabled = false;
        }
    }

    void OnDisable()
    {
        running = false;
        udp?.Close();
        udp = null;
        pending.Clear();
    }

    void ResetStats()
    {
        sentCount = 0;
        receivedCount = 0;
        rttSamples.Clear();
        rttSum = 0f;
        lastSampleForJitter = -1f;
        jitterAccum = 0f;
        jitterCount = 0;
        nextMsgId = 1;
        pending.Clear();
        CurrentStats = new Stats { running = true, minRttMs = float.MaxValue };
    }

    void Update()
    {
        // Send pings on the main thread at a fixed cadence.
        sendTimer += Time.unscaledDeltaTime;
        if (sendTimer >= sendInterval)
        {
            sendTimer = 0f;
            SendPing();
        }

        // Expire pings nobody answered within the timeout.
        double now = Time.realtimeSinceStartupAsDouble * 1000.0;
        foreach (var kvp in pending)
        {
            if (now - kvp.Value > timeoutSeconds * 1000.0)
            {
                if (pending.TryRemove(kvp.Key, out _))
                {
                    // counted as lost via sent-received below
                }
            }
        }

        // Drain RTT results reported by the receive thread.
        while (rttQueue.TryDequeue(out float rtt))
        {
            ApplyRtt(rtt);
        }

        PublishStats();
    }

    void SendPing()
    {
        if (udp == null) return;

        var msg = new BenchmarkMessage
        {
            msg_id = nextMsgId++,
            timestamp = BenchmarkMessage.NowMillis(),
            payload = BenchmarkMessage.PingPayload
        };

        pending[msg.msg_id] = Time.realtimeSinceStartupAsDouble * 1000.0;
        sentCount++;

        try
        {
            string json = JsonUtility.ToJson(msg);
            byte[] data = Encoding.UTF8.GetBytes(json);
            udp.Send(data, data.Length, targetIP, targetPort);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[NetworkBenchmark] Send failed: " + e.Message);
        }
    }

    void ReceiveLoop()
    {
        IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
        while (running)
        {
            try
            {
                byte[] data = udp.Receive(ref remote);
                string json = Encoding.UTF8.GetString(data);
                var msg = JsonUtility.FromJson<BenchmarkMessage>(json);
                if (msg == null || msg.payload != BenchmarkMessage.PongPayload) continue;

                if (pending.TryRemove(msg.msg_id, out double sentAtMs))
                {
                    double nowMs = Time.realtimeSinceStartupAsDouble * 1000.0;
                    float rtt = (float)(nowMs - sentAtMs);
                    rttQueue.Enqueue(rtt);
                }
            }
            catch (ObjectDisposedException)
            {
                return; // socket closed on OnDisable, exit quietly
            }
            catch (SocketException)
            {
                if (!running) return;
            }
            catch (Exception e)
            {
                if (running) Debug.LogWarning("[NetworkBenchmark] Receive error: " + e.Message);
            }
        }
    }

    void ApplyRtt(float rtt)
    {
        receivedCount++;

        rttSamples.Enqueue(rtt);
        rttSum += rtt;
        while (rttSamples.Count > rollingWindow)
        {
            rttSum -= rttSamples.Dequeue();
        }

        if (lastSampleForJitter >= 0f)
        {
            jitterAccum += Mathf.Abs(rtt - lastSampleForJitter);
            jitterCount++;
        }
        lastSampleForJitter = rtt;

        var s = CurrentStats;
        s.lastRttMs = rtt;
        s.minRttMs = Mathf.Min(s.minRttMs == float.MaxValue ? rtt : s.minRttMs, rtt);
        s.maxRttMs = Mathf.Max(s.maxRttMs, rtt);
        CurrentStats = s;
    }

    void PublishStats()
    {
        var s = CurrentStats;
        s.running = true;
        s.sent = sentCount;
        s.received = receivedCount;
        s.lost = Mathf.Max(0, sentCount - receivedCount - pending.Count);
        s.avgRttMs = rttSamples.Count > 0 ? rttSum / rttSamples.Count : 0f;
        s.jitterMs = jitterCount > 0 ? jitterAccum / jitterCount : 0f;
        s.lossPercent = sentCount > 0 ? 100f * s.lost / sentCount : 0f;
        if (s.minRttMs == float.MaxValue) s.minRttMs = 0f;
        CurrentStats = s;
    }
}
