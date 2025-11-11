using UnityEngine;

/// <summary>
/// Collectible item (diamond) that can be picked up by the player
/// </summary>
public class Collectible : MonoBehaviour
{
    [Header("Visual Settings")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color collectibleColor = Color.green;
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobHeight = 0.2f;
    
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
    private Quaternion startRotation;
    private Collider2D myCollider;

    void Start()
    {
        // Store initial position for bobbing animation
        startPosition = transform.position;
        startRotation = transform.rotation;
        
        // Get or add components
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        
        if (spriteRenderer != null)
        {
            spriteRenderer.color = collectibleColor;
        }
        
        // Validate collider
        myCollider = GetComponent<Collider2D>();
        if (myCollider == null)
        {
            Debug.LogWarning($"[Collectible] {gameObject.name} has no Collider2D! Adding CircleCollider2D.");
            myCollider = gameObject.AddComponent<CircleCollider2D>();
        }
        
        // Make sure it's a trigger
        if (!myCollider.isTrigger)
        {
            myCollider.isTrigger = true;
            if (showDebug)
            {
                Debug.Log($"[Collectible] {gameObject.name} collider set to trigger");
            }
        }
        
        if (showDebug)
        {
            Debug.Log($"[Collectible] {gameObject.name} initialized at {transform.position}");
        }
    }

    void Update()
    {
        if (isCollected) return;
        
        // Rotate the diamond
        transform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);
        
        // Bobbing animation
        float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Check if already collected
        if (isCollected) return;
        
        if (showDebug)
        {
            Debug.Log($"[Collectible] Trigger entered by: {other.gameObject.name} (Tag: {other.tag})");
        }
        
        // Check if it's the player
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
            Debug.Log($"[Collectible] ✦ {gameObject.name} collected!");
        }
        
        // Notify the manager
        if (CollectibleManager.Instance != null)
        {
            CollectibleManager.Instance.OnCollectibleCollected(this);
        }
        else
        {
            Debug.LogWarning("[Collectible] CollectibleManager.Instance is null!");
        }
        
        // Play effects
        PlayCollectEffects();
        
        // Disable the collectible
        gameObject.SetActive(false);
    }

    private void PlayCollectEffects()
    {
        // Spawn particle effect
        if (collectEffectPrefab != null)
        {
            GameObject effect = Instantiate(collectEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 2f); // Clean up after 2 seconds
        }
        
        // Play sound
        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position, soundVolume);
        }
        
        // Optional: Camera shake
        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake(0.1f, 0.1f);
        }
    }

    /// <summary>
    /// Reset the collectible (make it appear again)
    /// </summary>
    public void ResetCollectible()
    {
        isCollected = false;
        gameObject.SetActive(true);
        transform.position = startPosition;
        transform.rotation = startRotation;
        
        if (showDebug)
        {
            Debug.Log($"[Collectible] {gameObject.name} reset");
        }
    }

    /// <summary>
    /// Check if this collectible has been collected
    /// </summary>
    public bool IsCollected()
    {
        return isCollected;
    }

    // Visualize collectible in Scene view
    void OnDrawGizmos()
    {
        if (Application.isPlaying && isCollected)
        {
            return; // Don't draw if collected
        }
        
        // Draw diamond shape
        Gizmos.color = collectibleColor;
        
        Vector3 pos = Application.isPlaying ? transform.position : transform.position;
        float size = 0.5f;
        
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
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
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
        // Draw collection range when selected
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 1f);
    }
}
