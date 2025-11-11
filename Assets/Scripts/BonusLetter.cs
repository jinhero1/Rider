using UnityEngine;

/// <summary>
/// Individual bonus letter collectible (B, O, N, U, S)
/// </summary>
public class BonusLetter : MonoBehaviour
{
    [Header("Letter Settings")]
    [SerializeField] private BonusLetterType letterType;
    [SerializeField] private string displayText; // "B", "O", "N", "U", "S"
    
    [Header("Visual Settings")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color letterColor = new Color(1f, 0.84f, 0f); // Gold color
    [SerializeField] private float rotationSpeed = 80f;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobHeight = 0.15f;
    [SerializeField] private float scaleAnimation = 0.1f;
    
    [Header("Collection Settings")]
    [SerializeField] private bool isCollected = false;
    [SerializeField] private string playerTag = "Player";
    
    [Header("Effects")]
    [SerializeField] private GameObject collectEffectPrefab;
    [SerializeField] private AudioClip collectSound;
    [SerializeField] private float soundVolume = 1f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebug = false;
    
    private Vector3 startPosition;
    private Collider2D myCollider;
    private float animationTimer = 0f;

    void Start()
    {
        startPosition = transform.position;
        
        // Get or add components
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        
        if (spriteRenderer != null)
        {
            spriteRenderer.color = letterColor;
        }
        
        // Set display text based on letter type
        if (string.IsNullOrEmpty(displayText))
        {
            displayText = letterType.ToString();
        }
        
        // Validate collider
        myCollider = GetComponent<Collider2D>();
        if (myCollider == null)
        {
            Debug.LogWarning($"[BonusLetter] {gameObject.name} has no Collider2D! Adding CircleCollider2D.");
            myCollider = gameObject.AddComponent<CircleCollider2D>();
        }
        
        if (!myCollider.isTrigger)
        {
            myCollider.isTrigger = true;
        }
        
        // Random animation offset for variety
        animationTimer = Random.Range(0f, Mathf.PI * 2f);
        
        if (showDebug)
        {
            Debug.Log($"[BonusLetter] {letterType} initialized at {transform.position}");
        }
    }

    void Update()
    {
        if (isCollected) return;
        
        animationTimer += Time.deltaTime;
        
        // Rotation animation
        transform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);
        
        // Bobbing animation
        float newY = startPosition.y + Mathf.Sin(animationTimer * bobSpeed) * bobHeight;
        
        // Scale pulse animation
        float scale = 1f + Mathf.Sin(animationTimer * 3f) * scaleAnimation;
        
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        transform.localScale = Vector3.one * scale;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected) return;
        
        if (showDebug)
        {
            Debug.Log($"[BonusLetter] Trigger entered by: {other.gameObject.name} (Tag: {other.tag})");
        }
        
        if (other.CompareTag(playerTag))
        {
            Collect();
        }
    }

    private void Collect()
    {
        if (isCollected) return;
        
        isCollected = true;
        
        if (showDebug)
        {
            Debug.Log($"[BonusLetter] ★ {letterType} collected!");
        }
        
        // Notify the manager
        if (BonusLetterManager.Instance != null)
        {
            BonusLetterManager.Instance.OnLetterCollected(this);
        }
        else
        {
            Debug.LogWarning("[BonusLetter] BonusLetterManager.Instance is null!");
        }
        
        // Play effects
        PlayCollectEffects();
        
        // Hide the letter
        gameObject.SetActive(false);
    }

    private void PlayCollectEffects()
    {
        // Spawn particle effect
        if (collectEffectPrefab != null)
        {
            GameObject effect = Instantiate(collectEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
        
        // Play sound
        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position, soundVolume);
        }
        
        // Camera shake
        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake(0.15f, 0.15f);
        }
    }

    public void ResetLetter()
    {
        isCollected = false;
        gameObject.SetActive(true);
        transform.position = startPosition;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;
        animationTimer = Random.Range(0f, Mathf.PI * 2f);
        
        if (showDebug)
        {
            Debug.Log($"[BonusLetter] {letterType} reset");
        }
    }

    public bool IsCollected()
    {
        return isCollected;
    }

    public BonusLetterType GetLetterType()
    {
        return letterType;
    }

    public string GetDisplayText()
    {
        return displayText;
    }

    // Visualize in Scene view
    void OnDrawGizmos()
    {
        if (Application.isPlaying && isCollected)
        {
            return;
        }
        
        // Draw letter indicator
        Gizmos.color = letterColor;
        
        Vector3 pos = Application.isPlaying ? transform.position : transform.position;
        float size = 0.4f;
        
        // Draw box
        Gizmos.DrawWireCube(pos, Vector3.one * size);
        
        // Draw trigger radius
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.color = new Color(1f, 0.84f, 0f, 0.3f);
            if (col is CircleCollider2D circle)
            {
                Gizmos.DrawWireSphere(pos, circle.radius);
            }
            else if (col is BoxCollider2D box)
            {
                Gizmos.DrawWireCube(pos, box.size);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 1f);
        
        // Draw letter type label in scene view
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 1.5f,
            $"{letterType}"
        );
        #endif
    }
}

public enum BonusLetterType
{
    B = 0,
    O = 1,
    N = 2,
    U = 3,
    S = 4
}
