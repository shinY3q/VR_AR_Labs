using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float acceleration = 25f;
    [SerializeField] private float braking = 20f;
    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private Transform cameraTransform;

    private Rigidbody body;
    private Vector2 moveInput;
    private float yaw;
    private float pitch;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        yaw = transform.eulerAngles.y;
    }

    private void Update()
    {
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");

        if (Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, -80f, 80f);
        }
    }

    private void FixedUpdate()
    {
        Vector3 direction =
            transform.right * moveInput.x +
            transform.forward * moveInput.y;

        direction = direction.normalized;

        Vector3 horizontalVelocity = new Vector3(
            body.linearVelocity.x,
            0f,
            body.linearVelocity.z
        );

        if (direction.sqrMagnitude > 0.01f)
        {
            body.AddForce(
                direction * acceleration,
                ForceMode.Acceleration
            );
        }
        else
        {
            body.AddForce(
                -horizontalVelocity * braking,
                ForceMode.Acceleration
            );
        }

        horizontalVelocity = new Vector3(
            body.linearVelocity.x,
            0f,
            body.linearVelocity.z
        );

        if (horizontalVelocity.magnitude > maxSpeed)
        {
            Vector3 limitedVelocity =
                horizontalVelocity.normalized * maxSpeed;

            body.linearVelocity = new Vector3(
                limitedVelocity.x,
                body.linearVelocity.y,
                limitedVelocity.z
            );
        }

        body.MoveRotation(
            Quaternion.Euler(0f, yaw, 0f)
        );
    }

    private void LateUpdate()
    {
        if (cameraTransform != null)
        {
            cameraTransform.localRotation =
                Quaternion.Euler(pitch, 0f, 0f);
        }
    }
}