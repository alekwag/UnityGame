// ScannerCamera.cs
// Attach to the scanner gun's dedicated Camera GameObject.
//
// Setup checklist:
//  1. Create a child Camera on the scanner gun model → attach this script.
//  2. Create a RenderTexture asset (e.g. 512×512, R8G8B8A24) → assign to renderTexture.
//  3. Assign the same RenderTexture to the screen quad material's _MainTex.
//  4. In Tags & Layers → Layers, create:
//       "Vent"           (vent meshes live here)
//       "VentHighlight"  (not a real layer – highlight is a shader trick, but keep for docs)
//  5. The scanner camera's Culling Mask should include EVERYTHING the main camera sees,
//     because the highlight shader draws on top regardless (ZTest Always).
//  6. The main camera's Culling Mask must EXCLUDE nothing special – highlights are
//     invisible there because the overlay materials are only visible via the scanner camera
//     control below.

using UnityEngine;

namespace ScannerSystem
{
    [RequireComponent(typeof(Camera))]
    public class ScannerCamera : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────
        [Header("Render Texture")]
        [Tooltip("Assign the RenderTexture asset that the screen quad material samples.")]
        [SerializeField] private RenderTexture renderTexture;

        [Header("Follow")]
        [Tooltip("Main camera transform – scanner cam mirrors its position/rotation with an offset.")]
        [SerializeField] private Transform mainCameraTransform;

        [Tooltip("Offset from main camera (usually zero – the gun model handles positioning).")]
        [SerializeField] private Vector3 positionOffset = Vector3.zero;

        [Header("Scanner Highlight Layer")]
        [Tooltip("Layer name used for vent GameObjects.")]
        [SerializeField] private string ventLayerName = "Vent";

        // ── Runtime ──────────────────────────────────────────────────────────
        private Camera scannerCam;
        private Camera mainCam;

        public Camera ScannerCam => scannerCam;

        // ── Unity lifecycle ──────────────────────────────────────────────────
        private void Awake()
        {
            scannerCam = GetComponent<Camera>();
            scannerCam.targetTexture = renderTexture;

            // Find main camera if not manually assigned
            if (mainCameraTransform == null)
                mainCameraTransform = Camera.main?.transform;

            mainCam = mainCameraTransform?.GetComponent<Camera>();
            ConfigureScannerCamera();
        }

        private void LateUpdate()
        {
            // Mirror main camera's FOV in case it changes at runtime
            if (mainCam != null)
                scannerCam.fieldOfView = mainCam.fieldOfView;
        }

        // ── Public ───────────────────────────────────────────────────────────

        /// <summary>
        /// Call this to shoot a raycast from the scanner camera's centre.
        /// Returns true if a scannable target was hit.
        /// </summary>
        public bool Shoot(out IScannableTarget hitTarget, out string label)
        {
            hitTarget = null;
            label = string.Empty;

            // Ray from the scanner camera centre (matches what is shown on the screen quad)
            Ray ray = scannerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
            {
                hitTarget = hit.collider.GetComponentInParent<IScannableTarget>();
                if (hitTarget != null)
                {
                    hitTarget.OnScanned();
                    label = hitTarget.GetScanLabel();
                    return true;
                }
            }
            return false;
        }

        // ── Internals ────────────────────────────────────────────────────────
        private void ConfigureScannerCamera()
        {
            // The scanner camera must render everything so walls draw normally.
            // The highlight overlay materials use ZTest Always, so they appear
            // through walls automatically without changing the culling mask.
            scannerCam.cullingMask = -1;    // all layers

            // Depth: render after the main camera so the RenderTexture is complete
            // before any potential post-processing reads it.
            scannerCam.depth = -2;

            // Don't clear to a solid colour – clear flags match main camera
            if (mainCam != null)
            {
                scannerCam.clearFlags      = mainCam.clearFlags;
                scannerCam.backgroundColor = mainCam.backgroundColor;
            }
        }

        // ── Editor helpers ───────────────────────────────────────────────────
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Camera c = GetComponent<Camera>();
            if (c == null) return;
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.4f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawFrustum(Vector3.zero, c.fieldOfView, c.farClipPlane, c.nearClipPlane, c.aspect);
        }
#endif
    }
}
