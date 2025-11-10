using UnityEngine;

/// <summary>
/// Controls the bike's physics behavior, input, and flip mechanics
/// </summary>
public class BikeController : MonoBehaviour
{
    [Header("Wheel Components")]
    [SerializeField] private WheelJoint2D rearWheel;
    [SerializeField] private WheelJoint2D frontWheel;
    [SerializeField] private Rigidbody2D bikeBody;
    
    [Header("Physics Parameters")]
    [SerializeField] private float motorTorque = 500f;
    [SerializeField] private float maxSpeed = 20f;
    [SerializeField] private float airRotationSpeed = 200f;
    
    [Header("Collision Detection")]
    [SerializeField] private Transform crashDetector; // Head/top collision detector
    [SerializeField] private float crashAngleThreshold = 120f; // Crash angle threshold
    
    [Header("Status")]
    public bool isGrounded = false;
    public bool isCrashed = false;
    public int flipCount = 0;
    
    private bool isAccelerating = false;
    private float currentRotation = 0f;
    private float lastRotation = 0f;
    private JointMotor2D motor;

    void Start()
    {
        // Initialize rear wheel motor
        motor = rearWheel.motor;
        motor.motorSpeed = 0f;
        motor.maxMotorTorque = motorTorque;
        rearWheel.motor = motor;
    }

    void Update()
    {
        if (isCrashed) return;
        
        // Detect input (touch or spacebar/mouse)
        if (Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space))
        {
            isAccelerating = true;
        }
        else
        {
            isAccelerating = false;
        }
        
        // Detect flips
        DetectFlips();
        
        // Check crash
        CheckCrash();
    }

    void FixedUpdate()
    {
        if (isCrashed) return;
        
        // Apply motor torque
        if (isAccelerating)
        {
            motor.motorSpeed = -maxSpeed; // Negative value means forward
            motor.maxMotorTorque = motorTorque;
        }
        else
        {
            motor.motorSpeed = 0f;
            motor.maxMotorTorque = 0f;
        }
        rearWheel.motor = motor;
        
        // Apply rotation when in air (backflip effect)
        if (!isGrounded && isAccelerating)
        {
            bikeBody.AddTorque(-airRotationSpeed * Time.fixedDeltaTime);
        }
    }

    void DetectFlips()
    {
        // Track rotation to detect complete 360-degree flips
        currentRotation = bikeBody.rotation;
        float rotationDelta = currentRotation - lastRotation;
        
        // Check if 360-degree rotation is completed
        if (Mathf.Abs(rotationDelta) > 350f && !isGrounded)
        {
            flipCount++;
            GameManager.Instance?.OnFlipCompleted();
        }
        
        lastRotation = currentRotation;
    }

    void CheckCrash()
    {
        // Check if angle exceeds threshold
        float angle = Mathf.Abs(bikeBody.rotation % 360f);
        if (angle > 180f) angle = 360f - angle;
        
        if (isGrounded && angle > crashAngleThreshold)
        {
            Crash();
        }
    }

    public void Crash()
    {
        if (isCrashed) return;
        
        isCrashed = true;
        motor.maxMotorTorque = 0f;
        rearWheel.motor = motor;
        
        GameManager.Instance?.OnCrash();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }

    public void ResetBike()
    {
        isCrashed = false;
        flipCount = 0;
        bikeBody.linearVelocity = Vector2.zero;
        bikeBody.angularVelocity = 0f;
        bikeBody.rotation = 0f;
        transform.position = GameManager.Instance.startPosition;
    }
}