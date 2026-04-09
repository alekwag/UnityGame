using UnityEngine;

public class CameraLean : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private CharacterController characterController;

    [Header("Lean Settings")]
    [SerializeField] private float leanAngle = 10f;      // max degrees
    [SerializeField] private float leanSpeed = 6f;       // transition speed
    [SerializeField] private float leanDeadzone = 0.1f;  // ignore tiny inputs

    private float currentLeanAngle = 0f;

    private void LateUpdate()
    {
        if (cameraPivot == null || characterController == null) return;

        // Get horizontal velocity relative to the player's own transform
        Vector3 localVelocity = transform.InverseTransformDirection(characterController.velocity);
        float horizontalInput = localVelocity.x;

        // Normalise to -1/1 range based on walk speed, clamp so sprinting doesnt over-lean
        float leanInput = Mathf.Clamp(horizontalInput / 5f, -1f, 1f);

        // Kill lean if input is within deadzone
        if (Mathf.Abs(leanInput) < leanDeadzone) leanInput = 0f;

        float targetAngle = -leanInput * leanAngle; // negative so lean follows movement direction
        currentLeanAngle = Mathf.Lerp(currentLeanAngle, targetAngle, leanSpeed * Time.deltaTime);

        // Apply as Z rotation on top of existing camera rotation
        Vector3 euler = cameraPivot.localEulerAngles;
        cameraPivot.localEulerAngles = new Vector3(euler.x, euler.y, currentLeanAngle);
    }
}