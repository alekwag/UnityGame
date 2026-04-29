using UnityEngine;
using UnityEngine.InputSystem;

public class ElevatorPanel : MonoBehaviour
{
    [Header("References")]
    public ElevatorController elevator;

    [Header("Settings")]
    public bool isInsidePanel = false;  // tick this on the inside panel
    public int targetFloor;             // only used by outside panels
    public float interactRange = 3f;

    [Header("Optional UI")]
    public GameObject promptUI;

    private bool playerNearby = false;

    private void Update()
    {
        if (promptUI != null)
            promptUI.SetActive(playerNearby && !elevator.IsMoving());

        if (playerNearby && Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (isInsidePanel)
            {
                // Toggle to whichever floor we're not on
                int destination = elevator.CurrentFloor() == 0 ? 1 : 0;
                elevator.RequestFloor(destination);
            }
            else
            {
                elevator.RequestFloor(targetFloor);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            playerNearby = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            playerNearby = false;
    }
}