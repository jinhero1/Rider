using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// UI component for displaying showcase level information
/// </summary>
public class ShowcaseLevelUI : MonoBehaviour
{
    [Header("Level Info Display")]
    [SerializeField] private GameObject levelInfoPanel;
    [SerializeField] private TextMeshProUGUI levelTitleText;
    [SerializeField] private TextMeshProUGUI levelNumberText;
    [SerializeField] private Image levelIconImage;
    
    [Header("Navigation Buttons")]
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button previousLevelButton;
    [SerializeField] private TextMeshProUGUI nextButtonText;
    [SerializeField] private TextMeshProUGUI previousButtonText;
    
    [Header("Progress Display")]
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private bool showProgress = true;
    
    [Header("Animation")]
    [SerializeField] private bool animateLevelInfo = true;
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    
    [Header("Auto Hide")]
    [SerializeField] private bool autoHideLevelInfo = true;
    [SerializeField] private float autoHideDelay = 3f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebug = true;
    
    private CanvasGroup levelInfoCanvasGroup;
    private Coroutine autoHideCoroutine;

    void Start()
    {
        // Setup canvas group for fading
        if (levelInfoPanel != null)
        {
            levelInfoCanvasGroup = levelInfoPanel.GetComponent<CanvasGroup>();
            if (levelInfoCanvasGroup == null)
            {
                levelInfoCanvasGroup = levelInfoPanel.AddComponent<CanvasGroup>();
            }
        }
        
        // Setup button listeners
        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.AddListener(OnNextButtonClicked);
        }
        
        if (previousLevelButton != null)
        {
            previousLevelButton.onClick.AddListener(OnPreviousButtonClicked);
        }
        
        // Hide level info initially
        if (levelInfoPanel != null)
        {
            levelInfoPanel.SetActive(false);
        }
        
        // Update navigation buttons
        UpdateNavigationButtons();
        
        if (showDebug)
        {
            Debug.Log("[ShowcaseLevelUI] Initialized");
        }
    }

    /// <summary>
    /// Show level information
    /// </summary>
    public void ShowLevelInfo(LevelData level, float displayDuration = 3f)
    {
        if (level == null) return;
        
        // Update UI elements
        if (levelTitleText != null)
        {
            levelTitleText.text = level.LevelTitle;
        }
        
        if (levelNumberText != null && ShowcaseLevelManager.Instance != null)
        {
            int current = ShowcaseLevelManager.Instance.GetCurrentLevelIndex() + 1;
            int total = ShowcaseLevelManager.Instance.GetTotalLevels();
            levelNumberText.text = $"Level {current} / {total}";
        }
        
        if (levelIconImage != null && level.LevelIcon != null)
        {
            levelIconImage.sprite = level.LevelIcon;
            levelIconImage.color = level.LevelColor;
        }
        
        // Update progress
        UpdateProgress();
        
        // Update navigation buttons
        UpdateNavigationButtons();
        
        // Show panel
        if (levelInfoPanel != null)
        {
            levelInfoPanel.SetActive(true);
            
            if (animateLevelInfo)
            {
                StartCoroutine(FadeInPanel());
            }
            else if (levelInfoCanvasGroup != null)
            {
                levelInfoCanvasGroup.alpha = 1f;
            }
        }
        
        // Auto hide
        if (autoHideLevelInfo)
        {
            if (autoHideCoroutine != null)
            {
                StopCoroutine(autoHideCoroutine);
            }
            autoHideCoroutine = StartCoroutine(AutoHideLevelInfo(displayDuration));
        }
        
        if (showDebug)
        {
            Debug.Log($"[ShowcaseLevelUI] Showing: {level.LevelTitle}");
        }
    }

    /// <summary>
    /// Show track information (for Prefab-based system)
    /// </summary>
    public void ShowTrackInfo(string title, int currentIndex, int totalTracks, float displayDuration = 3f)
    {
        // Update UI elements
        if (levelTitleText != null)
        {
            levelTitleText.text = title;
        }
        
        if (levelNumberText != null)
        {
            levelNumberText.text = $"Level {currentIndex} / {totalTracks}";
        }
        
        // Update progress
        UpdateProgress();
        
        // Update navigation buttons
        UpdateNavigationButtons();
        
        // Show panel
        if (levelInfoPanel != null)
        {
            levelInfoPanel.SetActive(true);
            
            if (animateLevelInfo)
            {
                StartCoroutine(FadeInPanel());
            }
            else if (levelInfoCanvasGroup != null)
            {
                levelInfoCanvasGroup.alpha = 1f;
            }
        }
        
        // Auto hide
        if (autoHideLevelInfo)
        {
            if (autoHideCoroutine != null)
            {
                StopCoroutine(autoHideCoroutine);
            }
            autoHideCoroutine = StartCoroutine(AutoHideLevelInfo(displayDuration));
        }
        
        if (showDebug)
        {
            Debug.Log($"[ShowcaseLevelUI] Showing track: {title}");
        }
    }

    /// <summary>
    /// Hide level information
    /// </summary>
    public void HideLevelInfo()
    {
        if (animateLevelInfo)
        {
            StartCoroutine(FadeOutPanel());
        }
        else if (levelInfoPanel != null)
        {
            levelInfoPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Update progress display
    /// </summary>
    private void UpdateProgress()
    {
        if (!showProgress) return;
        
        if (ShowcaseLevelManager.Instance != null)
        {
            int current = ShowcaseLevelManager.Instance.GetCurrentLevelIndex() + 1;
            int total = ShowcaseLevelManager.Instance.GetTotalLevels();
            
            if (progressText != null)
            {
                progressText.text = $"{current} / {total}";
            }
            
            if (progressSlider != null)
            {
                progressSlider.maxValue = total;
                progressSlider.value = current;
            }
        }
    }

    /// <summary>
    /// Update navigation button states
    /// </summary>
    private void UpdateNavigationButtons()
    {
        if (ShowcaseLevelManager.Instance == null) return;
        
        bool isLastLevel = ShowcaseLevelManager.Instance.IsLastLevel();
        bool isFirstLevel = ShowcaseLevelManager.Instance.IsFirstLevel();
        
        // Update next button
        if (nextLevelButton != null)
        {
            nextLevelButton.interactable = true; // Always enabled with circular navigation
        }
        
        if (nextButtonText != null)
        {
            // Show "Restart" or "Loop" text on last level if circular
            nextButtonText.text = isLastLevel ? "Restart" : "Next";
        }
        
        // Update previous button
        if (previousLevelButton != null)
        {
            previousLevelButton.interactable = true; // Always enabled with circular navigation
        }
        
        if (previousButtonText != null)
        {
            previousButtonText.text = isFirstLevel ? "Last" : "Previous";
        }
    }

    /// <summary>
    /// Fade in panel
    /// </summary>
    private IEnumerator FadeInPanel()
    {
        if (levelInfoCanvasGroup == null) yield break;
        
        float elapsed = 0f;
        levelInfoCanvasGroup.alpha = 0f;
        
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            levelInfoCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            yield return null;
        }
        
        levelInfoCanvasGroup.alpha = 1f;
    }

    /// <summary>
    /// Fade out panel
    /// </summary>
    private IEnumerator FadeOutPanel()
    {
        if (levelInfoCanvasGroup == null) yield break;
        
        float elapsed = 0f;
        levelInfoCanvasGroup.alpha = 1f;
        
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            levelInfoCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            yield return null;
        }
        
        levelInfoCanvasGroup.alpha = 0f;
        
        if (levelInfoPanel != null)
        {
            levelInfoPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Auto hide level info after delay
    /// </summary>
    private IEnumerator AutoHideLevelInfo(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideLevelInfo();
    }

    /// <summary>
    /// Handle next button click
    /// </summary>
    private void OnNextButtonClicked()
    {
        if (ShowcaseLevelManager.Instance != null)
        {
            ShowcaseLevelManager.Instance.LoadNextLevel();
            
            if (showDebug)
            {
                Debug.Log("[ShowcaseLevelUI] Next button clicked");
            }
        }
    }

    /// <summary>
    /// Handle previous button click
    /// </summary>
    private void OnPreviousButtonClicked()
    {
        if (ShowcaseLevelManager.Instance != null)
        {
            ShowcaseLevelManager.Instance.LoadPreviousLevel();
            
            if (showDebug)
            {
                Debug.Log("[ShowcaseLevelUI] Previous button clicked");
            }
        }
    }

    /// <summary>
    /// Manually show level info (for inspector button)
    /// </summary>
    [ContextMenu("Show Current Level Info")]
    public void ShowCurrentLevelInfo()
    {
        if (ShowcaseLevelManager.Instance != null)
        {
            LevelData currentLevel = ShowcaseLevelManager.Instance.GetCurrentLevel();
            if (currentLevel != null)
            {
                ShowLevelInfo(currentLevel, autoHideDelay);
            }
        }
    }

    /// <summary>
    /// Toggle level info visibility
    /// </summary>
    public void ToggleLevelInfo()
    {
        if (levelInfoPanel != null)
        {
            if (levelInfoPanel.activeSelf)
            {
                HideLevelInfo();
            }
            else
            {
                ShowCurrentLevelInfo();
            }
        }
    }
}
