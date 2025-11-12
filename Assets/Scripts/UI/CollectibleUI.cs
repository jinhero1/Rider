using UnityEngine;
using TMPro;

/// <summary>
/// Displays collectible counter
/// Single Responsibility: Collectible counter UI only
/// </summary>
public class CollectibleUI : MonoBehaviour
{
    private GameEventSystem eventSystem;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI collectibleCountText;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color completedColor = GameConstants.Colors.BONUS_YELLOW;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private void Awake()
    {
        InitializeServices();
    }

    private void Start()
    {
        SubscribeToEvents();
        UpdateDisplay(0, 0);
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
            Debug.LogError("[CollectibleUI] GameEventSystem not found!");
        }
    }

    private void SubscribeToEvents()
    {
        if (eventSystem == null) return;

        eventSystem.Subscribe<CollectibleProgressEvent>(OnCollectibleProgress);
    }

    private void UnsubscribeFromEvents()
    {
        if (eventSystem == null) return;

        eventSystem.Unsubscribe<CollectibleProgressEvent>(OnCollectibleProgress);
    }

    #endregion

    #region Event Handlers

    private void OnCollectibleProgress(CollectibleProgressEvent evt)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[CollectibleUI] Collectibles: {evt.CollectedCount}/{evt.TotalCount}");
        }

        UpdateDisplay(evt.CollectedCount, evt.TotalCount);
    }

    #endregion

    #region UI Update

    private void UpdateDisplay(int collected, int total)
    {
        if (collectibleCountText == null) return;

        collectibleCountText.text = $"{collected}";

        // Change color if all collected
        if (total > 0 && collected >= total)
        {
            collectibleCountText.color = completedColor;
        }
        else
        {
            collectibleCountText.color = normalColor;
        }
    }

    #endregion
}
