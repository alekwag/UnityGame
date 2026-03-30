using UnityEngine;
using UnityEngine.InputSystem;

public class FlashlightController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Light flashlightLight;

    [Header("Input")]
    [SerializeField] private Key toggleKey = Key.F;

    [Header("State")]
    [SerializeField] private bool startWithFlashlight;
    [SerializeField] private bool startEnabled;

    private bool hasFlashlight;
    private bool isOn;

    private void Awake()
    {
        hasFlashlight = startWithFlashlight;
        isOn = startEnabled && hasFlashlight;
        ApplyLightState();
    }

    private void Update()
    {
        if (!hasFlashlight || Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current[toggleKey].wasPressedThisFrame)
        {
            isOn = !isOn;
            ApplyLightState();
        }
    }

    public void GrantFlashlight(bool turnOnImmediately = true)
    {
        hasFlashlight = true;
        if (turnOnImmediately)
        {
            isOn = true;
        }

        ApplyLightState();
    }

    private void ApplyLightState()
    {
        if (flashlightLight == null)
        {
            return;
        }

        flashlightLight.enabled = hasFlashlight && isOn;
    }
}
