using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Displays BONUS letters UI
/// Single Responsibility: BONUS letters display only
/// </summary>
public class BonusLetterUI : MonoBehaviour
{
    private GameEventSystem eventSystem;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI[] bonusLetterTexts = new TextMeshProUGUI[GameConstants.Collectibles.BONUS_LETTER_COUNT];

    [Header("Colors")]
    [SerializeField] private Color collectedColor = GameConstants.Colors.BONUS_YELLOW;
    [SerializeField] private Color uncollectedColor = GameConstants.Colors.UNCOLLECTED_LETTER_GRAY;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private readonly string[] letterNames = { "B", "O", "N", "U", "S" };

    private void Awake()
    {
        InitializeServices();
    }

    private void Start()
    {
        SubscribeToEvents();
        InitializeLetterDisplay();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    #region Initialization

    private void InitializeServices()
    {
        eventSystem = ServiceLocator.Instance.Get<GameEventSystem>();

        if (eventSystem == null)
        {
            Debug.LogError("[BonusLetterUI] GameEventSystem not found!");
        }
    }

    private void SubscribeToEvents()
    {
        if (eventSystem == null) return;

        eventSystem.Subscribe<BonusLetterCollectedEvent>(OnLetterCollected);
        eventSystem.Subscribe<BonusWordCompletedEvent>(OnWordCompleted);
    }

    private void UnsubscribeFromEvents()
    {
        if (eventSystem == null) return;

        eventSystem.Unsubscribe<BonusLetterCollectedEvent>(OnLetterCollected);
        eventSystem.Unsubscribe<BonusWordCompletedEvent>(OnWordCompleted);
    }

    private void InitializeLetterDisplay()
    {
        for (int i = 0; i < bonusLetterTexts.Length && i < letterNames.Length; i++)
        {
            if (bonusLetterTexts[i] != null)
            {
                bonusLetterTexts[i].text = letterNames[i];
                bonusLetterTexts[i].color = uncollectedColor;
            }
        }
    }

    #endregion

    #region Event Handlers

    private void OnLetterCollected(BonusLetterCollectedEvent evt)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[BonusLetterUI] Letter collected: {evt.LetterName} ({evt.TotalCollected}/5)");
        }

        UpdateLetterDisplay(evt.LetterIndex, true);
    }

    private void OnWordCompleted(BonusWordCompletedEvent evt)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[BonusLetterUI] BONUS word completed! Treasure: {evt.TreasurePoints}");
        }

        StartCoroutine(PlayWordCompletionAnimation());
    }

    #endregion

    #region UI Update

    private void UpdateLetterDisplay(int letterIndex, bool isCollected)
    {
        if (letterIndex < 0 || letterIndex >= bonusLetterTexts.Length) return;
        if (bonusLetterTexts[letterIndex] == null) return;

        bonusLetterTexts[letterIndex].color = isCollected ? collectedColor : uncollectedColor;
    }

    public void UpdateAllLetters(bool[] collectedStatus)
    {
        if (collectedStatus == null || collectedStatus.Length != GameConstants.Collectibles.BONUS_LETTER_COUNT)
        {
            return;
        }

        for (int i = 0; i < bonusLetterTexts.Length && i < collectedStatus.Length; i++)
        {
            UpdateLetterDisplay(i, collectedStatus[i]);
        }
    }

    #endregion

    #region Animations

    private IEnumerator PlayWordCompletionAnimation()
    {
        // Pulse each letter sequentially
        for (int i = 0; i < bonusLetterTexts.Length; i++)
        {
            if (bonusLetterTexts[i] != null)
            {
                StartCoroutine(PulseLetter(bonusLetterTexts[i]));
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    private IEnumerator PulseLetter(TextMeshProUGUI letterText)
    {
        Color originalColor = letterText.color;
        Vector3 originalScale = letterText.transform.localScale;
        
        float elapsed = 0f;
        float duration = 0.5f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Glow effect
            letterText.color = Color.Lerp(originalColor, Color.white, Mathf.Sin(t * Mathf.PI * 2f));
            
            // Scale pulse
            float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.2f;
            letterText.transform.localScale = originalScale * scale;

            yield return null;
        }
        
        letterText.color = originalColor;
        letterText.transform.localScale = originalScale;
    }

    #endregion
}
