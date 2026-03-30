using UnityEngine;
using UnityEngine.InputSystem;

public class DirectionalWeaponVisual : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform weaponPivot;
    [SerializeField] private Transform weaponMesh;
    [SerializeField] private LookDirectionTracker lookTracker;
    [SerializeField] private DirectionalBlock block;

    [Header("Mesh Offset (optional)")]
    [SerializeField] private Vector3 meshLocalPositionOffset;
    [SerializeField] private Vector3 meshLocalRotationOffset;

    [Header("Input (Attack)")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string playerMapName = "Player";
    [SerializeField] private string attackActionName = "Attack";

    [Header("Swing")]
    [SerializeField] private float swingAngle = 55f;
    [SerializeField] private float anticipationAngle = 20f;
    [SerializeField] private float followThroughAngle = 18f;
    [SerializeField] private float anticipationTime = 0.06f;
    [SerializeField] private float swingTime = 0.12f;
    [SerializeField] private float returnTime = 0.10f;
    [SerializeField] private float swingCooldown = 0.20f;

    [Header("Idle Direction Bias")]
    [SerializeField] private float idleYawBias = 10f;
    [SerializeField] private float idlePitchBias = 6f;
    [SerializeField] private float idleRollBias = 10f;
    [SerializeField] private float idleSideOffset = 0.12f;
    [SerializeField] private float idleVerticalOffset = 0.03f;
    [SerializeField] private float idleForwardOffset = 0.00f;
    [SerializeField] private float idleLerpSpeed = 10f;

    [Header("Block Pose")]
    [SerializeField] private float blockAngle = 35f;
    [SerializeField] private float blockMoveUp = 0.05f;
    [SerializeField] private float blockLerpSpeed = 18f;

    private InputAction attackAction;

    private Quaternion baseLocalRotation;
    private Vector3 baseLocalPosition;
    private Quaternion meshBaseLocalRotation;
    private Vector3 meshBaseLocalPosition;

    private float nextSwingAllowedTime;
    private float swingTimer;
    private Vector2 swingVector = Vector2.right;

    private void Awake()
    {
        if (weaponPivot == null)
        {
            weaponPivot = transform;
        }

        if (lookTracker == null)
        {
            lookTracker = GetComponentInParent<LookDirectionTracker>();
        }

        if (block == null)
        {
            block = GetComponentInParent<DirectionalBlock>();
        }

        if (weaponMesh == null && weaponPivot != null && weaponPivot.childCount > 0)
        {
            weaponMesh = weaponPivot.GetChild(0);
        }

        baseLocalRotation = weaponPivot.localRotation;
        baseLocalPosition = weaponPivot.localPosition;
        if (weaponMesh != null)
        {
            meshBaseLocalRotation = weaponMesh.localRotation;
            meshBaseLocalPosition = weaponMesh.localPosition;
        }

        if (inputActions == null)
        {
            Debug.LogError("DirectionalWeaponVisual: Input Actions asset is missing.", this);
            return;
        }

        attackAction = inputActions.FindAction(playerMapName + "/" + attackActionName, true);
    }

    private void Update()
    {
        if (weaponPivot == null || attackAction == null)
        {
            return;
        }

        bool isBlocking = block != null && block.IsBlocking;
        if (!isBlocking)
        {
            TryStartSwing();
        }

        ApplyWeaponPose(isBlocking);
    }

    private void TryStartSwing()
    {
        if (!attackAction.WasPressedThisFrame() || Time.time < nextSwingAllowedTime)
        {
            return;
        }

        nextSwingAllowedTime = Time.time + swingCooldown;
        swingTimer = anticipationTime + swingTime + returnTime;
        swingVector = lookTracker != null ? lookTracker.LastLookVector : Vector2.right;
    }

    private void ApplyWeaponPose(bool isBlocking)
    {
        Vector2 lastLook = lookTracker != null ? lookTracker.LastLookVector : Vector2.right;

        // Idle pose shifts to the *opposite* side of last look direction.
        Vector2 idleBiasVector = -lastLook;
        Quaternion idleRot = baseLocalRotation
            * Quaternion.AngleAxis(idleBiasVector.x * idleYawBias, Vector3.up)
            * Quaternion.AngleAxis(-idleBiasVector.y * idlePitchBias, Vector3.right)
            * Quaternion.AngleAxis(idleBiasVector.x * idleRollBias, Vector3.forward);
        Vector3 idlePos = baseLocalPosition
            + new Vector3(idleBiasVector.x * idleSideOffset, idleBiasVector.y * idleVerticalOffset, idleForwardOffset);

        Quaternion targetRot = idleRot;
        Vector3 targetPos = idlePos;

        if (isBlocking)
        {
            Vector2 v = block != null ? block.BlockVector : lastLook;
            (targetRot, targetPos) = GetBlockPose360(v, idleRot);
        }
        else if (swingTimer > 0f)
        {
            float total = anticipationTime + swingTime + returnTime;
            float t = 1f - (swingTimer / total);
            float anticipationPortion = anticipationTime / total;
            float swingPortion = swingTime / total;
            float swingEnd = anticipationPortion + swingPortion;

            if (t <= anticipationPortion)
            {
                // Wind-up slightly farther opposite direction before cut.
                float windupT = Mathf.InverseLerp(0f, anticipationPortion, t);
                Vector2 start = -swingVector;
                Vector2 windup = -swingVector * 1.2f;
                Vector2 v = Vector2.Lerp(start, windup, Mathf.SmoothStep(0f, 1f, windupT));
                BuildSlashPose(v, anticipationAngle, out targetRot, out targetPos);
            }
            else if (t <= swingEnd)
            {
                // Main cut phase: travel across from opposite to attack side.
                float swingT = Mathf.InverseLerp(anticipationPortion, swingEnd, t);
                Vector2 across = Vector2.Lerp(-swingVector, swingVector, Mathf.SmoothStep(0f, 1f, swingT));
                BuildSlashPose(across, swingAngle, out targetRot, out targetPos);
            }
            else
            {
                // Follow-through then settle back to opposite-side idle.
                float returnT = Mathf.InverseLerp(swingEnd, 1f, t);
                Vector2 followVec = swingVector * 1.1f;
                BuildSlashPose(followVec, followThroughAngle, out Quaternion followRot, out Vector3 followPos);

                targetRot = Quaternion.Slerp(followRot, idleRot, returnT);
                targetPos = Vector3.Lerp(followPos, idlePos, returnT);
            }
            swingTimer -= Time.deltaTime;
        }

        float lerp = isBlocking ? blockLerpSpeed : (swingTimer > 0f ? 25f : idleLerpSpeed);
        weaponPivot.localRotation = Quaternion.Slerp(weaponPivot.localRotation, targetRot, lerp * Time.deltaTime);
        weaponPivot.localPosition = Vector3.Lerp(weaponPivot.localPosition, targetPos, lerp * Time.deltaTime);
        ApplyMeshOffset();
    }

    private (Quaternion rot, Vector3 pos) GetBlockPose360(Vector2 v, Quaternion baseRotForPose)
    {
        Quaternion rot = baseRotForPose;
        Vector3 pos = baseLocalPosition + new Vector3(0f, blockMoveUp, 0f);

        float yaw = v.x * blockAngle;
        float pitch = -v.y * blockAngle;
        rot *= Quaternion.AngleAxis(yaw, Vector3.up) * Quaternion.AngleAxis(pitch * 0.75f, Vector3.right);

        return (rot, pos);
    }

    private void BuildSlashPose(Vector2 v, float angle, out Quaternion rot, out Vector3 pos)
    {
        // Diagonal slashes get extra roll to feel more "sword-like" than mirrored stick arcs.
        float diagonal = Mathf.Abs(v.x * v.y);
        float yaw = v.x * angle;
        float pitch = -v.y * angle * 0.9f;
        float roll = v.x * (angle * 0.35f) + (-v.y * angle * 0.2f) + (Mathf.Sign(v.x) * diagonal * angle * 0.35f);

        rot = baseLocalRotation
            * Quaternion.AngleAxis(yaw, Vector3.up)
            * Quaternion.AngleAxis(pitch, Vector3.right)
            * Quaternion.AngleAxis(roll, Vector3.forward);

        pos = baseLocalPosition + new Vector3(v.x * idleSideOffset * 1.8f, v.y * idleVerticalOffset * 1.35f, idleForwardOffset);
    }

    private void ApplyMeshOffset()
    {
        if (weaponMesh == null)
        {
            return;
        }

        weaponMesh.localPosition = meshBaseLocalPosition + meshLocalPositionOffset;
        weaponMesh.localRotation = meshBaseLocalRotation * Quaternion.Euler(meshLocalRotationOffset);
    }
}

