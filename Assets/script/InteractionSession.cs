using System.Collections.Generic;
using UnityEngine;

// Put this on your GameManager object (one per scene). Singleton so CardInteractable
// and SwipeListener can both reach it without scene references.
//
// Supports MULTIPLE doors armed at the same time - important once you have several
// cards/phones in play at once (e.g. 8 different players near 8 different doors).
public class InteractionSession : MonoBehaviour
{
    public static InteractionSession Instance;

    [Tooltip("How long (seconds) after right-click the phone swipe is still accepted.")]
    public float armWindowSeconds = 10f;

    // doorID -> time (Time.time) at which this door's armed window expires
    private readonly Dictionary<string, float> armedDoors = new Dictionary<string, float>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // Called by CardInteractable on right-click
    public void BeginSwipeSession(string doorID)
    {
        armedDoors[doorID] = Time.time + armWindowSeconds;
        Debug.Log("[InteractionSession] Armed for door: " + doorID);
    }

    // Called by SwipeListener when a UDP "SWIPE:<doorID>" packet arrives
    public void OnSwipeReceived(string doorID)
    {
        if (armedDoors.TryGetValue(doorID, out float expireTime) && Time.time <= expireTime)
        {
            Debug.Log("[InteractionSession] Valid swipe -> opening: " + doorID);
            DoorManager.OpenDoorByID(doorID);
            armedDoors.Remove(doorID);
        }
        else
        {
            Debug.Log("[InteractionSession] Ignored swipe for '" + doorID + "' (not armed or expired)");
        }
    }

    // Cleans up expired entries so the dictionary doesn't grow forever.
    // Not strictly required at only 8 doors, but harmless and keeps logs/debugging tidy.
    void Update()
    {
        if (armedDoors.Count == 0) return;

        List<string> expired = null;
        foreach (var kvp in armedDoors)
        {
            if (Time.time > kvp.Value)
            {
                expired ??= new List<string>();
                expired.Add(kvp.Key);
            }
        }

        if (expired != null)
        {
            foreach (var id in expired)
            {
                armedDoors.Remove(id);
                Debug.Log("[InteractionSession] Session expired for: " + id);
            }
        }
    }
}