using UnityEngine;

public class Spike : MonoBehaviour
{
    private GameEventSystem eventSystem;
    private IEffectService effectService;
    private IAudioService audioService;
    private ICameraService cameraService;

    [Header("Spike Settings")]
    [SerializeField] private bool instantCrash = true;
    
    [Header("Visual Settings")]
    [SerializeField] private Color spikeColor = GameConstants.Colors.SPIKE_RED;
    [SerializeField] private bool showWarningGlow = true;
    [SerializeField] private float glowSpeed = GameConstants.Animation.GLOW_SPEED;
    [SerializeField] private float glowIntensity = GameConstants.Animation.GLOW_INTENSITY;
    
    [Header("Effects")]
    [SerializeField] private GameObject crashEffectPrefab;
    [SerializeField] private AudioClip crashSound;
    [SerializeField] private float soundVolume = 1f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebug = true;
    
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private bool hasTriggered = false;

    #region Initialization

    private void Awake()
    {
        InitializeServices();
        CacheComponents();
    }

    private void Start()
    {
        InitializeVisuals();
        ValidateCollider();
    }

    private void InitializeServices()
    {
        eventSystem = ServiceLocator.Instance.Get<GameEventSystem>();
        effectService = ServiceLocator.Instance.Get<IEffectService>();
        audioService = ServiceLocator.Instance.Get<IAudioService>();
        cameraService = ServiceLocator.Instance.Get<ICameraService>();

        if (eventSystem == null)
        {
            Debug.LogError("[Spike] GameEventSystem not found!");
        }
    }

    private void CacheComponents()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void InitializeVisuals()
    {
        if (spriteRenderer != null)
        {
            originalColor = spikeColor;
            spriteRenderer.color = spikeColor;
        }
    }

    private void ValidateCollider()
    {
        Collider2D col = GetComponent<Collider2D>();
        
        if (col == null)
        {
            Debug.LogWarning($"[Spike] {gameObject.name} has no Collider2D! Adding PolygonCollider2D.");
            gameObject.AddComponent<PolygonCollider2D>();
        }
        
        if (col != null && col.isTrigger && showDebug)
        {
            Debug.LogWarning($"[Spike] {gameObject.name} collider is Trigger. Consider solid collision.");
        }
        
        if (showDebug)
        {
            Debug.Log($"[Spike] {gameObject.name} initialized - Instant Crash: {instantCrash}");
        }
    }

    #endregion

    #region Update Loop

    private void Update()
    {
        AnimateWarningGlow();
    }

    private void AnimateWarningGlow()
    {
        if (showWarningGlow && spriteRenderer != null)
        {
            float glow = Mathf.PingPong(Time.time * glowSpeed, 1f) * glowIntensity;
            Color glowColor = Color.Lerp(originalColor, Color.white, glow);
            spriteRenderer.color = glowColor;
        }
    }

    #endregion

    #region Collision Detection

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag(GameConstants.Tags.PLAYER)) return;
        if (hasTriggered) return;
        
        ProcessSpikeHit(collision.gameObject, collision.contacts.Length > 0 ? collision.contacts[0].point : collision.transform.position);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(GameConstants.Tags.PLAYER)) return;
        if (hasTriggered) return;
        
        ProcessSpikeHit(other.gameObject, other.transform.position);
    }

    #endregion

    #region Spike Hit Processing

    private void ProcessSpikeHit(GameObject playerObject, Vector3 hitPosition)
    {
        hasTriggered = true;

        if (showDebug)
        {
            Debug.Log($"[Spike] ⚠️ {playerObject.name} hit spike {gameObject.name}!");
        }

        // Get player controller
        IPlayerController bike = playerObject.GetComponent<IPlayerController>();
        if (bike == null)
        {
            bike = playerObject.transform.parent?.GetComponent<IPlayerController>();
        }
        
        if (bike != null && instantCrash)
        {
            bike.Crash();
            
            if (showDebug)
            {
                Debug.Log($"[Spike] 💀 Bike crashed on {gameObject.name}");
            }
        }

        // Publish spike hit event
        PublishSpikeHitEvent(hitPosition);
        
        // Play effects
        PlayCrashEffects(hitPosition);
    }

    #endregion

    #region Event Publishing

    private void PublishSpikeHitEvent(Vector3 hitPosition)
    {
        if (eventSystem == null) return;

        eventSystem.Publish(new SpikeHitEvent
        {
            HitPosition = hitPosition,
            SpikeObject = gameObject
        });
    }

    #endregion

    #region Effects

    private void PlayCrashEffects(Vector3 position)
    {
        // Spawn crash effect
        if (crashEffectPrefab != null && effectService != null)
        {
            effectService.SpawnEffect(
                crashEffectPrefab, 
                position, 
                GameConstants.Collectibles.EFFECT_DESTROY_DELAY
            );
        }
        
        // Play crash sound
        if (crashSound != null && audioService != null)
        {
            audioService.PlaySound(crashSound, position, soundVolume);
        }
        
        // Camera shake
        cameraService?.Shake(
            GameConstants.Camera.DEFAULT_SHAKE_DURATION,
            GameConstants.Camera.SPIKE_SHAKE_INTENSITY
        );
    }

    #endregion

    #region Public API

    public void ResetSpike()
    {
        hasTriggered = false;
        
        if (showDebug)
        {
            Debug.Log($"[Spike] {gameObject.name} reset");
        }
    }

    #endregion

    #region Debug Visualization

    private void OnDrawGizmos()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return;

        // Draw danger zone
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        
        if (col is BoxCollider2D box)
        {
            Vector3 size = new Vector3(box.size.x, box.size.y, 0.1f);
            Vector3 center = transform.position + (Vector3)box.offset;
            Gizmos.DrawCube(center, size);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(center, size);
        }
        else if (col is PolygonCollider2D poly)
        {
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
        Vector3 warningPos = transform.position + Vector3.up * GameConstants.Gizmos.SPIKE_WARNING_OFFSET;
        Gizmos.DrawLine(warningPos, warningPos + Vector3.up * GameConstants.Gizmos.SPIKE_WARNING_HEIGHT);
        Gizmos.DrawWireSphere(warningPos + Vector3.up * (GameConstants.Gizmos.SPIKE_WARNING_HEIGHT + 0.2f), GameConstants.Gizmos.SPIKE_WARNING_RADIUS);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, GameConstants.Gizmos.SPIKE_DANGER_RADIUS);
    }

    #endregion

    #region Debug Context Menu

    [ContextMenu("Test Spike Crash")]
    private void TestSpikeCrash()
    {
        GameObject player = GameObject.FindGameObjectWithTag(GameConstants.Tags.PLAYER);
        if (player != null)
        {
            IPlayerController bike = player.GetComponent<IPlayerController>();
            if (bike != null)
            {
                Debug.Log("[Spike] TEST: Triggering crash...");
                bike.Crash();
                PlayCrashEffects(transform.position);
            }
            else
            {
                Debug.LogError("[Spike] TEST: BikeController not found!");
            }
        }
        else
        {
            Debug.LogError($"[Spike] TEST: No '{GameConstants.Tags.PLAYER}' tag found!");
        }
    }

    [ContextMenu("Reset Spike")]
    private void ManualReset()
    {
        ResetSpike();
    }

    #endregion
}
