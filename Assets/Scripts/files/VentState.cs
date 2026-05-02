// VentState.cs
// Shared enums and constants for the scanner system.
// Add new scannable types here as the system expands.

namespace ScannerSystem
{
    public enum VentState
    {
        Open,
        Closed,
        Locked
    }

    /// <summary>
    /// Extend this enum later for doors, enemies, rooms, etc.
    /// </summary>
    public enum ScannableType
    {
        Vent,
        // Door,
        // Enemy,
        // Room,
    }
}
