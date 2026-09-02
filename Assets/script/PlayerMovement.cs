using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 20f;
    public float mouseSensitivity = 5f;

    public float jumpHeight = 20f;
    public float gravity = -20f;

    public Transform cameraTransform;

    // Crouching
    public float crouchHeight = 1f;
    public float normalHeight = 2f;
    public float crouchSpeed = 10f;

    private CharacterController controller;
    private float verticalVelocity;
    private float xRotation = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        controller.height = normalHeight;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // =====================
        // MOVEMENT
        // =====================

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;

        // Crouch movement speed
        float currentSpeed = Input.GetKey(KeyCode.LeftShift)
            ? crouchSpeed
            : speed;

        controller.Move(move * currentSpeed * Time.deltaTime);


        // =====================
        // GRAVITY + JUMP
        // =====================

        if (controller.isGrounded)
        {
            if (verticalVelocity < 0)
                verticalVelocity = -2f;

            // Space = Jump
            if (Input.GetKeyDown(KeyCode.Space))
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 gravityMove = Vector3.up * verticalVelocity;

        controller.Move(gravityMove * Time.deltaTime);


        // =====================
        // CROUCH
        // =====================

        if (Input.GetKey(KeyCode.LeftShift))
        {
            controller.height = crouchHeight;
        }
        else
        {
            controller.height = normalHeight;
        }


        // =====================
        // MOUSE LOOK
        // =====================

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        cameraTransform.localRotation =
            Quaternion.Euler(xRotation, 0f, 0f);
    }
}
