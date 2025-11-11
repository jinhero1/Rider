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
    
    [Header("Status")]
    public bool isGrounded = false;
    public bool isCrashed = false;
    public int flipCount = 0;
    
    [Header("Debug Info")]
    [SerializeField] private bool showDebugInfo = true;
    public float currentSpeed = 0f;

    [Header("Flip Detection")]
    [SerializeField] private bool detectBothDirections = true;
    [SerializeField] private float flipThreshold = 320f;
    [SerializeField] private bool showFlipDebug = true;

    [Header("Distance Tracking")]
    [SerializeField] private bool trackDistance = true;
    [SerializeField] private float distanceMultiplier = 1f;

    private float totalDistance = 0f;
    private Vector3 lastPosition;
    private bool distanceInitialized = false;

    private float accumulatedRotation = 0f;
    private float lastFrameRotation = 0f;
    private bool wasGrounded = true;

    private bool isAccelerating = false;
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

        lastPosition = transform.position;
        distanceInitialized = true;
        totalDistance = 0f;

        lastFrameRotation = bikeBody.rotation;
        
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

        if (trackDistance && distanceInitialized && !isCrashed)
        {
            float deltaDistance = Vector3.Distance(transform.position, lastPosition);

            if (transform.position.x > lastPosition.x)
            {
                totalDistance += deltaDistance;
            }

            lastPosition = transform.position;
        }
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
        float currentRot = bikeBody.rotation;

        float delta = Mathf.DeltaAngle(lastFrameRotation, currentRot);

        if (!isGrounded)
        {
            if (detectBothDirections)
            {
                accumulatedRotation += Mathf.Abs(delta);

                if (showFlipDebug && accumulatedRotation > 90f)
                {
                    Debug.Log($"[Flip Progress] {accumulatedRotation:F0}° / {flipThreshold}°");
                }
            }

            if (accumulatedRotation >= flipThreshold)
            {
                flipCount++;
                GameManager.Instance?.OnFlipCompleted();

                Debug.Log($"<color=yellow>★★★ FLIP COMPLETED! ★★★ Total Flips: {flipCount}</color>");

                accumulatedRotation -= 360f;

                if (accumulatedRotation < 0)
                {
                    accumulatedRotation = 0;
                }
            }
        }
        else
        {
            if (!wasGrounded)
            {
                if (accumulatedRotation > 100f && showFlipDebug)
                {
                    Debug.Log($"[Flip Incomplete] Landed with {accumulatedRotation:F0}° rotation");
                }

                accumulatedRotation = 0f;
            }
        }
        
        lastFrameRotation = currentRot;
        wasGrounded = isGrounded;
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

    public int GetScore()
    {
        return Mathf.FloorToInt(totalDistance * distanceMultiplier);
    }

    public void AddFlipBonus(int bonusPoints)
    {
        totalDistance += bonusPoints;
        Debug.Log($"[Bike] Flip bonus added: +{bonusPoints}, Total distance: {totalDistance:F1}");
    }

    public void ResetBike()
    {
        gameObject.SetActive(true);
        isCrashed = false;
        flipCount = 0;
        bikeBody.linearVelocity = Vector2.zero;
        bikeBody.angularVelocity = 0f;
        bikeBody.rotation = 0f;
        totalDistance = 0f;
        accumulatedRotation = 0f;
        
        lastPosition = transform.position;

        if (rearWheelRB != null)
        {
            rearWheelRB.linearVelocity = Vector2.zero;
            rearWheelRB.angularVelocity = 0f;
        }

        transform.position = GameManager.Instance.startPosition;
        transform.rotation = Quaternion.identity;
    }
    
    public void HideBike()
    {
        gameObject.SetActive(false);
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