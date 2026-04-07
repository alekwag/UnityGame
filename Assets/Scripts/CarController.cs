using UnityEngine;
using UnityEngine.InputSystem;

public class CarController : MonoBehaviour
{
    [Header("Wheel Colliders")]
    public WheelCollider frontLeftWheel;
    public WheelCollider frontRightWheel;
    public WheelCollider rearLeftWheel;
    public WheelCollider rearRightWheel;

    [Header("Wheel Meshes")]
    public Transform frontLeftMesh;
    public Transform frontRightMesh;
    public Transform rearLeftMesh;
    public Transform rearRightMesh;

    [Header("Driving Settings")]
    public float motorTorque = 1500f;
    public float brakeTorque = 3000f;
    public float maxSteerAngle = 30f;
    public float maxSpeed = 30f;

    [Header("FP Look")]
    public float mouseSensitivity = 2f;
    public float verticalClamp = 60f;

    [Header("Enter/Exit")]
    public Transform exitPoint;
    public float enterRadius = 3f;

    private Rigidbody rb;
    private Camera fpsCamera;
    private float verticalRotation = 0f;
    private bool isDriving = false;
    private float moveInput;
    private float steerInput;
    private bool brakeInput;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0, -0.5f, 0);
        fpsCamera = Camera.main; // grabs the same FPS camera automatically
    }

    public void SetDriving(bool state)
    {
        isDriving = state;

        if (state)
        {
            // Lock cursor for mouse look
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Sync vertical rotation to current camera angle on entry
            verticalRotation = fpsCamera.transform.localEulerAngles.x;
            if (verticalRotation > 180f) verticalRotation -= 360f; // fix Unity's 0-360 range
        }
        else
        {
            ApplyBrakes(true);
        }
    }

    void Update()
    {
        if (!isDriving) return;

        // Driving inputs
        moveInput  = Keyboard.current.wKey.isPressed ? 1f :
                     Keyboard.current.sKey.isPressed ? -1f : 0f;
        steerInput = Keyboard.current.dKey.isPressed ? 1f :
                     Keyboard.current.aKey.isPressed ? -1f : 0f;
        brakeInput = Keyboard.current.spaceKey.isPressed;

        // Mouse look
        float mouseX = Mouse.current.delta.x.ReadValue() * mouseSensitivity * Time.deltaTime * 100f;
        float mouseY = Mouse.current.delta.y.ReadValue() * mouseSensitivity * Time.deltaTime * 100f;

        // Horizontal — rotate camera around world Y so it turns independently of car
        fpsCamera.transform.Rotate(Vector3.up, mouseX, Space.World);

        // Vertical — clamp so you cant flip upside down
        verticalRotation -= mouseY;
        verticalRotation  = Mathf.Clamp(verticalRotation, -verticalClamp, verticalClamp);

        Vector3 euler = fpsCamera.transform.localEulerAngles;
        fpsCamera.transform.localEulerAngles = new Vector3(verticalRotation, euler.y, 0f);
    }

    void FixedUpdate()
    {
        if (!isDriving)
        {
            ApplyBrakes(true);
            return;
        }

        HandleMotor();
        HandleSteering();
        ApplyBrakes(brakeInput);
        UpdateWheelMeshes();
    }

    void HandleMotor()
    {
        float speed = rb.linearVelocity.magnitude;
        float torque = speed < maxSpeed ? motorTorque * -moveInput : 0f;

        rearLeftWheel.motorTorque  = torque;
        rearRightWheel.motorTorque = torque;
    }

    void HandleSteering()
    {
        float angle = maxSteerAngle * steerInput;
        frontLeftWheel.steerAngle  = angle;
        frontRightWheel.steerAngle = angle;
    }

    void ApplyBrakes(bool braking)
    {
        float torque = braking ? brakeTorque : 0f;
        frontLeftWheel.brakeTorque  = torque;
        frontRightWheel.brakeTorque = torque;
        rearLeftWheel.brakeTorque   = torque;
        rearRightWheel.brakeTorque  = torque;
    }

    void UpdateWheelMeshes()
    {
        UpdateSingleWheel(frontLeftWheel,  frontLeftMesh);
        UpdateSingleWheel(frontRightWheel, frontRightMesh);
        UpdateSingleWheel(rearLeftWheel,   rearLeftMesh);
        UpdateSingleWheel(rearRightWheel,  rearRightMesh);
    }

    void UpdateSingleWheel(WheelCollider col, Transform mesh)
    {
        col.GetWorldPose(out Vector3 pos, out Quaternion rot);
        mesh.position = pos;
        mesh.rotation = rot;
    }
}