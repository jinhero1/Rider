using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Collectible : MonoBehaviour, ICollectible
{
    private GameEventSystem eventSystem;
    private IEffectService effectService;
    private IAudioService audioService;

    [Header("Visual Settings")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color collectibleColor = GameConstants.Colors.COLLECTIBLE_GREEN;
    [SerializeField] private float rotationSpeed = GameConstants.Animation.DEFAULT_ROTATION_SPEED;
    [SerializeField] private float bobSpeed = GameConstants.Animation.DEFAULT_BOB_SPEED;
    [SerializeField] private float bobHeight = GameConstants.Animation.DEFAULT_BOB_HEIGHT;
    
    [Header("Effects")]
    [SerializeField] private GameObject collectEffectPrefab;
    [SerializeField] private AudioClip collectSound;
    [SerializeField] private float soundVolume = 1f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebug = false;

    private bool isCollected = false;
    private Vector3 startPosition;
    private Collider2D myCollider;

    // ICollectible implementation
    public bool IsCollected => isCollected;
    public Vector3 Position => transform.position;

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

        if (eventSystem == null)
        {
            Debug.LogError("[Collectible] GameEventSystem not found!");
        }
    }

    private void CacheComponents()
    {
        startPosition = transform.position;
        
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        
        myCollider = GetComponent<Collider2D>();
    }

    private void InitializeVisuals()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = collectibleColor;
        }
    }

    private void ValidateCollider()
    {
        if (myCollider == null)
        {
            Debug.LogWarning($"[Collectible] {gameObject.name} has no Collider2D! Adding CircleCollider2D.");
            myCollider = gameObject.AddComponent<CircleCollider2D>();
        }
        
        if (!myCollider.isTrigger)
        {
            myCollider.isTrigger = true;
            
            if (showDebug)
            {
                Debug.Log($"[Collectible] {gameObject.name} collider set to trigger");
            }
        }
    }

    #endregion

    #region Update Loop

    private void Update()
    {
        if (isCollected) return;
        
        AnimateRotation();
        AnimateBobbing();
    }

    private void AnimateRotation()
    {
        transform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);
    }

    private void AnimateBobbing()
    {
        float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    #endregion

    #region Collision Detection

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected) return;
        
        if (showDebug)
        {
            Debug.Log($"[Collectible] Trigger: {other.gameObject.name} (Tag: {other.tag})");
        }
        
        if (other.CompareTag(GameConstants.Tags.PLAYER))
        {
            Collect();
        }
    }

    #endregion

    #region ICollectible Implementation

    public void Collect()
    {
        if (isCollected) return;
        
        isCollected = true;
        
        if (showDebug)
        {
            Debug.Log($"[Collectible] ✦ {gameObject.name} collected!");
        }
        
        // Publish event
        PublishCollectionEvent();
        
        // Play effects
        PlayCollectEffects();
        
        // Disable visual
        gameObject.SetActive(false);
    }

    public void Reset()
    {
        isCollected = false;
        gameObject.SetActive(true);
        transform.position = startPosition;
        transform.rotation = Quaternion.identity;
        
        if (showDebug)
        {
            Debug.Log($"[Collectible] {gameObject.name} reset");
        }
    }

    #endregion

    #region Event Publishing

    private void PublishCollectionEvent()
    {
        if (eventSystem == null) return;

        eventSystem.Publish(new CollectibleCollectedEvent
        {
            Position = transform.position,
            CollectedCount = 0,  // Will be updated by service
            TotalCount = 0       // Will be updated by service
        });
    }

    #endregion

    #region Effects

    private void PlayCollectEffects()
    {
        // Spawn particle effect
        if (collectEffectPrefab != null && effectService != null)
        {
            effectService.SpawnEffect(
                collectEffectPrefab, 
                transform.position, 
                GameConstants.Collectibles.EFFECT_DESTROY_DELAY
            );
        }
        
        // Play sound
        if (collectSound != null && audioService != null)
        {
            audioService.PlaySound(collectSound, transform.position, soundVolume);
        }
    }

    #endregion

    #region Debug Visualization

    private void OnDrawGizmos()
    {
        if (Application.isPlaying && isCollected) return;
        
        // Draw diamond shape
        Gizmos.color = collectibleColor;
        
        Vector3 pos = transform.position;
        float size = GameConstants.Collectibles.COLLECTION_TRIGGER_RADIUS;
        
        // Diamond outline
        Vector3 top = pos + Vector3.up * size;
        Vector3 bottom = pos + Vector3.down * size;
        Vector3 left = pos + Vector3.left * size * 0.7f;
        Vector3 right = pos + Vector3.right * size * 0.7f;
        
        Gizmos.DrawLine(top, left);
        Gizmos.DrawLine(top, right);
        Gizmos.DrawLine(bottom, left);
        Gizmos.DrawLine(bottom, right);
        
        // Draw trigger radius
        if (myCollider != null)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            
            if (myCollider is CircleCollider2D circle)
            {
                Gizmos.DrawWireSphere(pos, circle.radius);
            }
            else if (myCollider is BoxCollider2D box)
            {
                Gizmos.DrawWireCube(pos, box.size);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 1f);
    }

    #endregion
}
