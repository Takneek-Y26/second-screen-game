using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// One entry per moving piece of the door (the whole slab, or a bolt, or a handle).
// openLocalPosition / openLocalEulerAngles are OFFSETS added on top of wherever
// the part starts (its "closed" transform), not absolute world values.
// That means: just drag the door in, leave position/rotation as-is in the scene
// (that's your closed state), then only fill in how much it should move/rotate to open.
[System.Serializable]
public class DoorPart
{
    public Transform part;
    public Vector3 openLocalPosition;      // e.g. (0,0,0) if it only rotates
    public Vector3 openLocalEulerAngles;   // e.g. (0,90,0) for a 90 degree swing
    public float duration = 1f;            // seconds this part takes to move
    public float delay = 0f;               // wait this long after Open() before moving (for staging)

    [HideInInspector] public Vector3 closedLocalPosition;
    [HideInInspector] public Quaternion closedLocalRotation;
}

// Attach this directly to each door's root object in the scene.
// Set a unique doorID per door and match it to the CardInteractable that opens it.
public class DoorManager : MonoBehaviour
{
    [Header("Identity")]
    public string doorID = "door_1";

    [Header("Moving Parts")]
    public DoorPart[] parts;

    [Header("State")]
    public bool isOpen = false;

    // global lookup so InteractionSession can say "open door_1" without a scene reference
    private static readonly Dictionary<string, DoorManager> registry = new Dictionary<string, DoorManager>();

    void Awake()
    {
        if (parts != null)
        {
            foreach (var p in parts)
            {
                if (p.part == null) continue;
                p.closedLocalPosition = p.part.localPosition;
                p.closedLocalRotation = p.part.localRotation;
            }
        }

        if (!string.IsNullOrEmpty(doorID))
            registry[doorID] = this;
    }

    void OnDestroy()
    {
        if (!string.IsNullOrEmpty(doorID) && registry.TryGetValue(doorID, out var d) && d == this)
            registry.Remove(doorID);
    }

    public static void OpenDoorByID(string id)
    {
        if (registry.TryGetValue(id, out var door))
            door.Open();
        else
            Debug.LogWarning("[DoorManager] No door registered with id: " + id);
    }

    public void Open()
    {
        if (isOpen || parts == null) return;
        isOpen = true;

        foreach (var p in parts)
        {
            if (p.part != null)
                StartCoroutine(MovePart(p));
        }
    }

    // Optional: call this yourself (keyboard key, trigger volume, etc.) if you want doors to reset
    public void Close()
    {
        if (!isOpen || parts == null) return;
        isOpen = false;

        foreach (var p in parts)
        {
            if (p.part != null)
                StartCoroutine(MoveToClosed(p));
        }
    }

    IEnumerator MovePart(DoorPart p)
    {
        if (p.delay > 0f)
            yield return new WaitForSeconds(p.delay);

        Vector3 startPos = p.part.localPosition;
        Quaternion startRot = p.part.localRotation;

        Vector3 endPos = p.closedLocalPosition + p.openLocalPosition;
        Quaternion endRot = p.closedLocalRotation * Quaternion.Euler(p.openLocalEulerAngles);

        float t = 0f;
        while (t < p.duration)
        {
            t += Time.deltaTime;
            float f = Mathf.SmoothStep(0f, 1f, t / p.duration);
            p.part.localPosition = Vector3.Lerp(startPos, endPos, f);
            p.part.localRotation = Quaternion.Slerp(startRot, endRot, f);
            yield return null;
        }

        p.part.localPosition = endPos;
        p.part.localRotation = endRot;
    }

    IEnumerator MoveToClosed(DoorPart p)
    {
        Vector3 startPos = p.part.localPosition;
        Quaternion startRot = p.part.localRotation;

        float t = 0f;
        while (t < p.duration)
        {
            t += Time.deltaTime;
            float f = Mathf.SmoothStep(0f, 1f, t / p.duration);
            p.part.localPosition = Vector3.Lerp(startPos, p.closedLocalPosition, f);
            p.part.localRotation = Quaternion.Slerp(startRot, p.closedLocalRotation, f);
            yield return null;
        }

        p.part.localPosition = p.closedLocalPosition;
        p.part.localRotation = p.closedLocalRotation;
    }
}