using UnityEngine;

public class FlashlightPickup : MonoBehaviour
{
    public enum PickupType
    {
        White,
        Red,
        UV,
        UVBattery
    }

    [Header("Pickup Type")]
    [SerializeField] private PickupType pickupType;

    [Header("Pickup Behavior")]
    [SerializeField] private bool turnOnWhenPickedUp = true;
    [SerializeField] private float batteryAmount = 2f;
    [SerializeField] private GameObject objectToDisableOnPickup;

    private void Reset()
    {
        objectToDisableOnPickup = gameObject;
    }

    private void OnTriggerEnter(Collider other)
    {
        FlashlightController flashlight = other.GetComponentInParent<FlashlightController>();
        if (flashlight == null)
        {
            return;
        }

        switch (pickupType)
        {
            case PickupType.White:
                flashlight.GrantFlashlight(turnOnWhenPickedUp);
                break;

            case PickupType.Red:
                flashlight.UnlockRed();
                break;

            case PickupType.UV:
                flashlight.UnlockUV();
                break;

            case PickupType.UVBattery:
                flashlight.AddUVBattery(batteryAmount);
                break;
        }

        if (objectToDisableOnPickup != null)
        {
            objectToDisableOnPickup.SetActive(false);
        }
    }
}