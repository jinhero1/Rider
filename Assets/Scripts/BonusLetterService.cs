using UnityEngine;
using System.Linq;

public class BonusLetterService : MonoBehaviour, IBonusLetterService
{
    private GameEventSystem eventSystem;
    private IScoreService scoreService;

    [Header("BONUS Letters")]
    [SerializeField] private BonusLetter[] bonusLetters = new BonusLetter[GameConstants.Collectibles.BONUS_LETTER_COUNT];
    [SerializeField] private bool autoFindLetters = true;
    
    [Header("Treasure Configuration")]
    [SerializeField] private int treasureChestPoints = GameConstants.Collectibles.DEFAULT_TREASURE_CHEST_POINTS;
    
    [Header("Debug")]
    [SerializeField] private bool showDebug = true;

    private bool[] collectedStatus = new bool[GameConstants.Collectibles.BONUS_LETTER_COUNT];
    private int totalCollected = 0;
    private bool treasureUnlocked = false;

    // IBonusLetterService implementation
    public bool[] CollectedLetters => (bool[])collectedStatus.Clone();
    public int TotalCollected => totalCollected;
    public bool IsWordComplete => collectedStatus.All(status => status);

    #region Initialization

    private void Awake()
    {
        InitializeServices();
        InitializeCollectedStatus();
    }

    private void Start()
    {
        SubscribeToEvents();
        RefreshLetters();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void InitializeServices()
    {
        eventSystem = ServiceLocator.Instance.Get<GameEventSystem>();
        scoreService = ServiceLocator.Instance.Get<IScoreService>();

        if (eventSystem == null)
        {
            Debug.LogError("[BonusLetterService] GameEventSystem not found!");
        }

        // Register this service
        ServiceLocator.Instance.Register<IBonusLetterService>(this);
    }

    private void InitializeCollectedStatus()
    {
        if (collectedStatus == null || collectedStatus.Length != GameConstants.Collectibles.BONUS_LETTER_COUNT)
        {
            collectedStatus = new bool[GameConstants.Collectibles.BONUS_LETTER_COUNT];
        }
    }

    private void SubscribeToEvents()
    {
        if (eventSystem == null) return;

        eventSystem.Subscribe<BonusLetterCollectedEvent>(OnLetterCollected);
        eventSystem.Subscribe<TrackLoadedEvent>(OnTrackLoaded);
        eventSystem.Subscribe<LevelRestartRequestedEvent>(OnLevelRestart);
        eventSystem.Subscribe<LevelCompletedEvent>(OnLevelCompleted);
    }

    private void UnsubscribeFromEvents()
    {
        if (eventSystem == null) return;

        eventSystem.Unsubscribe<BonusLetterCollectedEvent>(OnLetterCollected);
        eventSystem.Unsubscribe<TrackLoadedEvent>(OnTrackLoaded);
        eventSystem.Unsubscribe<LevelRestartRequestedEvent>(OnLevelRestart);
        eventSystem.Unsubscribe<LevelCompletedEvent>(OnLevelCompleted);
    }

    #endregion

    #region Event Handlers

    private void OnLetterCollected(BonusLetterCollectedEvent evt)
    {
        int index = evt.LetterIndex;
        
        if (index < 0 || index >= GameConstants.Collectibles.BONUS_LETTER_COUNT)
        {
            Debug.LogError($"[BonusLetterService] Invalid letter index: {index}");
            return;
        }
        
        // Mark as collected
        if (!collectedStatus[index])
        {
            collectedStatus[index] = true;
            totalCollected++;
            
            if (showDebug)
            {
                Debug.Log($"[BonusLetterService] Letter collected: {evt.LetterName} ({totalCollected}/5)");
            }
            
            // Re-publish with updated total
            eventSystem?.Publish(new BonusLetterCollectedEvent
            {
                LetterIndex = index,
                LetterName = evt.LetterName,
                TotalCollected = totalCollected
            });
            
            // Check if BONUS word complete
            if (IsWordComplete)
            {
                OnBonusWordCompleted();
            }
        }
    }

    private void OnBonusWordCompleted()
    {
        if (treasureUnlocked) return;
        
        treasureUnlocked = true;
        
        if (showDebug)
        {
            Debug.Log($"[BonusLetterService] ✨ BONUS WORD COMPLETED! Treasure: {treasureChestPoints}");
        }
        
        // Publish event
        eventSystem?.Publish(new BonusWordCompletedEvent
        {
            TreasurePoints = treasureChestPoints
        });
    }

    private void OnTrackLoaded(TrackLoadedEvent evt)
    {
        if (showDebug)
        {
            Debug.Log("[BonusLetterService] Track loaded, refreshing letters");
        }

        RefreshLetters();
    }

    private void OnLevelRestart(LevelRestartRequestedEvent evt)
    {
        if (showDebug)
        {
            Debug.Log("[BonusLetterService] Level restarting, resetting letters");
        }

        ResetLetters();
    }

    private void OnLevelCompleted(LevelCompletedEvent evt)
    {
        if (showDebug)
        {
            Debug.Log("[BonusLetterService] Level completed");
        }
        
        // Publish treasure chest event
        eventSystem?.Publish(new ShowTreasureChestEvent
        {
            RewardPoints = treasureChestPoints
        });
        FullReset();
    }

    #endregion

    #region IBonusLetterService Implementation

    public void OnLetterCollected(int letterIndex)
    {
        // This method is called when manually notifying the service
        // Event-based collection is handled in OnLetterCollected(event)
        if (letterIndex < 0 || letterIndex >= GameConstants.Collectibles.BONUS_LETTER_COUNT)
        {
            return;
        }

        if (!collectedStatus[letterIndex])
        {
            collectedStatus[letterIndex] = true;
            totalCollected++;
            
            eventSystem?.Publish(new BonusLetterCollectedEvent
            {
                LetterIndex = letterIndex,
                LetterName = ((BonusLetter.LetterType)letterIndex).ToString(),
                TotalCollected = totalCollected
            });
        }
    }

    public void ResetLetters()
    {
        // Reset all letters but keep collected status
        foreach (var letter in bonusLetters)
        {
            if (letter != null)
            {
                int index = letter.LetterIndex;
                bool wasCollected = index >= 0 && index < collectedStatus.Length && collectedStatus[index];
                
                letter.ResetLetter();
                
                if (wasCollected)
                {
                    letter.SetCollectedStatus(true);
                }
            }
        }
        
        if (showDebug)
        {
            Debug.Log($"[BonusLetterService] Letters reset. Collected: {totalCollected}/5");
        }
    }

    public void RefreshLetters()
    {
        if (autoFindLetters)
        {
            FindAllBonusLetters();
        }
        
        // Check if we have letters
        if (bonusLetters.Length == 0 || bonusLetters.All(l => l == null))
        {
            if (showDebug)
            {
                Debug.Log("[BonusLetterService] No BONUS letters found in current track");
            }
            return;
        }
        
        // Apply saved collected status to letters
        RestoreCollectedStatus();
        
        if (showDebug)
        {
            Debug.Log($"[BonusLetterService] Found {bonusLetters.Length} BONUS letters");
        }
    }

    #endregion

    #region Helper Methods

    private void FindAllBonusLetters()
    {
        BonusLetter[] foundLetters = FindObjectsByType<BonusLetter>(FindObjectsSortMode.None);
        
        if (foundLetters.Length == 0)
        {
            bonusLetters = new BonusLetter[0];
            return;
        }
        
        // Sort by letter index
        bonusLetters = foundLetters.OrderBy(l => l.LetterIndex).ToArray();
        
        if (bonusLetters.Length != GameConstants.Collectibles.BONUS_LETTER_COUNT && showDebug)
        {
            Debug.LogWarning($"[BonusLetterService] Expected 5 letters, found {bonusLetters.Length}");
        }
        
        if (showDebug)
        {
            string letters = string.Join(", ", bonusLetters.Select(l => l.LetterName));
            Debug.Log($"[BonusLetterService] Found letters: {letters}");
        }
    }

    private void RestoreCollectedStatus()
    {
        for (int i = 0; i < bonusLetters.Length && i < collectedStatus.Length; i++)
        {
            if (bonusLetters[i] != null && collectedStatus[i])
            {
                bonusLetters[i].SetCollectedStatus(true);
            }
        }
    }

    private void FullReset()
    {
        collectedStatus = new bool[GameConstants.Collectibles.BONUS_LETTER_COUNT];
        totalCollected = 0;
        treasureUnlocked = false;
        
        foreach (var letter in bonusLetters)
        {
            letter?.FullReset();
        }

        if (showDebug)
        {
            Debug.Log("[BonusLetterService] Full reset complete");
        }
        
        // Publish event
        eventSystem?.Publish(new BonusLetterStatusEvent
        {
            CollectedStatus = collectedStatus
        });
    }

    #endregion

    #region Debug Context Menu

    [ContextMenu("Refresh Bonus Letters")]
    private void ManualRefresh()
    {
        RefreshLetters();
    }

    [ContextMenu("Reset All Letters")]
    private void ManualReset()
    {
        ResetLetters();
    }

    [ContextMenu("Full Reset")]
    private void ManualFullReset()
    {
        FullReset();
    }

    [ContextMenu("Collect All Letters (Debug)")]
    private void DebugCollectAll()
    {
        for (int i = 0; i < bonusLetters.Length; i++)
        {
            if (bonusLetters[i] != null)
            {
                bonusLetters[i].Collect();
            }
        }
    }

    #endregion
}
