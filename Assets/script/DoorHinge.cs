using UnityEngine;

// Rotates a door open/closed around a hinge point whenever a shake event
// arrives from PhoneReceiver (sent as "SWIPE:DOOR" by doorShake.html).
//
// SETUP (this part matters more than the code):
// 1. In your scene, create an empty GameObject where the door's hinge
//    actually is (the edge it swings from, not the center of the door).
//    Name it something like "DoorHingePivot".
// 2. Drag your door mesh/model so it becomes a CHILD of DoorHingePivot,
//    then position the mesh so its hinge edge lines up with the pivot's
//    origin (0,0,0 in local space). This offset between the pivot and the
//    mesh is what makes rotating the pivot look like the door swinging
//    around its hinge instead of spinning around its own center.
// 3. Attach this script to DoorHingePivot (or any object in the scene).
// 4. Drag DoorHingePivot into the 'Door Hinge' field below.
// 5. Leave 'Phone Receiver' empty and it will find the one already in
//    your scene automatically (same object CardSwipeController uses).
public class DoorHingeController : MonoBehaviour
{
    [Header("References")]
    public PhoneReceiver phoneReceiver;
    public Transform doorHinge; // the pivot Transform, NOT the door mesh itself

    [Header("Door Settings")]
    [Tooltip("How far the door swings open, in degrees around the hinge's up axis.")]
    public float openAngle = 90f;

    [Tooltip("Higher = snappier rotation, lower = slower/more eased.")]
    public float rotateSpeed = 3f;

    [Tooltip("Only react to shakes tagged 'DOOR' from doorShake.html. Turn off to open on ANY swipe/shake message.")]
    public bool onlyRespondToDoorTag = true;

    private bool isOpen = false;
    private Quaternion closedRotation;
    private Quaternion openRotation;

    void Start()
    {
        if (phoneReceiver == null)
            phoneReceiver = FindObjectOfType<PhoneReceiver>();

        if (doorHinge == null)
            doorHinge = transform;

        closedRotation = doorHinge.localRotation;
        openRotation = closedRotation * Quaternion.Euler(0f, openAngle, 0f);
    }

    void Update()
    {
        if (phoneReceiver == null || doorHinge == null)
            return;

        if (phoneReceiver.swipeTriggered)
        {
            bool matchesTag = !onlyRespondToDoorTag || phoneReceiver.swipeDirection == "DOOR";

            if (matchesTag)
            {
                isOpen = !isOpen;
                Debug.Log("Door " + (isOpen ? "opening" : "closing"));
            }
        }

        Quaternion target = isOpen ? openRotation : closedRotation;

        doorHinge.localRotation = Quaternion.Slerp(
            doorHinge.localRotation,
            target,
            Time.deltaTime * rotateSpeed
        );
    }
}
