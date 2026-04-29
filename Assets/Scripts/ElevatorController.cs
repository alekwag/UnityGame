using System.Collections;
using UnityEngine;

public class ElevatorController : MonoBehaviour
{
    [Header("Floors")]
    public Transform floorA;
    public Transform floorB;
    public float moveSpeed = 3f;
    public float accelerationDistance = 2f;

   [Header("Elevator Doors (inner)")]
public Transform innerLeftDoor;
public Transform innerRightDoor;
public float innerDoorOpenDistance = 0.6f;  // ← renamed
public float doorSpeed = 2f;
public float doorWaitTime = 2f;

[Header("Outer Doors")]
public Transform outerLeftDoorA;
public Transform outerRightDoorA;
public Transform outerLeftDoorB;
public Transform outerRightDoorB;
public float outerDoorOpenDistance = 1.2f;  // ← new field

    private int currentFloor = 0;
    private bool isMoving = false;
    private bool doorsOpen = false;

    private Vector3 innerLeftClosedPos;
    private Vector3 innerRightClosedPos;
    private Vector3 outerLeftAClosedPos;
    private Vector3 outerRightAClosedPos;
    private Vector3 outerLeftBClosedPos;
    private Vector3 outerRightBClosedPos;

    private void Start()
    {
        // Store all closed positions
        if (innerLeftDoor  != null) innerLeftClosedPos   = innerLeftDoor.localPosition;
        if (innerRightDoor != null) innerRightClosedPos  = innerRightDoor.localPosition;

        if (outerLeftDoorA  != null) outerLeftAClosedPos  = outerLeftDoorA.localPosition;
        if (outerRightDoorA != null) outerRightAClosedPos = outerRightDoorA.localPosition;
        if (outerLeftDoorB  != null) outerLeftBClosedPos  = outerLeftDoorB.localPosition;
        if (outerRightDoorB != null) outerRightBClosedPos = outerRightDoorB.localPosition;

        // Snap to floor A, everything starts closed
        transform.position = floorA.position;
        SetAllDoorsClosed();

        // Open doors at starting floor after a short delay
        StartCoroutine(OpenThenWait());
    }

    public void RequestFloor(int floor)
    {
        if (isMoving) return;
        if (floor == currentFloor)
        {
            if (!doorsOpen) StartCoroutine(OpenThenWait());
            return;
        }

        StartCoroutine(TravelRoutine(floor));
    }

    private IEnumerator TravelRoutine(int floor)
    {
        isMoving = true;

        yield return StartCoroutine(CloseDoors(currentFloor));
        yield return StartCoroutine(MoveToFloor(floor));

        currentFloor = floor;
        isMoving = false;

        yield return StartCoroutine(OpenThenWait());
    }

    private IEnumerator OpenThenWait()
    {
        yield return StartCoroutine(OpenDoors(currentFloor));
        yield return new WaitForSeconds(doorWaitTime);
        yield return StartCoroutine(CloseDoors(currentFloor));
    }

    private IEnumerator MoveToFloor(int floor)
    {
        Vector3 origin      = transform.position;
        Vector3 destination = floor == 0 ? floorA.position : floorB.position;
        float   totalDist   = Vector3.Distance(origin, destination);

        while (Vector3.Distance(transform.position, destination) > 0.01f)
        {
            float remaining = Vector3.Distance(transform.position, destination);
            float travelled = totalDist - remaining;

            float ramp = Mathf.Min(
                Mathf.Clamp01(travelled  / accelerationDistance),
                Mathf.Clamp01(remaining  / accelerationDistance)
            );
            ramp = Mathf.Max(ramp, 0.15f);

            transform.position = Vector3.MoveTowards(
                transform.position, destination,
                moveSpeed * ramp * Time.deltaTime);

            yield return null;
        }

        transform.position = destination;
    }

    private IEnumerator OpenDoors(int floor)
    {
        doorsOpen = true;
        yield return StartCoroutine(AnimateAllDoors(floor, true));
    }

    private IEnumerator CloseDoors(int floor)
    {
        yield return StartCoroutine(AnimateAllDoors(floor, false));
        doorsOpen = false;
    }

    // Animates both inner doors and the outer doors for the given floor together
    private IEnumerator AnimateAllDoors(int floor, bool open)
    {
        if (innerLeftDoor == null || innerRightDoor == null) yield break;

        // Inner door targets
        Vector3 innerLeftTarget = open
        ? innerLeftClosedPos  + Vector3.left  * innerDoorOpenDistance
        : innerLeftClosedPos;
        Vector3 innerRightTarget = open
        ? innerRightClosedPos + Vector3.right * innerDoorOpenDistance
        : innerRightClosedPos;

        // Outer door targets for this floor
        Transform outerLeft  = floor == 0 ? outerLeftDoorA  : outerLeftDoorB;
        Transform outerRight = floor == 0 ? outerRightDoorA : outerRightDoorB;
        Vector3   outerLeftClosed  = floor == 0 ? outerLeftAClosedPos  : outerLeftBClosedPos;
        Vector3   outerRightClosed = floor == 0 ? outerRightAClosedPos : outerRightBClosedPos;

        Vector3 outerLeftTarget  = open && outerLeft  != null
            ? outerLeftClosed  + Vector3.left  * outerDoorOpenDistance
            : outerLeftClosed;
        Vector3 outerRightTarget = open && outerRight != null
            ? outerRightClosed + Vector3.right * outerDoorOpenDistance
            : outerRightClosed;

        bool done = false;
        while (!done)
        {
            // Move inner doors
            innerLeftDoor.localPosition  = Vector3.MoveTowards(
                innerLeftDoor.localPosition,  innerLeftTarget,  doorSpeed * Time.deltaTime);
            innerRightDoor.localPosition = Vector3.MoveTowards(
                innerRightDoor.localPosition, innerRightTarget, doorSpeed * Time.deltaTime);

            // Move outer doors
            if (outerLeft != null)
                outerLeft.localPosition  = Vector3.MoveTowards(
                    outerLeft.localPosition,  outerLeftTarget,  doorSpeed * Time.deltaTime);
            if (outerRight != null)
                outerRight.localPosition = Vector3.MoveTowards(
                    outerRight.localPosition, outerRightTarget, doorSpeed * Time.deltaTime);

            done = Vector3.Distance(innerLeftDoor.localPosition, innerLeftTarget) < 0.01f;
            yield return null;
        }

        // Snap everything cleanly
        innerLeftDoor.localPosition  = innerLeftTarget;
        innerRightDoor.localPosition = innerRightTarget;
        if (outerLeft  != null) outerLeft.localPosition  = outerLeftTarget;
        if (outerRight != null) outerRight.localPosition = outerRightTarget;
    }

    // Instantly close everything on Start with no animation
    private void SetAllDoorsClosed()
    {
        if (innerLeftDoor  != null) innerLeftDoor.localPosition  = innerLeftClosedPos;
        if (innerRightDoor != null) innerRightDoor.localPosition = innerRightClosedPos;

        if (outerLeftDoorA  != null) outerLeftDoorA.localPosition  = outerLeftAClosedPos;
        if (outerRightDoorA != null) outerRightDoorA.localPosition = outerRightAClosedPos;
        if (outerLeftDoorB  != null) outerLeftDoorB.localPosition  = outerLeftBClosedPos;
        if (outerRightDoorB != null) outerRightDoorB.localPosition = outerRightBClosedPos;
    }

    public bool IsMoving()    => isMoving;
    public int CurrentFloor() => currentFloor;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            FPSCharacterController fps = other.GetComponent<FPSCharacterController>();
            if (fps != null) fps.SetPlatform(transform);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            FPSCharacterController fps = other.GetComponent<FPSCharacterController>();
            if (fps != null) fps.SetPlatform(null);
        }
    }
}