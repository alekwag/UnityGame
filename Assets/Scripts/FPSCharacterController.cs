using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FPSCharacterController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string playerMapName = "Player";
    [SerializeField] private string moveActionName = "Move";
    [SerializeField] private string lookActionName = "Look";
    [SerializeField] private string jumpActionName = "Jump";
    [SerializeField] private string sprintActionName = "Sprint";
    [SerializeField] private string crouchActionName = "Crouch";

    [Header("References")]
    [SerializeField] private Transform cameraPivot;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -25f;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 0.08f;
    [SerializeField] private float gamepadLookSpeed = 180f;
    [SerializeField] private float maxLookAngle = 85f;

    [Header("Crouch")]
    [SerializeField] private float crouchHeight = 1.1f;
    [SerializeField] private float standingHeight = 1.8f;
    [SerializeField] private float standingCameraHeight = 1.6f;
    [SerializeField] private float crouchingCameraHeight = 1.0f;
    [SerializeField] private float crouchTransitionSpeed = 12f;
    



    private CharacterController characterController;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
    private InputAction crouchAction;

    private float verticalVelocity;
    private float pitch;
    private bool isCrouching;
    public bool IsCrouching => isCrouching;
    public bool IsSprinting { get; private set; }
    public float CurrentSpeed { get; private set; }
    
    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (cameraPivot == null && Camera.main != null)
        {
            cameraPivot = Camera.main.transform;
        }

        ResolveActions();
        characterController.height = standingHeight;
        characterController.center = new Vector3(0f, standingHeight * 0.5f, 0f);
        SetCameraHeightImmediate(standingCameraHeight);
    }

    private void OnEnable()
    {
        LockCursor(true);
    }

    private void OnDisable()
    {
        LockCursor(false);
    }

    private void Update()
    {
        if (moveAction == null || lookAction == null)
        {
            return;
        }

        HandleLook();
        HandleMovement();
        HandleCrouch();

        CurrentSpeed = characterController.velocity.magnitude;
    }

    private void ResolveActions()
    {
        if (inputActions == null)
        {
            Debug.LogError("FPSCharacterController: Input Actions asset is missing.", this);
            return;
        }

        moveAction = inputActions.FindAction(playerMapName + "/" + moveActionName, true);
        lookAction = inputActions.FindAction(playerMapName + "/" + lookActionName, true);
        jumpAction = inputActions.FindAction(playerMapName + "/" + jumpActionName, false);
        sprintAction = inputActions.FindAction(playerMapName + "/" + sprintActionName, false);
        crouchAction = inputActions.FindAction(playerMapName + "/" + crouchActionName, false);
    }

    private void HandleLook()
    {
        if (cameraPivot == null)
        {
            return;
        }

        Vector2 lookInput = lookAction.ReadValue<Vector2>();
        bool usingMouse = Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 0.0001f;

        float yawDelta;
        float pitchDelta;

        if (usingMouse)
        {
            yawDelta = lookInput.x * mouseSensitivity;
            pitchDelta = lookInput.y * mouseSensitivity;
        }
        else
        {
            yawDelta = lookInput.x * gamepadLookSpeed * Time.deltaTime;
            pitchDelta = lookInput.y * gamepadLookSpeed * Time.deltaTime;
        }

        transform.Rotate(Vector3.up * yawDelta);

        pitch -= pitchDelta;
        pitch = Mathf.Clamp(pitch, -maxLookAngle, maxLookAngle);
        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void HandleMovement()
    {
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        if (move.sqrMagnitude > 1f)
        {
            move.Normalize();
        }

        bool grounded = characterController.isGrounded;
        if (grounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        bool jumpPressed = jumpAction != null && jumpAction.WasPressedThisFrame();
        if (grounded && jumpPressed && !isCrouching)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        IsSprinting = sprintAction != null && sprintAction.IsPressed() && !isCrouching && moveInput.y > 0.05f;
        float speed = IsSprinting ? sprintSpeed : walkSpeed;

        verticalVelocity += gravity * Time.deltaTime;
        Vector3 velocity = move * speed;
        velocity.y = verticalVelocity;

        characterController.Move(velocity * Time.deltaTime);
    }

    private void HandleCrouch()
    {
        if (crouchAction != null && crouchAction.WasPressedThisFrame())
        {
            isCrouching = !isCrouching;
        }

        float targetHeight = isCrouching ? crouchHeight : standingHeight;
        float newHeight = Mathf.Lerp(characterController.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);
        characterController.height = newHeight;
        characterController.center = new Vector3(0f, newHeight * 0.5f, 0f);

        if (cameraPivot != null)
        {
            float targetCameraHeight = isCrouching ? crouchingCameraHeight : standingCameraHeight;
            Vector3 localPos = cameraPivot.localPosition;
            localPos.y = Mathf.Lerp(localPos.y, targetCameraHeight, crouchTransitionSpeed * Time.deltaTime);
            cameraPivot.localPosition = localPos;
        }
    }

    private void SetCameraHeightImmediate(float height)
    {
        if (cameraPivot == null)
        {
            return;
        }

        Vector3 localPos = cameraPivot.localPosition;
        localPos.y = height;
        cameraPivot.localPosition = localPos;
    }

    private static void LockCursor(bool shouldLock)
    {
        Cursor.lockState = shouldLock ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !shouldLock;
    }

}
