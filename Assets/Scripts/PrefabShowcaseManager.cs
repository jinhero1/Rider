using UnityEngine;
using System.Collections;

/// <summary>
/// Manages showcase tracks using Prefab instantiation (faster, more efficient)
/// </summary>
public class PrefabShowcaseManager : MonoBehaviour
{
    public static PrefabShowcaseManager Instance;
    
    [Header("Track Configuration")]
    [SerializeField] private TrackData[] tracks;
    [SerializeField] private int currentTrackIndex = 0;
    [SerializeField] private bool enableCircularNavigation = true;
    
    [Header("Track Container")]
    [SerializeField] private Transform trackContainer;
    [SerializeField] private bool createContainerIfMissing = true;
    
    [Header("Track Display")]
    [SerializeField] private ShowcaseLevelUI levelUI;
    [SerializeField] private bool showTrackTitleOnLoad = true;
    [SerializeField] private float titleDisplayDuration = 3f;
    
    [Header("Transition")]
    [SerializeField] private bool useFadeTransition = true;
    [SerializeField] private float transitionDuration = 0.3f;
    [SerializeField] private CanvasGroup transitionPanel;
    
    [Header("Bike Settings")]
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
        
        // Create track container if missing
        if (trackContainer == null && createContainerIfMissing)
        {
            GameObject container = new GameObject("TrackContainer");
            trackContainer = container.transform;
            trackContainer.SetParent(transform);
        }
        
        // Find bike if not assigned
        if (bikeController == null)
        {
            bikeController = FindFirstObjectByType<BikeController>();
        }
        
        // Create transition panel if missing
        if (transitionPanel == null && useFadeTransition)
        {
            CreateTransitionPanel();
        }
    }

    void Start()
    {
        // Validate tracks
        if (tracks == null || tracks.Length == 0)
        {
            Debug.LogError("[PrefabShowcaseManager] No tracks assigned!");
            return;
        }
        
        // Load the first track
        LoadTrack(currentTrackIndex, false);
        
        if (showDebug)
        {
            Debug.Log($"[PrefabShowcaseManager] Initialized with {tracks.Length} tracks");
        }
    }

    /// <summary>
    /// Load a track by index
    /// </summary>
    public void LoadTrack(int trackIndex, bool useTransition = true)
    {
        if (isTransitioning) return;
        
        // Validate index
        if (trackIndex < 0 || trackIndex >= tracks.Length)
        {
            Debug.LogError($"[PrefabShowcaseManager] Invalid track index: {trackIndex}");
            return;
        }
        
        currentTrackIndex = trackIndex;
        currentTrack = tracks[trackIndex];
        
        // Validate track data
        if (!currentTrack.IsValid())
        {
            return;
        }
        
        if (showDebug)
        {
            Debug.Log($"[PrefabShowcaseManager] Loading track {trackIndex}: {currentTrack.TrackName}");
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

    /// <summary>
    /// Load the next track in sequence
    /// </summary>
    public void LoadNextTrack()
    {
        int nextIndex = currentTrackIndex + 1;
        
        // Circular navigation
        if (enableCircularNavigation && nextIndex >= tracks.Length)
        {
            nextIndex = 0;
            
            if (showDebug)
            {
                Debug.Log("[PrefabShowcaseManager] Reached end, cycling back to first track");
            }
        }
        else if (nextIndex >= tracks.Length)
        {
            Debug.LogWarning("[PrefabShowcaseManager] Already at last track");
            return;
        }
        
        LoadTrack(nextIndex);
    }

    /// <summary>
    /// Load the previous track in sequence
    /// </summary>
    public void LoadPreviousTrack()
    {
        int prevIndex = currentTrackIndex - 1;
        
        // Circular navigation
        if (enableCircularNavigation && prevIndex < 0)
        {
            prevIndex = tracks.Length - 1;
            
            if (showDebug)
            {
                Debug.Log("[PrefabShowcaseManager] At first track, cycling to last track");
            }
        }
        else if (prevIndex < 0)
        {
            Debug.LogWarning("[PrefabShowcaseManager] Already at first track");
            return;
        }
        
        LoadTrack(prevIndex);
    }

    /// <summary>
    /// Load track immediately without transition
    /// </summary>
    private void LoadTrackImmediate(TrackData track)
    {
        // Destroy current track
        if (currentTrackInstance != null)
        {
            Destroy(currentTrackInstance);
        }
        
        // Instantiate new track
        currentTrackInstance = Instantiate(track.TrackPrefab, trackContainer);
        currentTrackInstance.name = track.TrackName;
        
        // Reset bike position
        if (resetBikeOnTrackChange && bikeController != null)
        {
            bikeController.transform.position = track.SpawnPosition;
            bikeController.ResetBike();
        }
        
        // Show track UI
        ShowTrackUI(track);
        
        // Preload next track
        if (preloadNextTrack)
        {
            PreloadNext();
        }
        
        if (showDebug)
        {
            Debug.Log($"[PrefabShowcaseManager] Track loaded: {track.TrackTitle}");
        }
    }

    /// <summary>
    /// Load track with fade transition
    /// </summary>
    private IEnumerator LoadTrackWithTransition(TrackData track)
    {
        isTransitioning = true;
        
        // Fade out
        if (transitionPanel != null)
        {
            yield return StartCoroutine(FadeTransition(0f, 1f, transitionDuration));
        }
        
        // Destroy current track
        if (currentTrackInstance != null)
        {
            Destroy(currentTrackInstance);
        }
        
        // Wait a frame
        yield return null;
        
        // Instantiate new track
        currentTrackInstance = Instantiate(track.TrackPrefab, trackContainer);
        currentTrackInstance.name = track.TrackName;
        
        // Reset bike position
        if (resetBikeOnTrackChange && bikeController != null)
        {
            bikeController.transform.position = track.SpawnPosition;
            bikeController.ResetBike();
        }
        
        // Wait a frame for physics to settle
        yield return null;
        
        // Fade in
        if (transitionPanel != null)
        {
            yield return StartCoroutine(FadeTransition(1f, 0f, transitionDuration));
        }
        
        // Show track UI
        ShowTrackUI(track);
        
        // Preload next track
        if (preloadNextTrack)
        {
            PreloadNext();
        }
        
        isTransitioning = false;
        
        if (showDebug)
        {
            Debug.Log($"[PrefabShowcaseManager] Track transition complete: {track.TrackTitle}");
        }
    }

    /// <summary>
    /// Show track UI after loading
    /// </summary>
    private void ShowTrackUI(TrackData track)
    {
        // Find UI if not assigned
        if (levelUI == null)
        {
            levelUI = FindFirstObjectByType<ShowcaseLevelUI>();
        }
        
        // Show track info
        if (levelUI != null && showTrackTitleOnLoad)
        {
            // Convert TrackData to LevelData format for UI
            levelUI.ShowTrackInfo(
                track.TrackTitle,
                currentTrackIndex + 1,
                tracks.Length,
                titleDisplayDuration
            );
        }
    }

    /// <summary>
    /// Preload next track for faster switching
    /// </summary>
    private void PreloadNext()
    {
        if (!preloadNextTrack) return;
        
        int nextIndex = (currentTrackIndex + 1) % tracks.Length;
        TrackData nextTrack = tracks[nextIndex];
        
        if (nextTrack != null && nextTrack.TrackPrefab != null)
        {
            // Destroy old preloaded track
            if (preloadedTrack != null)
            {
                Destroy(preloadedTrack);
            }
            
            // Instantiate but keep inactive
            preloadedTrack = Instantiate(nextTrack.TrackPrefab);
            preloadedTrack.SetActive(false);
            preloadedTrack.name = $"Preloaded_{nextTrack.TrackName}";
            
            if (showDebug)
            {
                Debug.Log($"[PrefabShowcaseManager] Preloaded next track: {nextTrack.TrackName}");
            }
        }
    }

    /// <summary>
    /// Fade transition coroutine
    /// </summary>
    private IEnumerator FadeTransition(float startAlpha, float endAlpha, float duration)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            if (transitionPanel != null)
            {
                transitionPanel.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            }
            
            yield return null;
        }
        
        if (transitionPanel != null)
        {
            transitionPanel.alpha = endAlpha;
        }
    }

    /// <summary>
    /// Create transition panel if not assigned
    /// </summary>
    private void CreateTransitionPanel()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[PrefabShowcaseManager] No Canvas found, cannot create transition panel");
            return;
        }
        
        GameObject panelObj = new GameObject("TransitionPanel");
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
            Debug.Log("[PrefabShowcaseManager] Created transition panel");
        }
    }

    /// <summary>
    /// Get current track data
    /// </summary>
    public TrackData GetCurrentTrack()
    {
        return currentTrack;
    }

    /// <summary>
    /// Get total number of tracks
    /// </summary>
    public int GetTotalTracks()
    {
        return tracks != null ? tracks.Length : 0;
    }

    /// <summary>
    /// Get current track index
    /// </summary>
    public int GetCurrentTrackIndex()
    {
        return currentTrackIndex;
    }

    /// <summary>
    /// Check if this is the last track
    /// </summary>
    public bool IsLastTrack()
    {
        return currentTrackIndex >= tracks.Length - 1;
    }

    /// <summary>
    /// Check if this is the first track
    /// </summary>
    public bool IsFirstTrack()
    {
        return currentTrackIndex <= 0;
    }

    // Hotkeys for testing
    void Update()
    {
        if (showDebug)
        {
            // Press N for next track
            if (Input.GetKeyDown(KeyCode.N))
            {
                LoadNextTrack();
            }
            
            // Press P for previous track
            if (Input.GetKeyDown(KeyCode.P))
            {
                LoadPreviousTrack();
            }
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        
        // Clean up preloaded track
        if (preloadedTrack != null)
        {
            Destroy(preloadedTrack);
        }
    }
}
