using UnityEngine;

public class firstPersonController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 8f;
    [Tooltip("Upward speed applied on jump.")]
    public float jumpHeight = 3f;

    [Header("Look")]
    [Tooltip("Degrees per second of turn, at full phone-stick deflection.")]
    public float sensitivity = 2f;
    [Tooltip("Separate sensitivity for keyboard/mouse look (mouse delta isn't on the same scale as a -1..1 stick).")]
    public float mouseSensitivity = 2f;
    public Transform playerCamera;

    [Header("Gravity")]
    [Tooltip("If this is left at 0 (or positive) the player will never fall or be able to jump properly, so a sane negative fallback is used automatically - fix this value in the Inspector to control it directly.")]
    public float gravity = -20f;
    public float groundedForce = -2f;

    [Header("Stamina")]
    public float maxStamina = 100f;
    public float currStamina = 100f;
    public float staminaDrain = 25f;
    public float staminaRegen = 15f;
    public UnityEngine.UI.Slider staminaBar;

    [Header("Crouching")]
    public float crouchHeight = 1f;
    public float standingHeight = 2f;
    public float crouchSpeed = 2.5f;

    [Header("Camera Height")]
    public float cameraStandingHeight = 1.7f;
    public float cameraCrouchingHeight = 0.9f;

    [Header("Phone Input")]
    public PhoneReceiver phoneReceiver;

    [Header("Keyboard/Mouse Input")]
    [Tooltip("Lets you play/test from the PC directly, in addition to the phone. Turn off if you only ever want phone input.")]
    public bool allowKeyboardMouse = true;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.LeftControl;
    public KeyCode jumpKey = KeyCode.Space;

    [Header("Input")]
    public float stickDeadzone = 0.1f;

    private CharacterController charControl;

    private float vertiVelo;
    private float cameraPitch = 0f;

    private bool isCrouching = false;

    // gravity == 0 (or positive) can't produce any downward acceleration or
    // a valid jump velocity (sqrt of a non-negative number), which is what
    // made the controller feel broken/floaty. Falls back to a sane default
    // instead of silently doing nothing.
    private float EffectiveGravity => gravity < 0f ? gravity : -20f;

    void Start()
    {
        charControl = GetComponent<CharacterController>();
        if (charControl == null)
        {
            Debug.LogError("firstPersonController: No CharacterController on this GameObject - movement will not work.");
            enabled = false;
            return;
        }

        if (playerCamera == null)
        {
            Debug.LogError("firstPersonController: 'Player Camera' is not assigned - look will not work.");
        }

        if (phoneReceiver == null)
        {
            phoneReceiver = GetComponent<PhoneReceiver>();
        }

        if (phoneReceiver == null && !allowKeyboardMouse)
        {
            Debug.LogWarning(
                "firstPersonController: No PhoneReceiver assigned or found, and keyboard/mouse input is disabled - nothing will control this player."
            );
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Make sure the CharacterController starts correctly
        charControl.height = standingHeight;

        // Keep the bottom of the controller at the player's feet
        charControl.center = new Vector3(
            0f,
            standingHeight / 2f,
            0f
        );

        currStamina = maxStamina;

        if (staminaBar != null)
        {
            staminaBar.maxValue = maxStamina;
            staminaBar.value = currStamina;
        }
    }

    void Update()
    {
        // =====================================================
        // INPUT
        // =====================================================

        float lookX = 0f;
        float lookY = 0f;
        float moveX = 0f;
        float moveZ = 0f;

        bool sprintHeld = false;
        bool crouchHeld = false;
        bool jumpPressed = false;

        // Phone stick/buttons drive the base input.
        if (phoneReceiver != null)
        {
            lookX = ApplyDeadzone(phoneReceiver.rightX) * sensitivity;
            lookY = ApplyDeadzone(phoneReceiver.rightY) * sensitivity;

            moveX = ApplyDeadzone(phoneReceiver.leftX);
            moveZ = ApplyDeadzone(phoneReceiver.leftY);

            sprintHeld = phoneReceiver.sprint;
            crouchHeld = phoneReceiver.crouch;
        }

        // Keyboard/mouse can drive (or add to) the same input, so the
        // controller is testable/playable straight from the PC.
        if (allowKeyboardMouse)
        {
            lookX += Input.GetAxis("Mouse X") * mouseSensitivity;
            lookY += Input.GetAxis("Mouse Y") * mouseSensitivity;

            moveX += Input.GetAxis("Horizontal");
            moveZ += Input.GetAxis("Vertical");

            sprintHeld = sprintHeld || Input.GetKey(sprintKey);
            crouchHeld = crouchHeld || Input.GetKey(crouchKey);
            jumpPressed = Input.GetKeyDown(jumpKey);
        }

        moveX = Mathf.Clamp(moveX, -1f, 1f);
        moveZ = Mathf.Clamp(moveZ, -1f, 1f);

        // =====================================================
        // LOOK
        // =====================================================

        // The phone stick's contribution is a turn *rate* (deg/sec), so it
        // needs Time.deltaTime to be framerate-independent. Mouse delta is
        // already a per-frame value, so it's added on top afterwards.
        float yaw = lookX * Time.deltaTime * 60f;
        float pitch = lookY * Time.deltaTime * 60f;

        transform.Rotate(Vector3.up * yaw);

        cameraPitch -= pitch;
        cameraPitch = Mathf.Clamp(cameraPitch, -90f, 90f);

        if (playerCamera != null)
        {
            playerCamera.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }

        // =====================================================
        // CROUCH
        // =====================================================

        isCrouching = crouchHeld;

        float targetHeight = isCrouching ? crouchHeight : standingHeight;

        // Smoothly change CharacterController height
        charControl.height = Mathf.Lerp(
            charControl.height,
            targetHeight,
            10f * Time.deltaTime
        );

        // IMPORTANT:
        // Keep the bottom of the CharacterController
        // at the same position while changing height.
        charControl.center = new Vector3(
            0f,
            charControl.height / 2f,
            0f
        );

        // =====================================================
        // CAMERA HEIGHT
        // =====================================================

        if (playerCamera != null)
        {
            float targetCameraHeight = isCrouching ? cameraCrouchingHeight : cameraStandingHeight;

            Vector3 cameraPosition = playerCamera.localPosition;

            cameraPosition.y = Mathf.Lerp(
                cameraPosition.y,
                targetCameraHeight,
                10f * Time.deltaTime
            );

            playerCamera.localPosition = cameraPosition;
        }

        // =====================================================
        // MOVEMENT
        // =====================================================

        Vector3 move =
            transform.right * moveX +
            transform.forward * moveZ;

        // Prevent diagonal movement from being faster
        if (move.magnitude > 1f)
        {
            move.Normalize();
        }

        float currentSpeed = walkSpeed;

        if (isCrouching)
        {
            currentSpeed = crouchSpeed;
        }
        else if (
            sprintHeld &&
            move.magnitude > 0.1f &&
            currStamina > 0f
        )
        {
            currentSpeed = sprintSpeed;

            currStamina -= staminaDrain * Time.deltaTime;
        }
        else
        {
            currStamina += staminaRegen * Time.deltaTime;
        }

        currStamina = Mathf.Clamp(currStamina, 0f, maxStamina);

        // =====================================================
        // STAMINA UI
        // =====================================================

        if (staminaBar != null)
        {
            staminaBar.value = currStamina;
        }

        // =====================================================
        // GRAVITY + JUMP
        // =====================================================

        if (charControl.isGrounded)
        {
            // Small downward force keeps the controller firmly attached to
            // the ground. A groundedForce of 0 (or positive) does nothing
            // useful here either, so fall back the same way gravity does.
            float landingForce = groundedForce < 0f ? groundedForce : -2f;

            if (vertiVelo < 0f)
            {
                vertiVelo = landingForce;
            }

            if (jumpPressed)
            {
                vertiVelo = Mathf.Sqrt(jumpHeight * -2f * EffectiveGravity);
            }
        }
        else
        {
            vertiVelo += EffectiveGravity * Time.deltaTime;
        }

        // =====================================================
        // FINAL MOVEMENT
        // =====================================================

        Vector3 finalMove = move * currentSpeed;
        finalMove.y = vertiVelo;

        // ONE Move call per frame
        charControl.Move(finalMove * Time.deltaTime);
    }

    private float ApplyDeadzone(float value)
    {
        return Mathf.Abs(value) < stickDeadzone
            ? 0f
            : value;
    }
}
