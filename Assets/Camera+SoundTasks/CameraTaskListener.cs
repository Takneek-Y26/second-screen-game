using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using TMPro;

// Mirrors SoundTaskListener.cs, but for camera-task. This script does NOT
// touch the laptop's webcam at all — it only polls the server to find out
// when the PHONE has captured and uploaded its photo. Do not combine this
// with GameManager.cs (which does open the laptop's own webcam) unless you
// actually want both devices capturing independently.
public class CameraTaskListener : MonoBehaviour
{
    [Header("UI References")]
    public Button startButton;
    public TMP_Text resultText;

    [Header("Server Settings")]
    [Tooltip("Same server pc_controller.py and the phone talk to.")]
    public string serverUrl = "https://127.0.0.1:5000";
    [Tooltip("Must match a key in multi_task_server.py's tasks_state dict.")]
    public string taskName = "camera-task";
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
        if (startButton != null) startButton.onClick.AddListener(OnStartTaskClicked);
    }

    void OnStartTaskClicked()
    {
        if (!isListening && !taskCompleted)
        {
            StartCoroutine(PollForCompletion());
        }
    }

    IEnumerator PollForCompletion()
    {
        isListening = true;
        startButton.interactable = false;
        if (resultText != null) resultText.text = "Waiting for camera task on phone...";

        // Optional: reset server state so a previous run's "completed" flag
        // doesn't instantly finish this one.
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
        startButton.interactable = true;
        if (resultText != null) resultText.text = "<color=green>Task Completed!</color>";
    }

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
            Debug.LogWarning($"[CameraTaskListener] {path} failed: {request.error}");
        }

        request.Dispose();
    }

    private class BypassCertificate : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData) => true;
    }
}