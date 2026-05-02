// ScannerGun.cs  (REVISED — New Input System)
// Uses UnityEngine.InputSystem instead of the legacy Input class.
//
// SETUP:
//   Option A — PlayerInput component (recommended):
//     1. Add a PlayerInput component to this GameObject
//     2. Assign your InputActionAsset
//     3. In the "Events" section, wire the fire action to ScannerGun.OnFireInput
//
//   Option B — Direct InputAction reference (used here by default):
//     1. Assign the fireAction field in the Inspector
//        (drag an action from your InputActionAsset, or create a new one)
//     2. The script enables/disables it automatically
//
//   Option C — Call Fire() directly from any other script (no input wiring needed)

using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

namespace ScannerSystem
{
    public class ScannerGun : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────────────
        [Header("Components")]
        [SerializeField] private ScannerCamera scannerCamera;
        [SerializeField] private Renderer screenQuadRenderer;

        [Header("Input (New Input System)")]
        [Tooltip("Drag a fire InputAction here, OR leave empty and use OnFireInput() / Fire() directly.")]
        [SerializeField] private InputActionReference fireActionReference;

        [SerializeField] private float fireCooldown = 0.3f;

        [Header("HUD Feedback")]
        [SerializeField] private TextMeshProUGUI hudLabel;
        [SerializeField] private float labelDisplayTime = 2f;

        [Header("Screen")]
        [SerializeField] private bool screenAlwaysOn = true;

        // ── Runtime ──────────────────────────────────────────────────────────
        private float lastFireTime = -999f;
        private Coroutine labelCoroutine;

        // ── Unity lifecycle ──────────────────────────────────────────────────
        private void OnEnable()
        {
            if (fireActionReference != null)
            {
                fireActionReference.action.Enable();
                fireActionReference.action.performed += OnFireInput;
            }
        }

        private void OnDisable()
        {
            if (fireActionReference != null)
                fireActionReference.action.performed -= OnFireInput;
        }

        private void Start()
        {
            SetScreenActive(screenAlwaysOn);
            HideLabel();

            if (fireActionReference == null)
                Debug.LogWarning("[ScannerGun] No fireActionReference assigned. " +
                                 "Call Fire() directly or wire up PlayerInput.Events → OnFireInput.", this);
        }

        // ── Input callbacks ──────────────────────────────────────────────────

        /// <summary>
        /// Wire this to PlayerInput → Events → [your fire action] in the Inspector,
        /// OR it's called automatically if fireActionReference is assigned.
        /// </summary>
        public void OnFireInput(InputAction.CallbackContext ctx)
        {
            if (ctx.performed) Fire();
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>Call this from anywhere to fire the scanner.</summary>
        public void Fire()
        {
            if (Time.time - lastFireTime < fireCooldown) return;
            lastFireTime = Time.time;

            if (scannerCamera == null)
            {
                Debug.LogError("[ScannerGun] scannerCamera is not assigned!", this);
                return;
            }

            bool hit = scannerCamera.Shoot(out IScannableTarget target, out string label);
            ShowLabel(hit ? label : "NO TARGET");
        }

        public void SetGunRaised(bool raised)
        {
            if (!screenAlwaysOn) SetScreenActive(raised);
        }

        // ── Internals ────────────────────────────────────────────────────────
        private void SetScreenActive(bool active)
        {
            if (screenQuadRenderer != null) screenQuadRenderer.enabled = active;
            if (scannerCamera      != null) scannerCamera.ScannerCam.enabled = active;
        }

        private void ShowLabel(string text)
        {
            if (hudLabel == null) return;
            hudLabel.text = text;
            hudLabel.gameObject.SetActive(true);
            if (labelCoroutine != null) StopCoroutine(labelCoroutine);
            labelCoroutine = StartCoroutine(HideLabelAfterDelay());
        }

        private void HideLabel()
        {
            if (hudLabel != null) hudLabel.gameObject.SetActive(false);
        }

        private IEnumerator HideLabelAfterDelay()
        {
            yield return new WaitForSeconds(labelDisplayTime);
            HideLabel();
        }
    }
}