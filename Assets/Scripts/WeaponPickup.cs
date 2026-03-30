using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    [Header("Pickup Behavior")]
    [SerializeField] private GameObject objectToDisableOnPickup;

    private void Reset()
    {
        objectToDisableOnPickup = gameObject;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Tell player to equip weapon
        PlayerWeapons playerWeapons = other.GetComponentInParent<PlayerWeapons>();
        if (playerWeapons != null)
        {
            playerWeapons.EquipWeapon();
        }

        // Hide or remove pickup object
        if (objectToDisableOnPickup != null)
        {
            objectToDisableOnPickup.SetActive(false);
        }
        
    }
}