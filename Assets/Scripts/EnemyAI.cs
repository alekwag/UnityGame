using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private NavMeshAgent agent;

    private FlashlightController playerFlashlight;

    [Header("Detection")]
    [SerializeField] private float focusDetectionRange = 30f;
    [SerializeField] private float FocusFieldOfView = 90f;
    [SerializeField] private float peripheralDetectionRange = 5f;
    [SerializeField] private float peripheralFieldOfView = 200f;

    [Header("Behavior Speeds")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float chaseSpeed = 4.5f;
    [SerializeField] private float fleeSpeed = 6f;
    [SerializeField] private float health = 100f;
    [SerializeField] private float rotationSpeed = 8f;

    [Header("Hearing")]
    [SerializeField] private PlayerNoise playerNoise;
// ...existing code...

    private enum State
    {
        Patrol,
        Chase,
        Search,
        Flee
    }

    [SerializeField] private Transform[] patrolPoints;
    private int patrolIndex;
    private Vector3 lastKnownPosition;
    private bool locationToSearch;
    private float searchTimer = 0f;

    private float fleeTimer = 0f;

    private State currentState;



    private void Start()
    {
        playerFlashlight = player.GetComponent<FlashlightController>();
        playerNoise = player.GetComponent<PlayerNoise>();
        currentState = State.Patrol;
        
    }

    void Update()
    {
        if (player == null || playerFlashlight == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        bool canSeePlayerDirectly = CanSeePlayer();
        bool lightOnEnemy = LightOnEnemy();


        //  Detection logic
        bool detected = false;
        if (canSeePlayerDirectly || lightOnEnemy)
        {
            detected = true;
        }
         // hearing
       if (playerNoise != null)
        {
            float noiseRadius = playerNoise.currentNoiseRadius;
            if (noiseRadius > 0f && distance <= noiseRadius)
            {
                detected = true;
            }
        }

        //  State switching
        if (playerFlashlight.IsUVLightOn() && lightOnEnemy || currentState == State.Flee)
        {
            currentState = State.Flee;
            TakeDamage(50f * Time.deltaTime);
        }
        else if (detected)
        {
            currentState = State.Chase;
            lastKnownPosition = player.position;
            locationToSearch = true;
        }
        else if (locationToSearch)
        {
            currentState = State.Search;
        }
        else
        {
            currentState = State.Patrol;
        }
        
        HandleState();

        if (agent.hasPath)
        {
            RotateTowards(agent.destination);
        }
    }

    private void RotateTowards(Vector3 target)
    {
        Vector3 direction = target - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f) return;

        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            lookRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    void HandleState()
    {
        switch (currentState)
        {
            case State.Patrol: Patrol(); break;
            case State.Chase:
                agent.speed = chaseSpeed;
                agent.SetDestination(player.position);
                RotateTowards(player.position);
                break;
            case State.Search: Search(); break;
            case State.Flee: Flee(); break;
        }
    }

    private bool CanSeePlayer()
    {
        Vector3 directionToPlayer = player.position - transform.position;

        // Distance check
        if (directionToPlayer.magnitude > focusDetectionRange)
            return false;

        // Angle check (FOV)
        float angle = Vector3.Angle(transform.forward, directionToPlayer);

        if (angle > peripheralFieldOfView / 2f)
            return false;

        
        if (angle < FocusFieldOfView / 2f)
            return LineOfSight(directionToPlayer);

        else if (directionToPlayer.magnitude < peripheralDetectionRange)
            return LineOfSight(directionToPlayer);

        return false;
    }

    private bool LineOfSight(Vector3 directionToPlayer)
    {
         // Raycast (line of sight)
        float radius = 0.3f;
        int layerMask = ~LayerMask.GetMask("Enemy"); // ignore enemy layer

        if (Physics.SphereCast(transform.position + Vector3.up, radius, directionToPlayer.normalized, out RaycastHit hit, focusDetectionRange, layerMask))
        {
            if (hit.transform == player)
                return true;
        }

        return false;
    }

    private bool LightOnEnemy()
    {
        if (playerFlashlight == null) return false;

        if (!playerFlashlight.IsWhiteLightOn()
            && !playerFlashlight.IsUVLightOn())
            return false;

        Light light = playerFlashlight.GetComponentInChildren<Light>();
        if (light == null || !light.enabled) return false;

        Vector3 targetPoint = transform.position + Vector3.up * 1.0f;
        Vector3 dirToEnemy = targetPoint - light.transform.position; // 1m above feet

        // Distance check
        if (dirToEnemy.magnitude > light.range)
            return false;

        // Angle check (spotlight cone)
        float angle = Vector3.Angle(light.transform.forward, dirToEnemy);
        if (angle > light.spotAngle * 0.5f)
            return false;

        // Line of sight
        if (Physics.Raycast(light.transform.position, dirToEnemy.normalized, out RaycastHit hit, light.range))
        {
            if (hit.transform == transform)
                return true;
        }

        return false;
    }

    void Patrol()
    {
        agent.speed = patrolSpeed;

        if (!agent.hasPath || agent.remainingDistance < 1f)
        {
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
            agent.SetDestination(patrolPoints[patrolIndex].position);
        }
        if (agent.hasPath)
        {
            RotateTowards(agent.destination); 
        }
    }

    void Search()
    {
        agent.speed = patrolSpeed;
        agent.SetDestination(lastKnownPosition);
        RotateTowards(lastKnownPosition); //

        searchTimer += Time.deltaTime;

        if (searchTimer > 5f)
        {
            searchTimer = 0f;
            locationToSearch = false;
        }
    }

    void Flee()
    {
        agent.speed = fleeSpeed;

        Vector3 dir = (transform.position - player.position).normalized;
        Vector3 target = transform.position + dir * 10f;

        agent.SetDestination(target);
        RotateTowards(target);

        fleeTimer += Time.deltaTime;

        if (fleeTimer > 20f)
        {
            fleeTimer = 0f;
            currentState = State.Patrol;
        }
    }

    void TakeDamage(float amount)
    {
        health -= amount;

        if (health <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Destroy(gameObject);
    }

}