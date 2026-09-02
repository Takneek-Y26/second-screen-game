using UnityEngine;
using UnityEngine.UI;

public class ObjectInteractor : MonoBehaviour
{
    [Header("Settings")]
    public float interactionDistance = 3f;
    public Text promptText; // Drag your UI Text element here
    public string serverUrl = "http://:5000/start-minigame";

    private Transform playerTransform;
    private bool isPlayerNearby = false;

    void Start()
    {
        // Automatically find the player by tag, or assign manually
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }

        if (promptText != null)
        {
            promptText.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (playerTransform == null) return;

        float distance = Vector3.Distance(transform.position, playerTransform.position);

        if (distance <= interactionDistance)
        {
            if (!isPlayerNearby)
            {
                isPlayerNearby = true;
                if (promptText != null)
                {
                    promptText.text = "Press RMB to interact";
                    promptText.gameObject.SetActive(true);
                }
            }

            // Check for Right Mouse Button click (Mouse 1)
            if (Input.GetMouseButtonDown(1))
            {
                TriggerMinigame();
            }
        }
        else
        {
            if (isPlayerNearby)
            {
                isPlayerNearby = false;
                if (promptText != null)
                {
                    promptText.gameObject.SetActive(false);
                }
            }
        }
    }

    void TriggerMinigame()
    {
        Debug.Log("Interacted! Sending request to Python server...");
        StartCoroutine(SendServerRequest());
    }

    System.Collections.IEnumerator SendServerRequest()
    {
        // Using UnityWebRequest to talk to your Python backend
        using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(serverUrl))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Debug.LogError("Server Error: " + www.error);
            }
            else
            {
                Debug.Log("Server Response: " + www.downloadHandler.text);
                // Handle opening the mobile minigame view or loading a scene here
            }
        }
    }
}