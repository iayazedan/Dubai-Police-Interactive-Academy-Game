// Required Packages:
// - com.unity.inputsystem

using UnityEngine;
using UnityEngine.InputSystem; // Required for the new Input System

// Namespace declaration (optional but recommended for organization)
namespace DubaiPoliceInteractiveAcademy.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInput))] // Ensure PlayerInput component is present
    public class PlayerHubController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float gravity = -15.0f; // Adjusted for better feel
        [SerializeField] private float groundCheckDistance = 0.2f;
        [SerializeField] private LayerMask groundMask;
        [SerializeField] private Transform groundCheck;

        [Header("Look Settings")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float lookSensitivity = 0.1f;
        [SerializeField] private float maxPitch = 85f;
        [SerializeField] private float minPitch = -85f;

        [Header("Interaction Settings")]
        [SerializeField] private float interactionDistance = 3f;
        [SerializeField] private LayerMask interactionLayerMask = ~0; // Interact with everything by default

        // Component References
        private CharacterController characterController;
        private PlayerInput playerInput;

        // Internal State
        private Vector2 moveInput;
        private Vector2 lookInput;
        private bool interactInputPressed;
        private Vector3 playerVelocity;
        private bool isGrounded;
        private float cameraPitch = 0f;
        private IInteractable currentInteractable;
        private IInteractable lastInteractable; // To track changes for potential UI updates

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            playerInput = GetComponent<PlayerInput>(); // Get the PlayerInput component

            if (cameraTransform == null)
            {
                // Attempt to find the main camera if not assigned
                Camera mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    cameraTransform = mainCamera.transform;
                    Debug.LogWarning("PlayerHubController: Camera Transform was not assigned. Using Main Camera.", this);
                }
                else
                {
                    Debug.LogError("PlayerHubController: Camera Transform is not assigned and Main Camera could not be found!", this);
                    enabled = false; // Disable script if camera is missing
                    return;
                }
            }

            if (groundCheck == null)
            {
                 Debug.LogError("PlayerHubController: Ground Check Transform is not assigned!", this);
                 enabled = false;
                 return;
            }

            // Lock and hide cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // --- Input System Event Handlers ---
        // These methods are called by the PlayerInput component based on Action names

        public void OnMove(InputAction.CallbackContext context)
        {
            moveInput = context.ReadValue<Vector2>();
        }

        public void OnLook(InputAction.CallbackContext context)
        {
            lookInput = context.ReadValue<Vector2>();
        }

        public void OnInteract(InputAction.CallbackContext context)
        {
            if (context.performed) // Trigger on button press down
            {
                interactInputPressed = true;
            }
        }

        // --- Core Logic ---

        private void Update()
        {
            HandleGroundCheck();
            HandleGravity();
            HandleMovement();
            HandleLook();
            CheckForInteractable(); // Check for interactables every frame
            HandleInteraction(); // Process interaction input
        }

        private void HandleGroundCheck()
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore);

            if (isGrounded && playerVelocity.y < 0)
            {
                playerVelocity.y = -2f; // Keep grounded firmly
            }
        }

        private void HandleGravity()
        {
             // Apply gravity
            playerVelocity.y += gravity * Time.deltaTime;
        }

         private void HandleMovement()
        {
            if (characterController == null || !characterController.enabled) return;

            // Calculate movement direction based on player forward and right vectors
            Vector3 moveDirection = transform.forward * moveInput.y + transform.right * moveInput.x;
            moveDirection.Normalize(); // Ensure consistent speed regardless of diagonal input

            // Apply movement speed
            Vector3 move = moveDirection * moveSpeed;

            // Combine movement and gravity
            characterController.Move((move + playerVelocity) * Time.deltaTime);
        }

        private void HandleLook()
        {
            if (cameraTransform == null) return;

            // Adjust pitch (up/down look) based on input and sensitivity
            cameraPitch -= lookInput.y * lookSensitivity;
            cameraPitch = Mathf.Clamp(cameraPitch, minPitch, maxPitch);

            // Apply pitch rotation to the camera
            cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);

            // Apply yaw (left/right look) rotation to the player body
            transform.Rotate(Vector3.up * (lookInput.x * lookSensitivity));
        }


        /// <summary>
        /// Detects interactable objects in front of the player's camera.
        /// </summary>
        private void CheckForInteractable()
        {
            lastInteractable = currentInteractable; // Store previous interactable
            currentInteractable = null; // Reset current interactable

            if (cameraTransform == null) return;

            Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
            if (Physics.Raycast(ray, out RaycastHit hitInfo, interactionDistance, interactionLayerMask, QueryTriggerInteraction.Ignore))
            {
                // Check if the hit object has a component implementing IInteractable
                currentInteractable = hitInfo.collider.GetComponent<IInteractable>();
            }

            // Optional: Signal UI Manager about interactable state change
            // Consider implementing UI updates here if needed based on `currentInteractable != lastInteractable`
            // Example:
            // if (currentInteractable != lastInteractable)
            // {
            //    if (currentInteractable != null) Debug.Log($"Looking at interactable: {((Component)currentInteractable).name}");
            //    else Debug.Log("Stopped looking at interactable.");
            // }
        }

        /// <summary>
        /// Handles the interaction input, triggering the interaction if possible.
        /// </summary>
        private void HandleInteraction()
        {
            if (interactInputPressed && currentInteractable != null)
            {
                InteractWithObject(currentInteractable);
            }
            // Reset the input flag after processing
            interactInputPressed = false;
        }

        /// <summary>
        /// Calls the Interact method on the provided interactable object.
        /// </summary>
        /// <param name="interactable">The object to interact with.</param>
        private void InteractWithObject(IInteractable interactable)
        {
            if (interactable == null)
            {
                Debug.LogWarning("Attempted to interact with a null object.", this);
                return;
            }
            interactable.Interact();
        }

        // --- Public Methods (Called by Input System) ---
        // These are kept minimal as InputSystem events handle the direct calls.
        // Public Move/Look might be useful if controlling the player from other scripts.

        /// <summary>
        /// Sets the desired movement input direction (e.g., from an AI or external source).
        /// Primarily used if not relying solely on PlayerInput component events.
        /// </summary>
        /// <param name="direction">Normalized direction vector.</param>
        public void Move(Vector3 direction)
        {
             Debug.LogWarning("PlayerHubController.Move(Vector3) called directly. Ensure this is intended usage alongside Input System.", this);
        }

        /// <summary>
        /// Sets the desired look input delta (e.g., from an AI or external source).
        /// Primarily used if not relying solely on PlayerInput component events.
        /// </summary>
        /// <param name="delta">Look input delta.</param>
        public void Look(Vector2 delta)
        {
            Debug.LogWarning("PlayerHubController.Look(Vector2) called directly. Ensure this is intended usage alongside Input System.", this);
        }

        // Optional: Gizmos for visualizing interaction ray and ground check
        private void OnDrawGizmosSelected()
        {
            // Draw Interaction Ray
            if (cameraTransform != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(cameraTransform.position, cameraTransform.position + cameraTransform.forward * interactionDistance);
            }

            // Draw Ground Check Sphere
            if(groundCheck != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(groundCheck.position, groundCheckDistance);
            }
        }
    }
}