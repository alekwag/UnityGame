// IScannableTarget.cs
// Every object the scanner gun can interact with implements this interface.
// Keeps the gun code decoupled from specific target types.

using UnityEngine;

namespace ScannerSystem
{
    public interface IScannableTarget
    {
        /// <summary>Called when the scanner gun shoots this target.</summary>
        void OnScanned();

        /// <summary>Returns the renderer(s) that should receive highlight overlays.</summary>
        Renderer[] GetHighlightRenderers();

        /// <summary>Human-readable label shown on the scanner HUD (optional).</summary>
        string GetScanLabel();
    }
}
