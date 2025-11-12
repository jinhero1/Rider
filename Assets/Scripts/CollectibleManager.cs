using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages all collectible items in the level with Track Prefab support
/// </summary>
public class CollectibleManager : MonoBehaviour
{
    public static CollectibleManager Instance;
    
    [Header("Collectibles")]
    [SerializeField] private List<Collectible> allCollectibles = new List<Collectible>();
    [SerializeField] private bool autoFindCollectibles = true;
    
    [Header("Statistics")]
    [SerializeField] private int totalCollectibles = 0;
    [SerializeField] private int collectedCount = 0;
    
    [Header("Debug")]
    [SerializeField] private bool showDebug = true;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        InitializeCollectibles();
    }

    private void InitializeCollectibles()
    {
        // Auto-find all collectibles in the scene if enabled
        if (autoFindCollectibles)
        {
            FindAllCollectibles();
        }
        
        totalCollectibles = allCollectibles.Count;
        
        if (showDebug)
        {
            Debug.Log($"[CollectibleManager] Initialized with {totalCollectibles} collectibles. Collected: {collectedCount}");
        }
        
        // Update UI
        UpdateUI();
    }

    /// <summary>
    /// Find all collectibles in the scene (including in Track Prefabs)
    /// </summary>
    private void FindAllCollectibles()
    {
        Collectible[] foundCollectibles = FindObjectsByType<Collectible>(FindObjectsSortMode.None);
        allCollectibles.Clear();
        allCollectibles.AddRange(foundCollectibles);
        
        if (showDebug)
        {
            Debug.Log($"[CollectibleManager] Found {foundCollectibles.Length} collectibles in scene");
        }
    }

    /// <summary>
    /// Refresh collectibles list (call after loading new Track)
    /// </summary>
    public void RefreshCollectibles()
    {
        if (autoFindCollectibles)
        {
            FindAllCollectibles();
        }
        
        totalCollectibles = allCollectibles.Count;
        
        // Update UI
        UpdateUI();
        
        if (showDebug)
        {
            Debug.Log($"[CollectibleManager] Refreshed. Total: {totalCollectibles}, Collected: {collectedCount}");
        }
    }

    /// <summary>
    /// Called when a collectible is collected
    /// </summary>
    public void OnCollectibleCollected(Collectible collectible)
    {
        if (collectible == null)
        {
            Debug.LogError("[CollectibleManager] OnCollectibleCollected called with NULL collectible!");
            return;
        }
        
        if (!allCollectibles.Contains(collectible))
        {
            if (showDebug)
            {
                Debug.LogWarning($"[CollectibleManager] Collected item not in list: {collectible.gameObject.name}. Refreshing list...");
            }
            
            // Refresh the list and try again
            RefreshCollectibles();
            
            if (!allCollectibles.Contains(collectible))
            {
                Debug.LogError($"[CollectibleManager] Collectible {collectible.gameObject.name} still not found after refresh!");
                return;
            }
        }
        
        collectedCount++;
        
        if (showDebug)
        {
            Debug.Log($"[CollectibleManager] ✦ Collected {collectedCount}/{totalCollectibles}");
        }
        
        // Update UI
        UpdateUI();
        
        // Check if all collected
        if (collectedCount >= totalCollectibles && totalCollectibles > 0)
        {
            OnAllCollected();
        }
    }

    private void OnAllCollected()
    {
        if (showDebug)
        {
            Debug.Log("<color=yellow>[CollectibleManager] ★★★ ALL COLLECTIBLES COLLECTED! ★★★</color>");
        }
        
        // Optional: Trigger bonus or achievement
        // GameManager.Instance?.OnAllCollectiblesCollected();
    }

    private void UpdateUI()
    {
        if (GameManager.Instance != null && GameManager.Instance.uiManager != null)
        {
            GameManager.Instance.uiManager.UpdateCollectibleCount(collectedCount, totalCollectibles);
        }
    }

    /// <summary>
    /// Reset all collectibles (for level restart - keeps collected count)
    /// </summary>
    public void ResetAllCollectibles()
    {
        foreach (Collectible collectible in allCollectibles)
        {
            if (collectible != null)
            {
                collectible.ResetCollectible();
            }
        }
        
        // Note: Keep collected count for same level replay
        // Don't reset collectedCount here
        
        if (showDebug)
        {
            Debug.Log($"[CollectibleManager] All collectibles reset. Current count: {collectedCount}/{totalCollectibles}");
        }
        
        UpdateUI();
    }

    /// <summary>
    /// Full reset (clear collected count - for new level/track)
    /// </summary>
    public void FullReset()
    {
        // Clear collected count
        collectedCount = 0;
        
        // Reset all collectibles
        foreach (Collectible collectible in allCollectibles)
        {
            if (collectible != null)
            {
                collectible.ResetCollectible();
            }
        }
        
        // Update UI
        UpdateUI();
        
        if (showDebug)
        {
            Debug.Log($"[CollectibleManager] Full reset complete. Total: {totalCollectibles}, Collected: 0");
        }
    }

    /// <summary>
    /// Get current collected count
    /// </summary>
    public int GetCollectedCount()
    {
        return collectedCount;
    }

    /// <summary>
    /// Get total collectibles in level
    /// </summary>
    public int GetTotalCount()
    {
        return totalCollectibles;
    }

    /// <summary>
    /// Get collection progress as percentage
    /// </summary>
    public float GetCollectionProgress()
    {
        if (totalCollectibles == 0) return 0f;
        return (float)collectedCount / totalCollectibles;
    }

    /// <summary>
    /// Check if all collectibles are collected
    /// </summary>
    public bool AreAllCollected()
    {
        return totalCollectibles > 0 && collectedCount >= totalCollectibles;
    }

    // Manual controls from Inspector
    [ContextMenu("Refresh Collectibles")]
    public void ManualRefresh()
    {
        RefreshCollectibles();
    }

    [ContextMenu("Reset All Collectibles")]
    public void ManualResetCollectibles()
    {
        ResetAllCollectibles();
    }

    [ContextMenu("Full Reset")]
    public void ManualFullReset()
    {
        FullReset();
    }

    [ContextMenu("Collect All (Test)")]
    public void TestCollectAll()
    {
        foreach (Collectible collectible in allCollectibles)
        {
            if (collectible != null && !collectible.IsCollected())
            {
                collectible.gameObject.SetActive(false);
            }
        }
        collectedCount = totalCollectibles;
        UpdateUI();
        Debug.Log("[CollectibleManager] TEST: All collectibles collected");
    }

    // Visualize in Scene view
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        
        // Draw progress bar in scene view
        Gizmos.color = Color.green;
        float progress = GetCollectionProgress();
        
        // Draw a simple progress indicator above the manager
        Vector3 barStart = transform.position + Vector3.up * 2f + Vector3.left * 2f;
        Vector3 barEnd = barStart + Vector3.right * 4f;
        
        // Background
        Gizmos.color = Color.gray;
        Gizmos.DrawLine(barStart, barEnd);
        
        // Progress
        Gizmos.color = Color.green;
        Vector3 progressEnd = Vector3.Lerp(barStart, barEnd, progress);
        Gizmos.DrawLine(barStart, progressEnd);
        
        // Draw collectible locations
        foreach (Collectible collectible in allCollectibles)
        {
            if (collectible != null)
            {
                Gizmos.color = collectible.IsCollected() ? Color.gray : Color.green;
                Gizmos.DrawWireSphere(collectible.transform.position, 0.3f);
            }
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
