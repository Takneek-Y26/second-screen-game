using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using TMPro;

// Generic replacement for SoundTaskListener.cs / CameraTaskListener.cs.
//
// HOW TO WIRE A TRIGGER TO THIS (done entirely in the Editor, no code edits):
//   1. Add this component to a GameObject (one per task, e.g. one for
//      "sound-task", one for "camera-task").
//   2. Set "Task Name" in the Inspector to match the key in
//      multi_task_server.py's tasks_state dict (e.g. "sound-task").
//   3. Under "Input Trigger" in the Inspector, set "Trigger Key" to any
//      keyboard key (e.g. Space) and/or "Trigger Mouse Button" to
//      0 (Left) / 1 (Right) / 2 (Middle). Leave Trigger Key as None and
//      Trigger Mouse Button as -1 to disable either one.
//   4. (Optional, still supported) You can also wire a UI Button's
//      "On Click ()" list to call TaskListener -> StartTask() directly,
//      exactly as before - the key/mouse trigger and a Button both just
//      call the same public StartTask() method.
//   5. (Optional) Drag a Button into the "Start Button" field in the
//      Inspector if you want this script to disable/re-enable it and show
//      a "waiting..." state automatically while polling.
//
// You can give as many different TaskListener instances (different Task
// Name values) their own key/mouse trigger as you like, all chosen from
// the Editor - nothing is hardcoded in code.
public class TaskListener : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Optional: only used to disable/re-enable + show status while polling. The button that actually calls StartTask() is chosen in that Button's own OnClick() list in the Editor - it does not need to be this one.")]
    public Button startButton;
    public TMP_Text resultText;

    [Header("Input Trigger")]
    [Tooltip("Keyboard key that starts the task. Set to None to disable keyboard triggering.")]
    public KeyCode triggerKey = KeyCode.None;
    [Tooltip("Mouse button that starts the task: -1 = disabled, 0 = Left, 1 = Right, 2 = Middle.")]
    public int triggerMouseButton = -1;

    [Header("Server Settings")]
    [Tooltip("Same server pc_controller.py and the phone talk to.")]
    public string serverUrl = "https://172.23.146.230:7777";
    [Tooltip("Must match a key in multi_task_server.py's tasks_state dict, e.g. \"sound-task\" or \"camera-task\".")]
    public string taskName = "sound-task";
    [Tooltip("How often to poll status, in seconds.")]
    public float pollInterval = 1f;

    private bool isListening = false;
    private bool taskCompleted = false;

    [System.Serializable]
    private class StatusResponse
    {
        public bool completed;
    }

    void Start()
    {
        if (resultText != null) resultText.text = "Press Start to begin";
        // No automatic AddListener here on purpose: wire StartTask() to
        // whichever button you want from that button's own OnClick() list
        // in the Inspector instead.
    }

    void Update()
    {
        if (triggerKey != KeyCode.None && Input.GetKeyDown(triggerKey))
        {
            StartTask();
        }

        if (triggerMouseButton >= 0 && Input.GetMouseButtonDown(triggerMouseButton))
        {
            StartTask();
        }
    }

    // Public so it shows up in any Button's OnClick() dropdown in the Editor.
    public void StartTask()
    {
        if (!isListening && !taskCompleted)
        {
            StartCoroutine(PollForCompletion());
        }
    }

    IEnumerator PollForCompletion()
    {
        isListening = true;
        if (startButton != null) startButton.interactable = false;
        if (resultText != null) resultText.text = $"Waiting for {taskName} on phone...";

        // Optional: reset server state so a previous run's "completed" flag
        // doesn't instantly finish this one. Comment out if you handle
        // resetting elsewhere (e.g. a dedicated Reset button on the PC).
        yield return CallEndpoint($"/api/{taskName}/reset", null, true);

        while (!taskCompleted)
        {
            yield return CallEndpoint($"/api/{taskName}/status", (json) =>
            {
                var status = JsonUtility.FromJson<StatusResponse>(json);
                if (status.completed)
                {
                    taskCompleted = true;
                }
            }, false);

            if (taskCompleted) break;

            yield return new WaitForSeconds(pollInterval);
        }

        isListening = false;
        if (startButton != null) startButton.interactable = true;
        if (resultText != null) resultText.text = "<color=green>Task Completed!</color>";

        // Record this task as done and let it check whether every required
        // task (camera + microphone) is now complete, so the win screen can
        // show up automatically.
        if (GameEndManager.Instance != null)
        {
            GameEndManager.Instance.NotifyTaskCompleted(taskName);
        }
    }

    // Small helper so GET (status) and POST (reset) share one code path.
    IEnumerator CallEndpoint(string path, System.Action<string> onSuccess, bool isPost)
    {
        string url = serverUrl + path;
        UnityWebRequest request = isPost
            ? UnityWebRequest.PostWwwForm(url, "")
            : UnityWebRequest.Get(url);

        // The server uses a self-signed dev cert (ssl_context='adhoc' in
        // multi_task_server.py). This bypass is fine for your own local dev
        // server but should never ship against a real/public endpoint.
        request.certificateHandler = new BypassCertificate();

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            onSuccess?.Invoke(request.downloadHandler.text);
        }
        else
        {
            Debug.LogWarning($"[TaskListener:{taskName}] {path} failed: {request.error}");
        }

        request.Dispose();
    }

    private class BypassCertificate : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData) => true;
    }
}