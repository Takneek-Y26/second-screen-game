using UnityEngine;

// Attach to the card model. Needs a Collider on this object with "Is Trigger" checked,
// sized to whatever radius counts as "near the card".
// Your player object must have the tag "Player" and any Collider.
public class CardInteractable : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("Must match the doorID on the DoorManager of the door this card opens.")]
    public string doorID = "door_1";

    [Tooltip("A UI element (e.g. 'Right-click to swipe') shown only while in range.")]
    public GameObject interactPromptUI;

    private bool playerInRange = false;

    void Start()
    {
        if (interactPromptUI != null)
            interactPromptUI.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = true;
        if (interactPromptUI != null)
            interactPromptUI.SetActive(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;
        if (interactPromptUI != null)
            interactPromptUI.SetActive(false);
    }

    void Update()
    {
        if (!playerInRange) return;

        if (Input.GetMouseButtonDown(1)) // right click = "pick up" / begin swipe session
        {
            if (InteractionSession.Instance != null)
            {
                InteractionSession.Instance.BeginSwipeSession(doorID);
                Debug.Log("[CardInteractable] Picked up card, waiting for phone swipe: " + doorID);
            }
            else
            {
                Debug.LogWarning("[CardInteractable] No InteractionSession found in the scene.");
            }
        }
    }
}