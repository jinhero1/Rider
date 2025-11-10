using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages all UI element display and updates
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("In-Game UI")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI flipMessageText;

    [Header("Completion Screen")]
    [SerializeField] private GameObject levelCompletePanel;
    [SerializeField] private GameObject levelFailPanel;
    [SerializeField] private TextMeshProUGUI finalTimeText;
    [SerializeField] private GameObject[] stars; // 3 star objects
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

    public void UpdateTimer(float time)
    {
        if (timerText != null)
        {
            timerText.text = $"Time: {time:F2}s";
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

    private void ShowFinalTimeText(float finalTime)
    {
        if (finalTimeText != null)
        {
            finalTimeText.text = $"Time: {finalTime:F2}s";
        }        
    }

    public void ShowLevelComplete(float finalTime, int starCount)
    {
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(true);
        }

        ShowFinalTimeText(finalTime);        

        // Display stars
        for (int i = 0; i < stars.Length; i++)
        {
            if (stars[i] != null)
            {
                stars[i].SetActive(i < starCount);
            }
        }
    }
    
    public void ShowLevelFail(float finalTime)
    {
        if (levelFailPanel != null)
        {
            levelFailPanel.SetActive(true);
        }

        ShowFinalTimeText(finalTime); 
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