using System;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

// Put this on the same object as PhoneDiscovery (or any object) and drag
// PhoneDiscovery into the 'discovery' field. Attach this near your card UI image.
public class GyroSwipeDetector : MonoBehaviour
{
    [Header("References")]
    public PhoneDiscovery discovery;

    [Header("Which door this swipe opens (must match CardInteractable's doorID)")]
    public string doorID = "door_1";

    [Header("Swipe Detection Tuning")]
    [Tooltip("Rotation rate (rad/s) on the chosen axis that counts as a swipe. Tune by testing.")]
    public float swipeThreshold = 3.5f;

    [Tooltip("Minimum seconds between two accepted swipes, to avoid double-fires.")]
    public float cooldown = 1.0f;

    [Tooltip("Which axis of phone rotation corresponds to your physical swipe motion.")]
    public Axis swipeAxis = Axis.Y;

    public enum Axis { X, Y, Z }

    private float lastSwipeTime = -10f;
    private bool gyroReady = false;

    void Start()
    {
        if (SystemInfo.supportsGyroscope)
        {
            Input.gyro.enabled = true;
            gyroReady = true;
        }
        else
        {
            Debug.LogWarning("[GyroSwipeDetector] This device has no gyroscope.");
        }
    }

    void Update()
    {
        if (!gyroReady) return;

        Vector3 rate = Input.gyro.rotationRateUnbiased; // x, y, z in rad/s
        float value = swipeAxis == Axis.X ? rate.x : (swipeAxis == Axis.Y ? rate.y : rate.z);

        if (Mathf.Abs(value) > swipeThreshold && Time.time - lastSwipeTime > cooldown)
        {
            lastSwipeTime = Time.time;
            SendSwipe();
        }
    }

    void SendSwipe()
    {
        if (discovery == null || !discovery.isConnected || string.IsNullOrEmpty(discovery.hostIP))
        {
            Debug.LogWarning("[GyroSwipeDetector] Not connected to a host yet - swipe ignored.");
            return;
        }

        try
        {
            byte[] msg = Encoding.UTF8.GetBytes("SWIPE:" + doorID);
            using (UdpClient sender = new UdpClient())
            {
                sender.Send(msg, msg.Length, discovery.hostIP, discovery.swipePort);
            }
            Debug.Log("[GyroSwipeDetector] Swipe sent for " + doorID);
        }
        catch (Exception e)
        {
            Debug.LogError("[GyroSwipeDetector] Failed to send swipe: " + e.Message);
        }
    }
}