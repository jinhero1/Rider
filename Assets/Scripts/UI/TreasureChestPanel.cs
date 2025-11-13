using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TreasureChestPanel : MonoBehaviour
{
    private GameEventSystem eventSystem;

    [Header("Panel References")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Image chestImage;
    [SerializeField] private Button continueButton;
    
    [Header("Chest Animation")]
    [SerializeField] private Sprite closedChestSprite;
    [SerializeField] private Sprite openChestSprite;
    [SerializeField] private float openDelay = GameConstants.UI.CHEST_OPEN_DELAY;
    [SerializeField] private float openDuration = GameConstants.UI.CHEST_OPEN_DURATION;
    
    [Header("Particle Effects")]
    [SerializeField] private ParticleSystem rewardParticles;
    [SerializeField] private GameObject glowEffect;
    
    [Header("Animation Settings")]
    [SerializeField] private float scaleInDuration = GameConstants.UI.CHEST_SCALE_IN_DURATION;
    [SerializeField] private AnimationCurve scaleInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Audio")]
    [SerializeField] private AudioClip chestOpenSound;
    [SerializeField] private AudioClip rewardSound;
    
    [Header("Debug")]
    [SerializeField] private bool showDebug = true;
    
    private bool isShowing = false;
    private System.Action onContinueCallback;

    #region Initialization

    private void Awake()
    {
        InitializeServices();
    }

    private void Start()
    {
        SubscribeToEvents();
        SetupUI();
        HidePanel();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void InitializeServices()
    {
        eventSystem = ServiceLocator.Instance.Get<GameEventSystem>();

        if (eventSystem == null)
        {
            Debug.LogError("[TreasureChestPanel] GameEventSystem not found!");
        }
    }

    private void SubscribeToEvents()
    {
        if (eventSystem == null) return;

        eventSystem.Subscribe<ShowTreasureChestEvent>(OnShowTreasureChest);
    }

    private void UnsubscribeFromEvents()
    {
        if (eventSystem == null) return;

        eventSystem.Unsubscribe<ShowTreasureChestEvent>(OnShowTreasureChest);
    }

    private void SetupUI()
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnContinueClicked);
        }
        
        if (glowEffect != null)
        {
            glowEffect.SetActive(false);
        }
    }

    #endregion

    #region Event Handlers

    private void OnShowTreasureChest(ShowTreasureChestEvent evt)
    {
        if (showDebug)
        {
            //Debug.Log($"[TreasureChestPanel] Showing chest with reward: {evt.RewardPoints}");
        }

        ShowTreasureChest(() => {
            // After treasure chest animation, show level result
            eventSystem?.Publish(new UIRefreshRequestedEvent());
        });
    }

    #endregion

    #region Treasure Chest Display

    public void ShowTreasureChest(System.Action onContinue = null)
    {
        if (isShowing)
        {
            if (showDebug)
            {
                Debug.Log("[TreasureChestPanel] Already showing");
            }
            return;
        }
        
        isShowing = true;
        onContinueCallback = onContinue;
        
        if (showDebug)
        {
            Debug.Log("[TreasureChestPanel] Starting chest reveal");
        }
        
        // Show panel
        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }
        
        // Start animation sequence
        StartCoroutine(ChestRevealSequence());
    }

    public void HidePanel()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
        
        isShowing = false;
        
        // Reset for next time
        ResetChest();
    }

    private void ResetChest()
    {
        if (chestImage != null && closedChestSprite != null)
        {
            chestImage.sprite = closedChestSprite;
            chestImage.transform.localScale = Vector3.one;
        }
        
        if (glowEffect != null)
        {
            glowEffect.SetActive(false);
        }
        
        if (continueButton != null)
        {
            continueButton.interactable = false;
        }
    }

    #endregion

    #region Animation Sequence

    private IEnumerator ChestRevealSequence()
    {
        // Phase 1: Scale in panel
        if (panelRoot != null)
        {
            yield return StartCoroutine(ScaleInPanel());
        }
        
        // Phase 2: Wait before opening
        yield return new WaitForSeconds(openDelay);
        
        // Phase 3: Open chest
        yield return StartCoroutine(OpenChest());
        
        // Phase 4: Show rewards
        ShowRewards();
        
        // Enable continue button
        if (continueButton != null)
        {
            continueButton.interactable = true;
        }
    }

    private IEnumerator ScaleInPanel()
    {
        float elapsed = 0f;
        Vector3 startScale = Vector3.zero;
        Vector3 targetScale = Vector3.one;
        
        Transform panelTransform = panelRoot.transform;
        panelTransform.localScale = startScale;
        
        while (elapsed < scaleInDuration)
        {
            elapsed += Time.deltaTime;
            float t = scaleInCurve.Evaluate(elapsed / scaleInDuration);
            panelTransform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }
        
        panelTransform.localScale = targetScale;
    }

    private IEnumerator OpenChest()
    {
        if (chestImage == null) yield break;
        
        // Show closed chest first
        if (closedChestSprite != null)
        {
            chestImage.sprite = closedChestSprite;
        }
        
        // Play open sound
        if (chestOpenSound != null)
        {
            IAudioService audioService = ServiceLocator.Instance.Get<IAudioService>();
            audioService?.PlaySound(chestOpenSound, Camera.main.transform.position);
        }
        
        // Animate chest opening
        float elapsed = 0f;
        Vector3 startScale = chestImage.transform.localScale;
        
        while (elapsed < openDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / openDuration;
            
            // Bounce effect
            float scale = 1f + Mathf.Sin(t * Mathf.PI) * GameConstants.UI.CHEST_BOUNCE_MULTIPLIER;
            chestImage.transform.localScale = startScale * scale;
            
            // Change sprite halfway
            if (t > 0.5f && openChestSprite != null)
            {
                chestImage.sprite = openChestSprite;
            }
            
            yield return null;
        }
        
        chestImage.transform.localScale = startScale;
        
        // Set to open sprite
        if (openChestSprite != null)
        {
            chestImage.sprite = openChestSprite;
        }
    }

    private void ShowRewards()
    {
        // Play reward particles
        if (rewardParticles != null)
        {
            rewardParticles.Play();
        }
        
        // Show glow effect
        if (glowEffect != null)
        {
            glowEffect.SetActive(true);
        }
        
        // Play reward sound
        if (rewardSound != null)
        {
            IAudioService audioService = ServiceLocator.Instance.Get<IAudioService>();
            audioService?.PlaySound(rewardSound, Camera.main.transform.position);
        }
        
        if (showDebug)
        {
            Debug.Log("[TreasureChestPanel] Rewards revealed!");
        }
    }

    #endregion

    #region Button Handler

    private void OnContinueClicked()
    {
        if (showDebug)
        {
            Debug.Log("[TreasureChestPanel] Continue clicked");
        }
        
        HidePanel();
        onContinueCallback?.Invoke();
    }

    #endregion

    #region Debug

    [ContextMenu("Test Show Chest")]
    private void TestShowChest()
    {
        ShowTreasureChest(() => {
            Debug.Log("Test: Continue clicked");
        });
    }

    [ContextMenu("Test Hide Chest")]
    private void TestHideChest()
    {
        HidePanel();
    }

    #endregion
}
