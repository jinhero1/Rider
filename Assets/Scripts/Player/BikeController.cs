using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BikeController : MonoBehaviour, IPlayerController
{
    // Dependencies (injected)
    private GameEventSystem eventSystem;
    private IInputService inputService;
    private IScoreService scoreService;
    private IGameStateService gameStateService;

    [Header("Wheel Components")]
    [SerializeField] private WheelJoint2D rearWheel;
    [SerializeField] private WheelJoint2D frontWheel;
    [SerializeField] private Rigidbody2D bikeBody;
    
    [Header("Physics Parameters")]
    [SerializeField] private float motorTorque = GameConstants.Physics.DEFAULT_MOTOR_TORQUE;
    [SerializeField] private float maxSpeed = GameConstants.Physics.DEFAULT_MAX_SPEED;
    [SerializeField] private float airRotationSpeed = GameConstants.Physics.DEFAULT_AIR_ROTATION_SPEED;
    [SerializeField] private float forwardForce = GameConstants.Physics.DEFAULT_FORWARD_FORCE;
    [SerializeField] private float speedMultiplier = GameConstants.Physics.DEFAULT_SPEED_MULTIPLIER;
    
    [Header("Flip Detection")]
    [SerializeField] private bool detectBothDirections = true;
    [SerializeField] private float flipThreshold = GameConstants.FlipDetection.DEFAULT_FLIP_THRESHOLD;
    [SerializeField] private bool showFlipDebug = true;

    [Header("Distance Tracking")]
    [SerializeField] private float distanceMultiplier = GameConstants.Score.DEFAULT_DISTANCE_MULTIPLIER;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    // State
    private BikePhysicsState physicsState;
    private FlipDetectionState flipState;
    private DistanceTracker distanceTracker;

    // Properties
    public bool IsCrashed => physicsState.IsCrashed;
    public int FlipCount => flipState.FlipCount;
    public float CurrentSpeed => physicsState.CurrentSpeed;
    public Vector3 Position => transform.position;
    public bool IsGrounded => physicsState.IsGrounded;

    #region Initialization

    private void Awake()
    {
        InitializeDependencies();
        InitializeComponents();
        InitializeState();
    }

    private void InitializeDependencies()
    {
        // Get services from ServiceLocator
        eventSystem = ServiceLocator.Instance.Get<GameEventSystem>();
        inputService = ServiceLocator.Instance.Get<IInputService>();
        scoreService = ServiceLocator.Instance.Get<IScoreService>();
        gameStateService = ServiceLocator.Instance.Get<IGameStateService>();

        if (eventSystem == null)
        {
            Debug.LogError("[BikeController] GameEventSystem not found in ServiceLocator!");
        }
    }

    private void InitializeComponents()
    {
        if (bikeBody == null)
        {
            bikeBody = GetComponent<Rigidbody2D>();
        }

        // Optimize center of mass for stability
        bikeBody.centerOfMass = new Vector2(0, GameConstants.Physics.CENTER_OF_MASS_Y_OFFSET);

        // Initialize wheel motor
        JointMotor2D motor = rearWheel.motor;
        motor.motorSpeed = 0f;
        motor.maxMotorTorque = motorTorque;
        rearWheel.motor = motor;
    }

    private void InitializeState()
    {
        physicsState = new BikePhysicsState
        {
            IsCrashed = false,
            IsGrounded = false,
            CurrentSpeed = 0f,
            RearWheelRB = rearWheel.GetComponent<Rigidbody2D>()
        };

        flipState = new FlipDetectionState
        {
            FlipCount = 0,
            AccumulatedRotation = 0f,
            LastFrameRotation = bikeBody.rotation,
            WasGrounded = true
        };

        distanceTracker = new DistanceTracker(transform.position, distanceMultiplier);

        Debug.Log($"[BikeController] Initialized - Torque: {motorTorque}, MaxSpeed: {maxSpeed}");
    }

    #endregion

    #region Update Loop

    private void Update()
    {
        if (physicsState.IsCrashed) return;
        
        // Check if level is completed - stop accepting input
        if (gameStateService != null && gameStateService.IsState(GameState.Completed))
        {
            physicsState.IsAccelerating = false;
            return;
        }
        
        // Get input from service
        if (inputService != null)
        {
            physicsState.IsAccelerating = inputService.IsAccelerating;
        }
        
        // Update current speed
        physicsState.CurrentSpeed = bikeBody.linearVelocity.magnitude;
        
        // Debug info
        if (showDebugInfo && physicsState.IsAccelerating)
        {
            Debug.Log($"Speed: {physicsState.CurrentSpeed:F2} | Grounded: {physicsState.IsGrounded}");
        }

        // Detect flips
        DetectFlips();

        // Track distance
        distanceTracker.Update(transform.position, physicsState.IsCrashed);
    }

    private void FixedUpdate()
    {
        if (physicsState.IsCrashed) return;
        
        ApplyPhysics();
        LimitVelocity();
    }

    #endregion

    #region Physics

    private void ApplyPhysics()
    {
        JointMotor2D motor = rearWheel.motor;

        if (physicsState.IsAccelerating)
        {
            // Set motor speed with multiplier
            motor.motorSpeed = maxSpeed * speedMultiplier;
            motor.maxMotorTorque = motorTorque;
            
            // Apply additional horizontal force
            bikeBody.AddForce(Vector2.right * forwardForce);
            
            // Add force to rear wheel for better traction
            if (physicsState.IsGrounded && physicsState.RearWheelRB != null)
            {
                physicsState.RearWheelRB.AddForce(Vector2.right * forwardForce * GameConstants.Boost.CONTINUOUS_FORCE_MULTIPLIER);
            }
            
            // Apply rotation when in air (backflip effect)
            if (!physicsState.IsGrounded)
            {
                bikeBody.AddTorque(-airRotationSpeed * Time.fixedDeltaTime);
            }
            else
            {
                ApplyGroundStabilization();
            }
        }
        else
        {
            motor.motorSpeed = 0f;
            motor.maxMotorTorque = 0f;
        }
        
        rearWheel.motor = motor;
    }

    private void ApplyGroundStabilization()
    {
        float angle = bikeBody.rotation % GameConstants.FlipDetection.FULL_ROTATION_DEGREES;
        
        if (Mathf.Abs(angle) > GameConstants.Physics.GROUND_STABILIZATION_ANGLE_THRESHOLD && 
            Mathf.Abs(angle) < GameConstants.Physics.GROUND_STABILIZATION_ANGLE_MAX)
        {
            bikeBody.AddTorque(-angle * GameConstants.Physics.GROUND_STABILIZATION_TORQUE_MULTIPLIER);
        }
    }

    private void LimitVelocity()
    {
        float maxVelocity = maxSpeed * GameConstants.Physics.SPEED_VELOCITY_LIMIT_MULTIPLIER;
        
        if (bikeBody.linearVelocity.magnitude > maxVelocity)
        {
            bikeBody.linearVelocity = bikeBody.linearVelocity.normalized * maxVelocity;
        }
    }

    #endregion

    #region Flip Detection

    private void DetectFlips()
    {
        float currentRot = bikeBody.rotation;
        float delta = Mathf.DeltaAngle(flipState.LastFrameRotation, currentRot);

        if (!physicsState.IsGrounded)
        {
            if (detectBothDirections)
            {
                flipState.AccumulatedRotation += Mathf.Abs(delta);

                if (showFlipDebug && flipState.AccumulatedRotation > GameConstants.FlipDetection.FLIP_PROGRESS_LOG_THRESHOLD)
                {
                    Debug.Log($"[Flip Progress] {flipState.AccumulatedRotation:F0}° / {flipThreshold}°");
                }
            }

            if (flipState.AccumulatedRotation >= flipThreshold)
            {
                OnFlipCompleted();
            }
        }
        else
        {
            if (!flipState.WasGrounded)
            {
                if (flipState.AccumulatedRotation > GameConstants.FlipDetection.INCOMPLETE_FLIP_THRESHOLD && showFlipDebug)
                {
                    Debug.Log($"[Flip Incomplete] Landed with {flipState.AccumulatedRotation:F0}° rotation");
                }

                flipState.AccumulatedRotation = 0f;
            }
        }
        
        flipState.LastFrameRotation = currentRot;
        flipState.WasGrounded = physicsState.IsGrounded;
    }

    private void OnFlipCompleted()
    {
        flipState.FlipCount++;
        flipState.AccumulatedRotation -= GameConstants.FlipDetection.FULL_ROTATION_DEGREES;

        if (flipState.AccumulatedRotation < 0)
        {
            flipState.AccumulatedRotation = 0;
        }

        int bonusPoints = GameConstants.Score.DEFAULT_FLIP_BONUS_POINTS;

        // Add to distance tracker
        distanceTracker.AddBonus(bonusPoints);

        // Publish flip event
        if (eventSystem != null)
        {
            eventSystem.Publish(new FlipCompletedEvent
            {
                TotalFlipCount = flipState.FlipCount,
                BonusPoints = bonusPoints,
                Position = transform.position
            });
        }

        Debug.Log($"<color=yellow>★★★ FLIP COMPLETED! ★★★ Total: {flipState.FlipCount}</color>");
    }

    #endregion

    #region Public API

    public void Crash()
    {
        if (physicsState.IsCrashed) return;
        
        physicsState.IsCrashed = true;
        
        JointMotor2D motor = rearWheel.motor;
        motor.maxMotorTorque = 0f;
        rearWheel.motor = motor;
        
        // Publish crash event
        if (eventSystem != null)
        {
            eventSystem.Publish(new PlayerCrashedEvent
            {
                CrashPosition = transform.position,
                CrashObject = gameObject
            });
        }
        
        // Hide player object
        gameObject.SetActive(false);

        Debug.Log("[BikeController] Player crashed!");
    }

    public void ResetPlayer()
    {
        gameObject.SetActive(true);
        
        physicsState.IsCrashed = false;
        flipState.FlipCount = 0;
        flipState.AccumulatedRotation = 0f;
        
        bikeBody.linearVelocity = Vector2.zero;
        bikeBody.angularVelocity = 0f;
        bikeBody.rotation = 0f;
        
        if (physicsState.RearWheelRB != null)
        {
            physicsState.RearWheelRB.linearVelocity = Vector2.zero;
            physicsState.RearWheelRB.angularVelocity = 0f;
        }

        transform.rotation = Quaternion.identity;
        
        distanceTracker.Reset(transform.position);

        Debug.Log("[BikeController] Player reset");
    }

    public void StopPlayer()
    {
        physicsState.IsAccelerating = false;
        bikeBody.linearVelocity *= 0.5f;
    }

    public int GetScore()
    {
        return (int)(rearWheel.motor.motorSpeed * distanceMultiplier);
    }

    #endregion

    #region Collision Detection

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(GameConstants.Tags.GROUND))
        {
            physicsState.IsGrounded = true;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(GameConstants.Tags.GROUND))
        {
            physicsState.IsGrounded = false;
        }
    }

    #endregion

    #region Debug Visualization

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || bikeBody == null) return;

        // Draw velocity vector
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)bikeBody.linearVelocity);
        
        // Draw speed indicator
        float speedRatio = physicsState.CurrentSpeed / maxSpeed;
        Gizmos.color = Color.Lerp(Color.red, Color.green, speedRatio);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.2f + speedRatio);
    }

    #endregion
}

#region Helper Classes

/// <summary>
/// Encapsulates bike physics state
/// Follows Single Responsibility Principle
/// </summary>
public class BikePhysicsState
{
    public bool IsCrashed;
    public bool IsGrounded;
    public bool IsAccelerating;
    public float CurrentSpeed;
    public Rigidbody2D RearWheelRB;
}

/// <summary>
/// Encapsulates flip detection state
/// Follows Single Responsibility Principle
/// </summary>
public class FlipDetectionState
{
    public int FlipCount;
    public float AccumulatedRotation;
    public float LastFrameRotation;
    public bool WasGrounded;
}

/// <summary>
/// Handles distance tracking and score calculation
/// Follows Single Responsibility Principle
/// </summary>
public class DistanceTracker
{
    private float totalDistance;
    private Vector3 lastPosition;
    private readonly float distanceMultiplier;

    public DistanceTracker(Vector3 startPosition, float multiplier)
    {
        lastPosition = startPosition;
        distanceMultiplier = multiplier;
        totalDistance = 0f;
    }

    public void Update(Vector3 currentPosition, bool isCrashed)
    {
        if (isCrashed) return;

        float deltaDistance = GetDeltaDistance(currentPosition);

        if (currentPosition.x > lastPosition.x)
        {
            totalDistance += deltaDistance;
        }

        lastPosition = currentPosition;
    }

    public float GetDeltaDistance(Vector3 currentPosition)
    {
        return Vector3.Distance(currentPosition, lastPosition);
    }

    public void AddBonus(int bonusPoints)
    {
        totalDistance += bonusPoints;
        Debug.Log($"[DistanceTracker] Bonus added: +{bonusPoints}, Total: {totalDistance:F1}");
    }

    public int GetScore()
    {
        return Mathf.FloorToInt(totalDistance * distanceMultiplier);
    }

    public void Reset(Vector3 newPosition)
    {
        totalDistance = 0f;
        lastPosition = newPosition;
    }
}

#endregion
