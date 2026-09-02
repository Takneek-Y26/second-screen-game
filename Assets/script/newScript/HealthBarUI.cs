using UnityEngine;
using UnityEngine.UI;

// Drives a UI Slider that displays the player's health.
//
// Setup:
// 1. In your Canvas: right-click -> UI -> Slider. Name it "HealthBar".
// 2. (Optional) Delete/hide the Slider's "Handle Slide Area" child if
//    you just want a plain fill bar with no draggable handle.
// 3. Recolor the "Fill" child's Image (e.g. red/green) to taste.
// 4. Put this script on the Slider GameObject.
// 5. Drag the Slider component into 'Health Slider' (or leave empty -
//    it will use the Slider on this same GameObject) and drag your
//    Player (with PlayerHealth) into 'Player Health'.
public class HealthBarUI : MonoBehaviour
{
    public Slider healthSlider;
    public PlayerHealth playerHealth;

    void Start()
    {
        if (playerHealth == null)
            playerHealth = FindObjectOfType<PlayerHealth>();

        if (healthSlider == null)
            healthSlider = GetComponent<Slider>();

        if (playerHealth == null || healthSlider == null)
        {
            Debug.LogError("HealthBarUI: missing PlayerHealth or Slider reference.");
            return;
        }

        healthSlider.minValue = 0f;
        healthSlider.maxValue = playerHealth.maxHealth;
        healthSlider.value = playerHealth.currentHealth;

        playerHealth.onHealthChanged.AddListener(UpdateHealthBar);
    }

    void UpdateHealthBar(float current, float max)
    {
        healthSlider.maxValue = max;
        healthSlider.value = current;
    }

    void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.onHealthChanged.RemoveListener(UpdateHealthBar);
    }
}
