using UnityEngine;
using TMPro;

public class GyroDropUnlock : MonoBehaviour
{
    [Header("Drop Settings")]
    [Tooltip("Percentage of the card's height it must travel (0.75 = 75%)")]
    public float unlockDistancePercentage = 6f;

    [Tooltip("Adjust this only if you want to change the physical effort needed. 400 is your previous default.")]
    public float baseDropSensitivity = 400f;

    public float springForce = 3f;
    public float movementDeadzone = 0.1f;

    [Header("UI Elements")]
    public TextMeshProUGUI statusText;

    private RectTransform card;
    private Vector2 startPos;
    private float currentYOffset = 0f;
    private bool isUnlocked = false;

    // These are calculated automatically based on the card's size
    private float actualUnlockThreshold;
    private float dynamicSensitivity;

    void Start()
    {
        if (SystemInfo.supportsGyroscope)
        {
            Input.gyro.enabled = true;
        }

        card = GetComponent<RectTransform>();
        startPos = card.anchoredPosition;

        // 1. Calculate the distance in pixels representing 75% of the card's physical height
        actualUnlockThreshold = card.rect.height * unlockDistancePercentage;

        // 2. Scale the sensitivity so the physical force remains identical to your previous settings
        // (It compares the new calculated distance to your old 1200f threshold)
        dynamicSensitivity = baseDropSensitivity * (actualUnlockThreshold / 1200f);

        if (statusText != null)
        {
            statusText.text = "LOCKED";
            SetTextAlpha(1f);
            statusText.color = Color.white;
        }
    }

    void Update()
    {
        if (!SystemInfo.supportsGyroscope || isUnlocked) return;

        float downwardAccel = Input.gyro.userAcceleration.y;

        if (downwardAccel < -movementDeadzone)
        {
            // We use the new dynamic sensitivity here so the UI moves perfectly 
            // in sync with the new distance requirement
            currentYOffset += downwardAccel * dynamicSensitivity;
        }

        currentYOffset = Mathf.Lerp(currentYOffset, 0f, Time.deltaTime * springForce);
        card.anchoredPosition = new Vector2(startPos.x, startPos.y + currentYOffset);

        if (currentYOffset < -actualUnlockThreshold)
        {
            Unlock();
        }
    }

    void Unlock()
    {
        isUnlocked = true;
        card.anchoredPosition = new Vector2(startPos.x, startPos.y - actualUnlockThreshold);

        if (statusText != null)
        {
            statusText.text = "UNLOCKED";
            statusText.color = new Color(0.2f, 0.8f, 0.2f, 1f);
        }
    }

    void SetTextAlpha(float alpha)
    {
        Color c = statusText.color;
        c.a = alpha;
        statusText.color = c;
    }
}