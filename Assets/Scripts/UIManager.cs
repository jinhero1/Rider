using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UIManager : MonoBehaviour
{
    [Header("In-Game UI")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI flipMessageText;
    [SerializeField] private TextMeshProUGUI bonusScoreText;
    [SerializeField] private TextMeshProUGUI collectibleCountText;

    [Header("Score Animation")]
    [SerializeField] private Color bonusColor = Color.yellow;
    [SerializeField] private float bonusFontSizeMultiplier = 1.5f;

    [Header("Completion Screen")]
    [SerializeField] private GameObject levelResultPanel;
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private Button nextButton;
    
    private float flipMessageTimer = 0f;
    private bool isAnimatingBonus = false;
    private int currentDisplayScore = 0;

    void Start()
    {
        if (levelResultPanel != null)
            levelResultPanel.SetActive(false);
            
        if (flipMessageText != null)
            flipMessageText.gameObject.SetActive(false);
            
        if (bonusScoreText != null)
            bonusScoreText.gameObject.SetActive(false);
        
        // Initialize collectible counter
        if (collectibleCountText != null)
        {
            collectibleCountText.text = "0";
        }
            
        // Setup button events            
        if (nextButton != null)
            nextButton.onClick.AddListener(() => GameManager.Instance?.RestartLevel());
    }

    void Update()
    {
        // Flip message timer
        if (flipMessageTimer > 0f)
        {
            flipMessageTimer -= Time.deltaTime;
            if (flipMessageTimer <= 0f && flipMessageText != null)
            {
                flipMessageText.gameObject.SetActive(false);
            }
        }
    }

    public void UpdateScore(int score)
    {
        if (scoreText != null && !isAnimatingBonus)
        {
            currentDisplayScore = score;
            scoreText.text = score.ToString();
        }
    }

    public void ShowFlipMessage(int flipCount)
    {
        if (flipMessageText != null)
        {
            flipMessageText.text = $"FLIP +{flipCount}!";
            flipMessageText.gameObject.SetActive(true);
            flipMessageTimer = 1f;
        }
        
        ShowFlipBonus(flipCount);
    }

    public void ShowFlipBonus(int bonusPoints)
    {
        if (isAnimatingBonus) return;
        
        StartCoroutine(AnimateFlipBonus(bonusPoints));
    }

    private IEnumerator AnimateFlipBonus(int bonusPoints)
    {
        isAnimatingBonus = true;
        
        int startScore = currentDisplayScore;
        int targetScore = startScore + bonusPoints;
        
        if (bonusScoreText != null)
        {
            bonusScoreText.gameObject.SetActive(true);
            bonusScoreText.text = $"+{bonusPoints}";
            bonusScoreText.color = bonusColor;
            bonusScoreText.transform.position = scoreText.transform.position + new Vector3(100f, 0f, 0f);
            
            float originalSize = bonusScoreText.fontSize;
            bonusScoreText.fontSize = originalSize * bonusFontSizeMultiplier;
        }
        
        yield return new WaitForSeconds(0.3f);
        
        if (bonusScoreText != null)
        {
            float elapsed = 0f;
            Vector3 startPos = bonusScoreText.transform.position;
            Vector3 targetPos = scoreText.transform.position + new Vector3(50f, 30f, 0f);
            float startSize = bonusScoreText.fontSize;
            
            while (elapsed < 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / 0.5f;
                
                bonusScoreText.transform.position = Vector3.Lerp(startPos, targetPos, t);
                bonusScoreText.fontSize = Mathf.Lerp(startSize, startSize * 0.5f, t);

                Color color = bonusScoreText.color;
                color.a = Mathf.Lerp(1f, 0f, t);
                bonusScoreText.color = color;
                
                yield return null;
            }
            
            bonusScoreText.gameObject.SetActive(false);
        }

        if (scoreText != null)
        {
            float elapsed = 0f;
            float duration = 0.5f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                int displayScore = Mathf.RoundToInt(Mathf.Lerp(startScore, targetScore, t));
                scoreText.text = displayScore.ToString();
                
                float scale = 1f + Mathf.Sin(t * Mathf.PI * 4f) * 0.1f;
                scoreText.transform.localScale = Vector3.one * scale;
                
                yield return null;
            }
            
            scoreText.text = targetScore.ToString();
            scoreText.transform.localScale = Vector3.one;
        }
        
        currentDisplayScore = targetScore;
        isAnimatingBonus = false;
    }

    private void ShowFinalScoreText(int score)
    {
        if (finalScoreText != null)
        {
            finalScoreText.text = $"Best Score\n{score}";
        }
    }

    public void ShowLevelResult(int score)
    {
        if (levelResultPanel != null)
        {
            levelResultPanel.SetActive(true);
        }

        ShowFinalScoreText(score);
    }

    public void HideLevelResult()
    {
        if (levelResultPanel != null)
        {
            levelResultPanel.SetActive(false);
        }

        if (scoreText != null)
        {
            scoreText.transform.localScale = Vector3.one;
        }
        
        currentDisplayScore = 0;
    }

    /// <summary>
    /// Update the collectible counter display
    /// </summary>
    public void UpdateCollectibleCount(int collected, int total)
    {
        if (collectibleCountText != null)
        {
            collectibleCountText.text = collected.ToString();
        }
    }
}