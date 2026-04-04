using UnityEngine;

public class PlayerNoise : MonoBehaviour
{
    public float crouchNoise = 3f;
    public float walkNoise = 6f;
    public float sprintNoise = 18f;

    

    public float currentNoiseRadius { get; private set; }

    private FPSCharacterController controller;

    void Start()
    {
        controller = GetComponent<FPSCharacterController>();
    }

    void Update()
    {

        if (controller.CurrentSpeed < 0.1f)
        {
            currentNoiseRadius = 0f;
        }
        else if (controller.IsCrouching)
        {
            currentNoiseRadius = crouchNoise;
        }
        else if (controller.IsSprinting)
        {
            currentNoiseRadius = sprintNoise;
        }
        else
        {
            currentNoiseRadius = walkNoise;
        }
    }
}