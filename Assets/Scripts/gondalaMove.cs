using UnityEngine;

public class GondolaMover : MonoBehaviour
{
    public Transform[] points;
    public float maxSpeed = 4f;
    public float acceleration = 2f;
    public float slowDownDistance = 5f;
    public float waitTime = 2f;

    private int currentIndex = 0;
    private int direction = 1;

    private float currentSpeed = 0.1f; // small initial speed to avoid stutter

    private bool waiting = false;
    private float waitCounter;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            other.GetComponent<FPSCharacterController>().SetPlatform(transform);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            other.GetComponent<FPSCharacterController>().SetPlatform(null);
    }

    void Update()
    {
        if (points.Length == 0) return;

        // Waiting at a station
        if (waiting)
        {
            waitCounter -= Time.deltaTime;
            if (waitCounter <= 0f)
            {
                waiting = false;

                // Reverse direction at the ends
                if (currentIndex >= points.Length - 1)
                    direction = -1;
                else if (currentIndex <= 0)
                    direction = 1;

                currentIndex = Mathf.Clamp(currentIndex + direction, 0, points.Length - 1);
            }
            return;
        }

        Transform target = points[currentIndex];
        float distance = Vector3.Distance(transform.position, target.position);

        // Acceleration / Deceleration
        if (distance < slowDownDistance)
        {
            // Slow down when approaching station
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, acceleration * Time.deltaTime);
        }
        else
        {
            // Speed up when far away
            currentSpeed = Mathf.MoveTowards(currentSpeed, maxSpeed, acceleration * Time.deltaTime);
        }

        // Move
        transform.position = Vector3.MoveTowards(
            transform.position,
            target.position,
            currentSpeed * Time.deltaTime
        );

        // Arrived at station (safe check)
        if (!waiting && distance < 0.1f)
        {
            transform.position = target.position; // snap cleanly
            currentSpeed = 0f;
            waiting = true;
            waitCounter = waitTime;
        }
    }

    // --- Gizmos ---
    private void OnDrawGizmos()
    {
        if (points == null || points.Length == 0) return;

        Gizmos.color = Color.cyan;

        for (int i = 0; i < points.Length; i++)
        {
            if (points[i] != null)
            {
                // Draw a sphere at each point
                Gizmos.DrawSphere(points[i].position, 5f);

                // Draw line to the next point (looping if needed)
                if (i < points.Length - 1)
                    Gizmos.DrawLine(points[i].position, points[i + 1].position);
            }
        }
    }

}