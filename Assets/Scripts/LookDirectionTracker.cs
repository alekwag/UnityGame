using UnityEngine;

public class LookDirectionTracker : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraTransform;

    [Header("Tuning")]
    [SerializeField] private float minAngleDeltaToRegister = 0.08f;

    public CombatDirection LastDirection { get; private set; } = CombatDirection.Right;
    public Vector2 LastLookVector { get; private set; } = Vector2.right;

    private float previousYaw;
    private float previousPitch;

    private void Awake()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void Start()
    {
        if (cameraTransform == null)
        {
            return;
        }

        previousYaw = cameraTransform.eulerAngles.y;
        previousPitch = cameraTransform.eulerAngles.x;
    }

    private void Update()
    {
        if (cameraTransform == null)
        {
            return;
        }

        float currentYaw = cameraTransform.eulerAngles.y;
        float currentPitch = cameraTransform.eulerAngles.x;

        float yawDelta = Mathf.DeltaAngle(previousYaw, currentYaw);
        float pitchDelta = Mathf.DeltaAngle(previousPitch, currentPitch);

        previousYaw = currentYaw;
        previousPitch = currentPitch;

        float absYaw = Mathf.Abs(yawDelta);
        float absPitch = Mathf.Abs(pitchDelta);
        if (absYaw < minAngleDeltaToRegister && absPitch < minAngleDeltaToRegister)
        {
            return;
        }

        // Convert camera rotation delta into a 2D "intent" vector.
        // X = yaw (right positive), Y = pitch (up positive).
        float upDown = -pitchDelta;
        Vector2 v = new Vector2(yawDelta, upDown);
        if (v.sqrMagnitude > 0.0001f)
        {
            LastLookVector = v.normalized;
        }

        if (absYaw >= absPitch)
        {
            LastDirection = yawDelta >= 0f ? CombatDirection.Right : CombatDirection.Left;
        }
        else
        {
            // Increasing camera X rotation is looking down in a typical FPS rig.
            LastDirection = pitchDelta >= 0f ? CombatDirection.Down : CombatDirection.Up;
        }
    }
}
