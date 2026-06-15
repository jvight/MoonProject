using UnityEngine;

public class MoonRoverController : MonoBehaviour
{
    [Header("Components")]
    [Tooltip("The Rigidbody attached to the Physics Sphere.")]
    [SerializeField] private Rigidbody sphereRigidbody;
    [Tooltip("The visual model of the rover.")]
    [SerializeField] private Transform visualBody;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private float deceleration = 5f;
    [SerializeField] private float turnSpeed = 90f;
    [SerializeField] private float gravityMultiplier = 0.5f; // Less than 1 for low gravity feel
    [SerializeField] private float fallGravityMultiplier = 2.0f; // Increased gravity when falling
    [SerializeField] private float downForce = 20f; // Force to stick to ground when going over hills

    [Header("Alignment Settings")]
    [SerializeField] private float alignSpeed = 5f;
    [SerializeField] private float raycastDistance = 1.5f;
    [SerializeField] private LayerMask groundLayer;

    private float moveInput;
    private float turnInput;
    private Vector3 currentNormal = Vector3.up;

    void Start()
    {
        // Lock and hide cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (sphereRigidbody == null) 
            sphereRigidbody = GetComponent<Rigidbody>();

        // Detach visual body so it can rotate independently of the sphere's rolling
        if (visualBody != null)
        {
            visualBody.parent = null;
        }
        
        // Setup rigidbody constraints for the Sphere trick
        if (sphereRigidbody != null)
        {
            // We want the sphere to roll freely if needed, but in this implementation, 
            // the sphere acts as a sliding/rolling puck. Let's just let it act naturally.
            sphereRigidbody.useGravity = true;
        }
    }

    void Update()
    {
        // Get Input (W/S for move, A/D for turn) using New Input System
        moveInput = 0f;
        turnInput = 0f;
        
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            if (UnityEngine.InputSystem.Keyboard.current.wKey.isPressed || UnityEngine.InputSystem.Keyboard.current.upArrowKey.isPressed) moveInput += 1f;
            if (UnityEngine.InputSystem.Keyboard.current.sKey.isPressed || UnityEngine.InputSystem.Keyboard.current.downArrowKey.isPressed) moveInput -= 1f;
            
            if (UnityEngine.InputSystem.Keyboard.current.dKey.isPressed || UnityEngine.InputSystem.Keyboard.current.rightArrowKey.isPressed) turnInput += 1f;
            if (UnityEngine.InputSystem.Keyboard.current.aKey.isPressed || UnityEngine.InputSystem.Keyboard.current.leftArrowKey.isPressed) turnInput -= 1f;
        }

        if (visualBody != null && sphereRigidbody != null)
        {
            // Rotate the visual body steering smoothly based on input
            visualBody.Rotate(0f, turnInput * turnSpeed * Time.deltaTime, 0f, Space.Self);
            
            // 1. Follow the sphere's interpolated position directly
            // We MUST use sphereRigidbody.transform.position to get the interpolated position!
            // Using sphereRigidbody.position gets the raw physics position, which causes severe jitter.
            visualBody.position = sphereRigidbody.transform.position;

            // 2. Smoothly transition the normal to the ground normal
            currentNormal = Vector3.Lerp(currentNormal, targetNormal, Time.deltaTime * alignSpeed);

            // 3. Align the visual body to the current normal while maintaining its steering direction
            Vector3 projectedForward = Vector3.ProjectOnPlane(visualBody.forward, currentNormal).normalized;
            if (projectedForward != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(projectedForward, currentNormal);
                visualBody.rotation = Quaternion.Slerp(visualBody.rotation, targetRotation, Time.deltaTime * alignSpeed);
            }
        }
    }

    private Vector3 targetNormal = Vector3.up;

    void FixedUpdate()
    {
        if (sphereRigidbody == null || visualBody == null) return;

        // Apply Forward Movement to the Sphere
        // Force is applied in the direction the Visual Body is facing
        Vector3 forwardForce = visualBody.forward * moveInput * moveSpeed;
        sphereRigidbody.AddForce(forwardForce, ForceMode.Acceleration);

        // Apply deceleration (braking) when no input is provided
        if (Mathf.Abs(moveInput) < 0.1f)
        {
            Vector3 flatVel = new Vector3(sphereRigidbody.linearVelocity.x, 0f, sphereRigidbody.linearVelocity.z);
            sphereRigidbody.AddForce(-flatVel * deceleration, ForceMode.Acceleration);
        }

        // Cap the maximum horizontal speed
        Vector3 currentVel = sphereRigidbody.linearVelocity;
        Vector3 flatVelocity = new Vector3(currentVel.x, 0f, currentVel.z);
        if (flatVelocity.magnitude > maxSpeed)
        {
            Vector3 limitedVel = flatVelocity.normalized * maxSpeed;
            sphereRigidbody.linearVelocity = new Vector3(limitedVel.x, currentVel.y, limitedVel.z);
        }

        // Raycast to find the ground normal for alignment
        RaycastHit hit;
        bool isGrounded = Physics.Raycast(sphereRigidbody.position, Vector3.down, out hit, raycastDistance, groundLayer);
        
        if (isGrounded)
        {
            targetNormal = hit.normal;
            
            // Custom Moon Gravity when grounded
            Vector3 extraGravity = Physics.gravity * (gravityMultiplier - 1f);
            sphereRigidbody.AddForce(extraGravity, ForceMode.Acceleration);

            // Apply downforce along the negative normal to stick to slopes and avoid flying up
            sphereRigidbody.AddForce(-hit.normal * downForce, ForceMode.Acceleration);
        }
        else
        {
            // If floating/falling, slowly return to upright position
            targetNormal = Vector3.up;

            // Apply heavier gravity when falling to prevent floaty feeling and fall faster
            Vector3 extraGravity = Physics.gravity * (fallGravityMultiplier - 1f);
            sphereRigidbody.AddForce(extraGravity, ForceMode.Acceleration);
        }
    }
}
