using UnityEngine;

// Reads swipe events coming from PhoneReceiver (SWIPE:LEFT/RIGHT/UP/DOWN)
// and drives whatever "card" object represents that input in the scene.
//
// Setup:
// 1. Attach this to a GameObject in the same scene as PhoneReceiver.
// 2. Drag your PhoneReceiver object into the 'Phone Receiver' field
//    (or leave empty - it will find one automatically).
// 3. Drag the object you want swipes to move/affect into 'Card'.
public class CardSwipeController : MonoBehaviour
{
    public PhoneReceiver phoneReceiver;
    public Transform card;

    public float moveDistance = 2f;
    public float moveSpeed = 8f;

    private Vector3 targetPosition;

    void Start()
    {
        if (phoneReceiver == null)
            phoneReceiver = FindObjectOfType<PhoneReceiver>();

        if (card != null)
            targetPosition = card.position;
    }

    void Update()
    {
        if (phoneReceiver == null || card == null)
            return;

        if (phoneReceiver.swipeTriggered)
        {
            OnSwipe(phoneReceiver.swipeDirection);
        }

        card.position = Vector3.Lerp(
            card.position,
            targetPosition,
            Time.deltaTime * moveSpeed
        );
    }

    void OnSwipe(string direction)
    {
        Debug.Log("Card swiped: " + direction);

        switch (direction)
        {
            case "LEFT":
                targetPosition = card.position + Vector3.left * moveDistance;
                break;

            case "RIGHT":
                targetPosition = card.position + Vector3.right * moveDistance;
                break;

            case "UP":
                targetPosition = card.position + Vector3.up * moveDistance;
                break;

            case "DOWN":
                targetPosition = card.position + Vector3.down * moveDistance;
                break;
        }

        // TODO: this is a placeholder reaction (nudging the card's position).
        // Replace/extend this with whatever the swipe should actually do in
        // your game scene - flip the card, play an animation, advance
        // dialogue, trigger a game event, etc.
    }
}