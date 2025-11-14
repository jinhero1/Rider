using UnityEngine;

public class FinishLine : MonoBehaviour
{
    private GameEventSystem eventSystem;
    private IGameStateService gameStateService;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private Color gizmoColor = Color.green;
    
    private bool hasTriggered = false;
    private int triggerAttempts = 0;
    private Collider2D myCollider;

    #region Initialization

    private void Awake()
    {
        InitializeServices();
    }

    private void Start()
    {
        ValidateSetup();
        SubscribeToEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void InitializeServices()
    {
        eventSystem = ServiceLocator.Instance.Get<GameEventSystem>();
        gameStateService = ServiceLocator.Instance.Get<IGameStateService>();

        if (eventSystem == null)
        {
            Debug.LogError("[FinishLine] GameEventSystem not found!");
        }
    }

    private void ValidateSetup()
    {
        myCollider = GetComponent<Collider2D>();
        
        if (myCollider == null)
        {
            Debug.LogError("[FinishLine] No Collider2D found!");
        }
        else
        {
            if (showDebugInfo)
            {
                Debug.Log($"[FinishLine] Collider: {myCollider.GetType().Name}");
            }
            
            if (!myCollider.isTrigger)
            {
                Debug.LogError("[FinishLine] Collider is NOT set as Trigger!");
                Debug.LogError("  → Fix: Select FinishLine → Check 'Is Trigger'");
            }
            else if (showDebugInfo)
            {
                Debug.Log("[FinishLine] ✓ Collider is set as Trigger");
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[FinishLine] Initialized at {transform.position}");
            Debug.Log($"[FinishLine] Active: {gameObject.activeInHierarchy}");
            Debug.Log($"[FinishLine] Layer: {LayerMask.LayerToName(gameObject.layer)}");
        }
    }

    private void SubscribeToEvents()
    {
        if (eventSystem == null) return;

        eventSystem.Subscribe<LevelRestartRequestedEvent>(OnLevelRestart);
    }

    private void UnsubscribeFromEvents()
    {
        if (eventSystem == null) return;

        eventSystem.Unsubscribe<LevelRestartRequestedEvent>(OnLevelRestart);
    }

    #endregion

    #region Event Handlers

    private void OnLevelRestart(LevelRestartRequestedEvent evt)
    {
        ResetFinishLine();
    }

    #endregion

    #region Collision Detection

    private void OnTriggerEnter2D(Collider2D other)
    {
        triggerAttempts++;
        
        if (showDebugInfo)
        {
            Debug.Log($"[FinishLine] >>> Trigger #{triggerAttempts}");
            Debug.Log($"[FinishLine]     Object: {other.gameObject.name}");
            Debug.Log($"[FinishLine]     Tag: '{other.tag}'");
            Debug.Log($"[FinishLine]     Layer: {LayerMask.LayerToName(other.gameObject.layer)}");
        }
        
        if (other.CompareTag(GameConstants.Tags.PLAYER))
        {
            if (showDebugInfo)
            {
                Debug.Log("[FinishLine] ✓ Tag matches 'Player'");
            }
            
            if (!hasTriggered)
            {
                TriggerLevelComplete(other.gameObject);
            }
            else if (showDebugInfo)
            {
                Debug.Log("[FinishLine] Already triggered, ignoring");
            }
        }
        else if (showDebugInfo)
        {
            Debug.Log($"[FinishLine] ✗ Tag mismatch: Expected 'Player', got '{other.tag}'");
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (showDebugInfo && !hasTriggered)
        {
            Debug.Log($"[FinishLine] OnTriggerStay2D: {other.gameObject.name} (Frame {Time.frameCount})");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (showDebugInfo)
        {
            Debug.Log($"[FinishLine] OnTriggerExit2D: {other.gameObject.name}");
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (showDebugInfo)
        {
            Debug.LogWarning($"[FinishLine] OnCollisionEnter2D called with {collision.gameObject.name}");
            Debug.LogWarning("  → Collider is NOT set as Trigger!");
        }
    }

    #endregion

    #region Level Completion

    private void TriggerLevelComplete(GameObject player)
    {
        hasTriggered = true;
        
        Debug.Log("<color=green>[FinishLine] ★★★ FINISH LINE CROSSED! ★★★</color>");
        
        if (gameStateService == null)
        {
            Debug.LogError("[FinishLine] GameStateService is NULL!");
            return;
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[FinishLine] State BEFORE: {gameStateService.CurrentState}");
        }
        
        // Get player info
        IPlayerController playerController = player.GetComponent<IPlayerController>();
        int flipCount = playerController?.FlipCount ?? 0;
        
        IScoreService scoreService = ServiceLocator.Instance.Get<IScoreService>();
        int finalScore = scoreService?.CurrentScore ?? 0;
        
        ICollectibleService collectibleService = ServiceLocator.Instance.Get<ICollectibleService>();
        int collectiblesCollected = collectibleService?.CollectedCount ?? 0;
        
        // Publish level completed event
        eventSystem?.Publish(new LevelCompletedEvent
        {
            FinalScore = finalScore,
            FlipCount = flipCount,
            CollectiblesCollected = collectiblesCollected,
            CompletionTime = Time.time
        });
        
        if (showDebugInfo)
        {
            Debug.Log($"[FinishLine] State AFTER: {gameStateService.CurrentState}");
        }
    }

    public void ResetFinishLine()
    {
        hasTriggered = false;
        
        if (showDebugInfo)
        {
            Debug.Log("[FinishLine] ✓ Reset - ready to trigger again");
        }
    }

    #endregion

    #region Debug Visualization

    private void OnDrawGizmos()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return;

        // Change color based on status
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
        
        // Draw finish flag
        Gizmos.color = Color.white;
        Vector3 flagPos = transform.position + Vector3.up * GameConstants.Gizmos.FINISH_LINE_FLAG_HEIGHT;
        Gizmos.DrawLine(flagPos, flagPos + Vector3.up * GameConstants.Gizmos.FINISH_LINE_FLAG_HEIGHT);
        Gizmos.DrawLine(
            flagPos + Vector3.up * GameConstants.Gizmos.FINISH_LINE_FLAG_HEIGHT, 
            flagPos + Vector3.up * GameConstants.Gizmos.FINISH_LINE_FLAG_HEIGHT + Vector3.right * GameConstants.Gizmos.FINISH_LINE_FLAG_WIDTH
        );
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, GameConstants.Gizmos.FINISH_LINE_DETECTION_RADIUS);
    }

    #endregion

    #region Debug Context Menu

    [ContextMenu("Test Finish Trigger")]
    private void TestFinishTrigger()
    {
        Debug.Log("=== MANUAL FINISH TEST ===");
        hasTriggered = false;
        
        GameObject player = GameObject.FindGameObjectWithTag(GameConstants.Tags.PLAYER);
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

    #endregion
}
