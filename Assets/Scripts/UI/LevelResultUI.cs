using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays level completion result screen
/// Single Responsibility: Level result UI only
/// </summary>
public class LevelResultUI : MonoBehaviour
{
    private GameEventSystem eventSystem;
    private IScoreService scoreService;

    [Header("UI References")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button nextLevelButton;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private void Awake()
    {
        InitializeServices();
        SetupButtons();
    }

    private void Start()
    {
        SubscribeToEvents();
        HidePanel();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    #region Initialization

    private void InitializeServices()
    {
        eventSystem = ServiceLocator.Instance.Get<GameEventSystem>();
        scoreService = ServiceLocator.Instance.Get<IScoreService>();

        if (eventSystem == null)
        {
            Debug.LogError("[LevelResultUI] GameEventSystem not found!");
        }
    }

    private void SetupButtons()
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(OnRestartClicked);
        }

        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.RemoveAllListeners();
            nextLevelButton.onClick.AddListener(OnNextLevelClicked);
        }
    }

    private void SubscribeToEvents()
    {
        if (eventSystem == null) return;

        eventSystem.Subscribe<LevelCompletedEvent>(OnLevelCompleted);
        eventSystem.Subscribe<PlayerCrashedEvent>(OnPlayerCrashed);
        eventSystem.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
    }

    private void UnsubscribeFromEvents()
    {
        if (eventSystem == null) return;

        eventSystem.Unsubscribe<LevelCompletedEvent>(OnLevelCompleted);
        eventSystem.Unsubscribe<PlayerCrashedEvent>(OnPlayerCrashed);
        eventSystem.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
    }

    #endregion

    #region Event Handlers

    private void OnLevelCompleted(LevelCompletedEvent evt)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[LevelResultUI] Level completed! Score: {evt.FinalScore}");
        }

        ShowResultPanel(evt.FinalScore);
    }

    private void OnPlayerCrashed(PlayerCrashedEvent evt)
    {
        if (showDebugLogs)
        {
            Debug.Log("[LevelResultUI] Player crashed, showing result");
        }

        int currentScore = scoreService?.CurrentScore ?? 0;
        ShowResultPanel(currentScore);
    }

    private void OnGameStateChanged(GameStateChangedEvent evt)
    {
        if (evt.NewState == GameState.Playing)
        {
            HidePanel();
        }
    }

    #endregion

    #region UI Control

    private void ShowResultPanel(int finalScore)
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
        }

        UpdateScoreDisplay(finalScore);
    }

    private void HidePanel()
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
    }

    private void UpdateScoreDisplay(int score)
    {
        if (finalScoreText == null) return;

        int highScore = scoreService?.HighScore ?? 0;
        
        finalScoreText.text = $"Best Score\n{highScore}";

        if (showDebugLogs)
        {
            Debug.Log($"[LevelResultUI] Score: {score}, High Score: {highScore}");
        }
    }

    #endregion

    #region Button Handlers

    private void OnRestartClicked()
    {
        if (showDebugLogs)
        {
            Debug.Log("[LevelResultUI] Restart button clicked");
        }

        eventSystem?.Publish(new LevelRestartRequestedEvent());
    }

    private void OnNextLevelClicked()
    {
        if (showDebugLogs)
        {
            Debug.Log("[LevelResultUI] Next level button clicked");
        }

        eventSystem?.Publish(new NextLevelRequestedEvent());
    }

    #endregion
}
