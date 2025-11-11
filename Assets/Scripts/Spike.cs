using UnityEngine;

/// <summary>
/// Spike obstacle that causes instant crash when bike touches it
/// </summary>
public class Spike : MonoBehaviour
{
    [Header("Spike Settings")]
    [SerializeField] private bool instantCrash = true;
    [SerializeField] private string targetTag = "Player";
    
    [Header("Visual Settings")]
    [SerializeField] private Color spikeColor = new Color(1f, 0.2f, 0.2f); // Red
    [SerializeField] private bool showWarningGlow = true;
    [SerializeField] private float glowSpeed = 2f;
    [SerializeField] private float glowIntensity = 0.3f;
    
    [Header("Effects")]
    [SerializeField] private GameObject crashEffectPrefab;
    [SerializeField] private AudioClip crashSound;
    [SerializeField] private float soundVolume = 1f;
    [SerializeField] private bool enableScreenShake = true;
    [SerializeField] private float shakeIntensity = 0.5f;
    [SerializeField] private float shakeDuration = 0.3f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebug = true;
    
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Material spriteMaterial;
    private bool hasTriggered = false;

    void Start()
    {
        // Get sprite renderer
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (spriteRenderer != null)
        {
            originalColor = spikeColor;
            spriteRenderer.color = spikeColor;
            spriteMaterial = spriteRenderer.material;
        }
        
        // Validate collider
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            Debug.LogWarning($"[Spike] {gameObject.name} has no Collider2D! Adding PolygonCollider2D.");
            gameObject.AddComponent<PolygonCollider2D>();
        }
        
        // Ensure it's NOT a trigger (we want collision, not trigger)
        if (col != null && col.isTrigger)
        {
            if (showDebug)
            {
                Debug.LogWarning($"[Spike] {gameObject.name} collider is set as Trigger. Consider using solid collision for spikes.");
            }
        }
        
        if (showDebug)
        {
            Debug.Log($"[Spike] {gameObject.name} initialized - Instant Crash: {instantCrash}");
        }
    }

    void Update()
    {
        // Warning glow effect
        if (showWarningGlow && spriteRenderer != null)
        {
            float glow = Mathf.PingPong(Time.time * glowSpeed, 1f) * glowIntensity;
            Color glowColor = Color.Lerp(originalColor, Color.white, glow);
            spriteRenderer.color = glowColor;
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if it's the player
        if (collision.gameObject.CompareTag(targetTag))
        {
            if (hasTriggered) return;
            
            hasTriggered = true;

            if (showDebug)
            {
                Debug.Log($"[Spike] ⚠️ {collision.gameObject.name} hit spike {gameObject.name}!");
            }

            // Get bike controller
            BikeController bike = collision.gameObject.GetComponent<BikeController>();
            if (bike == null)
            {
                bike = collision.transform.parent?.GetComponent<BikeController>();
            }
            
            if (bike != null)
            {
                // Trigger crash
                if (instantCrash)
                {
                    bike.Crash();

                    if (showDebug)
                    {
                        Debug.Log($"[Spike] 💀 Bike crashed on {gameObject.name}");
                    }
                }

                Vector3 effectPosition = collision.transform.position;
                if (collision.contactCount > 0)
                {
                    effectPosition = collision.contacts[0].point;
                }

                // Play effects
                PlayCrashEffects(effectPosition);
            }
            else
            {
                Debug.LogWarning($"[Spike] BikeController not found on {collision.gameObject.name}");
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Alternative: Support trigger-based collision
        if (other.CompareTag(targetTag))
        {
            if (hasTriggered) return;
            
            hasTriggered = true;
            
            if (showDebug)
            {
                Debug.Log($"[Spike] ⚠️ {other.gameObject.name} triggered spike {gameObject.name}!");
            }
            
            BikeController bike = other.GetComponent<BikeController>();
            
            if (bike != null)
            {
                if (instantCrash)
                {
                    bike.Crash();
                    
                    if (showDebug)
                    {
                        Debug.Log($"[Spike] 💀 Bike crashed on {gameObject.name} (Trigger)");
                    }
                }
                
                PlayCrashEffects(other.transform.position);
            }
        }
    }

    private void PlayCrashEffects(Vector3 position)
    {
        // Spawn crash effect
        if (crashEffectPrefab != null)
        {
            GameObject effect = Instantiate(crashEffectPrefab, position, Quaternion.identity);
            Destroy(effect, 2f);
        }
        
        // Play crash sound
        if (crashSound != null)
        {
            AudioSource.PlayClipAtPoint(crashSound, position, soundVolume);
        }
        
        // Camera shake
        if (enableScreenShake && CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake(shakeDuration, shakeIntensity);
        }
    }

    /// <summary>
    /// Reset the spike for level restart (allow triggering again)
    /// </summary>
    public void ResetSpike()
    {
        hasTriggered = false;
        
        if (showDebug)
        {
            Debug.Log($"[Spike] {gameObject.name} reset");
        }
    }

    // Visualize spike in Scene view
    void OnDrawGizmos()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            // Draw danger zone
            Gizmos.color = new Color(1f, 0f, 0f, 0.5f); // Semi-transparent red
            
            if (col is BoxCollider2D box)
            {
                Vector3 size = new Vector3(box.size.x, box.size.y, 0.1f);
                Vector3 center = transform.position + (Vector3)box.offset;
                Gizmos.DrawCube(center, size);
                
                // Draw outline
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(center, size);
            }
            else if (col is PolygonCollider2D poly)
            {
                // Draw polygon points
                Gizmos.color = Color.red;
                Vector2[] points = poly.points;
                for (int i = 0; i < points.Length; i++)
                {
                    Vector3 p1 = transform.TransformPoint(points[i]);
                    Vector3 p2 = transform.TransformPoint(points[(i + 1) % points.Length]);
                    Gizmos.DrawLine(p1, p2);
                }
            }
            else if (col is CircleCollider2D circle)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
                Gizmos.DrawSphere(transform.position + (Vector3)circle.offset, circle.radius);
                
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position + (Vector3)circle.offset, circle.radius);
            }
            
            // Draw warning icon
            Gizmos.color = Color.yellow;
            Vector3 warningPos = transform.position + Vector3.up * 1.5f;
            Gizmos.DrawLine(warningPos, warningPos + Vector3.up * 0.5f);
            Gizmos.DrawWireSphere(warningPos + Vector3.up * 0.7f, 0.2f);
        }
    }

    void OnDrawGizmosSelected()
    {
        // Draw danger radius when selected
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 2f);
        
        // Draw spike indicator
        Vector3[] spikeShape = new Vector3[]
        {
            transform.position + Vector3.left * 0.3f,
            transform.position + Vector3.up * 0.8f,
            transform.position + Vector3.right * 0.3f
        };
        
        for (int i = 0; i < spikeShape.Length; i++)
        {
            Gizmos.DrawLine(spikeShape[i], spikeShape[(i + 1) % spikeShape.Length]);
        }
    }

    // Manual test from Inspector
    [ContextMenu("Test Spike Crash")]
    public void TestSpikeCrash()
    {
        GameObject player = GameObject.FindGameObjectWithTag(targetTag);
        if (player != null)
        {
            BikeController bike = player.GetComponent<BikeController>();
            if (bike != null)
            {
                Debug.Log("[Spike] TEST: Triggering crash...");
                bike.Crash();
                PlayCrashEffects(transform.position);
            }
            else
            {
                Debug.LogError("[Spike] TEST: BikeController not found on player!");
            }
        }
        else
        {
            Debug.LogError($"[Spike] TEST: No object with tag '{targetTag}' found!");
        }
    }

    [ContextMenu("Reset Spike")]
    public void ManualReset()
    {
        ResetSpike();
    }
}
