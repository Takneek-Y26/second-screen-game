using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class PhoneReceiver : MonoBehaviour
{
    public int port = 7777;

    private UdpClient udp;
    private Thread receiveThread;

    private ConcurrentQueue<string> messageQueue =
        new ConcurrentQueue<string>();

    // Phone input values
    public float leftX;
    public float leftY;

    public float rightX;
    public float rightY;

    public bool sprint;
    public bool crouch;

    // One-shot button
    public bool interact;

    // Card swipe (accelerometer-based, one-shot per swipe)
    public string swipeDirection = "";
    public bool swipeTriggered;

    void Start()
    {
        receiveThread = new Thread(ReceiveData);
        receiveThread.IsBackground = true;
        receiveThread.Start();

        Debug.Log("Phone receiver started on port " + port);
    }

    void ReceiveData()
    {
        try
        {
            udp = new UdpClient(port);

            IPEndPoint remoteEndPoint =
                new IPEndPoint(IPAddress.Any, port);

            while (true)
            {
                byte[] data = udp.Receive(ref remoteEndPoint);

                string message =
                    Encoding.UTF8.GetString(data);

                messageQueue.Enqueue(message);
            }
        }
        catch
        {
            // Receiver stopped
        }
    }

    void Update()
    {
        while (messageQueue.TryDequeue(out string message))
        {
            ProcessInput(message);
        }

        // Reset one-shot values every frame
        interact = false;
        swipeTriggered = false;
    }

    void ProcessInput(string message)
    {
        Debug.Log("PHONE: " + message);

        // LEFT:x,y
        if (message.StartsWith("LEFT:"))
        {
            string values = message.Substring(5);

            string[] parts = values.Split(',');

            if (parts.Length == 2)
            {
                float.TryParse(parts[0], out leftX);
                float.TryParse(parts[1], out leftY);
            }
        }

        // RIGHT:x,y
        else if (message.StartsWith("RIGHT:"))
        {
            string values = message.Substring(6);

            string[] parts = values.Split(',');

            if (parts.Length == 2)
            {
                float.TryParse(parts[0], out rightX);
                float.TryParse(parts[1], out rightY);
            }
        }

        // SWIPE:LEFT / SWIPE:RIGHT / SWIPE:UP / SWIPE:DOWN
        else if (message.StartsWith("SWIPE:"))
        {
            swipeDirection = message.Substring(6);
            swipeTriggered = true;
        }

        // INTERACT
        else if (message == "INTERACT")
        {
            interact = true;
        }

        // SPRINT
        else if (message == "SPRINT")
        {
            sprint = true;
        }

        // SPRINT released
        else if (message == "SPRINT_UP")
        {
            sprint = false;
        }

        // CROUCH
        else if (message == "CROUCH")
        {
            crouch = true;
        }

        // CROUCH released
        else if (message == "CROUCH_UP")
        {
            crouch = false;
        }
    }

    void OnDestroy()
    {
        // Closing the socket makes the blocking udp.Receive() call inside
        // ReceiveData() throw immediately, which lets the background thread
        // exit on its own. Thread.Abort() (the old approach) throws
        // PlatformNotSupportedException on IL2CPP builds (Android/iOS) and
        // can also freeze the editor on stop, so it's avoided here.
        if (udp != null)
            udp.Close();
    }
}