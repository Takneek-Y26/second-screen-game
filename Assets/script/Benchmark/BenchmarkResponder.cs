using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

// Put this on the endpoint being benchmarked (the phone app, or the PC, whichever side
// NetworkBenchmark is NOT running on). Listens on `listenPort` and immediately echoes
// every ping_test message back as a pong_test with the same msg_id/timestamp, so the
// sender can compute round-trip time from its own clock.
public class BenchmarkResponder : MonoBehaviour
{
    public int listenPort = 47502;

    private UdpClient udp;
    private Thread listenThread;
    private volatile bool running;

    void OnEnable()
    {
        try
        {
            udp = new UdpClient(listenPort);
            running = true;
            listenThread = new Thread(Listen) { IsBackground = true };
            listenThread.Start();
            Debug.Log("[BenchmarkResponder] Listening on port " + listenPort);
        }
        catch (Exception e)
        {
            Debug.LogError("[BenchmarkResponder] Failed to start: " + e.Message);
            enabled = false;
        }
    }

    void OnDisable()
    {
        running = false;
        udp?.Close();
        udp = null;
    }

    void Listen()
    {
        IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
        while (running)
        {
            try
            {
                byte[] data = udp.Receive(ref remote);
                string json = Encoding.UTF8.GetString(data);
                var msg = JsonUtility.FromJson<BenchmarkMessage>(json);
                if (msg == null || msg.payload != BenchmarkMessage.PingPayload) continue;

                msg.payload = BenchmarkMessage.PongPayload;
                string reply = JsonUtility.ToJson(msg);
                byte[] replyData = Encoding.UTF8.GetBytes(reply);
                udp.Send(replyData, replyData.Length, remote);
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
                if (running) Debug.LogWarning("[BenchmarkResponder] Error: " + e.Message);
            }
        }
    }
}
