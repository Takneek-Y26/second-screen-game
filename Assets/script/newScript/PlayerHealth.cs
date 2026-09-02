using UnityEngine;
using UnityEngine.Events;

// Tracks the player's health, exposes TakeDamage/Heal, and fires events
// that the health bar UI and game-over screen listen to.
//
// Setup:
// 1. Put this script on your Player GameObject.
// 2. Make sure the Player GameObject's Tag is set to "Player"
//    (Inspector -> Tag dropdown -> Player). EnemyDamage relies on this.
public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Damage Settings")]
    [Tooltip("Seconds of immunity after being hit, so one long touch doesn't melt health instantly.")]
    public float invulnerabilityDuration = 0.5f;
    private float invulnerabilityTimer;

    [Header("Events")]
    // (current, max) - subscribed to by HealthBarUI
    public UnityEvent<float, float> onHealthChanged = new UnityEvent<float, float>();

    // fired once when health reaches 0 - subscribed to by GameOverManager
    public UnityEvent onPlayerDeath = new UnityEvent();

    public bool IsDead { get; private set; }

    void Awake()
    {
        currentHealth = maxHealth;
    }

    void Update()
    {
        if (invulnerabilityTimer > 0f)
            invulnerabilityTimer -= Time.deltaTime;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead) return;
        if (invulnerabilityTimer > 0f) return;
        if (amount <= 0f) return;

        currentHealth = Mathf.Clamp(currentHealth - amount, 0f, maxHealth);
        invulnerabilityTimer = invulnerabilityDuration;

        onHealthChanged.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (IsDead) return;
        if (amount <= 0f) return;

        currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
        onHealthChanged.Invoke(currentHealth, maxHealth);
    }

    void Die()
    {
        IsDead = true;
        onPlayerDeath.Invoke();
    }

    // Call this from GameOverManager.RestartGame() if you reset the
    // scene manually instead of reloading it.
    public void ResetHealth()
    {
        IsDead = false;
        currentHealth = maxHealth;
        invulnerabilityTimer = 0f;
        onHealthChanged.Invoke(currentHealth, maxHealth);
    }
}
