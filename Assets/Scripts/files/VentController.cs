// VentController.cs  (DEBUG VERSION)
// Extensive logging added to find exactly where the highlight setup fails.
// Check the Unity Console after pressing Play — the logs will tell you exactly what's wrong.

using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace ScannerSystem
{
    [RequireComponent(typeof(Collider))]
    public class VentController : MonoBehaviour, IScannableTarget
    {
        [Header("State")]
        [SerializeField] private VentState initialState = VentState.Closed;

        [Header("Renderers")]
        [Tooltip("Drag the MeshRenderer(s) of this vent's mesh here.")]
        [SerializeField] private Renderer[] ventRenderers;

        [Header("Highlight")]
        [Tooltip("Material using the ScannerHighlight shader. MUST be assigned.")]
        [SerializeField] private Material highlightMaterial;

        [Header("NavMesh")]
        [SerializeField] private NavMeshObstacle navMeshObstacle;

        [Header("Locked Glitch")]
        [SerializeField] private float flickerFrequency = 8f;
        [SerializeField] private float flickerIntensity = 0.4f;

        // ── Runtime ──────────────────────────────────────────────────────────
        private VentState currentState;
        private GameObject[] overlayObjects;
        private Material[]   overlayMaterials;
        private Coroutine    glitchCoroutine;

        private static readonly int ColorProp = Shader.PropertyToID("_HighlightColor");
        private static readonly int AlphaProp = Shader.PropertyToID("_Alpha");

        private static readonly Color ColorOpen   = new Color(0.0f, 1.0f, 0.2f,  0.55f);
        private static readonly Color ColorClosed = new Color(1.0f, 0.15f, 0.0f, 0.55f);
        private static readonly Color ColorLocked = new Color(1.0f, 0.55f, 0.55f, 1.0f);

        // ── Unity lifecycle ──────────────────────────────────────────────────
        private void Start()
        {
            currentState = initialState;

            Debug.Log($"[VentController] '{name}' starting setup...");

            // --- Check 1: highlight material ---
            if (highlightMaterial == null)
            {
                Debug.LogError($"[VentController] '{name}': highlightMaterial is NOT assigned in the Inspector! " +
                               "Create a material with the ScannerHighlight shader and drag it here.", this);
                return;
            }
            Debug.Log($"[VentController] '{name}': highlightMaterial OK → '{highlightMaterial.name}' " +
                      $"using shader '{highlightMaterial.shader.name}'");

            // --- Check 2: ventRenderers ---
            if (ventRenderers == null || ventRenderers.Length == 0)
            {
                // Auto-find renderers on this GameObject as a fallback
                ventRenderers = GetComponentsInChildren<Renderer>();
                Debug.LogWarning($"[VentController] '{name}': ventRenderers was empty. " +
                                 $"Auto-found {ventRenderers.Length} renderer(s) in children. " +
                                 "Assign them manually in the Inspector to be safe.", this);
            }

            if (ventRenderers.Length == 0)
            {
                Debug.LogError($"[VentController] '{name}': No renderers found at all! " +
                               "The vent needs at least one MeshRenderer.", this);
                return;
            }

            for (int i = 0; i < ventRenderers.Length; i++)
                Debug.Log($"[VentController] '{name}': ventRenderers[{i}] = " +
                          (ventRenderers[i] != null ? ventRenderers[i].name : "NULL"));

            // --- Check 3: VentHighlight layer ---
            int highlightLayer = LayerMask.NameToLayer("VentHighlight");
            if (highlightLayer == -1)
            {
                Debug.LogError($"[VentController] '{name}': Layer 'VentHighlight' does not exist! " +
                               "Go to Edit → Project Settings → Tags and Layers and add it.", this);
                return;
            }
            Debug.Log($"[VentController] '{name}': 'VentHighlight' layer found at index {highlightLayer}.");

            // --- Build overlay objects ---
            BuildOverlayObjects(highlightLayer);

            // --- Apply initial state ---
            ApplyState(currentState, force: true);

            Debug.Log($"[VentController] '{name}': Setup complete. State = {currentState}. " +
                      $"Overlay objects created: {(overlayObjects != null ? overlayObjects.Length : 0)}");
        }

        // ── IScannableTarget ─────────────────────────────────────────────────
        public void OnScanned()
        {
            if (currentState == VentState.Locked)
            {
                Debug.Log($"[VentController] '{name}': Scanned but LOCKED — no state change.");
                return;
            }
            VentState next = currentState == VentState.Open ? VentState.Closed : VentState.Open;
            Debug.Log($"[VentController] '{name}': Scanned → {currentState} → {next}");
            ApplyState(next);
        }

        public Renderer[] GetHighlightRenderers() => ventRenderers;
        public string GetScanLabel()              => $"VENT [{currentState.ToString().ToUpper()}]";

        // ── Public API ───────────────────────────────────────────────────────
        public VentState CurrentState => currentState;
        public void SetState(VentState newState) => ApplyState(newState, force: true);

        public void EnsureOverlayLayer(int highlightLayer)
        {
            if (overlayObjects == null) return;
            foreach (var obj in overlayObjects)
                if (obj != null) obj.layer = highlightLayer;
        }

        // ── Internals ────────────────────────────────────────────────────────
        private void BuildOverlayObjects(int highlightLayer)
        {
            overlayObjects   = new GameObject[ventRenderers.Length];
            overlayMaterials = new Material[ventRenderers.Length];

            for (int i = 0; i < ventRenderers.Length; i++)
            {
                Renderer src = ventRenderers[i];
                if (src == null)
                {
                    Debug.LogWarning($"[VentController] '{name}': ventRenderers[{i}] is null, skipping.");
                    continue;
                }

                // Create child GameObject on the VentHighlight layer
                var go = new GameObject($"_Highlight_{src.name}");
                go.layer = highlightLayer;
                go.transform.SetParent(src.transform, worldPositionStays: false);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale    = Vector3.one;

                // Copy mesh from source renderer
                var srcFilter = src.GetComponent<MeshFilter>();
                if (srcFilter == null || srcFilter.sharedMesh == null)
                {
                    Debug.LogWarning($"[VentController] '{name}': renderer '{src.name}' has no MeshFilter " +
                                     "or no mesh — overlay will have no geometry!", this);
                }
                else
                {
                    var mf = go.AddComponent<MeshFilter>();
                    mf.sharedMesh = srcFilter.sharedMesh;
                    Debug.Log($"[VentController] '{name}': overlay[{i}] copied mesh '{srcFilter.sharedMesh.name}'.");
                }

                // Instance the highlight material so each vent has its own colour
                var mat              = new Material(highlightMaterial);
                overlayMaterials[i]  = mat;

                var mr                   = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial        = mat;
                mr.shadowCastingMode     = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows        = false;

                overlayObjects[i] = go;

                Debug.Log($"[VentController] '{name}': overlay GameObject '{go.name}' created on layer {highlightLayer}.");
            }
        }

        private void ApplyState(VentState newState, bool force = false)
        {
            if (!force && newState == currentState) return;

            if (glitchCoroutine != null) { StopCoroutine(glitchCoroutine); glitchCoroutine = null; }

            currentState = newState;
            UpdateNavMesh();

            switch (currentState)
            {
                case VentState.Open:
                    SetOverlayColor(ColorOpen);
                    break;
                case VentState.Closed:
                    SetOverlayColor(ColorClosed);
                    break;
                case VentState.Locked:
                    SetOverlayColor(ColorLocked);
                    glitchCoroutine = StartCoroutine(GlitchFlicker());
                    break;
            }
        }

        private void SetOverlayColor(Color c)
        {
            if (overlayMaterials == null)
            {
                Debug.LogWarning($"[VentController] '{name}': SetOverlayColor called but overlayMaterials is null.");
                return;
            }
            foreach (var mat in overlayMaterials)
            {
                if (mat == null) continue;
                mat.SetColor(ColorProp, c);
                mat.SetFloat(AlphaProp, c.a);
            }
        }

        private void UpdateNavMesh()
        {
            if (navMeshObstacle == null) return;
            navMeshObstacle.enabled = (currentState != VentState.Open);
            navMeshObstacle.carving = navMeshObstacle.enabled;
        }

        private IEnumerator GlitchFlicker()
        {
            while (true)
            {
                float noise = Mathf.PerlinNoise(Time.time * flickerFrequency, 0f);
                float alpha = Mathf.Clamp01(ColorLocked.a + (noise - 0.5f) * flickerIntensity);
                Color c = ColorLocked;
                c.a = alpha;
                SetOverlayColor(c);
                yield return null;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = currentState == VentState.Open   ? Color.green
                         : currentState == VentState.Closed ? Color.red
                                                            : Color.grey;
            Gizmos.DrawWireCube(transform.position, transform.lossyScale);
        }
#endif
    }
}