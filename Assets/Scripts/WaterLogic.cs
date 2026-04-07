using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class WaterLogic : MonoBehaviour
{
    [Header("Swimming Settings")]
    [SerializeField] private float swimSpeed = 3f;
    [SerializeField] private float swimVerticalSpeed = 2f;
    [SerializeField] private float sinkingForce = 0.3f; // Downward force like gravity
    [SerializeField] private float underwaterOffset = 0.5f; // How far below water surface to apply effects

    [Header("Render Settings")]
    [SerializeField] private Color underwaterFogColor = Color.blue;
    [SerializeField] private float underwaterFogDensity = 0.01f;
    [SerializeField] private Color surfaceFogColor = new Color(212f/255f, 212f/255f, 212f/255f);
    [SerializeField] private float surfaceFogDensity = 0.001f;

    private CharacterController characterController;
    private FPSCharacterController fpsController;
    private bool isUnderwater = false;
    private bool renderSettingsApplied = false;
    private float verticalVelocity = 0f;
    private float waterSurfaceY = 0f;

    private void Awake()
    {
        // Initialize will be done on trigger enter
    }

    private void Update()
    {
        if (!isUnderwater || characterController == null) return;

        // Check if player is below the underwater offset threshold
        bool playerBelowThreshold = characterController.transform.position.y < waterSurfaceY - underwaterOffset;

        if (playerBelowThreshold && !renderSettingsApplied)
        {
            RenderSettings.fogDensity = underwaterFogDensity;
            RenderSettings.fogColor = underwaterFogColor;
            renderSettingsApplied = true;
        }
        else if (!playerBelowThreshold && renderSettingsApplied)
        {
            RenderSettings.fogDensity = surfaceFogDensity;
            RenderSettings.fogColor = surfaceFogColor;
            renderSettingsApplied = false;
        }

        Vector2 input = Vector2.zero;

        if (Keyboard.current != null)
        {
            input.x = (Keyboard.current.dKey.isPressed ? 1f : 0f) + (Keyboard.current.aKey.isPressed ? -1f : 0f);
            input.y = (Keyboard.current.wKey.isPressed ? 1f : 0f) + (Keyboard.current.sKey.isPressed ? -1f : 0f);
        }

        Transform playerTransform = characterController.transform;
        Vector3 move = playerTransform.right * input.x + playerTransform.forward * input.y;

    if (move.sqrMagnitude > 1f)
        move.Normalize();

    float vertical = 0f;

    if (Keyboard.current.spaceKey.isPressed)
        vertical = 1f;
    else if (Keyboard.current.cKey.isPressed)
        vertical = -1f;
    else
        vertical = - sinkingForce;

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
        if (other.CompareTag("Player"))
        {
            characterController = other.GetComponent<CharacterController>();
            fpsController = other.GetComponent<FPSCharacterController>();
            
            if (characterController != null && fpsController != null)
            {
                waterSurfaceY = transform.position.y + (transform.localScale.y / 2);
                isUnderwater = true;
                fpsController.SetSwimming(true);
                renderSettingsApplied = false;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            RenderSettings.fogDensity = surfaceFogDensity;
            RenderSettings.fogColor = surfaceFogColor;
            isUnderwater = false;
            fpsController?.SetSwimming(false);
            renderSettingsApplied = false;
            
            characterController = null;
            fpsController = null;
        }
    }
}