using UnityEngine;

public class BoostPlatform : MonoBehaviour
{
    private GameEventSystem eventSystem;
    private IEffectService effectService;
    private IAudioService audioService;
    private ICameraService cameraService;

    [Header("Boost Settings")]
    [SerializeField] private float boostForce = GameConstants.Boost.DEFAULT_BOOST_FORCE;
    [SerializeField] private Vector2 boostDirection = Vector2.right;
    [SerializeField] private bool normalizeDirection = true;
    
    [Header("Boost Type")]
    [SerializeField] private BoostType boostType = BoostType.Impulse;
    
    [Header("Visual Feedback")]
    [SerializeField] private Color platformColor = GameConstants.Colors.BOOST_PLATFORM_GREEN;
    [SerializeField] private bool showArrow = true;
    [SerializeField] private float arrowScale = 1f;
    
    [Header("Effects")]
    [SerializeField] private GameObject boostEffectPrefab;
    [SerializeField] private ParticleSystem boostParticles;
    [SerializeField] private bool enableScreenShake = true;
    [SerializeField] private float shakeIntensity = GameConstants.Boost.DEFAULT_SHAKE_INTENSITY;
    
    [Header("Sound")]
    [SerializeField] private AudioClip boostSound;
    [SerializeField] private float soundVolume = 1f;
    
    [Header("Cooldown")]
    [SerializeField] private bool useCooldown = false;
    [SerializeField] private float cooldownTime = GameConstants.Boost.DEFAULT_COOLDOWN_TIME;
    
    [Header("Debug")]
    [SerializeField] private bool showDebug = true;
    
    private bool isOnCooldown = false;
    private float cooldownTimer = 0f;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    public enum BoostType
    {
        Impulse,
        Continuous,
        SpeedSet
    }

    #region Initialization

    private void Awake()
    {
        InitializeServices();
        CacheComponents();
    }

    private void Start()
    {
        InitializeVisuals();
        NormalizeDirection();
        ValidateCollider();
    }

    private void InitializeServices()
    {
        eventSystem = ServiceLocator.Instance.Get<GameEventSystem>();
        effectService = ServiceLocator.Instance.Get<IEffectService>();
        audioService = ServiceLocator.Instance.Get<IAudioService>();
        cameraService = ServiceLocator.Instance.Get<ICameraService>();

        if (eventSystem == null && showDebug)
        {
            Debug.LogWarning("[BoostPlatform] GameEventSystem not found!");
        }
    }

    private void CacheComponents()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
    }

    private void InitializeVisuals()
    {
        originalColor = platformColor;
        spriteRenderer.color = platformColor;
    }

    private void NormalizeDirection()
    {
        if (normalizeDirection && boostDirection != Vector2.zero)
        {
            boostDirection = boostDirection.normalized;
        }
    }

    private void ValidateCollider()
    {
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

    #endregion

    #region Update Loop

    private void Update()
    {
        UpdateCooldown();
    }

    private void UpdateCooldown()
    {
        if (!isOnCooldown) return;

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

    #endregion

    #region Collision Detection

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsPlayer(collision.gameObject)) return;
        
        ApplyBoost(collision.rigidbody, collision.rigidbody.position);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (boostType != BoostType.Continuous) return;
        if (isOnCooldown) return;
        if (!IsPlayer(collision.gameObject)) return;
        
        ApplyBoost(collision.rigidbody, collision.rigidbody.position);
    }

    private bool IsPlayer(GameObject obj)
    {
        return obj.CompareTag(GameConstants.Tags.PLAYER) || 
               obj.layer == LayerMask.NameToLayer(GameConstants.Layers.PLAYER);
    }

    #endregion

    #region Boost Application

    private void ApplyBoost(Rigidbody2D bikeRB, Vector2 position)
    {
        if (bikeRB == null) return;
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
        
        // Publish boost event
        PublishBoostEvent(position);
        
        // Trigger effects
        TriggerEffects(position);
        
        // Start cooldown
        if (useCooldown)
        {
            isOnCooldown = true;
            cooldownTimer = cooldownTime;
        }
    }

    #endregion

    #region Event Publishing

    private void PublishBoostEvent(Vector2 position)
    {
        if (eventSystem == null) return;

        eventSystem.Publish(new BoostActivatedEvent
        {
            BoostPosition = position,
            BoostDirection = boostDirection,
            BoostForce = boostForce
        });
    }

    #endregion

    #region Effects

    private void TriggerEffects(Vector2 position)
    {
        // Spawn boost effect
        if (boostEffectPrefab != null && effectService != null)
        {
            effectService.SpawnEffect(boostEffectPrefab, position, Quaternion.identity);
        }
        
        // Play particles
        if (boostParticles != null && !boostParticles.isPlaying)
        {
            boostParticles.Play();
        }
        
        // Play sound
        if (boostSound != null && audioService != null)
        {
            audioService.PlaySound(boostSound, position, soundVolume);
        }
        
        // Camera shake
        if (enableScreenShake && cameraService != null)
        {
            cameraService.Shake(0.1f, shakeIntensity);
        }
    }

    #endregion

    #region Debug Visualization

    private void OnDrawGizmos()
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
            float arrowLength = GameConstants.Boost.ARROW_LENGTH_SCALE * arrowScale;
            Vector3 endPos = startPos + (Vector3)direction * arrowLength;
            
            // Arrow shaft
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(startPos, endPos);
            
            // Arrow head
            Vector3 right = Quaternion.Euler(0, 0, -GameConstants.Boost.ARROW_HEAD_ANGLE) * -direction * GameConstants.Boost.ARROW_HEAD_LENGTH * arrowScale;
            Vector3 left = Quaternion.Euler(0, 0, GameConstants.Boost.ARROW_HEAD_ANGLE) * -direction * GameConstants.Boost.ARROW_HEAD_LENGTH * arrowScale;
            
            Gizmos.DrawLine(endPos, endPos + right);
            Gizmos.DrawLine(endPos, endPos + left);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw boost force visualization
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Vector3 forceVector = (Vector3)boostDirection.normalized * (boostForce / GameConstants.Boost.FORCE_VISUALIZATION_DIVISOR);
        Gizmos.DrawRay(transform.position, forceVector);
        
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 2f,
            $"Boost: {boostForce}\nType: {boostType}\nDir: {boostDirection}"
        );
        #endif
    }

    #endregion
}
