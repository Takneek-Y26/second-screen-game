using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

// Shows a Game Over panel when PlayerHealth reports death, and provides
// two button hooks:
//   - RestartGame()    respawns the player at the spot they died and
//                       resets every "Enemy"-tagged object back to its
//                       starting position, without reloading the scene
//   - RespawnPlayer()  moves the player back to a fixed spawn point and
//                       resets their health (enemies untouched)
//
// Setup:
// 1. In your Canvas: create a Panel named "GameOverPanel" with a
//    "GAME OVER" Text/TMP element and a "Restart" Button inside it.
// 2. Select GameOverPanel in the Hierarchy and uncheck the active
//    checkbox next to its name in the Inspector, so it starts hidden.
// 3. Put this script on any GameObject (e.g. an empty "GameManager").
// 4. Drag your GameOverPanel into 'Game Over Panel', and your Player
//    (with PlayerHealth) into 'Player Health'.
// 5. Tag every enemy GameObject with the "Enemy" tag (Inspector -> Tag
//    dropdown -> Enemy; add the tag first if it doesn't exist yet).
//    RestartGame() will snap all of them back to wherever they were
//    when the scene started.
// 6. (Optional) If you also want a fixed-spawn-point respawn option,
//    create an empty GameObject at that spot (e.g. "SpawnPoint") and
//    drag it into 'Respawn Point', then use RespawnPlayer() instead.
// 7. On the Restart Button's OnClick() list (+), drag this GameManager
//    object in and pick GameOverManager -> RestartGame.
public class GameOverManager : MonoBehaviour
{
    public PlayerHealth playerHealth;
    public GameObject gameOverPanel;

    [Header("Respawn (optional - only needed for RespawnPlayer)")]
    public Transform respawnPoint;

    [Header("Enemy Reset")]
    [Tooltip("Tag used to find enemies whose starting position/rotation should be restored on RestartGame().")]
    public string enemyTag = "Enemy";

    Vector3 deathPosition;
    Quaternion deathRotation;

    // Snapshot of every enemy's starting transform, taken once at Start().
    class EnemyState
    {
        public Transform transform;
        public Vector3 startPosition;
        public Quaternion startRotation;
    }
    readonly List<EnemyState> enemyStates = new List<EnemyState>();

    void Start()
    {
        if (playerHealth == null)
            playerHealth = FindObjectOfType<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.onPlayerDeath.AddListener(ShowGameOver);
            // Sensible default in case death happens before any position is recorded.
            deathPosition = playerHealth.transform.position;
            deathRotation = playerHealth.transform.rotation;
        }
        else
        {
            Debug.LogError("GameOverManager: no PlayerHealth found in the scene.");
        }

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        CacheEnemyStartStates();
    }

    void CacheEnemyStartStates()
    {
        enemyStates.Clear();

        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        foreach (GameObject enemy in enemies)
        {
            enemyStates.Add(new EnemyState
            {
                transform = enemy.transform,
                startPosition = enemy.transform.position,
                startRotation = enemy.transform.rotation
            });
        }
    }

    void ShowGameOver()
    {
        // Remember exactly where/how the player was facing when they died
        // so RestartGame() can put them back there.
        if (playerHealth != null)
        {
            deathPosition = playerHealth.transform.position;
            deathRotation = playerHealth.transform.rotation;
        }

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        // Freeze gameplay behind the game over screen.
        // Remove this line if you want things to keep animating instead.
        Time.timeScale = 0f;

        // If your player uses mouse-look, the cursor is likely locked and
        // hidden during play - unlock it here so the Restart button is
        // actually clickable.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Respawns the player at the position they died, resets their health,
    // and puts every enemy back where it started. Does NOT reload the scene.
    public void RestartGame()
    {
        if (playerHealth == null)
        {
            Debug.LogError("GameOverManager: can't restart, no PlayerHealth assigned.");
            return;
        }

        TeleportPlayer(deathPosition, deathRotation);
        playerHealth.ResetHealth();
        ResetEnemies();

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Optional alternative: respawn at a fixed, hand-placed spawn point
    // instead of where the player died. Enemies are left untouched.
    public void RespawnPlayer()
    {
        if (playerHealth == null)
        {
            Debug.LogError("GameOverManager: can't respawn, no PlayerHealth assigned.");
            return;
        }

        if (respawnPoint == null)
        {
            Debug.LogError("GameOverManager: can't respawn, no Respawn Point assigned.");
            return;
        }

        TeleportPlayer(respawnPoint.position, respawnPoint.rotation);
        playerHealth.ResetHealth();

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void TeleportPlayer(Vector3 position, Quaternion rotation)
    {
        Transform playerTransform = playerHealth.transform;

        // CharacterController ignores/interferes with direct transform
        // changes while enabled, so disable it around the teleport.
        CharacterController controller = playerTransform.GetComponent<CharacterController>();
        if (controller != null)
            controller.enabled = false;

        playerTransform.position = position;
        playerTransform.rotation = rotation;

        if (controller != null)
            controller.enabled = true;

        // Zero out any leftover momentum so the player doesn't go flying
        // off in whatever direction they were moving/falling before death.
        Rigidbody rb = playerTransform.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    void ResetEnemies()
    {
        foreach (EnemyState state in enemyStates)
        {
            // Enemy might have been destroyed since Start() (e.g. killed) -
            // skip it rather than throwing.
            if (state.transform == null)
                continue;

            // NavMeshAgent, like CharacterController, fights direct transform
            // writes while enabled, so disable/re-enable around the teleport.
            NavMeshAgent agent = state.transform.GetComponent<NavMeshAgent>();
            if (agent != null)
                agent.enabled = false;

            state.transform.position = state.startPosition;
            state.transform.rotation = state.startRotation;

            if (agent != null)
                agent.enabled = true;

            Rigidbody rb = state.transform.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.onPlayerDeath.RemoveListener(ShowGameOver);
    }
}