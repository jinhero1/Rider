using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Refactored level service for scene-based level progression
/// Single Responsibility: Scene-based level loading only
/// Alternative to TrackService (for scene-based instead of prefab-based)
/// </summary>
public class LevelService : MonoBehaviour
{
    private GameEventSystem eventSystem;
    private IGameStateService gameStateService;

    [Header("Level Configuration")]
    [SerializeField] private LevelData[] levels;
    [SerializeField] private int currentLevelIndex = 0;
    [SerializeField] private bool enableCircularNavigation = true;
    
    [Header("Transition")]
    [SerializeField] private bool useFadeTransition = true;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private CanvasGroup transitionPanel;
    
    [Header("Debug")]
    [SerializeField] private bool showDebug = true;
    
    private LevelData currentLevel;
    private bool isTransitioning = false;

    public int CurrentLevelIndex => currentLevelIndex;
    public int TotalLevels => levels != null ? levels.Length : 0;

    #region Initialization

    private void Awake()
    {
        InitializeServices();
        SetupPersistence();
        CreateTransitionPanelIfNeeded();
    }

    private void Start()
    {
        ValidateLevels();
        SubscribeToEvents();
        LoadInitialLevel();
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
            Debug.LogError("[LevelService] GameEventSystem not found!");
        }
        
        if (showDebug)
        {
            Debug.Log("[LevelService] Service initialized");
        }
    }

    private void SetupPersistence()
    {
        // Persist across scenes
        DontDestroyOnLoad(gameObject);
    }

    private void ValidateLevels()
    {
        if (levels == null || levels.Length == 0)
        {
            Debug.LogError("[LevelService] No levels assigned!");
            return;
        }
        
        if (showDebug)
        {
            Debug.Log($"[LevelService] Initialized with {levels.Length} levels");
        }
    }

    private void SubscribeToEvents()
    {
        if (eventSystem == null) return;

        eventSystem.Subscribe<NextLevelRequestedEvent>(OnNextLevelRequested);
    }

    private void UnsubscribeFromEvents()
    {
        if (eventSystem == null) return;

        eventSystem.Unsubscribe<NextLevelRequestedEvent>(OnNextLevelRequested);
    }

    private void LoadInitialLevel()
    {
        LoadLevel(currentLevelIndex, false);
    }

    #endregion

    #region Event Handlers

    private void OnNextLevelRequested(NextLevelRequestedEvent evt)
    {
        LoadNextLevel();
    }

    #endregion

    #region Level Loading

    public void LoadLevel(int levelIndex, bool useTransition = true)
    {
        if (isTransitioning)
        {
            if (showDebug)
            {
                Debug.LogWarning("[LevelService] Already transitioning");
            }
            return;
        }
        
        if (!ValidateLevelIndex(levelIndex)) return;
        
        currentLevelIndex = levelIndex;
        currentLevel = levels[levelIndex];
        
        if (showDebug)
        {
            Debug.Log($"[LevelService] Loading level {levelIndex}: {currentLevel.LevelName}");
        }
        
        if (useTransition && useFadeTransition)
        {
            StartCoroutine(LoadLevelWithTransition(currentLevel));
        }
        else
        {
            LoadLevelImmediate(currentLevel);
        }
    }

    public void LoadNextLevel()
    {
        int nextIndex = currentLevelIndex + 1;
        
        if (enableCircularNavigation && nextIndex >= levels.Length)
        {
            nextIndex = 0;
            
            if (showDebug)
            {
                Debug.Log("[LevelService] Cycling back to first level");
            }
        }
        else if (nextIndex >= levels.Length)
        {
            Debug.LogWarning("[LevelService] Already at last level");
            return;
        }
        
        LoadLevel(nextIndex);
    }

    public void LoadPreviousLevel()
    {
        int prevIndex = currentLevelIndex - 1;
        
        if (enableCircularNavigation && prevIndex < 0)
        {
            prevIndex = levels.Length - 1;
            
            if (showDebug)
            {
                Debug.Log("[LevelService] Cycling to last level");
            }
        }
        else if (prevIndex < 0)
        {
            Debug.LogWarning("[LevelService] Already at first level");
            return;
        }
        
        LoadLevel(prevIndex);
    }

    private void LoadLevelImmediate(LevelData level)
    {
        LoadScene(level);
        StartCoroutine(PublishLevelLoadedEventAfterSceneLoad(level));
    }

    private IEnumerator LoadLevelWithTransition(LevelData level)
    {
        isTransitioning = true;
        
        // Fade out
        if (transitionPanel != null)
        {
            yield return StartCoroutine(FadeTransition(0f, 1f, fadeOutDuration));
        }
        
        // Load scene
        LoadScene(level);
        
        // Wait for scene to load
        yield return new WaitForSeconds(GameConstants.SceneManagement.SCENE_LOAD_WAIT_TIME);
        
        // Fade in
        if (transitionPanel != null)
        {
            yield return StartCoroutine(FadeTransition(1f, 0f, fadeInDuration));
        }
        
        // Publish event
        PublishLevelLoadedEvent(level);
        
        isTransitioning = false;
        
        if (showDebug)
        {
            Debug.Log($"[LevelService] Level loaded: {level.LevelTitle}");
        }
    }

    private void LoadScene(LevelData level)
    {
        if (level.UseSceneName)
        {
            SceneManager.LoadScene(level.SceneName);
        }
        else
        {
            SceneManager.LoadScene(level.SceneIndex);
        }
    }

    private IEnumerator PublishLevelLoadedEventAfterSceneLoad(LevelData level)
    {
        yield return new WaitForSeconds(GameConstants.SceneManagement.SCENE_LOAD_WAIT_TIME_LONG);
        PublishLevelLoadedEvent(level);
    }

    private void PublishLevelLoadedEvent(LevelData level)
    {
        if (eventSystem == null) return;

        // Note: For scene-based loading, we don't have spawn position in LevelData
        // You may want to add it to LevelData or find it in the scene
        eventSystem.Publish(new TrackLoadedEvent
        {
            TrackName = level.LevelName,
            TrackIndex = currentLevelIndex,
            SpawnPosition = Vector3.zero // Find in scene or add to LevelData
        });
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
        
        // Find or create canvas
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("LevelTransitionCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            DontDestroyOnLoad(canvasObj);
        }
        
        // Create panel
        GameObject panelObj = new GameObject("LevelTransitionPanel");
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
            Debug.Log("[LevelService] Created transition panel");
        }
    }

    #endregion

    #region Validation

    private bool ValidateLevelIndex(int index)
    {
        if (index < 0 || index >= levels.Length)
        {
            Debug.LogError($"[LevelService] Invalid level index: {index}");
            return false;
        }
        return true;
    }

    #endregion

    #region Public API

    public LevelData GetCurrentLevel()
    {
        return currentLevel;
    }

    public bool IsLastLevel()
    {
        return currentLevelIndex >= levels.Length - 1;
    }

    public bool IsFirstLevel()
    {
        return currentLevelIndex <= 0;
    }

    #endregion

    #region Hotkeys

    private void Update()
    {
        if (!showDebug) return;

        // Debug hotkeys
        if (Input.GetKeyDown(KeyCode.N))
        {
            LoadNextLevel();
        }
        
        if (Input.GetKeyDown(KeyCode.P))
        {
            LoadPreviousLevel();
        }
    }

    #endregion

    #region Debug

    [ContextMenu("Load Next Level")]
    private void DebugLoadNext()
    {
        LoadNextLevel();
    }

    [ContextMenu("Load Previous Level")]
    private void DebugLoadPrevious()
    {
        LoadPreviousLevel();
    }

    #endregion
}
