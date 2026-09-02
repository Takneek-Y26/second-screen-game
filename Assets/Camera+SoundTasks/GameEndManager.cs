using TMPro;
using UnityEngine;

// Tracks whether the camera task and the microphone/sound task have both
// been completed, and shows the win screen the moment they both are.
//
// Setup:
// 1. Put this on a persistent GameObject (e.g. the same GameManager object
//    that holds InteractionSession).
// 2. Create a "WinScreen" Panel in your Canvas with a TMP_Text child, start
//    the panel inactive (uncheck its active checkbox in the Inspector).
// 3. Drag that panel into 'Win Panel' and the TMP_Text into 'Win Text'
//    below. Leave 'Win Message' as-is or change the wording.
// 4. Make sure 'Camera Task Name' / 'Sound Task Name' match the "Task Name"
//    field on the two TaskListener components in the scene (they default to
//    "camera-task" / "sound-task", same as TaskListener.cs).
// 5. Nothing else to wire up - TaskListener calls NotifyTaskCompleted()
//    automatically once its poll loop sees completed == true.
public class GameEndManager : MonoBehaviour
{
    public static GameEndManager Instance { get; private set; }

    [Header("Required task names")]
    [Tooltip("Must match the Task Name on the camera TaskListener.")]
    public string cameraTaskName = "camera-task";
    [Tooltip("Must match the Task Name on the microphone/sound TaskListener.")]
    public string soundTaskName = "sound-task";

    [Header("Win UI")]
    public GameObject winPanel;
    public TMP_Text winText;
    public string winMessage = "You Win the Game!";

    // Stored so the win condition can be checked (or displayed elsewhere,
    // e.g. a task checklist) at any time, not just at the instant a task
    // finishes.
    public bool CameraTaskCompleted { get; private set; }
    public bool SoundTaskCompleted { get; private set; }
    public bool GameWon { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (winPanel != null) winPanel.SetActive(false);
    }

    // Called by TaskListener once its polling sees the server report
    // completed == true for that task. Safe to call more than once for the
    // same task (e.g. if the player redoes it) - it just stays true.
    public void NotifyTaskCompleted(string taskName)
    {
        if (taskName == cameraTaskName)
        {
            CameraTaskCompleted = true;
            Debug.Log("[GameEndManager] Camera task completed.");
        }
        else if (taskName == soundTaskName)
        {
            SoundTaskCompleted = true;
            Debug.Log("[GameEndManager] Microphone/sound task completed.");
        }

        CheckForWin();
    }

    void CheckForWin()
    {
        if (GameWon) return;
        if (!CameraTaskCompleted || !SoundTaskCompleted) return;

        GameWon = true;
        ShowWinScreen();
    }

    void ShowWinScreen()
    {
        Debug.Log("[GameEndManager] Both tasks complete - you win!");

        if (winText != null) winText.text = winMessage;
        if (winPanel != null) winPanel.SetActive(true);
    }
}
