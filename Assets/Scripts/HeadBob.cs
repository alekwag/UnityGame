using UnityEngine;

public class HeadBob : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private FPSCharacterController fpsController;

    [Header("Bob Settings")]
    [SerializeField] private float bobFrequency = 6f;
    [SerializeField] private float bobAmplitudeY = 0.06f;
    [SerializeField] private float bobAmplitudeX = 0.03f;
    [SerializeField] private float bobSprintMultiplier = 1.6f;
    [SerializeField] private float bobSmoothing = 10f;

    private float bobTimer = 0f;
    private Vector3 bobOffset = Vector3.zero;
    private Vector3 bobVelocity = Vector3.zero;

    private void Update()
    {
        if (cameraPivot == null || characterController == null) return;

        bool grounded = characterController.isGrounded;
        float horizontalSpeed = new Vector3(
            characterController.velocity.x, 0f,
            characterController.velocity.z).magnitude;

        bool isMoving = grounded && horizontalSpeed > 0.1f;

        if (isMoving)
        {
            float multiplier = fpsController != null && fpsController.IsSprinting
                ? bobSprintMultiplier : 1f;

            bobTimer += Time.deltaTime * bobFrequency * multiplier;

            Vector3 targetOffset = new Vector3(
                Mathf.Sin(bobTimer * 0.5f) * bobAmplitudeX * multiplier,
                Mathf.Sin(bobTimer)        * bobAmplitudeY * multiplier,
                0f
            );

            bobOffset = Vector3.SmoothDamp(
                bobOffset, targetOffset, ref bobVelocity, 1f / bobSmoothing);
        }
        else
        {
            bobOffset = Vector3.SmoothDamp(
                bobOffset, Vector3.zero, ref bobVelocity, 1f / bobSmoothing);
        }

        Vector3 pos = cameraPivot.localPosition;
        pos.x = bobOffset.x;
        pos.y += bobOffset.y * Time.deltaTime * 60f;
        cameraPivot.localPosition = pos;
    }
}