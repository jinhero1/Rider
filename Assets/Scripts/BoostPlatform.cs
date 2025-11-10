using UnityEngine;

/// <summary>
/// Boost platform that accelerates the bike when touched
/// </summary>
public class BoostPlatform : MonoBehaviour
{
    [Header("Boost Settings")]
    [SerializeField] private float boostForce = 500f;
    [SerializeField] private Vector2 boostDirection = Vector2.right; // Default: forward boost
    [SerializeField] private bool normalizeDirection = true;
    
    [Header("Boost Type")]
    [SerializeField] private BoostType boostType = BoostType.Impulse;
    [SerializeField] private float boostDuration = 0.5f; // For continuous boost
    
    [Header("Visual Feedback")]
    [SerializeField] private Color platformColor = Color.green;
    [SerializeField] private bool showArrow = true;
    [SerializeField] private float arrowScale = 1f;
    
    [Header("Effects")]
    [SerializeField] private GameObject boostEffectPrefab;
    [SerializeField] private ParticleSystem boostParticles;
    [SerializeField] private bool enableScreenShake = true;
    [SerializeField] private float shakeIntensity = 0.1f;
    
    [Header("Sound")]
    [SerializeField] private AudioClip boostSound;
    [SerializeField] private float soundVolume = 1f;
    
    [Header("Cooldown")]
    [SerializeField] private bool useCooldown = false;
    [SerializeField] private float cooldownTime = 1f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebug = true;
    
    private bool isOnCooldown = false;
    private float cooldownTimer = 0f;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    public enum BoostType
    {
        Impulse,      // One-time instant boost
        Continuous,   // Continuous force while on platform
        SpeedSet      // Set speed to specific value
    }

    void Start()
    {
        // Get or add sprite renderer
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
        
        // Set platform color
        originalColor = platformColor;
        spriteRenderer.color = platformColor;
        
        // Normalize boost direction if needed
        if (normalizeDirection && boostDirection != Vector2.zero)
        {
            boostDirection = boostDirection.normalized;
        }
        
        // Validate collider
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            Debug.LogWarning("[BoostPlatform] No Collider2D found! Adding BoxCollider2D.");
            gameObject.AddComponent<BoxCollider2D>();
        }
        
        if (showDebug)
        {
            Debug.Log($"[BoostPlatform] Initialized: Force={boostForce}, Direction={boostDirection}");
        }
    }

    void Update()
    {
        // Handle cooldown
        if (isOnCooldown)
        {
            cooldownTimer -= Time.deltaTime;
            
            if (cooldownTimer <= 0f)
            {
                isOnCooldown = false;
                spriteRenderer.color = originalColor;
            }
            else
            {
                // Flash during cooldown
                float alpha = Mathf.PingPong(Time.time * 3f, 1f);
                spriteRenderer.color = Color.Lerp(Color.gray, originalColor, alpha);
            }
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if bike hit the platform
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            ApplyBoost(collision.rigidbody);
        }
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        // For continuous boost
        if (boostType == BoostType.Continuous && !isOnCooldown)
        {
            if (collision.gameObject.CompareTag("Player") || collision.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                ApplyBoost(collision.rigidbody);
            }
        }
    }

    void ApplyBoost(Rigidbody2D bikeRB)
    {
        if (bikeRB == null) return;
        
        // Check cooldown
        if (isOnCooldown && useCooldown) return;
        
        if (showDebug)
        {
            Debug.Log($"[BoostPlatform] Applying boost to {bikeRB.gameObject.name}");
        }
        
        // Apply boost based on type
        switch (boostType)
        {
            case BoostType.Impulse:
                bikeRB.AddForce(boostDirection * boostForce, ForceMode2D.Impulse);
                break;
                
            case BoostType.Continuous:
                bikeRB.AddForce(boostDirection * boostForce * Time.fixedDeltaTime);
                break;
                
            case BoostType.SpeedSet:
                bikeRB.linearVelocity = boostDirection * boostForce;
                break;
        }
        
        // Trigger effects
        TriggerEffects(bikeRB.position);
        
        // Start cooldown
        if (useCooldown)
        {
            isOnCooldown = true;
            cooldownTimer = cooldownTime;
        }
    }

    void TriggerEffects(Vector2 position)
    {
        // Spawn boost effect
        if (boostEffectPrefab != null)
        {
            Instantiate(boostEffectPrefab, position, Quaternion.identity);
        }
        
        // Play particles
        if (boostParticles != null && !boostParticles.isPlaying)
        {
            boostParticles.Play();
        }
        
        // Play sound
        if (boostSound != null)
        {
            AudioSource.PlayClipAtPoint(boostSound, position, soundVolume);
        }
        
        // Camera shake
        if (enableScreenShake && CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake(0.1f, shakeIntensity);
        }
    }

    // Visualize boost platform in editor
    void OnDrawGizmos()
    {
        Gizmos.color = platformColor;
        
        // Draw platform bounds
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.DrawWireCube(transform.position, col.bounds.size);
        }
        
        // Draw boost direction arrow
        if (showArrow && boostDirection != Vector2.zero)
        {
            Vector3 startPos = transform.position;
            Vector3 direction = boostDirection.normalized;
            float arrowLength = 2f * arrowScale;
            Vector3 endPos = startPos + (Vector3)direction * arrowLength;
            
            // Arrow shaft
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(startPos, endPos);
            
            // Arrow head
            Vector3 right = Quaternion.Euler(0, 0, -30) * -direction * 0.5f * arrowScale;
            Vector3 left = Quaternion.Euler(0, 0, 30) * -direction * 0.5f * arrowScale;
            
            Gizmos.DrawLine(endPos, endPos + right);
            Gizmos.DrawLine(endPos, endPos + left);
        }
    }

    void OnDrawGizmosSelected()
    {
        // Draw boost force visualization
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Vector3 forceVector = (Vector3)boostDirection.normalized * (boostForce / 100f);
        Gizmos.DrawRay(transform.position, forceVector);
        
        // Draw text (boost info)
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 2f,
            $"Boost: {boostForce}\nType: {boostType}\nDir: {boostDirection}"
        );
        #endif
    }
}