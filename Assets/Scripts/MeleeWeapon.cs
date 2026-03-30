using UnityEngine;
using UnityEngine.InputSystem;

public class MeleeWeapon : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string playerMapName = "Player";
    [SerializeField] private string attackActionName = "Attack";

    [Header("Attack")]
    [SerializeField] private Transform attackOrigin;
    [SerializeField] private float range = 2f;
    [SerializeField] private float hitRadius = 0.35f;
    [SerializeField] private float damage = 25f;
    [SerializeField] private float cooldown = 0.45f;
    [SerializeField] private LayerMask hitMask = ~0;

    [Header("Debug")]
    [SerializeField] private bool drawDebugRay = true;

    private InputAction attackAction;
    private float nextAttackTime;

    private void Awake()
    {
        if (attackOrigin == null && Camera.main != null)
        {
            attackOrigin = Camera.main.transform;
        }

        if (inputActions == null)
        {
            Debug.LogError("MeleeWeapon: Input Actions asset is missing.", this);
            return;
        }

        attackAction = inputActions.FindAction(playerMapName + "/" + attackActionName, true);
    }

    private void Update()
    {
        if (attackAction == null)
        {
            return;
        }

        if (!attackAction.WasPressedThisFrame() || Time.time < nextAttackTime)
        {
            return;
        }

        nextAttackTime = Time.time + cooldown;
        PerformAttack();
    }

    private void PerformAttack()
    {
        if (attackOrigin == null)
        {
            return;
        }

        Vector3 origin = attackOrigin.position;
        Vector3 direction = attackOrigin.forward;

        bool hasHit = Physics.SphereCast(
            origin,
            hitRadius,
            direction,
            out RaycastHit hitInfo,
            range,
            hitMask,
            QueryTriggerInteraction.Ignore
        );

        if (drawDebugRay)
        {
            Color lineColor = hasHit ? Color.red : Color.green;
            Debug.DrawRay(origin, direction * range, lineColor, 0.25f);
        }

        if (!hasHit)
        {
            return;
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
