using UnityEngine;
using TMPro;

/// <summary>
/// Displays flip completion messages
/// Single Responsibility: Flip message UI only
/// </summary>
public class FlipMessageUI : MonoBehaviour
{
    private GameEventSystem eventSystem;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI flipMessageText;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private float messageTimer = 0f;

    private void Awake()
    {
        InitializeServices();
    }

    private void Start()
    {
        SubscribeToEvents();
        
        if (flipMessageText != null)
        {
            flipMessageText.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        UpdateMessageTimer();
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
            Debug.LogError("[FlipMessageUI] GameEventSystem not found!");
        }
    }

    private void SubscribeToEvents()
    {
        if (eventSystem == null) return;

        eventSystem.Subscribe<FlipCompletedEvent>(OnFlipCompleted);
    }

    private void UnsubscribeFromEvents()
    {
        if (eventSystem == null) return;

        eventSystem.Unsubscribe<FlipCompletedEvent>(OnFlipCompleted);
    }

    #endregion

    #region Event Handlers

    private void OnFlipCompleted(FlipCompletedEvent evt)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[FlipMessageUI] Showing flip message: +{evt.BonusPoints}");
        }

        ShowFlipMessage(evt.BonusPoints);
    }

    #endregion

    #region UI Update

    private void ShowFlipMessage(int bonusPoints)
    {
        if (flipMessageText == null) return;

        flipMessageText.text = $"FLIP +{bonusPoints}!";
        flipMessageText.gameObject.SetActive(true);
        messageTimer = GameConstants.UI.FLIP_MESSAGE_DURATION;
    }

    private void UpdateMessageTimer()
    {
        if (messageTimer > 0f)
        {
            messageTimer -= Time.deltaTime;
            
            if (messageTimer <= 0f && flipMessageText != null)
            {
                flipMessageText.gameObject.SetActive(false);
            }
        }
    }

    #endregion
}
