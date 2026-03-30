using UnityEngine;

public class PlayerWeapons : MonoBehaviour
{
    public GameObject weaponPivot; // assign WeaponPivot in Inspector

    public void EquipWeapon()
    {
        if (weaponPivot != null)
        {
            weaponPivot.SetActive(true);
            Debug.Log("Weapon equipped!");
        }
    }

    public void UnequipWeapon()
    {
        if (weaponPivot != null)
        {
            weaponPivot.SetActive(false);
            Debug.Log("Weapon unequipped!");
        }
    }
}