using UnityEngine;

/// <summary>
/// Individual BONUS letter collectible
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BonusLetter : MonoBehaviour
{
    [Header("Letter Settings")]
    [SerializeField] private LetterType letter = LetterType.B;
    [SerializeField] private string playerTag = "Player";
    
    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color normalColor = Color.yellow;
    [SerializeField] private Color collectedColor = Color.gray;
    
    [Header("Effects")]
    [SerializeField] private GameObject collectEffect;
    [SerializeField] private AudioClip collectSound;
    [SerializeField] private float soundVolume = 1f;
    
    [Header("Animation")]
    [SerializeField] private bool rotateAnimation = true;
    [SerializeField] private float rotationSpeed = 50f;
    [SerializeField] private bool floatAnimation = true;
    [SerializeField] private float floatSpeed = 2f;
    [SerializeField] private float floatAmount = 0.3f;
    
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

    void Start()
    {
        startPosition = transform.position;
        
        // Get sprite renderer if not assigned
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        
        // Set initial color
        if (spriteRenderer != null && !isCollected)
        {
            spriteRenderer.color = normalColor;
        }
        
        // Random float offset
        floatTimer = Random.Range(0f, Mathf.PI * 2f);
    }

    void Update()
    {
        if (isCollected) return;
        
        // Rotation animation
        if (rotateAnimation)
        {
            transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
        }
        
        // Float animation
        if (floatAnimation)
        {
            floatTimer += Time.deltaTime * floatSpeed;
            float offset = Mathf.Sin(floatTimer) * floatAmount;
            transform.position = startPosition + Vector3.up * offset;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected) return;
        
        if (other.CompareTag(playerTag))
        {
            Collect();
        }
    }

    /// <summary>
    /// Collect this letter
    /// </summary>
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
        
        // Notify manager
        if (BonusLetterManager.Instance != null)
        {
            BonusLetterManager.Instance.OnLetterCollected(this);
        }
        
        // Hide the letter
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Set collected status without notifying manager (for restore)
    /// </summary>
    public void SetCollectedWithoutNotify(bool collected)
    {
        isCollected = collected;
        
        if (collected)
        {
            // Hide and change color
            if (spriteRenderer != null)
            {
                spriteRenderer.color = collectedColor;
            }
            gameObject.SetActive(false);
        }
        else
        {
            // Show and restore color
            if (spriteRenderer != null)
            {
                spriteRenderer.color = normalColor;
            }
            gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Play collection effects
    /// </summary>
    private void PlayCollectEffects()
    {
        // Spawn effect
        if (collectEffect != null)
        {
            GameObject effect = Instantiate(collectEffect, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
        
        // Play sound
        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position, soundVolume);
        }
    }

    /// <summary>
    /// Reset letter (make it reappear if not collected)
    /// </summary>
    public void ResetLetter()
    {
        if (!isCollected)
        {
            // If not collected, just make sure it's visible
            gameObject.SetActive(true);
            if (spriteRenderer != null)
            {
                spriteRenderer.color = normalColor;
            }
        }
        // If collected, stay hidden (collected status persists)
        
        if (showDebug)
        {
            Debug.Log($"[BonusLetter] {letter} reset. Collected: {isCollected}");
        }
    }

    /// <summary>
    /// Fully reset letter (uncollect)
    /// </summary>
    public void FullReset()
    {
        isCollected = false;
        gameObject.SetActive(true);
        
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

    /// <summary>
    /// Get letter index (0-4 for B, O, N, U, S)
    /// </summary>
    public int GetLetterIndex()
    {
        return (int)letter;
    }

    /// <summary>
    /// Get letter name
    /// </summary>
    public string GetLetterName()
    {
        return letter.ToString();
    }

    /// <summary>
    /// Check if collected
    /// </summary>
    public bool IsCollected()
    {
        return isCollected;
    }

    // Manual test from Inspector
    [ContextMenu("Collect Letter")]
    public void TestCollect()
    {
        Collect();
    }

    [ContextMenu("Reset Letter")]
    public void TestReset()
    {
        ResetLetter();
    }

    [ContextMenu("Full Reset")]
    public void TestFullReset()
    {
        FullReset();
    }

    // Visualize in Scene view
    void OnDrawGizmos()
    {
        Gizmos.color = isCollected ? Color.gray : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }

    void OnDrawGizmosSelected()
    {
        // Draw letter text
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 1f,
            $"[{letter}]"
        );
        #endif
    }
}
