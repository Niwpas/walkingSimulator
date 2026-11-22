using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    private CharacterController controller;

    [Header("Movement Speeds")]
    [SerializeField] private float walkSpeed = 3.5f;
    [SerializeField] private float runSpeed = 6.0f;
    [SerializeField] private float gravity = -20.0f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDistance = 0.3f;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float fallMultiplier = 3f;
    private Vector3 velocity;
    private bool isGrounded;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        // Continuous ground check using a small sphere at the player's feet
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small constant downward force to snap player firmly to ground
        }

        // Gather input axes
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // Determine speed modifier (e.g., Left Shift to run)
        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float currentSpeed = isRunning ? runSpeed : walkSpeed;

        // Calculate movement vector relative to player orientation
        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * currentSpeed * Time.deltaTime);
if (Input.GetButtonDown("Jump") && isGrounded)
{
    velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
}
        // Apply vertical gravity velocity
        float gravityMultiplier = velocity.y < 0 ? fallMultiplier : 1f;
velocity.y += gravity * gravityMultiplier * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}