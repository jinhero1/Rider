using UnityEngine;

public class GameCoordinator : MonoBehaviour
{
    private GameEventSystem eventSystem;
    private IScoreService scoreService;
    private IGameStateService gameStateService;
    private IEffectService effectService;
    private ICameraService cameraService;

    [Header("Player Reference")]
    [SerializeField] private BikeController bikeController;
    
    [Header("Spawn Configuration")]
    [SerializeField] private Vector3 startPosition;
    
    [Header("Effect Prefabs")]
    [SerializeField] private GameObject crashEffectPrefab;
    [SerializeField] private GameObject flipEffectPrefab;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private void Awake()
    {
        InitializeServices();
        CacheStartPosition();
    }

    private void Start()
    {
        SubscribeToEvents();
        InitializeGame();
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
        gameStateService = ServiceLocator.Instance.Get<IGameStateService>();
        effectService = ServiceLocator.Instance.Get<IEffectService>();
        cameraService = ServiceLocator.Instance.Get<ICameraService>();

        if (eventSystem == null)
        {
            Debug.LogError("[GameCoordinator] GameEventSystem not found!");
        }
    }

    private void CacheStartPosition()
    {
        if (bikeController != null)
        {
            startPosition = bikeController.transform.position;
        }
    }

    private void InitializeGame()
    {
        gameStateService?.ChangeState(GameState.Playing);
        
        if (showDebugLogs)
        {
            Debug.Log("[GameCoordinator] Game initialized");
        }
    }

    #endregion

    #region Event Subscription

    private void SubscribeToEvents()
    {
        if (eventSystem == null) return;

        eventSystem.Subscribe<FlipCompletedEvent>(OnFlipCompleted);
        eventSystem.Subscribe<PlayerCrashedEvent>(OnPlayerCrashed);
        eventSystem.Subscribe<LevelCompletedEvent>(OnLevelCompleted);
        eventSystem.Subscribe<LevelRestartRequestedEvent>(OnRestartRequested);
        eventSystem.Subscribe<NextLevelRequestedEvent>(OnNextLevelRequested);
    }

    private void UnsubscribeFromEvents()
    {
        if (eventSystem == null) return;

        eventSystem.Unsubscribe<FlipCompletedEvent>(OnFlipCompleted);
        eventSystem.Unsubscribe<PlayerCrashedEvent>(OnPlayerCrashed);
        eventSystem.Unsubscribe<LevelCompletedEvent>(OnLevelCompleted);
        eventSystem.Unsubscribe<LevelRestartRequestedEvent>(OnRestartRequested);
        eventSystem.Unsubscribe<NextLevelRequestedEvent>(OnNextLevelRequested);
    }

    #endregion

    #region Event Handlers

    private void OnFlipCompleted(FlipCompletedEvent evt)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[GameCoordinator] Flip completed! Count: {evt.TotalFlipCount}, Bonus: {evt.BonusPoints}");
        }

        // Spawn flip effect
        if (flipEffectPrefab != null && effectService != null)
        {
            effectService.SpawnEffect(
                flipEffectPrefab, 
                evt.Position, 
                GameConstants.Collectibles.EFFECT_DESTROY_DELAY
            );
        }

        // Add score
        scoreService?.AddScore(evt.BonusPoints);
    }

    private void OnPlayerCrashed(PlayerCrashedEvent evt)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[GameCoordinator] Player crashed at {evt.CrashPosition}");
        }

        // Change state
        gameStateService?.ChangeState(GameState.Crashed);

        // Spawn crash effect
        if (crashEffectPrefab != null && effectService != null)
        {
            effectService.SpawnEffect(
                crashEffectPrefab,
                evt.CrashPosition,
                GameConstants.Collectibles.EFFECT_DESTROY_DELAY
            );
        }

        // Camera shake
        cameraService?.Shake(
            GameConstants.Camera.CRASH_SHAKE_INTENSITY,
            GameConstants.Camera.CRASH_SHAKE_INTENSITY
        );

        // Save high score
        scoreService?.SaveHighScore();
    }

    private void OnLevelCompleted(LevelCompletedEvent evt)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[GameCoordinator] Level completed! Score: {evt.FinalScore}");
        }

        // Change state
        gameStateService?.ChangeState(GameState.Completed);

        // Stop player
        bikeController?.StopPlayer();

        // Save high score
        scoreService?.SaveHighScore();
    }

    private void OnRestartRequested(LevelRestartRequestedEvent evt)
    {
        if (showDebugLogs)
        {
            Debug.Log("[GameCoordinator] Restart requested");
        }

        RestartLevel();
    }

    private void OnNextLevelRequested(NextLevelRequestedEvent evt)
    {
        if (showDebugLogs)
        {
            Debug.Log("[GameCoordinator] Next level requested");
        }

        LoadNextLevel();
    }

    #endregion

    #region Game Flow Control

    private void RestartLevel()
    {
        // Reset state
        gameStateService?.ChangeState(GameState.Playing);

        // Reset score
        scoreService?.ResetScore();

        // Reset player
        if (bikeController != null)
        {
            bikeController.transform.position = startPosition;
            bikeController.ResetPlayer();
        }

        // Publish restart event for other systems
        eventSystem?.Publish(new UIRefreshRequestedEvent());

        if (showDebugLogs)
        {
            Debug.Log("[GameCoordinator] Level restarted");
        }
    }

    private void LoadNextLevel()
    {
        // Delegate to track service
        ITrackService trackService = ServiceLocator.Instance.Get<ITrackService>();
        
        if (trackService != null)
        {
            trackService.LoadNextTrack();
            RestartLevel();
        }
        else
        {
            Debug.LogWarning("[GameCoordinator] TrackService not found");
        }
    }

    #endregion

    #region Public API

    public Vector3 GetStartPosition()
    {
        return startPosition;
    }

    public void SetStartPosition(Vector3 position)
    {
        startPosition = position;
    }

    #endregion

    #region Debug

    [ContextMenu("Restart Level")]
    private void DebugRestartLevel()
    {
        RestartLevel();
    }

    [ContextMenu("Trigger Level Complete")]
    private void DebugLevelComplete()
    {
        eventSystem?.Publish(new LevelCompletedEvent
        {
            FinalScore = scoreService?.CurrentScore ?? 0,
            FlipCount = bikeController?.FlipCount ?? 0,
            CollectiblesCollected = 0,
            CompletionTime = Time.time
        });
    }

    #endregion
}
