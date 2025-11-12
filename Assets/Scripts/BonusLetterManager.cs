using UnityEngine;
using System.Linq;

/// <summary>
/// Manages BONUS letter collection system with Track Prefab support
/// </summary>
public class BonusLetterManager : MonoBehaviour
{
    public static BonusLetterManager Instance;
    
    [Header("BONUS Letters")]
    [SerializeField] private BonusLetter[] bonusLetters = new BonusLetter[5];
    [SerializeField] private bool autoFindLetters = true;
    
    [Header("Collection Tracking")]
    [SerializeField] private bool[] collectedStatus = new bool[5]; // B, O, N, U, S
    [SerializeField] private int totalCollected = 0;
    
    [Header("Treasure Chest Reward")]
    [SerializeField] private int treasureChestPoints = 100;
    [SerializeField] private bool treasureUnlocked = false;
    
    [Header("UI")]
    [SerializeField] private UIManager uiManager;
    
    [Header("Track Support")]
    [SerializeField] private bool supportDynamicTracks = true;
    [SerializeField] private bool hideUIWhenNoLetters = true;
    
    [Header("Debug")]
    [SerializeField] private bool showDebug = true;
    
    private bool isInitialized = false;

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
        
        // Initialize collected status array
        if (collectedStatus == null || collectedStatus.Length != 5)
        {
            collectedStatus = new bool[5];
        }
    }

    void Start()
    {
        // Find UI Manager
        if (uiManager == null)
        {
            uiManager = FindFirstObjectByType<UIManager>();
        }
        
        // Initial scan for letters
        RefreshBonusLetters();
        
        isInitialized = true;
        
        if (showDebug)
        {
            Debug.Log($"[BonusLetterManager] Initialized. Dynamic Track Support: {supportDynamicTracks}");
        }
    }

    /// <summary>
    /// Refresh/rescan for bonus letters (call after loading new Track)
    /// </summary>
    public void RefreshBonusLetters()
    {
        if (autoFindLetters || bonusLetters.Length == 0)
        {
            FindAllBonusLetters();
        }
        
        // Check if we have letters
        if (bonusLetters.Length == 0 || bonusLetters.All(l => l == null))
        {
            if (hideUIWhenNoLetters && uiManager != null)
            {
                // Hide BONUS UI when no letters in current Track
                uiManager.UpdateBonusLetters(new bool[5]);
            }
            
            if (showDebug)
            {
                Debug.Log("[BonusLetterManager] No BONUS letters found in current Track");
            }
            return;
        }
        
        // Apply saved collected status to letters
        RestoreCollectedStatus();
        
        // Update UI
        UpdateBonusLettersUI();
        
        if (showDebug)
        {
            Debug.Log($"[BonusLetterManager] Found {bonusLetters.Length} BONUS letters");
        }
    }

    /// <summary>
    /// Find all bonus letters in the scene (including in Track Prefabs)
    /// </summary>
    private void FindAllBonusLetters()
    {
        BonusLetter[] foundLetters = FindObjectsOfType<BonusLetter>();
        
        if (foundLetters.Length == 0)
        {
            bonusLetters = new BonusLetter[0];
            return;
        }
        
        // Sort by letter index (B=0, O=1, N=2, U=3, S=4)
        bonusLetters = foundLetters.OrderBy(l => l.GetLetterIndex()).ToArray();
        
        // Validate we have the right letters
        if (bonusLetters.Length != 5)
        {
            if (showDebug)
            {
                Debug.LogWarning($"[BonusLetterManager] Expected 5 letters, found {bonusLetters.Length}");
            }
        }
        
        if (showDebug)
        {
            string letters = string.Join(", ", bonusLetters.Select(l => l.GetLetterName()));
            Debug.Log($"[BonusLetterManager] Found letters: {letters}");
        }
    }

    /// <summary>
    /// Restore collected status to letters after Track reload
    /// </summary>
    private void RestoreCollectedStatus()
    {
        for (int i = 0; i < bonusLetters.Length && i < collectedStatus.Length; i++)
        {
            if (bonusLetters[i] != null && collectedStatus[i])
            {
                // Letter was already collected, set it as collected
                bonusLetters[i].SetCollectedWithoutNotify(true);
            }
        }
    }

    /// <summary>
    /// Called when a bonus letter is collected
    /// </summary>
    public void OnLetterCollected(BonusLetter letter)
    {
        int index = letter.GetLetterIndex();
        
        if (index < 0 || index >= 5)
        {
            Debug.LogError($"[BonusLetterManager] Invalid letter index: {index}");
            return;
        }
        
        // Mark as collected
        if (!collectedStatus[index])
        {
            collectedStatus[index] = true;
            totalCollected++;
            
            if (showDebug)
            {
                Debug.Log($"[BonusLetterManager] Letter collected: {letter.GetLetterName()} ({totalCollected}/5)");
            }
            
            // Update UI
            UpdateBonusLettersUI();
            
            // Check if BONUS word complete
            if (IsBonusWordComplete())
            {
                OnBonusWordCompleted();
            }
        }
    }

    /// <summary>
    /// Check if all BONUS letters are collected
    /// </summary>
    public bool IsBonusWordComplete()
    {
        return collectedStatus.All(status => status);
    }

    /// <summary>
    /// Called when all BONUS letters are collected
    /// </summary>
    private void OnBonusWordCompleted()
    {
        if (treasureUnlocked) return; // Already unlocked
        
        treasureUnlocked = true;
        
        if (showDebug)
        {
            Debug.Log($"[BonusLetterManager] ✨ BONUS WORD COMPLETED! Unlocking treasure chest!");
        }
        
        // Award points
        if (GameManager.Instance != null && GameManager.Instance.bikeController != null)
        {
            GameManager.Instance.bikeController.AddFlipBonus(treasureChestPoints);
        }
        
        // Notify UI
        if (uiManager != null)
        {
            uiManager.OnBonusWordCompleted();
        }
    }

    /// <summary>
    /// Check if treasure chest should be shown (for level completion)
    /// </summary>
    public bool ShouldShowTreasureChest()
    {
        return IsBonusWordComplete() && treasureUnlocked;
    }

    /// <summary>
    /// Update bonus letters UI
    /// </summary>
    private void UpdateBonusLettersUI()
    {
        if (uiManager != null)
        {
            uiManager.UpdateBonusLetters(collectedStatus);
        }
    }

    /// <summary>
    /// Reset all letters (make them reappear, but keep collected status)
    /// </summary>
    public void ResetAllLetters()
    {
        foreach (var letter in bonusLetters)
        {
            if (letter != null)
            {
                // Check if this letter was collected
                int index = letter.GetLetterIndex();
                bool wasCollected = index >= 0 && index < collectedStatus.Length && collectedStatus[index];
                
                // Reset letter (will show/hide based on collected status)
                letter.ResetLetter();
                
                // If was collected, keep it hidden
                if (wasCollected)
                {
                    letter.SetCollectedWithoutNotify(true);
                }
            }
        }
        
        // Update UI
        UpdateBonusLettersUI();
        
        if (showDebug)
        {
            Debug.Log($"[BonusLetterManager] Letters reset. Collected: {totalCollected}/5");
        }
    }

    /// <summary>
    /// Fully reset everything (new game)
    /// </summary>
    public void FullReset()
    {
        // Reset collected status
        collectedStatus = new bool[5];
        totalCollected = 0;
        treasureUnlocked = false;
        
        // Reset all letters
        foreach (var letter in bonusLetters)
        {
            if (letter != null)
            {
                letter.ResetLetter();
            }
        }
        
        // Update UI
        UpdateBonusLettersUI();
        
        if (showDebug)
        {
            Debug.Log("[BonusLetterManager] Full reset complete");
        }
    }

    /// <summary>
    /// Get current collection status
    /// </summary>
    public bool[] GetCollectedStatus()
    {
        return (bool[])collectedStatus.Clone();
    }

    /// <summary>
    /// Get total collected count
    /// </summary>
    public int GetTotalCollected()
    {
        return totalCollected;
    }

    // Manual controls from Inspector
    [ContextMenu("Refresh Bonus Letters")]
    public void ManualRefresh()
    {
        RefreshBonusLetters();
    }

    [ContextMenu("Reset All Letters")]
    public void ManualResetLetters()
    {
        ResetAllLetters();
    }

    [ContextMenu("Full Reset")]
    public void ManualFullReset()
    {
        FullReset();
    }

    [ContextMenu("Collect All Letters (Debug)")]
    public void DebugCollectAll()
    {
        for (int i = 0; i < bonusLetters.Length; i++)
        {
            if (bonusLetters[i] != null)
            {
                bonusLetters[i].Collect();
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
