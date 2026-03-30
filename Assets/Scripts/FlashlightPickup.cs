using UnityEngine;

public class FlashlightPickup : MonoBehaviour
{
    [Header("Pickup Behavior")]
    [SerializeField] private bool turnOnWhenPickedUp = true;
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

        flashlight.GrantFlashlight(turnOnWhenPickedUp);

        if (objectToDisableOnPickup != null)
        {
            objectToDisableOnPickup.SetActive(false);
        }
    }
}
