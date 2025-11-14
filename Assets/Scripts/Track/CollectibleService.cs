using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Refactored collectible service
/// Manages all collectibles via events
/// Single Responsibility: Collectible management only
/// </summary>
public class CollectibleService : MonoBehaviour, ICollectibleService
{
    private GameEventSystem eventSystem;

    [Header("Collectible Management")]
    [SerializeField] private List<ICollectible> allCollectibles = new List<ICollectible>();
    [SerializeField] private bool autoFindCollectibles = true;
    
    [Header("Debug")]
    [SerializeField] private bool showDebug = true;

    private int collectedCount = 0;
    private int totalCount = 0;

    // ICollectibleService implementation
    public int CollectedCount => collectedCount;
    public int TotalCount => totalCount;
    public float CollectionProgress => totalCount > 0 ? (float)collectedCount / totalCount : 0f;

    #region Initialization

    private void Awake()
    {
        InitializeServices();
    }

    private void Start()
    {
        SubscribeToEvents();
        InitializeCollectibles();
        PublishInitialState();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void InitializeServices()
    {
        eventSystem = ServiceLocator.Instance.Get<GameEventSystem>();

        if (eventSystem == null)
        {
            Debug.LogError("[CollectibleService] GameEventSystem not found!");
        }

        // Register this service
        ServiceLocator.Instance.Register<ICollectibleService>(this);
    }

    private void SubscribeToEvents()
    {
        if (eventSystem == null) return;

        eventSystem.Subscribe<CollectibleCollectedEvent>(OnCollectibleCollected);
        eventSystem.Subscribe<TrackLoadedEvent>(OnTrackLoaded);
        eventSystem.Subscribe<LevelRestartRequestedEvent>(OnLevelRestart);
    }

    private void UnsubscribeFromEvents()
    {
        if (eventSystem == null) return;

        eventSystem.Unsubscribe<CollectibleCollectedEvent>(OnCollectibleCollected);
        eventSystem.Unsubscribe<TrackLoadedEvent>(OnTrackLoaded);
        eventSystem.Unsubscribe<LevelRestartRequestedEvent>(OnLevelRestart);
    }

    private void InitializeCollectibles()
    {
        if (autoFindCollectibles)
        {
            FindAllCollectibles();
        }
        
        totalCount = allCollectibles.Count;
        collectedCount = 0;
        
        if (showDebug)
        {
            Debug.Log($"[CollectibleService] Initialized with {totalCount} collectibles");
        }
    }

    private void PublishInitialState()
    {
        eventSystem?.Publish(new CollectibleProgressEvent
        {
            CollectedCount = collectedCount,
            TotalCount = totalCount
        });
    }

    #endregion

    #region Event Handlers

    private void OnCollectibleCollected(CollectibleCollectedEvent evt)
    {
        collectedCount++;
        
        if (showDebug)
        {
            Debug.Log($"[CollectibleService] ✦ Collected {collectedCount}/{totalCount}");
        }
        
        // Re-publish with updated counts
        eventSystem?.Publish(new CollectibleProgressEvent
        {
            CollectedCount = collectedCount,
            TotalCount = totalCount
        });
        
        // Check if all collected
        if (collectedCount >= totalCount && totalCount > 0)
        {
            OnAllCollected();
        }
    }

    private void OnTrackLoaded(TrackLoadedEvent evt)
    {
        if (showDebug)
        {
            Debug.Log("[CollectibleService] Track loaded, refreshing collectibles");
        }

        RefreshCollectibles();
    }

    private void OnLevelRestart(LevelRestartRequestedEvent evt)
    {
        if (showDebug)
        {
            Debug.Log("[CollectibleService] Level restarting, resetting collectibles");
        }

        ResetCollectibles();
    }

    private void OnAllCollected()
    {
        if (showDebug)
        {
            Debug.Log("<color=yellow>[CollectibleService] ★★★ ALL COLLECTIBLES COLLECTED! ★★★</color>");
        }
    }

    #endregion

    #region ICollectibleService Implementation

    public void RegisterCollectible(ICollectible collectible)
    {
        if (collectible == null) return;
        
        if (!allCollectibles.Contains(collectible))
        {
            allCollectibles.Add(collectible);
            totalCount = allCollectibles.Count;
            
            if (showDebug)
            {
                Debug.Log($"[CollectibleService] Registered collectible. Total: {totalCount}");
            }
        }
    }

    public void OnCollectibleCollected(ICollectible collectible)
    {
        // This is called when manually notifying the service
        // Event-based collection is handled in OnCollectibleCollected(event)
        if (collectible == null || !allCollectibles.Contains(collectible))
        {
            return;
        }

        collectedCount++;
        
        eventSystem?.Publish(new CollectibleCollectedEvent
        {
            CollectedCount = collectedCount,
            TotalCount = totalCount,
            Position = collectible.Position
        });
    }

    public void ResetCollectibles()
    {
        foreach (ICollectible collectible in allCollectibles)
        {
            collectible?.Reset();
        }
        
        // Don't reset collected count for same level replay
        // This maintains progress for retry attempts
        
        PublishInitialState();
        
        if (showDebug)
        {
            Debug.Log($"[CollectibleService] Collectibles reset. Count: {collectedCount}/{totalCount}");
        }
    }

    public void RefreshCollectibles()
    {
        if (autoFindCollectibles)
        {
            FindAllCollectibles();
        }
        
        // Full reset when loading new track
        collectedCount = 0;
        totalCount = allCollectibles.Count;
        
        PublishInitialState();
        
        if (showDebug)
        {
            Debug.Log($"[CollectibleService] Refreshed. Total: {totalCount}");
        }
    }

    #endregion

    #region Helper Methods

    private void FindAllCollectibles()
    {
        Collectible[] foundCollectibles = FindObjectsByType<Collectible>(FindObjectsSortMode.None);
        
        allCollectibles.Clear();
        allCollectibles.AddRange(foundCollectibles.Cast<ICollectible>());
        
        if (showDebug)
        {
            Debug.Log($"[CollectibleService] Found {foundCollectibles.Length} collectibles in scene");
        }
    }

    #endregion

    #region Debug Context Menu

    [ContextMenu("Refresh Collectibles")]
    private void ManualRefresh()
    {
        RefreshCollectibles();
    }

    [ContextMenu("Reset All Collectibles")]
    private void ManualReset()
    {
        ResetCollectibles();
    }

    [ContextMenu("Collect All (Test)")]
    private void TestCollectAll()
    {
        foreach (ICollectible collectible in allCollectibles)
        {
            if (!collectible.IsCollected)
            {
                collectible.Collect();
            }
        }
        
        Debug.Log("[CollectibleService] TEST: All collectibles collected");
    }

    #endregion

    #region Debug Visualization

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        
        // Draw progress bar
        Gizmos.color = Color.green;
        float progress = CollectionProgress;
        
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
        foreach (ICollectible collectible in allCollectibles)
        {
            if (collectible != null)
            {
                Gizmos.color = collectible.IsCollected ? Color.gray : Color.green;
                Gizmos.DrawWireSphere(collectible.Position, 0.3f);
            }
        }
    }

    #endregion
}
