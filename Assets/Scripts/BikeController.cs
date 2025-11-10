using UnityEngine;

/// <summary>
/// Enhanced bike controller with better acceleration and speed
/// </summary>
public class BikeController : MonoBehaviour
{
    [Header("Wheel Components")]
    [SerializeField] private WheelJoint2D rearWheel;
    [SerializeField] private WheelJoint2D frontWheel;
    [SerializeField] private Rigidbody2D bikeBody;
    
    [Header("Physics Parameters")]
    [SerializeField] private float motorTorque = 2000f;
    [SerializeField] private float maxSpeed = 100f;
    [SerializeField] private float airRotationSpeed = 250f;
    
    [Header("Additional Boost")]
    [SerializeField] private bool useAdditionalForce = true;
    [SerializeField] private float forwardForce = 100f;  // Extra horizontal push
    [SerializeField] private float speedMultiplier = 1.5f;
    
    [Header("Collision Detection")]
    [SerializeField] private Transform crashDetector;
    [SerializeField] private float crashAngleThreshold = 120f;
    
    [Header("Status")]
    public bool isGrounded = false;
    public bool isCrashed = false;
    public int flipCount = 0;
    
    [Header("Debug Info")]
    [SerializeField] private bool showDebugInfo = true;
    public float currentSpeed = 0f;
    
    private bool isAccelerating = false;
    private float currentRotation = 0f;
    private float lastRotation = 0f;
    private JointMotor2D motor;
    private Rigidbody2D rearWheelRB;

    void Start()
    {
        // Initialize rear wheel motor
        motor = rearWheel.motor;
        motor.motorSpeed = 0f;
        motor.maxMotorTorque = motorTorque;
        rearWheel.motor = motor;
        
        // Get rear wheel rigidbody
        rearWheelRB = rearWheel.GetComponent<Rigidbody2D>();
        
        // Optimize center of mass for stability
        bikeBody.centerOfMass = new Vector2(0, -0.15f);
        
        Debug.Log($"Bike Controller Initialized - Motor Torque: {motorTorque}, Max Speed: {maxSpeed}");
    }

    void Update()
    {
        if (isCrashed) return;
        
        // Check if level is completed - stop accepting input
        if (GameManager.Instance != null && GameManager.Instance.currentState == GameState.Completed)
        {
            isAccelerating = false;
            return;
        }
        
        // Detect input (touch or spacebar/mouse)
        if (Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space))
        {
            isAccelerating = true;
        }
        else
        {
            isAccelerating = false;
        }
        
        // Calculate current speed for debug
        currentSpeed = bikeBody.linearVelocity.magnitude;
        
        // Debug info
        if (showDebugInfo && isAccelerating)
        {
            Debug.Log($"Speed: {currentSpeed:F2} | Motor Speed: {motor.motorSpeed} | Grounded: {isGrounded}");
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
            // Set motor speed with multiplier
            motor.motorSpeed = maxSpeed * speedMultiplier;
            motor.maxMotorTorque = motorTorque;
            
            // Apply additional horizontal force for faster acceleration
            if (useAdditionalForce)
            {
                bikeBody.AddForce(Vector2.right * forwardForce);
                
                // Also add force to rear wheel for better traction
                if (isGrounded && rearWheelRB != null)
                {
                    rearWheelRB.AddForce(Vector2.right * forwardForce * 0.5f);
                }
            }
            
            // Apply rotation when in air (backflip effect)
            if (!isGrounded)
            {
                bikeBody.AddTorque(-airRotationSpeed * Time.fixedDeltaTime);
            }
            else
            {
                // Ground stabilization - prevent unwanted rotation
                float angle = bikeBody.rotation % 360f;
                if (Mathf.Abs(angle) > 5f && Mathf.Abs(angle) < 90f)
                {
                    bikeBody.AddTorque(-angle * 2f);
                }
            }
        }
        else
        {
            motor.motorSpeed = 0f;
            motor.maxMotorTorque = 0f;
        }
        
        rearWheel.motor = motor;
        
        // Limit max velocity if needed (prevent unrealistic speeds)
        if (bikeBody.linearVelocity.magnitude > maxSpeed * 2f)
        {
            bikeBody.linearVelocity = bikeBody.linearVelocity.normalized * maxSpeed * 2f;
        }
    }

    void DetectFlips()
    {
        currentRotation = bikeBody.rotation;
        float rotationDelta = currentRotation - lastRotation;
        
        if (Mathf.Abs(rotationDelta) > 350f && !isGrounded)
        {
            flipCount++;
            GameManager.Instance?.OnFlipCompleted();
        }
        
        lastRotation = currentRotation;
    }

    void CheckCrash()
    {
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
    
    public void StopBike()
    {
        // Stop all input
        isAccelerating = false;
        
        // Gradually stop the bike (more realistic)
        bikeBody.linearVelocity *= 0.5f;
    }

    public void ResetBike()
    {
        isCrashed = false;
        flipCount = 0;
        bikeBody.linearVelocity = Vector2.zero;
        bikeBody.angularVelocity = 0f;
        bikeBody.rotation = 0f;
        
        if (rearWheelRB != null)
        {
            rearWheelRB.linearVelocity = Vector2.zero;
            rearWheelRB.angularVelocity = 0f;
        }
        
        transform.position = GameManager.Instance.startPosition;
        transform.rotation = Quaternion.identity;
    }
    
    // Visualize speed in Scene view
    void OnDrawGizmos()
    {
        if (Application.isPlaying && bikeBody != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)bikeBody.linearVelocity);
            
            // Draw speed indicator
            float speedRatio = currentSpeed / maxSpeed;
            Gizmos.color = Color.Lerp(Color.red, Color.green, speedRatio);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.2f + speedRatio);
        }
    }
}