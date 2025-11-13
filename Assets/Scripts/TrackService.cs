using UnityEngine;
using System.Collections;

/// <summary>
/// Refactored track service using Prefab instantiation
/// Single Responsibility: Track loading and management only
/// Implements ITrackService interface
/// </summary>
public class TrackService : MonoBehaviour, ITrackService
{
    private GameEventSystem eventSystem;
    private IGameStateService gameStateService;

    [Header("Track Configuration")]
    [SerializeField] private TrackData[] tracks;
    [SerializeField] private int currentTrackIndex = 0;
    [SerializeField] private bool enableCircularNavigation = true;
    
    [Header("Track Container")]
    [SerializeField] private Transform trackContainer;
    [SerializeField] private bool createContainerIfMissing = true;
    
    [Header("Transition")]
    [SerializeField] private bool useFadeTransition = true;
    [SerializeField] private float transitionDuration = GameConstants.UI.TRANSITION_FADE_DURATION;
    [SerializeField] private CanvasGroup transitionPanel;
    
    [Header("Player Settings")]
    [SerializeField] private BikeController bikeController;
    [SerializeField] private bool resetBikeOnTrackChange = true;
    
    [Header("Preloading")]
    [SerializeField] private bool preloadNextTrack = true;
    [SerializeField] private GameObject preloadedTrack;
    
    [Header("Debug")]
    [SerializeField] private bool showDebug = true;
    
    private TrackData currentTrack;
    private GameObject currentTrackInstance;
    private bool isTransitioning = false;

    // ITrackService implementation
    public int CurrentTrackIndex => currentTrackIndex;
    public int TotalTracks => tracks != null ? tracks.Length : 0;

    #region Initialization

    private void Awake()
    {
        InitializeServices();
        SetupTrackContainer();
        CreateTransitionPanelIfNeeded();
    }

    private void Start()
    {
        ValidateTracks();
        SubscribeToEvents();
        LoadInitialTrack();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
        CleanupPreloadedTrack();
    }

    private void InitializeServices()
    {
        eventSystem = ServiceLocator.Instance.Get<GameEventSystem>();
        gameStateService = ServiceLocator.Instance.Get<IGameStateService>();

        if (eventSystem == null)
        {
            Debug.LogError("[TrackService] GameEventSystem not found!");
        }

        // Register this service
        ServiceLocator.Instance.Register<ITrackService>(this);
        
        if (showDebug)
        {
            Debug.Log("[TrackService] Service registered");
        }
    }

    private void SetupTrackContainer()
    {
        if (trackContainer == null && createContainerIfMissing)
        {
            GameObject container = new GameObject("TrackContainer");
            trackContainer = container.transform;
            trackContainer.SetParent(transform);
        }
    }

    private void ValidateTracks()
    {
        if (tracks == null || tracks.Length == 0)
        {
            Debug.LogError("[TrackService] No tracks assigned!");
            return;
        }
        
        if (showDebug)
        {
            Debug.Log($"[TrackService] Initialized with {tracks.Length} tracks");
        }
    }

    private void SubscribeToEvents()
    {
        if (eventSystem == null) return;

        eventSystem.Subscribe<NextLevelRequestedEvent>(OnNextLevelRequested);
        eventSystem.Subscribe<LevelRestartRequestedEvent>(OnLevelRestart);
    }

    private void UnsubscribeFromEvents()
    {
        if (eventSystem == null) return;

        eventSystem.Unsubscribe<NextLevelRequestedEvent>(OnNextLevelRequested);
        eventSystem.Unsubscribe<LevelRestartRequestedEvent>(OnLevelRestart);
    }

    private void LoadInitialTrack()
    {
        LoadTrack(currentTrackIndex, false);
    }

    #endregion

    #region Event Handlers

    private void OnNextLevelRequested(NextLevelRequestedEvent evt)
    {
        LoadNextTrack();
    }

    private void OnLevelRestart(LevelRestartRequestedEvent evt)
    {
        // Track reload is handled by GameCoordinator
        // Just refresh collectibles/letters
        RefreshTrackContent();
    }

    #endregion

    #region ITrackService Implementation

    public void LoadTrack(int trackIndex, bool useTransition = true)
    {
        if (isTransitioning)
        {
            if (showDebug)
            {
                //Debug.LogWarning("[TrackService] Already transitioning");
            }
            return;
        }
        
        if (!ValidateTrackIndex(trackIndex)) return;
        
        currentTrackIndex = trackIndex;
        currentTrack = tracks[trackIndex];
        
        if (!currentTrack.IsValid())
        {
            Debug.LogError($"[TrackService] Track {trackIndex} is invalid!");
            return;
        }
        
        if (showDebug)
        {
            Debug.Log($"[TrackService] Loading track {trackIndex}: {currentTrack.TrackName}");
        }
        
        if (useTransition && useFadeTransition)
        {
            StartCoroutine(LoadTrackWithTransition(currentTrack));
        }
        else
        {
            LoadTrackImmediate(currentTrack);
        }
    }

    public void LoadNextTrack()
    {
        int nextIndex = currentTrackIndex + 1;
        
        if (enableCircularNavigation && nextIndex >= tracks.Length)
        {
            nextIndex = 0;
            
            if (showDebug)
            {
                Debug.Log("[TrackService] Cycling back to first track");
            }
        }
        else if (nextIndex >= tracks.Length)
        {
            Debug.LogWarning("[TrackService] Already at last track");
            return;
        }
        
        LoadTrack(nextIndex);
    }

    public void LoadPreviousTrack()
    {
        int prevIndex = currentTrackIndex - 1;
        
        if (enableCircularNavigation && prevIndex < 0)
        {
            prevIndex = tracks.Length - 1;
            
            if (showDebug)
            {
                Debug.Log("[TrackService] Cycling to last track");
            }
        }
        else if (prevIndex < 0)
        {
            Debug.LogWarning("[TrackService] Already at first track");
            return;
        }
        
        LoadTrack(prevIndex);
    }

    #endregion

    #region Track Loading

    private void LoadTrackImmediate(TrackData track)
    {
        DestroyCurrentTrack();
        InstantiateNewTrack(track);
        ResetBikePosition(track);
        RefreshTrackContent();
        PublishTrackLoadedEvent(track);
        PreloadNextTrackIfEnabled();
        
        if (showDebug)
        {
            Debug.Log($"[TrackService] Track loaded: {track.TrackTitle}");
        }
    }

    private IEnumerator LoadTrackWithTransition(TrackData track)
    {
        isTransitioning = true;
        
        // Fade out
        if (transitionPanel != null)
        {
            yield return StartCoroutine(FadeTransition(0f, 1f, transitionDuration));
        }
        
        DestroyCurrentTrack();
        yield return null;
        
        InstantiateNewTrack(track);
        ResetBikePosition(track);
        RefreshTrackContent();
        
        yield return null;
        
        // Fade in
        if (transitionPanel != null)
        {
            yield return StartCoroutine(FadeTransition(1f, 0f, transitionDuration));
        }
        
        PublishTrackLoadedEvent(track);
        PreloadNextTrackIfEnabled();
        
        isTransitioning = false;
        
        if (showDebug)
        {
            Debug.Log($"[TrackService] Track transition complete: {track.TrackTitle}");
        }
    }

    private void DestroyCurrentTrack()
    {
        if (currentTrackInstance != null)
        {
            Destroy(currentTrackInstance);
            currentTrackInstance = null;
        }
    }

    private void InstantiateNewTrack(TrackData track)
    {
        currentTrackInstance = Instantiate(track.TrackPrefab, trackContainer);
        currentTrackInstance.name = track.TrackName;
    }

    private void ResetBikePosition(TrackData track)
    {
        if (!resetBikeOnTrackChange) return;
        
        if (bikeController == null)
        {
            bikeController = FindFirstObjectByType<BikeController>();
        }
        
        if (bikeController != null)
        {
            bikeController.transform.position = track.SpawnPosition;
            bikeController.ResetPlayer();
        }
    }

    private void RefreshTrackContent()
    {
        // Collectibles
        ICollectibleService collectibleService = ServiceLocator.Instance.Get<ICollectibleService>();
        collectibleService?.RefreshCollectibles();
        
        // Bonus letters
        IBonusLetterService bonusLetterService = ServiceLocator.Instance.Get<IBonusLetterService>();
        bonusLetterService?.RefreshLetters();
        
        if (showDebug)
        {
            Debug.Log("[TrackService] Track content refreshed");
        }
    }

    private void PublishTrackLoadedEvent(TrackData track)
    {
        if (eventSystem == null) return;

        eventSystem.Publish(new TrackLoadedEvent
        {
            TrackName = track.TrackName,
            TrackIndex = currentTrackIndex,
            SpawnPosition = track.SpawnPosition
        });
    }

    #endregion

    #region Preloading

    private void PreloadNextTrackIfEnabled()
    {
        if (!preloadNextTrack) return;
        
        int nextIndex = (currentTrackIndex + 1) % tracks.Length;
        TrackData nextTrack = tracks[nextIndex];
        
        if (nextTrack != null && nextTrack.TrackPrefab != null)
        {
            CleanupPreloadedTrack();
            
            preloadedTrack = Instantiate(nextTrack.TrackPrefab);
            preloadedTrack.SetActive(false);
            preloadedTrack.name = $"Preloaded_{nextTrack.TrackName}";
            
            if (showDebug)
            {
                Debug.Log($"[TrackService] Preloaded next track: {nextTrack.TrackName}");
            }
        }
    }

    private void CleanupPreloadedTrack()
    {
        if (preloadedTrack != null)
        {
            Destroy(preloadedTrack);
            preloadedTrack = null;
        }
    }

    #endregion

    #region Transition Effects

    private IEnumerator FadeTransition(float startAlpha, float endAlpha, float duration)
    {
        if (transitionPanel == null) yield break;

        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            transitionPanel.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            
            yield return null;
        }
        
        transitionPanel.alpha = endAlpha;
    }

    private void CreateTransitionPanelIfNeeded()
    {
        if (transitionPanel != null || !useFadeTransition) return;
        
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[TrackService] No Canvas found, cannot create transition panel");
            return;
        }
        
        GameObject panelObj = new GameObject("TrackTransitionPanel");
        panelObj.transform.SetParent(canvas.transform, false);
        
        RectTransform rect = panelObj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        
        UnityEngine.UI.Image image = panelObj.AddComponent<UnityEngine.UI.Image>();
        image.color = Color.black;
        
        transitionPanel = panelObj.AddComponent<CanvasGroup>();
        transitionPanel.alpha = 0f;
        transitionPanel.blocksRaycasts = false;
        
        if (showDebug)
        {
            Debug.Log("[TrackService] Created transition panel");
        }
    }

    #endregion

    #region Validation

    private bool ValidateTrackIndex(int index)
    {
        if (index < 0 || index >= tracks.Length)
        {
            Debug.LogError($"[TrackService] Invalid track index: {index}");
            return false;
        }
        return true;
    }

    #endregion

    #region Public API

    public TrackData GetCurrentTrack()
    {
        return currentTrack;
    }

    public bool IsLastTrack()
    {
        return currentTrackIndex >= tracks.Length - 1;
    }

    public bool IsFirstTrack()
    {
        return currentTrackIndex <= 0;
    }

    #endregion

    #region Hotkeys

    private void Update()
    {
        if (!showDebug) return;

        // Debug hotkeys
        if (Input.GetKeyDown(KeyCode.N))
        {
            LoadNextTrack();
        }
        
        if (Input.GetKeyDown(KeyCode.P))
        {
            LoadPreviousTrack();
        }
    }

    #endregion

    #region Debug

    [ContextMenu("Load Next Track")]
    private void DebugLoadNext()
    {
        LoadNextTrack();
    }

    [ContextMenu("Load Previous Track")]
    private void DebugLoadPrevious()
    {
        LoadPreviousTrack();
    }

    #endregion
}
