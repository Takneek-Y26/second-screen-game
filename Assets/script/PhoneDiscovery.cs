using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

// Put this on a NetworkManager object in the phone scene.
// Call FindHost() from a "Connect" UI button.
public class PhoneDiscovery : MonoBehaviour
{
    [Header("Status (read-only, shown for debugging)")]
    public string hostIP = "";
    public bool isConnected = false;

    [Header("Ports (must match the PC scripts)")]
    public int discoveryPort = 47500;
    public int swipePort = 47501;

    private UdpClient udp;

    public void FindHost()
    {
        try
        {
            isConnected = false;
            hostIP = "";

            udp = new UdpClient();
            udp.EnableBroadcast = true;

            byte[] msg = Encoding.UTF8.GetBytes("DISCOVER_DOORHOST");
            udp.Send(msg, msg.Length, new IPEndPoint(IPAddress.Broadcast, discoveryPort));
            udp.BeginReceive(OnHostFound, null);

            Debug.Log("[PhoneDiscovery] Broadcast sent, waiting for host reply...");
        }
        catch (Exception e)
        {
            Debug.LogError("[PhoneDiscovery] FindHost failed: " + e.Message);
        }
    }

    void OnHostFound(IAsyncResult ar)
    {
        try
        {
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
            byte[] data = udp.EndReceive(ar, ref ep);
            string reply = Encoding.UTF8.GetString(data);

            if (reply == "DOORHOST_HERE")
            {
                hostIP = ep.Address.ToString();
                isConnected = true;
                Debug.Log("[PhoneDiscovery] Host found at " + hostIP);
            }
        }
        catch (ObjectDisposedException)
        {
            return; // socket closed, stop quietly
        }
        catch (Exception e)
        {
            Debug.LogWarning("[PhoneDiscovery] Receive error: " + e.Message);
        }
    }

    void OnDestroy()
    {
        udp?.Close();
    }
}