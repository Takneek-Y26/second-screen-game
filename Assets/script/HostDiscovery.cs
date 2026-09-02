using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

// Put this on your NetworkManager object alongside SwipeListener.
// Listens for a broadcast "DISCOVER_DOORHOST" from the phone and replies with this PC's IP.
public class HostDiscovery : MonoBehaviour
{
    const int DISCOVERY_PORT = 47500;

    private UdpClient discoveryListener;

    void Start()
    {
        try
        {
            discoveryListener = new UdpClient(DISCOVERY_PORT);
            discoveryListener.BeginReceive(OnDiscoveryReceived, null);
            Debug.Log("[HostDiscovery] Waiting for phone discovery broadcasts on port " + DISCOVERY_PORT);
        }
        catch (Exception e)
        {
            Debug.LogError("[HostDiscovery] Failed to start: " + e.Message);
        }
    }

    void OnDiscoveryReceived(IAsyncResult ar)
    {
        try
        {
            IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
            byte[] data = discoveryListener.EndReceive(ar, ref sender);
            string msg = Encoding.UTF8.GetString(data);

            if (msg == "DISCOVER_DOORHOST")
            {
                byte[] reply = Encoding.UTF8.GetBytes("DOORHOST_HERE");
                discoveryListener.Send(reply, reply.Length, sender); // reply straight to sender's IP
                Debug.Log("[HostDiscovery] Replied to phone at " + sender.Address);
            }
        }
        catch (ObjectDisposedException)
        {
            return; // socket closed, stop quietly
        }
        catch (Exception e)
        {
            Debug.LogWarning("[HostDiscovery] Receive error: " + e.Message);
        }

        try { discoveryListener.BeginReceive(OnDiscoveryReceived, null); }
        catch (ObjectDisposedException) { /* socket closed, fine */ }
    }

    void OnDestroy()
    {
        discoveryListener?.Close();
    }
}