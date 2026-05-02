using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Climbing System -- attach to the same GameObject as FPSCharacterController.
/// Tag any BoxCollider with "HangBlock" or "TopHangBlock" to make it climbable.
/// Orient blocks so local +Z faces away from the wall.
/// </summary>
[RequireComponent(typeof(FPSCharacterController))]
[RequireComponent(typeof(CharacterController))]
public class ClimbingSystem : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string playerMapName = "Player";
    [SerializeField] private string moveActionName = "Move";
    [SerializeField] private string jumpActionName = "Jump";

    [Header("Blocks")]
    [SerializeField] private string hangBlockTag = "HangBlock";
    [SerializeField] private string topHangBlockTag = "TopHangBlock"; 
    [SerializeField] private float gripDepthBelowLedgeTop = 0.15f;

    [Header("Auto-Grab")]
    [SerializeField] private float autoGrabDetectionRadius = 0.9f;
    [SerializeField] private float grabCheckHeightAboveRoot = 0.7f;

    [Header("Hang")]
    [SerializeField] private Vector3 hangBodyOffsetFromGrabPoint = new Vector3(0f, -1.1f, 0.25f);
    [SerializeField] private float snapToHangPositionSpeed = 14f;
    [SerializeField] private float shimmyLateralSpeed = 2.5f;

    [Header("Mantle (Top Hang)")]
    [SerializeField] private float mantleLookUpThreshold = 0.15f; 
    /// <summary> 
    /// How much you must face the wall to mantle. 
    /// 1.0 = Perfectly straight at wall, 0.0 = Side-on, -1.0 = Facing away.
    /// </summary>
    [SerializeField] private float mantleFacingThreshold = 0.4f; // roughly 60 degrees
    [SerializeField] private float mantleDuration = 0.6f;
    [SerializeField] private float mantleHeightOffset = 1.4f; 
    [SerializeField] private float mantleForwardDepth = 1.5f;

    [Header("Arc Jump")]
    [SerializeField] private float arcJumpMaxReachDistance = 5f;
    [SerializeField] private float arcJumpMaxArcHeight = 3f;
    [SerializeField] private float arcJumpTravelDuration = 0.45f;
    [SerializeField] private float arcJumpAimAssistMinDot = -0.3f;

    [Header("Kick-off")]
    [SerializeField] private float kickOffForwardImpulse = 5f;
    [SerializeField] private float lookAwayFromWallThreshold = -0.1f;

    [Header("Cooldown")]
    [SerializeField] private float sameColliderReCooldown = 0.5f;

    [Header("Predictive Highlight")]
    [SerializeField] private Color ledgeHighlightColor = new Color(0.4f, 0.8f, 1f);
    [SerializeField] private float ledgeHighlightIntensity = 1.4f;

    private struct Ledge
    {
        public BoxCollider col;
        public Transform tr;
        public float gripDepth;
        public bool isTopLedge; 

        public Vector3 GrabCenter
        {
            get
            {
                Vector3 localGripPoint = col.center + new Vector3(0f, col.size.y * 0.5f - gripDepth, 0f);
                return tr.TransformPoint(localGripPoint);
            }
        }

        public Vector3 LedgeRight => tr.right;
        public Vector3 WallOutwardNormal => tr.forward;
        public float HalfLength => col.size.x * tr.lossyScale.x * 0.5f;

        public Vector3 ClampToLedge(Vector3 worldPos)
        {
            Vector3 center = GrabCenter;
            float along = Vector3.Dot(worldPos - center, LedgeRight);
            return center + LedgeRight * Mathf.Clamp(along, -HalfLength, HalfLength);
        }

        public bool IsWithinRadius(Vector3 worldPos, float radius) =>
            Vector3.Distance(worldPos, ClampToLedge(worldPos)) <= radius;

        public float ComputeLedgeT(Vector3 worldPos)
        {
            float along = Vector3.Dot(ClampToLedge(worldPos) - GrabCenter, LedgeRight);
            return HalfLength > 0f ? Mathf.Clamp(along / HalfLength, -1f, 1f) : 0f;
        }
    }

    private enum ClimbState { Idle, Hanging, ArcJumping, Mantling }

    private FPSCharacterController playerController;
    private CharacterController characterController;
    private Transform cameraTransform;
    private InputAction moveInputAction;
    private InputAction jumpInputAction;

    private ClimbState currentState;
    private Ledge activeLedge;
    private float activeLedgeT;

    private Ledge arcTargetLedge;
    private float arcTargetLedgeT;
    private float arcElapsed;
    private float arcTotalDuration;
    private Vector3 arcStartPosition;
    private Vector3 arcEndPosition;

    private float mantleElapsed;
    private Vector3 mantleStartPos;

    private Collider lastReleasedCollider;
    private float releaseCooldownTimer;
    private readonly List<Ledge> allLedges = new List<Ledge>();

    private Renderer highlightedLedgeRenderer;
    private Color highlightedLedgeOriginalEmission;
    private bool highlightedLedgeHadEmission;

    private void Awake()
    {
        playerController = GetComponent<FPSCharacterController>();
        characterController = GetComponent<CharacterController>();
        if (Camera.main != null) cameraTransform = Camera.main.transform;

        if (inputActions != null)
        {
            moveInputAction = inputActions.FindAction(playerMapName + "/" + moveActionName, false);
            jumpInputAction = inputActions.FindAction(playerMapName + "/" + jumpActionName, false);
        }
        RefreshLedgeList();
    }

    private void OnDisable() => ClearLedgeHighlight();

    private void Update()
    {
        releaseCooldownTimer -= Time.deltaTime;
        if (releaseCooldownTimer <= 0f) lastReleasedCollider = null;

        switch (currentState)
        {
            case ClimbState.Idle: UpdateIdle(); break;
            case ClimbState.Hanging: UpdateHanging(); break;
            case ClimbState.ArcJumping: UpdateArcJumping(); break;
            case ClimbState.Mantling: UpdateMantling(); break;
        }
        ApplyPredictiveHighlight();
    }

    private void UpdateIdle()
    {
        if (releaseCooldownTimer > 0f) return;
        Vector3 grabCheckOrigin = cameraTransform.position;

        int bestIndex = -1;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < allLedges.Count; i++)
        {
            if (allLedges[i].col == lastReleasedCollider) continue;
            if (!allLedges[i].IsWithinRadius(grabCheckOrigin, autoGrabDetectionRadius)) continue;

            float dist = Vector3.Distance(grabCheckOrigin, allLedges[i].ClampToLedge(grabCheckOrigin));
            if (dist < bestDistance) { bestDistance = dist; bestIndex = i; }
        }

        if (bestIndex >= 0)
        {
            BeginGrab(allLedges[bestIndex], grabCheckOrigin);
        }
    }

    private void UpdateHanging()
    {
        if (activeLedge.col == null) { ReleaseFromLedge(); return; }

        Vector3 grabPoint = activeLedge.GrabCenter + activeLedge.LedgeRight * (activeLedgeT * activeLedge.HalfLength);
        Vector3 hangTarget = grabPoint
                             + activeLedge.tr.up * hangBodyOffsetFromGrabPoint.y
                             + activeLedge.WallOutwardNormal * hangBodyOffsetFromGrabPoint.z
                             + activeLedge.LedgeRight * hangBodyOffsetFromGrabPoint.x;

        characterController.Move(Vector3.Lerp(Vector3.zero, hangTarget - transform.position, snapToHangPositionSpeed * Time.deltaTime));

        Vector2 moveInput = moveInputAction != null ? moveInputAction.ReadValue<Vector2>() : Vector2.zero;
        if (Mathf.Abs(moveInput.x) > 0.05f)
        {
            activeLedgeT = Mathf.Clamp(activeLedgeT - moveInput.x * shimmyLateralSpeed * Time.deltaTime / Mathf.Max(activeLedge.HalfLength, 0.01f), -1f, 1f);
        }

        if (jumpInputAction != null && jumpInputAction.WasPressedThisFrame())
            AttemptJumpFromLedge();
    }

    private void UpdateMantling()
    {
        mantleElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(mantleElapsed / mantleDuration);
        float easedT = t * t * (3f - 2f * t); 

        Vector3 grabPoint = activeLedge.GrabCenter + activeLedge.LedgeRight * (activeLedgeT * activeLedge.HalfLength);
        Vector3 targetPos;

        if (easedT < 0.5f)
        {
            float phaseT = easedT * 2f;
            targetPos = Vector3.Lerp(mantleStartPos, grabPoint + Vector3.up * mantleHeightOffset, phaseT);
        }
        else
        {
            float phaseT = (easedT - 0.5f) * 2f;
            Vector3 peakPos = grabPoint + Vector3.up * mantleHeightOffset;
            Vector3 landPos = peakPos - (activeLedge.WallOutwardNormal * mantleForwardDepth);
            targetPos = Vector3.Lerp(peakPos, landPos, phaseT);
        }

        characterController.Move(targetPos - transform.position);

        if (t >= 1f)
        {
            lastReleasedCollider = activeLedge.col;
            releaseCooldownTimer = sameColliderReCooldown;
            currentState = ClimbState.Idle;
            playerController.SetMovementLocked(false);
            activeLedge = default;
        }
    }

    private void UpdateArcJumping()
    {
        arcElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(arcElapsed / arcTotalDuration);
        float easedT = t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t;

        Vector3 pos = Vector3.Lerp(arcStartPosition, arcEndPosition, easedT);
        float horizontalDist = Vector3.Distance(new Vector3(arcStartPosition.x, 0f, arcStartPosition.z), new Vector3(arcEndPosition.x, 0f, arcEndPosition.z));
        float arcHeight = Mathf.Clamp(horizontalDist * 0.3f, 0.1f, arcJumpMaxArcHeight);
        
        pos.y += Mathf.Sin(t * Mathf.PI) * arcHeight;
        characterController.Move(pos - transform.position);

        if (t >= 1f)
        {
            activeLedge = arcTargetLedge;
            activeLedgeT = arcTargetLedgeT;
            currentState = ClimbState.Hanging;
            lastReleasedCollider = null;
            releaseCooldownTimer = sameColliderReCooldown;
        }
    }

    private void BeginArc(Ledge target)
    {
        arcStartPosition = transform.position;
        arcTargetLedgeT  = target.ComputeLedgeT(transform.position);
        arcEndPosition   = target.GrabCenter
                           + target.LedgeRight        * (arcTargetLedgeT * target.HalfLength)
                           + target.tr.up              * hangBodyOffsetFromGrabPoint.y
                           + target.WallOutwardNormal  * hangBodyOffsetFromGrabPoint.z;

        arcTargetLedge       = target;
        arcElapsed           = 0f;
        arcTotalDuration     = arcJumpTravelDuration;
        lastReleasedCollider = activeLedge.col;
        releaseCooldownTimer = sameColliderReCooldown;
        activeLedge          = default;
        currentState         = ClimbState.ArcJumping;
    }

    private void AttemptJumpFromLedge()
    {
        Vector3 lookDirection = cameraTransform != null ? cameraTransform.forward : transform.forward;

        // --- NEW MANTLE CHECK ---
        if (activeLedge.isTopLedge)
        {
            // 1. Vertical check (Looking above flat)
            bool isLookingUp = lookDirection.y > mantleLookUpThreshold;

            // 2. Horizontal check (Looking toward the wall)
            // activeLedge.WallOutwardNormal faces AWAY from the wall, so we want the opposite
            Vector3 wallInwardDir = -activeLedge.WallOutwardNormal;
            
            // Flatten vectors to X/Z plane to check horizontal facing only
            Vector3 lookFlat = new Vector3(lookDirection.x, 0, lookDirection.z).normalized;
            Vector3 wallFlat = new Vector3(wallInwardDir.x, 0, wallInwardDir.z).normalized;
            
            float facingDot = Vector3.Dot(lookFlat, wallFlat);
            bool isFacingLedge = facingDot > mantleFacingThreshold;

            if (isLookingUp && isFacingLedge)
            {
                currentState = ClimbState.Mantling;
                mantleElapsed = 0f;
                mantleStartPos = transform.position;
                return;
            }
        }

        // If not mantling, decide between Kick-off or Arc Jump
        if (Vector3.Dot(lookDirection, activeLedge.WallOutwardNormal) > lookAwayFromWallThreshold)
        {
            KickOff(lookDirection);
        }
        else
        {
            int bestTargetIndex = FindBestArcJumpTarget(lookDirection);
            if (bestTargetIndex >= 0) BeginArc(allLedges[bestTargetIndex]);
            else KickOff(lookDirection);
        }
    }

    private void KickOff(Vector3 lookDirection)
    {
        ReleaseFromLedge();
        StartCoroutine(ApplyKickOffVelocity(lookDirection.normalized * kickOffForwardImpulse));
    }

    private IEnumerator ApplyKickOffVelocity(Vector3 velocity)
    {
        for (float timeLeft = 0.55f; timeLeft > 0f; timeLeft -= Time.deltaTime)
        {
            characterController.Move(velocity * Time.deltaTime);
            yield return null;
        }
    }

    private void BeginGrab(Ledge ledge, Vector3 fromPosition)
    {
        activeLedge = ledge;
        activeLedgeT = ledge.ComputeLedgeT(fromPosition);
        currentState = ClimbState.Hanging;
        playerController.SetMovementLocked(true);
    }

    private void ReleaseFromLedge()
    {
        lastReleasedCollider = activeLedge.col;
        releaseCooldownTimer = sameColliderReCooldown;
        activeLedge = default;
        currentState = ClimbState.Idle;
        playerController.SetMovementLocked(false);
    }

    public void RefreshLedgeList()
    {
        allLedges.Clear();
        foreach (GameObject go in GameObject.FindGameObjectsWithTag(hangBlockTag)) AddLedgeFromObject(go, false);
        foreach (GameObject go in GameObject.FindGameObjectsWithTag(topHangBlockTag)) AddLedgeFromObject(go, true);
    }

    private void AddLedgeFromObject(GameObject go, bool isTop)
    {
        if (go.TryGetComponent(out BoxCollider col))
            allLedges.Add(new Ledge { col = col, tr = col.transform, gripDepth = gripDepthBelowLedgeTop, isTopLedge = isTop });
    }

    private void ApplyPredictiveHighlight() => ApplyHighlightToRenderer(ResolvePredictedLedgeRenderer());

    private Renderer ResolvePredictedLedgeRenderer()
    {
        if (currentState == ClimbState.Idle)
        {
            Vector3 origin = cameraTransform.position;
            int bi = -1; float bd = float.MaxValue;
            for (int i = 0; i < allLedges.Count; i++) {
                if (allLedges[i].col == lastReleasedCollider) continue;
                if (!allLedges[i].IsWithinRadius(origin, autoGrabDetectionRadius)) continue;
                float d = Vector3.Distance(origin, allLedges[i].ClampToLedge(origin));
                if (d < bd) { bd = d; bi = i; }
            }
            return bi >= 0 ? allLedges[bi].col.GetComponentInChildren<Renderer>() : null;
        }
        if (currentState == ClimbState.Hanging)
        {
            Vector3 look = cameraTransform.forward;
            if (activeLedge.isTopLedge && look.y > mantleLookUpThreshold) return activeLedge.col.GetComponentInChildren<Renderer>();
            int ji = FindBestArcJumpTarget(look);
            return ji >= 0 ? allLedges[ji].col.GetComponentInChildren<Renderer>() : null;
        }
        return null;
    }

    private int FindBestArcJumpTarget(Vector3 lookDirection)
    {
        int bestIndex = -1; float bestScore = float.MinValue;
        for (int i = 0; i < allLedges.Count; i++) {
            if (allLedges[i].col == null || allLedges[i].col == activeLedge.col) continue;
            float distance = Vector3.Distance(transform.position, allLedges[i].GrabCenter);
            if (distance > arcJumpMaxReachDistance) continue;
            float aimDot = Vector3.Dot(lookDirection, (allLedges[i].GrabCenter - cameraTransform.position).normalized);
            if (aimDot < arcJumpAimAssistMinDot) continue;
            float score = aimDot - distance * 0.1f;
            if (score > bestScore) { bestScore = score; bestIndex = i; }
        }
        return bestIndex;
    }

    private void ApplyHighlightToRenderer(Renderer targetRenderer)
    {
        if (targetRenderer == highlightedLedgeRenderer) return;
        ClearLedgeHighlight();
        if (targetRenderer == null) return;
        highlightedLedgeRenderer = targetRenderer;
        Material mat = targetRenderer.material;
        highlightedLedgeHadEmission = mat.IsKeywordEnabled("_EMISSION");
        if (mat.HasProperty("_EmissionColor")) {
            mat.EnableKeyword("_EMISSION");
            highlightedLedgeOriginalEmission = mat.GetColor("_EmissionColor");
            mat.SetColor("_EmissionColor", ledgeHighlightColor * ledgeHighlightIntensity);
        } else if (mat.HasProperty("_Color")) {
            highlightedLedgeOriginalEmission = mat.GetColor("_Color");
            mat.SetColor("_Color", ledgeHighlightColor);
        }
    }

    private void ClearLedgeHighlight()
    {
        if (highlightedLedgeRenderer == null) return;
        Material mat = highlightedLedgeRenderer.material;
        if (mat.HasProperty("_EmissionColor")) {
            if (!highlightedLedgeHadEmission) mat.DisableKeyword("_EMISSION");
            else mat.SetColor("_EmissionColor", highlightedLedgeOriginalEmission);
        } else if (mat.HasProperty("_Color")) mat.SetColor("_Color", highlightedLedgeOriginalEmission);
        highlightedLedgeRenderer = null;
    }
}