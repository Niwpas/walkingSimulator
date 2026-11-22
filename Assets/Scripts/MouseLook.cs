using UnityEngine;

public class MouseLook : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerBody;

    [Header("Settings")]
    [SerializeField] private float mouseSensitivity = 2.0f;
    [SerializeField] private float maxVerticalAngle = 80f;
    [SerializeField] private float minVerticalAngle = -80f;

    private float xRotation = 0f;

    void Start()
    {
        // Lock cursor to center of screen and hide it
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Read raw mouse input scaled by sensitivity and frame delta
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Apply pitch (vertical rotation) and clamp to prevent flipping
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, minVerticalAngle, maxVerticalAngle);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Apply yaw (horizontal rotation) to the player body
        playerBody.Rotate(Vector3.up * mouseX);
    }
}