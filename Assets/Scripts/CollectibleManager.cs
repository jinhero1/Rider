using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages all collectible items in the level
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
            Collectible[] foundCollectibles = FindObjectsByType<Collectible>(FindObjectsSortMode.None);
            allCollectibles.Clear();
            allCollectibles.AddRange(foundCollectibles);
            
            if (showDebug)
            {
                Debug.Log($"[CollectibleManager] Auto-found {foundCollectibles.Length} collectibles");
            }
        }
        
        totalCollectibles = allCollectibles.Count;
        collectedCount = 0;
        
        if (showDebug)
        {
            Debug.Log($"[CollectibleManager] Initialized with {totalCollectibles} collectibles");
        }
        
        // Update UI
        UpdateUI();
    }

    /// <summary>
    /// Called when a collectible is collected
    /// </summary>
    public void OnCollectibleCollected(Collectible collectible)
    {
        if (!allCollectibles.Contains(collectible))
        {
            Debug.LogWarning($"[CollectibleManager] Collected item not in list: {collectible.gameObject.name}");
            return;
        }
        
        collectedCount++;
        
        if (showDebug)
        {
            Debug.Log($"[CollectibleManager] ✦ Collected {collectedCount}/{totalCollectibles}");
        }
        
        // Update UI
        UpdateUI();
        
        // Check if all collected
        if (collectedCount >= totalCollectibles)
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
    /// Reset all collectibles (for level restart)
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
        
        // Note: 根據需求，重新開始時不重置收集數
        // collectedCount = 0;
        
        if (showDebug)
        {
            Debug.Log($"[CollectibleManager] All collectibles reset. Current count: {collectedCount}/{totalCollectibles}");
        }
        
        UpdateUI();
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
        return collectedCount >= totalCollectibles;
    }

    // Manual reset from Inspector for testing
    [ContextMenu("Reset All Collectibles")]
    public void ManualResetCollectibles()
    {
        ResetAllCollectibles();
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
}
