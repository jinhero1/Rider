using UnityEngine;

/// <summary>
/// Camera controller that smoothly follows the bike
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(3f, 2f, -10f);
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private float lookAheadDistance = 2f;

    void LateUpdate()
    {
        if (target == null) return;
        
        // Calculate target position (looking slightly ahead)
        Vector3 desiredPosition = target.position + offset;
        desiredPosition.x += lookAheadDistance;
        
        // Smooth movement
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;
    }
}

/// <summary>
/// Camera shake effect
/// </summary>
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;
    
    private Vector3 originalPosition;
    private float shakeDuration = 0f;
    private float shakeMagnitude = 0.3f;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Update()
    {
        if (shakeDuration > 0)
        {
            transform.localPosition = originalPosition + Random.insideUnitSphere * shakeMagnitude;
            shakeDuration -= Time.deltaTime;
        }
        else
        {
            shakeDuration = 0f;
            transform.localPosition = originalPosition;
        }
    }

    public void Shake(float duration, float magnitude)
    {
        originalPosition = transform.localPosition;
        shakeDuration = duration;
        shakeMagnitude = magnitude;
    }
}