using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

// Put this on your NetworkManager object alongside HostDiscovery.
// Listens on PORT for "SWIPE:<doorID>" packets sent from the phone.
public class SwipeListener : MonoBehaviour
{
    public int port = 47501;

    private UdpClient udp;
    private readonly Queue<string> incomingMessages = new Queue<string>();
    private readonly object queueLock = new object();

    void Start()
    {
        try
        {
            udp = new UdpClient(port);
            udp.BeginReceive(OnReceive, null);
            Debug.Log("[SwipeListener] Listening for swipes on port " + port);
        }
        catch (Exception e)
        {
            Debug.LogError("[SwipeListener] Failed to start listener: " + e.Message);
        }
    }

    void OnReceive(IAsyncResult ar)
    {
        try
        {
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
            byte[] data = udp.EndReceive(ar, ref ep);
            string msg = Encoding.UTF8.GetString(data);

            lock (queueLock)
                incomingMessages.Enqueue(msg);
        }
        catch (ObjectDisposedException)
        {
            // socket was closed (e.g. on scene exit) - stop the receive loop quietly
            return;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[SwipeListener] Receive error: " + e.Message);
        }

        // keep listening for the next packet
        try { udp.BeginReceive(OnReceive, null); }
        catch (ObjectDisposedException) { /* socket closed, fine */ }
    }

    // Drain the queue on the main thread - never touch Unity objects from OnReceive directly,
    // since UDP callbacks fire on a background thread.
    void Update()
    {
        lock (queueLock)
        {
            while (incomingMessages.Count > 0)
            {
                string msg = incomingMessages.Dequeue();
                HandleMessage(msg);
            }
        }
    }

    void HandleMessage(string msg)
    {
        if (string.IsNullOrEmpty(msg)) return;

        if (msg.StartsWith("SWIPE:"))
        {
            string doorID = msg.Substring("SWIPE:".Length);

            if (InteractionSession.Instance != null)
                InteractionSession.Instance.OnSwipeReceived(doorID);
            else
                Debug.LogWarning("[SwipeListener] No InteractionSession in scene to handle swipe.");
        }
    }

    void OnDestroy()
    {
        udp?.Close();
    }
}