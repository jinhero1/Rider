using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages BONUS letter collection and treasure chest reveal
/// </summary>
public class BonusLetterManager : MonoBehaviour
{
    public static BonusLetterManager Instance;
    
    [Header("Letter Management")]
    [SerializeField] private List<BonusLetter> allLetters = new List<BonusLetter>();
    [SerializeField] private bool autoFindLetters = true;
    
    [Header("Collection Status")]
    [SerializeField] private bool[] collectedLetters = new bool[5]; // B, O, N, U, S
    [SerializeField] private int totalCollected = 0;
    [SerializeField] private bool allLettersCollected = false;
    
    [Header("Treasure Chest")]
    [SerializeField] private bool treasureChestShown = false;
    
    [Header("Debug")]
    [SerializeField] private bool showDebug = true;

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
    }

    void Start()
    {
        InitializeLetters();
    }

    private void InitializeLetters()
    {
        // Auto-find all bonus letters in the scene
        if (autoFindLetters)
        {
            BonusLetter[] foundLetters = FindObjectsOfType<BonusLetter>();
            allLetters.Clear();
            allLetters.AddRange(foundLetters);
            
            if (showDebug)
            {
                Debug.Log($"[BonusLetterManager] Auto-found {foundLetters.Length} bonus letters");
            }
        }
        
        // Initialize collection status
        collectedLetters = new bool[5];
        totalCollected = 0;
        allLettersCollected = false;
        treasureChestShown = false;
        
        if (showDebug)
        {
            Debug.Log($"[BonusLetterManager] Initialized with {allLetters.Count} letters");
        }
        
        // Update UI
        UpdateUI();
    }

    public void OnLetterCollected(BonusLetter letter)
    {
        if (!allLetters.Contains(letter))
        {
            Debug.LogWarning($"[BonusLetterManager] Collected letter not in list: {letter.gameObject.name}");
            return;
        }
        
        // Mark this letter type as collected
        int letterIndex = (int)letter.GetLetterType();
        
        if (collectedLetters[letterIndex])
        {
            if (showDebug)
            {
                Debug.Log($"[BonusLetterManager] Letter {letter.GetLetterType()} already collected");
            }
            return;
        }
        
        collectedLetters[letterIndex] = true;
        totalCollected++;
        
        if (showDebug)
        {
            Debug.Log($"[BonusLetterManager] ★ Letter {letter.GetLetterType()} collected! ({totalCollected}/5)");
        }
        
        // Update UI
        UpdateUI();
        
        // Check if all letters collected
        if (totalCollected >= 5 && !allLettersCollected)
        {
            OnAllLettersCollected();
        }
    }

    private void OnAllLettersCollected()
    {
        allLettersCollected = true;
        
        if (showDebug)
        {
            Debug.Log("<color=yellow>[BonusLetterManager] ★★★ ALL BONUS LETTERS COLLECTED! ★★★</color>");
        }
        
        // Notify UI
        if (GameManager.Instance != null && GameManager.Instance.uiManager != null)
        {
            GameManager.Instance.uiManager.OnBonusWordCompleted();
        }
    }

    private void UpdateUI()
    {
        if (GameManager.Instance != null && GameManager.Instance.uiManager != null)
        {
            GameManager.Instance.uiManager.UpdateBonusLetters(collectedLetters);
        }
    }

    public void ResetAllLetters()
    {
        foreach (BonusLetter letter in allLetters)
        {
            if (letter != null)
            {
                letter.ResetLetter();
            }
        }

        collectedLetters = new bool[5];
        totalCollected = 0;
        allLettersCollected = false;
        treasureChestShown = false;

        if (showDebug)
        {
            Debug.Log($"[BonusLetterManager] All letters reset. Collected status preserved.");
        }
        
        UpdateUI();
    }

    public bool AreAllLettersCollected()
    {
        return allLettersCollected;
    }

    public bool IsLetterCollected(BonusLetterType letterType)
    {
        return collectedLetters[(int)letterType];
    }

    public int GetCollectedCount()
    {
        return totalCollected;
    }

    public bool[] GetCollectedLetters()
    {
        return collectedLetters;
    }

    public void SetTreasureChestShown(bool shown)
    {
        treasureChestShown = shown;
    }

    public bool IsTreasureChestShown()
    {
        return treasureChestShown;
    }

    // Check if player should see treasure chest when completing level
    public bool ShouldShowTreasureChest()
    {
        return allLettersCollected && !treasureChestShown;
    }

    // Manual reset from Inspector
    [ContextMenu("Reset All Letters")]
    public void ManualResetLetters()
    {
        ResetAllLetters();
    }

    [ContextMenu("Collect All (Test)")]
    public void TestCollectAll()
    {
        for (int i = 0; i < 5; i++)
        {
            collectedLetters[i] = true;
        }
        totalCollected = 5;
        allLettersCollected = true;
        UpdateUI();
        Debug.Log("[BonusLetterManager] TEST: All letters collected");
    }

    [ContextMenu("Reset Collection Status")]
    public void ResetCollectionStatus()
    {
        collectedLetters = new bool[5];
        totalCollected = 0;
        allLettersCollected = false;
        treasureChestShown = false;
        UpdateUI();
        Debug.Log("[BonusLetterManager] Collection status reset");
    }

    // Visualize in Scene view
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        
        // Draw collection progress
        Gizmos.color = Color.yellow;
        float progress = totalCollected / 5f;
        
        Vector3 barStart = transform.position + Vector3.up * 2f + Vector3.left * 2.5f;
        Vector3 barEnd = barStart + Vector3.right * 5f;
        
        // Background
        Gizmos.color = Color.gray;
        Gizmos.DrawLine(barStart, barEnd);
        
        // Progress
        Gizmos.color = allLettersCollected ? Color.green : Color.yellow;
        Vector3 progressEnd = Vector3.Lerp(barStart, barEnd, progress);
        Gizmos.DrawLine(barStart, progressEnd);
        
        // Draw letter indicators
        for (int i = 0; i < 5; i++)
        {
            Vector3 letterPos = barStart + Vector3.right * (i * 1.25f);
            Gizmos.color = collectedLetters[i] ? Color.green : Color.red;
            Gizmos.DrawWireSphere(letterPos, 0.2f);
        }
    }

    void OnDrawGizmosSelected()
    {
        #if UNITY_EDITOR
        // Show collection status
        if (Application.isPlaying)
        {
            string status = "B O N U S\n";
            for (int i = 0; i < 5; i++)
            {
                status += collectedLetters[i] ? "✓ " : "✗ ";
            }
            
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 4f,
                status
            );
        }
        #endif
    }
}
