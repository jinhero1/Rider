using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ShowcaseLevelUI : MonoBehaviour
{
    private GameEventSystem eventSystem;

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
    [SerializeField] private float fadeInDuration = GameConstants.UI.LEVEL_INFO_FADE_IN_DURATION;
    [SerializeField] private float fadeOutDuration = GameConstants.UI.LEVEL_INFO_FADE_OUT_DURATION;
    
    [Header("Auto Hide")]
    [SerializeField] private bool autoHideLevelInfo = true;
    [SerializeField] private float autoHideDelay = GameConstants.UI.LEVEL_INFO_AUTO_HIDE_DELAY;
    
    [Header("Debug")]
    [SerializeField] private bool showDebug = true;
    
    private CanvasGroup levelInfoCanvasGroup;
    private Coroutine autoHideCoroutine;

    #region Initialization

    private void Awake()
    {
        InitializeServices();
        SetupCanvasGroup();
    }

    private void Start()
    {
        SubscribeToEvents();
        SetupButtons();
        HideLevelInfo();
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
            Debug.LogError("[ShowcaseLevelUI] GameEventSystem not found!");
        }
    }

    private void SetupCanvasGroup()
    {
        if (levelInfoPanel != null)
        {
            levelInfoCanvasGroup = levelInfoPanel.GetComponent<CanvasGroup>();
            if (levelInfoCanvasGroup == null)
            {
                levelInfoCanvasGroup = levelInfoPanel.AddComponent<CanvasGroup>();
            }
        }
    }

    private void SubscribeToEvents()
    {
        if (eventSystem == null) return;

        eventSystem.Subscribe<TrackLoadedEvent>(OnTrackLoaded);
    }

    private void UnsubscribeFromEvents()
    {
        if (eventSystem == null) return;

        eventSystem.Unsubscribe<TrackLoadedEvent>(OnTrackLoaded);
    }

    private void SetupButtons()
    {
        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.RemoveAllListeners();
            nextLevelButton.onClick.AddListener(OnNextButtonClicked);
        }
        
        if (previousLevelButton != null)
        {
            previousLevelButton.onClick.RemoveAllListeners();
            previousLevelButton.onClick.AddListener(OnPreviousButtonClicked);
        }
        
        UpdateNavigationButtons();
    }

    #endregion

    #region Event Handlers

    private void OnTrackLoaded(TrackLoadedEvent evt)
    {
        if (showDebug)
        {
            Debug.Log($"[ShowcaseLevelUI] Track loaded: {evt.TrackTitle}");
        }

        // Get track service to get current track info
        ITrackService trackService = ServiceLocator.Instance.Get<ITrackService>();
        
        if (trackService != null)
        {
            ShowTrackInfo(
                evt.TrackTitle,
                trackService.CurrentTrackIndex + 1,
                trackService.TotalTracks,
                autoHideDelay
            );
        }
    }

    #endregion

    #region UI Display

    public void ShowTrackInfo(string title, int currentIndex, int totalTracks, float displayDuration)
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
        
        UpdateProgress(currentIndex, totalTracks);
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
            Debug.Log($"[ShowcaseLevelUI] Showing: {title}");
        }
    }

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

    private void UpdateProgress(int current, int total)
    {
        if (!showProgress) return;
        
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

    private void UpdateNavigationButtons()
    {
        ITrackService trackService = ServiceLocator.Instance.Get<ITrackService>();
        if (trackService == null) return;
        
        bool isLastTrack = trackService.IsLastTrack();
        bool isFirstTrack = trackService.IsFirstTrack();
        
        // Update next button
        if (nextLevelButton != null)
        {
            nextLevelButton.interactable = true; // Always enabled with circular
        }
        
        if (nextButtonText != null)
        {
            nextButtonText.text = isLastTrack ? "Restart" : "Next";
        }
        
        // Update previous button
        if (previousLevelButton != null)
        {
            previousLevelButton.interactable = true; // Always enabled with circular
        }
        
        if (previousButtonText != null)
        {
            previousButtonText.text = isFirstTrack ? "Last" : "Previous";
        }
    }

    #endregion

    #region Animations

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

    private IEnumerator AutoHideLevelInfo(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideLevelInfo();
    }

    #endregion

    #region Button Handlers

    private void OnNextButtonClicked()
    {
        if (showDebug)
        {
            Debug.Log("[ShowcaseLevelUI] Next button clicked");
        }
        
        eventSystem?.Publish(new NextLevelRequestedEvent());
    }

    private void OnPreviousButtonClicked()
    {
        if (showDebug)
        {
            Debug.Log("[ShowcaseLevelUI] Previous button clicked");
        }
        
        ITrackService trackService = ServiceLocator.Instance.Get<ITrackService>();
        trackService?.LoadPreviousTrack();
    }

    #endregion

    #region Public API

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

    private void ShowCurrentLevelInfo()
    {
        ITrackService trackService = ServiceLocator.Instance.Get<ITrackService>();
        if (trackService != null)
        {
            TrackData currentTrack = trackService.GetCurrentTrack();
            if (currentTrack != null)
            {
                ShowTrackInfo(
                    currentTrack.TrackTitle,
                    trackService.CurrentTrackIndex + 1,
                    trackService.TotalTracks,
                    autoHideDelay
                );
            }
        }
    }

    #endregion

    #region Debug

    [ContextMenu("Show Current Level Info")]
    private void DebugShowCurrentInfo()
    {
        ShowCurrentLevelInfo();
    }

    [ContextMenu("Toggle Level Info")]
    private void DebugToggleInfo()
    {
        ToggleLevelInfo();
    }

    #endregion
}
