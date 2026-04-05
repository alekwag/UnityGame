using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

[RequireComponent(typeof(CharacterController))]
public class WaterLogic : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterController characterController;

    [Header("Swimming Settings")]
    [SerializeField] private float swimSpeed = 3f;
    [SerializeField] private float swimVerticalSpeed = 2f;
    [SerializeField] private float sinkingForce = 10f; // Downward force like gravity

    private bool isUnderwater = false;
    private FPSCharacterController fpsController;
    private float verticalVelocity = 0f;

    private void Awake()
    {
        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        fpsController = GetComponent<FPSCharacterController>();
    }

    private void Update()
{
    if (!isUnderwater) return;

    Vector2 input = Vector2.zero;

    if (Keyboard.current != null)
    {
        input.x = (Keyboard.current.dKey.isPressed ? 1f : 0f) + (Keyboard.current.aKey.isPressed ? -1f : 0f);
        input.y = (Keyboard.current.wKey.isPressed ? 1f : 0f) + (Keyboard.current.sKey.isPressed ? -1f : 0f);
    }

    Vector3 move = transform.right * input.x + transform.forward * input.y;

    if (move.sqrMagnitude > 1f)
        move.Normalize();

    float vertical = 0f;

    if (Keyboard.current.spaceKey.isPressed)
        vertical = 1f;
    else if (Keyboard.current.cKey.isPressed)
        vertical = -1f;
    else
        vertical = -0.3f; // slight sinking when idle (optional)

    move.y = vertical;

    Vector3 velocity = new Vector3(
        move.x * swimSpeed,
        move.y * swimVerticalSpeed,
        move.z * swimSpeed
    );

    characterController.Move(velocity * Time.deltaTime);
}

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Water"))
        {
            isUnderwater = true;
            fpsController?.SetSwimming(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Water"))
        {
            isUnderwater = false;
            fpsController?.SetSwimming(false);
        }
    }
}