using UnityEngine;
using UnityEngine.InputSystem;

public class FlashlightController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Light flashlightLight;

    [Header("Input")]
    [SerializeField] private Key toggleKey = Key.F;

    [Header("Modes Owned")]
    [SerializeField] private bool hasWhite = false;
    [SerializeField] private bool hasRed = false;
    [SerializeField] private bool hasUV = false;

    private FlashlightMode currentMode = FlashlightMode.Off;

    private enum FlashlightMode
    {
        Off,
        White,
        Red,
        UV
    }

    [Header("UV Battery")]
    [SerializeField] private float maxUVBattery = 10f;
    [SerializeField] private float uvDrainRate = 1f;

    private float currentUVBattery;

    private void Awake()
    {
        currentUVBattery = maxUVBattery;
        ApplyLightState();
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current[toggleKey].wasPressedThisFrame)
        {
            CycleMode();
            ApplyLightState();
        }

        HandleUVBattery();
    }

    private void CycleMode()
    {
        // Cycle through ONLY owned modes
        do
        {
            currentMode = (FlashlightMode)(((int)currentMode + 1) % 4);
        }
        while (!ModeUnlocked(currentMode));
    }

    private bool ModeUnlocked(FlashlightMode mode)
    {
        return mode switch
        {
            FlashlightMode.Off => true,
            FlashlightMode.White => hasWhite,
            FlashlightMode.Red => hasRed,
            FlashlightMode.UV => hasUV && currentUVBattery > 0,
            _ => false
        };
    }

    private void HandleUVBattery()
    {
        if (currentMode == FlashlightMode.UV)
        {
            currentUVBattery -= uvDrainRate * Time.deltaTime;

            if (currentUVBattery <= 0)
            {
                currentUVBattery = 0;
                currentMode = FlashlightMode.Off;
                ApplyLightState();
            }
        }
    }

    private void ApplyLightState()
    {
        if (flashlightLight == null) return;

        switch (currentMode)
        {
            case FlashlightMode.Off:
                flashlightLight.enabled = false;
                break;

            case FlashlightMode.White:
                flashlightLight.enabled = true;
                flashlightLight.color = Color.white;
                flashlightLight.intensity = 2f;
                break;

            case FlashlightMode.Red:
                flashlightLight.enabled = true;
                flashlightLight.color = Color.red;
                flashlightLight.intensity = 1f;
                break;

            case FlashlightMode.UV:
                flashlightLight.enabled = true;
                flashlightLight.color = new Color(0.6f, 0.2f, 1f); // purple UV vibe
                flashlightLight.intensity = 3f;
                break;
        }
    }

   public void GrantFlashlight(bool turnOnImmediately = true)
    {
        hasWhite = true; // player now owns the default light

        if (turnOnImmediately)
        {
            currentMode = FlashlightMode.White;
        }
        else
        {
            currentMode = FlashlightMode.Off;
        }

        ApplyLightState();
    }

    public void UnlockRed()
    {
        hasRed = true;
    }

    public void UnlockUV()
    {
        hasUV = true;
        currentUVBattery = maxUVBattery;
    }

    public void AddUVBattery(float amount)
    {
        currentUVBattery = Mathf.Clamp(currentUVBattery + amount, 0, maxUVBattery);
    }

    // For enemy logic later
    public bool IsWhiteLightOn() => currentMode == FlashlightMode.White;
    public bool IsRedLightOn() => currentMode == FlashlightMode.Red;
    public bool IsUVLightOn() => currentMode == FlashlightMode.UV;
}