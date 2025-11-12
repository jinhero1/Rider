using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Handles score display and animations
/// Single Responsibility: Score UI only
/// </summary>
public class ScoreUI : MonoBehaviour
{
    private GameEventSystem eventSystem;
    private IScoreService scoreService;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI bonusScoreText;

    [Header("Animation Settings")]
    [SerializeField] private Color bonusColor = GameConstants.Colors.BONUS_YELLOW;
    [SerializeField] private float bonusFontSizeMultiplier = GameConstants.UI.BONUS_FONT_SIZE_MULTIPLIER;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private bool isAnimatingBonus = false;
    private int currentDisplayScore = 0;

    private void Awake()
    {
        InitializeServices();
    }

    private void Start()
    {
        SubscribeToEvents();
        UpdateScoreDisplay(0);
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
            Debug.LogError("[ScoreUI] GameEventSystem not found!");
        }
    }

    private void SubscribeToEvents()
    {
        if (eventSystem == null) return;

        eventSystem.Subscribe<ScoreChangedEvent>(OnScoreChanged);
        eventSystem.Subscribe<FlipCompletedEvent>(OnFlipCompleted);
    }

    private void UnsubscribeFromEvents()
    {
        if (eventSystem == null) return;

        eventSystem.Unsubscribe<ScoreChangedEvent>(OnScoreChanged);
        eventSystem.Unsubscribe<FlipCompletedEvent>(OnFlipCompleted);
    }

    #endregion

    #region Event Handlers

    private void OnScoreChanged(ScoreChangedEvent evt)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[ScoreUI] Score changed: {evt.CurrentScore} ({evt.DeltaScore:+#;-#;0})");
        }

        if (!isAnimatingBonus)
        {
            UpdateScoreDisplay(evt.CurrentScore);
        }
    }

    private void OnFlipCompleted(FlipCompletedEvent evt)
    {
        if (evt.BonusPoints > 0)
        {
            StartCoroutine(AnimateFlipBonus(evt.BonusPoints));
        }
    }

    #endregion

    #region UI Update

    private void UpdateScoreDisplay(int score)
    {
        if (scoreText != null)
        {
            currentDisplayScore = score;
            scoreText.text = score.ToString();
        }
    }

    #endregion

    #region Animations

    private IEnumerator AnimateFlipBonus(int bonusPoints)
    {
        if (isAnimatingBonus) yield break;
        
        isAnimatingBonus = true;
        
        int startScore = currentDisplayScore;
        int targetScore = startScore + bonusPoints;
        
        // Show bonus text
        if (bonusScoreText != null)
        {
            bonusScoreText.gameObject.SetActive(true);
            bonusScoreText.text = $"+{bonusPoints}";
            bonusScoreText.color = bonusColor;
            
            if (scoreText != null)
            {
                bonusScoreText.transform.position = scoreText.transform.position + new Vector3(100f, 0f, 0f);
            }
            
            float originalSize = bonusScoreText.fontSize;
            bonusScoreText.fontSize = originalSize * bonusFontSizeMultiplier;
        }
        
        yield return new WaitForSeconds(GameConstants.UI.BONUS_ANIMATION_DELAY);
        
        // Animate bonus text movement
        if (bonusScoreText != null && scoreText != null)
        {
            float elapsed = 0f;
            Vector3 startPos = bonusScoreText.transform.position;
            Vector3 targetPos = scoreText.transform.position + new Vector3(50f, 30f, 0f);
            float startSize = bonusScoreText.fontSize;
            
            while (elapsed < GameConstants.UI.BONUS_ANIMATION_DURATION)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / GameConstants.UI.BONUS_ANIMATION_DURATION;
                
                bonusScoreText.transform.position = Vector3.Lerp(startPos, targetPos, t);
                bonusScoreText.fontSize = Mathf.Lerp(startSize, startSize * 0.5f, t);

                Color color = bonusScoreText.color;
                color.a = Mathf.Lerp(1f, 0f, t);
                bonusScoreText.color = color;
                
                yield return null;
            }
            
            bonusScoreText.gameObject.SetActive(false);
        }

        // Animate score counter
        if (scoreText != null)
        {
            float elapsed = 0f;
            
            while (elapsed < GameConstants.UI.BONUS_ANIMATION_DURATION)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / GameConstants.UI.BONUS_ANIMATION_DURATION;
                
                int displayScore = Mathf.RoundToInt(Mathf.Lerp(startScore, targetScore, t));
                scoreText.text = displayScore.ToString();
                
                float scale = 1f + Mathf.Sin(t * Mathf.PI * GameConstants.UI.SCORE_PULSE_FREQUENCY) * GameConstants.UI.SCORE_SCALE_PULSE;
                scoreText.transform.localScale = Vector3.one * scale;
                
                yield return null;
            }
            
            scoreText.text = targetScore.ToString();
            scoreText.transform.localScale = Vector3.one;
        }
        
        currentDisplayScore = targetScore;
        isAnimatingBonus = false;
    }

    #endregion
}
