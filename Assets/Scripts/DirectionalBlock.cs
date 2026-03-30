using UnityEngine;
using UnityEngine.InputSystem;

public class DirectionalBlock : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LookDirectionTracker lookTracker;

    public bool IsBlocking { get; private set; }
    public CombatDirection BlockDirection { get; private set; } = CombatDirection.Right;
    public Vector2 BlockVector { get; private set; } = Vector2.right;

    private void Awake()
    {
        if (lookTracker == null)
        {
            lookTracker = GetComponentInParent<LookDirectionTracker>();
        }
    }

    private void Update()
    {
        if (Mouse.current == null)
        {
            IsBlocking = false;
            return;
        }

        bool held = Mouse.current.rightButton.isPressed;

        if (!IsBlocking && held)
        {
            BlockDirection = lookTracker != null ? lookTracker.LastDirection : BlockDirection;
            BlockVector = lookTracker != null ? lookTracker.LastLookVector : BlockVector;
        }

        IsBlocking = held;
    }
}

