using UnityEngine;
using UnityEngine.InputSystem;

public class DirectionalMeleeWeapon : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LookDirectionTracker lookTracker;
    [SerializeField] private Transform attackOrigin;

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string playerMapName = "Player";
    [SerializeField] private string attackActionName = "Attack";

    [Header("Attack")]
    [SerializeField] private float range = 2f;
    [SerializeField] private float hitRadius = 0.35f;
    [SerializeField] private float damage = 25f;
    [SerializeField] private float cooldown = 0.45f;
    [SerializeField] private LayerMask hitMask = ~0;

    [Header("Directional Offsets (feel)")]
    [SerializeField] private float sideOffset = 0.25f;
    [SerializeField] private float verticalOffset = 0.15f;

    [Header("Debug")]
    [SerializeField] private bool drawDebugRay = true;

    private InputAction attackAction;
    private float nextAttackTime;

    private void Awake()
    {
        if (lookTracker == null)
        {
            lookTracker = GetComponentInParent<LookDirectionTracker>();
        }

        if (attackOrigin == null && Camera.main != null)
        {
            attackOrigin = Camera.main.transform;
        }

        if (inputActions == null)
        {
            Debug.LogError("DirectionalMeleeWeapon: Input Actions asset is missing.", this);
            return;
        }

        attackAction = inputActions.FindAction(playerMapName + "/" + attackActionName, true);
    }

    private void Update()
    {
        if (attackAction == null || attackOrigin == null)
        {
            return;
        }

        if (!attackAction.WasPressedThisFrame() || Time.time < nextAttackTime)
        {
            return;
        }

        nextAttackTime = Time.time + cooldown;
        PerformAttack(lookTracker != null ? lookTracker.LastLookVector : Vector2.right);
    }

    private void PerformAttack(Vector2 lookVector)
    {
        Vector3 origin = attackOrigin.position;
        Vector3 forward = attackOrigin.forward;

        // Continuous 360-direction offset based on last look movement.
        origin += attackOrigin.right * (lookVector.x * sideOffset);
        origin += attackOrigin.up * (lookVector.y * verticalOffset);

        bool hasHit = Physics.SphereCast(
            origin,
            hitRadius,
            forward,
            out RaycastHit hitInfo,
            range,
            hitMask,
            QueryTriggerInteraction.Ignore
        );

        if (drawDebugRay)
        {
            Color lineColor = hasHit ? Color.red : Color.green;
            Debug.DrawRay(origin, forward * range, lineColor, 0.25f);
        }

        if (!hasHit)
        {
            return;
        }

        // Block hook (optional): if target implements directional block, let it decide.
        // This is still compatible with the old 4-direction interface if you use it elsewhere.
        IDirectionalBlockable blockable4 = hitInfo.collider.GetComponentInParent<IDirectionalBlockable>();
        if (blockable4 != null)
        {
            CombatDirection approx = Mathf.Abs(lookVector.x) >= Mathf.Abs(lookVector.y)
                ? (lookVector.x >= 0 ? CombatDirection.Right : CombatDirection.Left)
                : (lookVector.y >= 0 ? CombatDirection.Up : CombatDirection.Down);
            if (blockable4.TryBlock(approx))
            {
                return;
            }
        }

        IDamageable damageable = hitInfo.collider.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
            return;
        }

        hitInfo.collider.gameObject.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
    }
}
