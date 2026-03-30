public interface IDirectionalBlockable
{
    /// <summary>
    /// Return true if the hit should be blocked/negated.
    /// </summary>
    bool TryBlock(CombatDirection incomingDirection);
}
