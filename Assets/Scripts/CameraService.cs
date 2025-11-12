using UnityEngine;

/// <summary>
/// Refactored camera service
/// Handles camera following and shake effects
/// Single Responsibility: Camera control only
/// Implements ICameraService interface
/// </summary>
public class CameraService : MonoBehaviour, ICameraService
{
    [Header("Follow Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(-2f, 2f, -10f);
    [SerializeField] private float smoothSpeed = GameConstants.Camera.DEFAULT_SMOOTH_SPEED;
    [SerializeField] private float lookAheadDistance = GameConstants.Camera.DEFAULT_LOOK_AHEAD_DISTANCE;

    [Header("Shake Settings")]
    [SerializeField] private bool enableShake = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private Vector3 originalPosition;
    private float shakeDuration = 0f;
    private float shakeMagnitude = 0f;
    private bool isShaking = false;

    #region Initialization

    private void Awake()
    {
        RegisterService();
        CacheOriginalPosition();
    }

    private void RegisterService()
    {
        ServiceLocator.Instance.Register<ICameraService>(this);
        
        if (showDebugLogs)
        {
            Debug.Log("[CameraService] Service registered");
        }
    }

    private void CacheOriginalPosition()
    {
        originalPosition = transform.localPosition;
    }

    #endregion

    #region Update Loop

    private void LateUpdate()
    {
        UpdateCameraFollow();
        UpdateCameraShake();
    }

    private void UpdateCameraFollow()
    {
        if (target == null) return;
        
        // Calculate target position (looking slightly ahead)
        Vector3 desiredPosition = target.position + offset;
        desiredPosition.x += lookAheadDistance;
        
        // Smooth movement
        Vector3 smoothedPosition = Vector3.Lerp(
            transform.position, 
            desiredPosition, 
            smoothSpeed * Time.deltaTime
        );
        
        transform.position = smoothedPosition;
    }

    private void UpdateCameraShake()
    {
        if (!enableShake) return;

        if (shakeDuration > 0)
        {
            isShaking = true;
            transform.localPosition = originalPosition + Random.insideUnitSphere * shakeMagnitude;
            shakeDuration -= Time.deltaTime;
        }
        else if (isShaking)
        {
            isShaking = false;
            shakeDuration = 0f;
            transform.localPosition = originalPosition;
        }
    }

    #endregion

    #region ICameraService Implementation

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        
        if (showDebugLogs)
        {
            Debug.Log($"[CameraService] Target set: {newTarget?.name ?? "null"}");
        }
    }

    public void Shake(float duration, float intensity)
    {
        if (!enableShake)
        {
            if (showDebugLogs)
            {
                Debug.Log("[CameraService] Shake disabled");
            }
            return;
        }

        originalPosition = transform.localPosition;
        shakeDuration = duration;
        shakeMagnitude = intensity;
        
        if (showDebugLogs)
        {
            Debug.Log($"[CameraService] Shake triggered: {duration}s @ {intensity}");
        }
    }

    public void ResetCamera()
    {
        shakeDuration = 0f;
        isShaking = false;
        transform.localPosition = originalPosition;
        
        if (showDebugLogs)
        {
            Debug.Log("[CameraService] Camera reset");
        }
    }

    #endregion

    #region Public API

    public void SetOffset(Vector3 newOffset)
    {
        offset = newOffset;
    }

    public void SetSmoothSpeed(float speed)
    {
        smoothSpeed = Mathf.Max(0f, speed);
    }

    public void SetLookAheadDistance(float distance)
    {
        lookAheadDistance = distance;
    }

    #endregion

    #region Debug

    [ContextMenu("Test Shake")]
    private void TestShake()
    {
        Shake(GameConstants.Camera.DEFAULT_SHAKE_DURATION, GameConstants.Camera.DEFAULT_SHAKE_MAGNITUDE);
    }

    #endregion
}
