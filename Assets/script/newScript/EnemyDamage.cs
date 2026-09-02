using UnityEngine;

// Attach to any enemy GameObject.
//
// Setup (3D):
// 1. The enemy needs a Collider component with "Is Trigger" checked.
// 2. The enemy (or the player) needs a Rigidbody for trigger events to
//    fire at all - a Kinematic Rigidbody on the enemy is enough if the
//    enemy is moved by script/animation rather than physics forces.
// 3. The Player GameObject's Tag must be set to "Player".
public class EnemyDamage : MonoBehaviour
{
    [Header("Damage Settings")]
    public float damageAmount = 10f;
    [Tooltip("Seconds between damage ticks while the player keeps touching the enemy.")]
    public float damageInterval = 1f;

    private float damageTimer;
    private PlayerHealth playerHealth;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerHealth = other.GetComponent<PlayerHealth>();

        if (playerHealth == null)
        {
            Debug.LogWarning("EnemyDamage: object tagged 'Player' has no PlayerHealth component.");
            return;
        }

        // Deal damage immediately on first contact, then start the tick timer
        playerHealth.TakeDamage(damageAmount);
        damageTimer = 0f;
    }

    void OnTriggerStay(Collider other)
    {
        if (playerHealth == null) return;
        if (!other.CompareTag("Player")) return;

        damageTimer += Time.deltaTime;

        if (damageTimer >= damageInterval)
        {
            damageTimer = 0f;
            playerHealth.TakeDamage(damageAmount);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerHealth = null;
        damageTimer = 0f;
    }
}
