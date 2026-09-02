using UnityEngine;

public class OrientationController : MonoBehaviour
{
    void Awake()
    {
        // Locks the device screen to portrait mode on launch
        Screen.orientation = ScreenOrientation.Portrait;
    }
}