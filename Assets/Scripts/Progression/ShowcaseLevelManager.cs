using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Manages showcase level progression with circular navigation
/// </summary>
public class ShowcaseLevelManager : MonoBehaviour
{
    public static ShowcaseLevelManager Instance;
    
    [Header("Level Configuration")]
    [SerializeField] private LevelData[] levels;
    [SerializeField] private int currentLevelIndex = 0;
    [SerializeField] private bool enableCircularNavigation = true;

    [Header("Level Display")]
    [SerializeField] private ShowcaseLevelUI levelUI;
    /*
    [SerializeField] private bool showLevelTitleOnStart = true;
    [SerializeField] private float titleDisplayDuration = 3f;\
    */
    
    [Header("Level Transition")]
    [SerializeField] private bool useFadeTransition = true;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private CanvasGroup transitionPanel;
    
    [Header("Debug")]
    [SerializeField] private bool showDebug = true;
    
    private LevelData currentLevel;
    private bool isTransitioning = false;

    void Awake()
    {
        // Singleton pattern - persist across scenes
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Initialize transition panel if not assigned
        if (transitionPanel == null && useFadeTransition)
        {
            CreateTransitionPanel();
        }
    }

    void Start()
    {
        // Validate levels
        if (levels == null || levels.Length == 0)
        {
            Debug.LogError("[ShowcaseLevelManager] No levels assigned!");
            return;
        }
        
        // Load the first level
        LoadLevel(currentLevelIndex, false);
        
        if (showDebug)
        {
            Debug.Log($"[ShowcaseLevelManager] Initialized with {levels.Length} levels");
        }
    }

    /// <summary>
    /// Load a level by index
    /// </summary>
    public void LoadLevel(int levelIndex, bool useTransition = true)
    {
        if (isTransitioning) return;
        
        // Validate index
        if (levelIndex < 0 || levelIndex >= levels.Length)
        {
            Debug.LogError($"[ShowcaseLevelManager] Invalid level index: {levelIndex}");
            return;
        }
        
        currentLevelIndex = levelIndex;
        currentLevel = levels[levelIndex];
        
        if (showDebug)
        {
            Debug.Log($"[ShowcaseLevelManager] Loading level {levelIndex}: {currentLevel.LevelName}");
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

    /// <summary>
    /// Load the next level in sequence
    /// </summary>
    public void LoadNextLevel()
    {
        int nextIndex = currentLevelIndex + 1;
        
        // Circular navigation
        if (enableCircularNavigation && nextIndex >= levels.Length)
        {
            nextIndex = 0;
            
            if (showDebug)
            {
                Debug.Log("[ShowcaseLevelManager] Reached end, cycling back to first level");
            }
        }
        else if (nextIndex >= levels.Length)
        {
            Debug.LogWarning("[ShowcaseLevelManager] Already at last level");
            return;
        }
        
        LoadLevel(nextIndex);
    }

    /// <summary>
    /// Load the previous level in sequence
    /// </summary>
    public void LoadPreviousLevel()
    {
        int prevIndex = currentLevelIndex - 1;
        
        // Circular navigation
        if (enableCircularNavigation && prevIndex < 0)
        {
            prevIndex = levels.Length - 1;
            
            if (showDebug)
            {
                Debug.Log("[ShowcaseLevelManager] At first level, cycling to last level");
            }
        }
        else if (prevIndex < 0)
        {
            Debug.LogWarning("[ShowcaseLevelManager] Already at first level");
            return;
        }
        
        LoadLevel(prevIndex);
    }

    /// <summary>
    /// Load level immediately without transition
    /// </summary>
    private void LoadLevelImmediate(LevelData level)
    {
        // Load scene
        if (level.UseSceneName)
        {
            SceneManager.LoadScene(level.SceneName);
        }
        else
        {
            SceneManager.LoadScene(level.SceneIndex);
        }
        
        // Show level UI after scene loads
        StartCoroutine(ShowLevelUIAfterLoad(level));
    }

    /// <summary>
    /// Load level with fade transition
    /// </summary>
    private IEnumerator LoadLevelWithTransition(LevelData level)
    {
        isTransitioning = true;
        
        // Fade out
        if (transitionPanel != null)
        {
            yield return StartCoroutine(FadeTransition(0f, 1f, fadeOutDuration));
        }
        
        // Load scene
        if (level.UseSceneName)
        {
            SceneManager.LoadScene(level.SceneName);
        }
        else
        {
            SceneManager.LoadScene(level.SceneIndex);
        }
        
        // Wait for scene to load
        yield return new WaitForSeconds(0.1f);
        
        // Fade in
        if (transitionPanel != null)
        {
            yield return StartCoroutine(FadeTransition(1f, 0f, fadeInDuration));
        }
        
        // Show level UI
        StartCoroutine(ShowLevelUIAfterLoad(level));
        
        isTransitioning = false;
    }

    /// <summary>
    /// Show level UI after scene loads
    /// </summary>
    private IEnumerator ShowLevelUIAfterLoad(LevelData level)
    {
        // Wait for scene to fully load
        yield return new WaitForSeconds(0.2f);

        // Find UI in new scene
        if (levelUI == null)
        {
            levelUI = FindFirstObjectByType<ShowcaseLevelUI>();
        }
        
        // Show level info
        /*
        if (levelUI != null && showLevelTitleOnStart)
        {
            levelUI.ShowLevelInfo(level, titleDisplayDuration);
        }
        */
        
        if (showDebug)
        {
            Debug.Log($"[ShowcaseLevelManager] Level loaded: {level.LevelTitle}");
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
        // Find or create canvas
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("TransitionCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            DontDestroyOnLoad(canvasObj);
        }
        
        // Create panel
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
            Debug.Log("[ShowcaseLevelManager] Created transition panel");
        }
    }

    /// <summary>
    /// Get current level data
    /// </summary>
    public LevelData GetCurrentLevel()
    {
        return currentLevel;
    }

    /// <summary>
    /// Get total number of levels
    /// </summary>
    public int GetTotalLevels()
    {
        return levels != null ? levels.Length : 0;
    }

    /// <summary>
    /// Get current level index
    /// </summary>
    public int GetCurrentLevelIndex()
    {
        return currentLevelIndex;
    }

    /// <summary>
    /// Check if this is the last level
    /// </summary>
    public bool IsLastLevel()
    {
        return currentLevelIndex >= levels.Length - 1;
    }

    /// <summary>
    /// Check if this is the first level
    /// </summary>
    public bool IsFirstLevel()
    {
        return currentLevelIndex <= 0;
    }

    // Hotkeys for testing
    void Update()
    {
        if (showDebug)
        {
            // Press N for next level
            if (Input.GetKeyDown(KeyCode.N))
            {
                LoadNextLevel();
            }
            
            // Press P for previous level
            if (Input.GetKeyDown(KeyCode.P))
            {
                LoadPreviousLevel();
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
