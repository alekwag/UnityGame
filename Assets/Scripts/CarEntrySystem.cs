using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class CarEntrySystem : MonoBehaviour
{
    [Header("References")]
    public CarController car;
    public GameObject playerObject;
    public CharacterController playerController;
    public FPSCharacterController fpsController;
    public Transform driverSeat;        // Empty GameObject inside car where camera sits

    [Header("UI (optional)")]
    public GameObject enterPromptUI;

    private bool playerNearby = false;
    private bool inCar = false;
    private Transform playerCameraTransform;

    void Start()
    {
        // Grab the FPS camera transform automatically
        playerCameraTransform = Camera.main.transform;
    }

    void Update()
    {
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (!inCar && playerNearby)
                StartCoroutine(EnterCarRoutine());
            else if (inCar)
                StartCoroutine(ExitCarRoutine());
        }

        if (enterPromptUI != null)
            enterPromptUI.SetActive(playerNearby && !inCar);
    }

    IEnumerator EnterCarRoutine()
    {
        inCar = true;

        // Disable player movement
        fpsController.enabled = false;
        playerController.enabled = false;

        yield return null;

        // Hide player body, attach camera to driver seat
        playerObject.SetActive(false);
        playerCameraTransform.SetParent(driverSeat);
        playerCameraTransform.localPosition = Vector3.zero;
        playerCameraTransform.localRotation = Quaternion.identity;

        car.SetDriving(true);
    }

    IEnumerator ExitCarRoutine()
    {
        inCar = false;
        car.SetDriving(false);

        yield return null;

        // Detach camera, re-enable player
        playerCameraTransform.SetParent(playerObject.transform);
        playerCameraTransform.localPosition = new Vector3(0, 0.7f, 0); // match your eye height
        playerCameraTransform.localRotation = Quaternion.identity;

        playerObject.SetActive(true);
        playerObject.transform.position = car.exitPoint.position;
        playerObject.transform.rotation = Quaternion.identity;

        yield return null;

        playerController.enabled = true;
        fpsController.enabled = true;
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