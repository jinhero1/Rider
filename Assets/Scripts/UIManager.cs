using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages all UI element display and updates
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("In-Game UI")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI flipMessageText;

    [Header("Completion Screen")]
    [SerializeField] private GameObject levelCompletePanel;
    [SerializeField] private GameObject levelFailPanel;
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button nextButton;
    
    private float flipMessageTimer = 0f;

    void Start()
    {
        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(false);
        if (levelFailPanel != null)
            levelFailPanel.SetActive(false);
            
        if (flipMessageText != null)
            flipMessageText.gameObject.SetActive(false);
            
        // Setup button events
        if (retryButton != null)
            retryButton.onClick.AddListener(() => GameManager.Instance?.RestartLevel());
            
        if (nextButton != null)
            nextButton.onClick.AddListener(() => Debug.Log("Next Level"));
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
        if (scoreText != null)
        {
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
    }

    private void ShowFinalScoreText(int score)
    {
        if (finalScoreText != null)
        {
            finalScoreText.text = score.ToString();
        }
    }

    public void ShowLevelComplete(int score)
    {
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(true);
        }

        ShowFinalScoreText(score);
    }
    
    public void ShowLevelFail(int score)
    {
        if (levelFailPanel != null)
        {
            levelFailPanel.SetActive(true);
        }

        ShowFinalScoreText(score);
    }

    public void HideLevelResult()
    {
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(false);
        }

        if (levelFailPanel != null)
        {
            levelFailPanel.SetActive(false);
        }
    }
}