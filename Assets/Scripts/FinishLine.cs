using UnityEngine;

/// <summary>
/// Detects when player reaches the finish line
/// </summary>
public class FinishLine : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private Color gizmoColor = Color.green;
    
    [Header("Status")]
    [SerializeField] private bool hasTriggered = false;
    [SerializeField] private int triggerAttempts = 0;
    
    private Collider2D myCollider;

    void Start()
    {
        // Validate setup
        myCollider = GetComponent<Collider2D>();
        
        if (myCollider == null)
        {
            Debug.LogError("[FinishLine] No Collider2D found!");
        }
        else
        {
            Debug.Log($"[FinishLine] Collider found: {myCollider.GetType().Name}");
            
            if (!myCollider.isTrigger)
            {
                Debug.LogError("[FinishLine] Collider is NOT set as Trigger!");
                Debug.LogError("  → Fix: Select FinishLine → Check 'Is Trigger' in Collider2D");
            }
            else
            {
                Debug.Log("[FinishLine] ✓ Collider is set as Trigger");
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[FinishLine] Initialized at position: {transform.position}");
            Debug.Log($"[FinishLine] GameObject active: {gameObject.activeInHierarchy}");
            Debug.Log($"[FinishLine] Layer: {LayerMask.LayerToName(gameObject.layer)}");
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        triggerAttempts++;
        
        if (showDebugInfo)
        {
            Debug.Log($"[FinishLine] >>> OnTriggerEnter2D called! (Attempt #{triggerAttempts})");
            Debug.Log($"[FinishLine]     Object: {other.gameObject.name}");
            Debug.Log($"[FinishLine]     Tag: '{other.tag}'");
            Debug.Log($"[FinishLine]     Layer: {LayerMask.LayerToName(other.gameObject.layer)}");
        }
        
        // Check tag
        if (other.CompareTag("Player"))
        {
            if (showDebugInfo)
            {
                Debug.Log("[FinishLine] ✓ Tag matches 'Player'");
            }
            
            if (!hasTriggered)
            {
                hasTriggered = true;
                
                Debug.Log("<color=green>[FinishLine] ★★★ FINISH LINE CROSSED! ★★★</color>");
                
                // Check GameManager
                if (GameManager.Instance == null)
                {
                    Debug.LogError("[FinishLine] GameManager.Instance is NULL!");
                    Debug.LogError("  → Make sure GameManager exists in scene");
                }
                else
                {
                    Debug.Log($"[FinishLine] Calling GameManager.OnLevelComplete()...");
                    Debug.Log($"[FinishLine] Current State BEFORE: {GameManager.Instance.currentState}");
                    
                    GameManager.Instance.OnLevelComplete();
                    
                    Debug.Log($"[FinishLine] Current State AFTER: {GameManager.Instance.currentState}");
                    
                    if (GameManager.Instance.currentState != GameState.Completed)
                    {
                        Debug.LogError("[FinishLine] State did NOT change to Completed!");
                        Debug.LogError("  → Check GameManager.OnLevelComplete() method");
                    }
                }
            }
            else
            {
                if (showDebugInfo)
                {
                    Debug.Log("[FinishLine] Already triggered, ignoring");
                }
            }
        }
        else
        {
            if (showDebugInfo)
            {
                Debug.Log($"[FinishLine] ✗ Tag mismatch: Expected 'Player', got '{other.tag}'");
            }
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        // This fires every frame while overlapping
        if (showDebugInfo && !hasTriggered)
        {
            Debug.Log($"[FinishLine] OnTriggerStay2D: {other.gameObject.name} (Frame {Time.frameCount})");
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (showDebugInfo)
        {
            Debug.Log($"[FinishLine] OnTriggerExit2D: {other.gameObject.name}");
        }
    }

    // Alternative detection using collision instead of trigger
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (showDebugInfo)
        {
            Debug.LogWarning($"[FinishLine] OnCollisionEnter2D called with {collision.gameObject.name}");
            Debug.LogWarning("  → This means collider is NOT set as Trigger!");
            Debug.LogWarning("  → Trigger events won't fire. Check 'Is Trigger' setting.");
        }
    }

    // Visualize finish line in Scene view
    void OnDrawGizmos()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            // Change color based on trigger status
            if (Application.isPlaying)
            {
                Gizmos.color = hasTriggered ? Color.blue : gizmoColor;
            }
            else
            {
                Gizmos.color = col.isTrigger ? gizmoColor : Color.red;
            }
            
            if (col is BoxCollider2D box)
            {
                Vector3 size = new Vector3(box.size.x, box.size.y, 0.1f);
                Vector3 center = transform.position + (Vector3)box.offset;
                
                Gizmos.DrawWireCube(center, size);
                Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.3f);
                Gizmos.DrawCube(center, size);
            }
            else if (col is CircleCollider2D circle)
            {
                Gizmos.DrawWireSphere(transform.position + (Vector3)circle.offset, circle.radius);
            }
            
            // Draw finish flag icon
            Gizmos.color = Color.white;
            Vector3 flagPos = transform.position + Vector3.up * 2f;
            Gizmos.DrawLine(flagPos, flagPos + Vector3.up * 2f);
            Gizmos.DrawLine(flagPos + Vector3.up * 2f, flagPos + Vector3.up * 2f + Vector3.right * 1f);
            
            // Draw trigger count if playing
            if (Application.isPlaying && triggerAttempts > 0)
            {
                Gizmos.color = Color.yellow;
                Vector3 textPos = transform.position + Vector3.up * 4f;
                Gizmos.DrawWireSphere(textPos, 0.3f);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        // Draw detection range when selected
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 2f);
        
        // Draw layer info
        if (Application.isPlaying)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 5f, Vector3.one * 0.5f);
        }
    }
    
    // Manual test from Inspector
    [ContextMenu("Test Finish Trigger")]
    public void TestFinishTrigger()
    {
        Debug.Log("=== MANUAL FINISH TEST ===");
        hasTriggered = false;
        
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Collider2D playerCollider = player.GetComponent<Collider2D>();
            if (playerCollider != null)
            {
                OnTriggerEnter2D(playerCollider);
            }
            else
            {
                Debug.LogError("Player has no Collider2D!");
            }
        }
        else
        {
            Debug.LogError("No object with 'Player' tag found!");
        }
    }
}