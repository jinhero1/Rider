using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BonusLetter : MonoBehaviour
{
    private GameEventSystem eventSystem;
    private IEffectService effectService;
    private IAudioService audioService;

    [Header("Letter Settings")]
    [SerializeField] private LetterType letter = LetterType.B;
    
    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color normalColor = GameConstants.Colors.BONUS_YELLOW;
    [SerializeField] private Color collectedColor = GameConstants.Colors.COLLECTED_GRAY;
    
    [Header("Effects")]
    [SerializeField] private GameObject collectEffect;
    [SerializeField] private AudioClip collectSound;
    [SerializeField] private float soundVolume = 1f;
    
    [Header("Animation")]
    [SerializeField] private bool rotateAnimation = true;
    [SerializeField] private float rotationSpeed = GameConstants.Animation.DEFAULT_ROTATION_SPEED;
    [SerializeField] private bool floatAnimation = true;
    [SerializeField] private float floatSpeed = GameConstants.Animation.DEFAULT_FLOAT_SPEED;
    [SerializeField] private float floatAmount = GameConstants.Animation.DEFAULT_FLOAT_AMOUNT;
    
    [Header("Debug")]
    [SerializeField] private bool showDebug = false;
    
    private bool isCollected = false;
    private Vector3 startPosition;
    private float floatTimer = 0f;

    public enum LetterType
    {
        B = 0,
        O = 1,
        N = 2,
        U = 3,
        S = 4
    }

    public bool IsCollected => isCollected;
    public int LetterIndex => (int)letter;
    public string LetterName => letter.ToString();

    #region Initialization

    private void Awake()
    {
        InitializeServices();
        CacheComponents();
    }

    private void Start()
    {
        InitializeVisuals();
        floatTimer = Random.Range(0f, Mathf.PI * 2f);
    }

    private void InitializeServices()
    {
        eventSystem = ServiceLocator.Instance.Get<GameEventSystem>();
        effectService = ServiceLocator.Instance.Get<IEffectService>();
        audioService = ServiceLocator.Instance.Get<IAudioService>();

        if (eventSystem == null)
        {
            Debug.LogError("[BonusLetter] GameEventSystem not found!");
        }
    }

    private void CacheComponents()
    {
        startPosition = transform.position;
        
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private void InitializeVisuals()
    {
        if (spriteRenderer != null && !isCollected)
        {
            spriteRenderer.color = normalColor;
        }
    }

    #endregion

    #region Update Loop

    private void Update()
    {
        if (isCollected) return;
        
        AnimateRotation();
        AnimateFloat();
    }

    private void AnimateRotation()
    {
        if (rotateAnimation)
        {
            transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
        }
    }

    private void AnimateFloat()
    {
        if (floatAnimation)
        {
            floatTimer += Time.deltaTime * floatSpeed;
            float offset = Mathf.Sin(floatTimer) * floatAmount;
            transform.position = startPosition + Vector3.up * offset;
        }
    }

    #endregion

    #region Collision Detection

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected) return;
        
        if (other.CompareTag(GameConstants.Tags.PLAYER))
        {
            Collect();
        }
    }

    #endregion

    #region Collection

    public void Collect()
    {
        if (isCollected) return;
        
        isCollected = true;
        
        if (showDebug)
        {
            Debug.Log($"[BonusLetter] {letter} collected!");
        }
        
        // Play effects
        PlayCollectEffects();
        
        // Change visual
        if (spriteRenderer != null)
        {
            spriteRenderer.color = collectedColor;
        }
        
        // Publish event
        PublishCollectionEvent();
        
        // Hide the letter
        gameObject.SetActive(false);
    }

    public void SetCollectedStatus(bool collected)
    {
        isCollected = collected;
        
        if (collected)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = collectedColor;
            }
            gameObject.SetActive(false);
        }
        else
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = normalColor;
            }
            gameObject.SetActive(true);
        }
    }

    public void ResetLetter()
    {
        if (!isCollected)
        {
            // If not collected, just ensure it's visible
            gameObject.SetActive(true);
            if (spriteRenderer != null)
            {
                spriteRenderer.color = normalColor;
            }
        }
        
        if (showDebug)
        {
            Debug.Log($"[BonusLetter] {letter} reset. Collected: {isCollected}");
        }
    }

    public void FullReset()
    {
        isCollected = false;
        
        if (spriteRenderer != null)
        {
            spriteRenderer.color = normalColor;
        }
        
        transform.position = startPosition;
        floatTimer = Random.Range(0f, Mathf.PI * 2f);
        
        if (showDebug)
        {
            Debug.Log($"[BonusLetter] {letter} fully reset");
        }
    }

    #endregion

    #region Event Publishing

    private void PublishCollectionEvent()
    {
        if (eventSystem == null) return;

        eventSystem.Publish(new BonusLetterCollectedEvent
        {
            LetterIndex = LetterIndex,
            LetterName = LetterName,
            TotalCollected = 0  // Will be updated by service
        });
    }

    #endregion

    #region Effects

    private void PlayCollectEffects()
    {
        // Spawn effect
        if (collectEffect != null && effectService != null)
        {
            effectService.SpawnEffect(
                collectEffect, 
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
        Gizmos.color = isCollected ? Color.gray : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, GameConstants.Collectibles.COLLECTION_TRIGGER_RADIUS);
    }

    private void OnDrawGizmosSelected()
    {
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 1f,
            $"[{letter}]"
        );
        #endif
    }

    #endregion

    #region Debug Context Menu

    [ContextMenu("Collect Letter")]
    private void TestCollect()
    {
        Collect();
    }

    [ContextMenu("Reset Letter")]
    private void TestReset()
    {
        ResetLetter();
    }

    [ContextMenu("Full Reset")]
    private void TestFullReset()
    {
        FullReset();
    }

    #endregion
}
