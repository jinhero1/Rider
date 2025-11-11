using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages all spikes in the level for easy reset
/// </summary>
public class SpikeManager : MonoBehaviour
{
    public static SpikeManager Instance;
    
    [Header("Spike Management")]
    [SerializeField] private List<Spike> allSpikes = new List<Spike>();
    [SerializeField] private bool autoFindSpikes = true;
    
    [Header("Statistics")]
    [SerializeField] private int totalSpikes = 0;
    [SerializeField] private int spikeHits = 0;
    
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
        InitializeSpikes();
    }

    private void InitializeSpikes()
    {
        // Auto-find all spikes in the scene
        if (autoFindSpikes)
        {
            Spike[] foundSpikes = FindObjectsByType<Spike>(FindObjectsSortMode.None);
            allSpikes.Clear();
            allSpikes.AddRange(foundSpikes);
            
            if (showDebug)
            {
                Debug.Log($"[SpikeManager] Auto-found {foundSpikes.Length} spikes in scene");
            }
        }
        
        totalSpikes = allSpikes.Count;
        spikeHits = 0;
        
        if (showDebug)
        {
            Debug.Log($"[SpikeManager] Initialized with {totalSpikes} spikes");
        }
    }

    /// <summary>
    /// Called when a spike is hit (for statistics)
    /// </summary>
    public void OnSpikeHit(Spike spike)
    {
        spikeHits++;
        
        if (showDebug)
        {
            Debug.Log($"[SpikeManager] Spike hit! Total hits: {spikeHits}");
        }
    }

    /// <summary>
    /// Reset all spikes for level restart
    /// </summary>
    public void ResetAllSpikes()
    {
        foreach (Spike spike in allSpikes)
        {
            if (spike != null)
            {
                spike.ResetSpike();
            }
        }
        
        // Note: We don't reset spikeHits counter for statistics
        
        if (showDebug)
        {
            Debug.Log($"[SpikeManager] All {totalSpikes} spikes reset");
        }
    }

    /// <summary>
    /// Get total number of spikes in level
    /// </summary>
    public int GetTotalSpikes()
    {
        return totalSpikes;
    }

    /// <summary>
    /// Get number of times spikes were hit
    /// </summary>
    public int GetSpikeHits()
    {
        return spikeHits;
    }

    /// <summary>
    /// Add a spike to the managed list
    /// </summary>
    public void RegisterSpike(Spike spike)
    {
        if (!allSpikes.Contains(spike))
        {
            allSpikes.Add(spike);
            totalSpikes = allSpikes.Count;
            
            if (showDebug)
            {
                Debug.Log($"[SpikeManager] Spike registered: {spike.gameObject.name}");
            }
        }
    }

    /// <summary>
    /// Remove a spike from the managed list
    /// </summary>
    public void UnregisterSpike(Spike spike)
    {
        if (allSpikes.Contains(spike))
        {
            allSpikes.Remove(spike);
            totalSpikes = allSpikes.Count;
            
            if (showDebug)
            {
                Debug.Log($"[SpikeManager] Spike unregistered: {spike.gameObject.name}");
            }
        }
    }

    // Manual reset from Inspector
    [ContextMenu("Reset All Spikes")]
    public void ManualResetSpikes()
    {
        ResetAllSpikes();
    }

    [ContextMenu("Refresh Spike List")]
    public void RefreshSpikeList()
    {
        InitializeSpikes();
    }

    [ContextMenu("Reset Statistics")]
    public void ResetStatistics()
    {
        spikeHits = 0;
        Debug.Log("[SpikeManager] Statistics reset");
    }

    // Visualize spikes in Scene view
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        
        // Draw connection lines to all spikes
        Gizmos.color = Color.red;
        foreach (Spike spike in allSpikes)
        {
            if (spike != null)
            {
                Gizmos.DrawLine(transform.position, spike.transform.position);
            }
        }
        
        // Draw statistics indicator
        if (spikeHits > 0)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.5f);
        }
    }

    void OnDrawGizmosSelected()
    {
        #if UNITY_EDITOR
        // Show spike count
        if (Application.isPlaying)
        {
            string info = $"Spikes: {totalSpikes}\nHits: {spikeHits}";
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 3f,
                info
            );
        }
        #endif
    }
}
