using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Treasure chest reveal panel that shows before level complete screen
/// </summary>
public class TreasureChestPanel : MonoBehaviour
{
    [Header("Panel References")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Image chestImage;
    [SerializeField] private Button continueButton;
    
    [Header("Chest Animation")]
    [SerializeField] private Sprite closedChestSprite;
    [SerializeField] private Sprite openChestSprite;
    [SerializeField] private float openDelay = 0.5f;
    [SerializeField] private float openDuration = 0.3f;
    
    [Header("Particle Effects")]
    [SerializeField] private ParticleSystem rewardParticles;
    [SerializeField] private GameObject glowEffect;
    
    [Header("Animation Settings")]
    [SerializeField] private float scaleInDuration = 0.5f;
    [SerializeField] private AnimationCurve scaleInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Audio")]
    [SerializeField] private AudioClip chestOpenSound;
    [SerializeField] private AudioClip rewardSound;
    
    [Header("Debug")]
    [SerializeField] private bool showDebug = true;
    
    private bool isShowing = false;
    private System.Action onContinueCallback;

    void Start()
    {
        // Hide panel initially
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
        
        // Setup button
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueClicked);
        }
        
        // Hide glow effect initially
        if (glowEffect != null)
        {
            glowEffect.SetActive(false);
        }
    }

    public void ShowTreasureChest(System.Action onContinue = null)
    {
        if (isShowing)
        {
            if (showDebug)
            {
                Debug.Log("[TreasureChestPanel] Already showing, ignoring");
            }
            return;
        }
        
        isShowing = true;
        onContinueCallback = onContinue;
        
        if (showDebug)
        {
            Debug.Log("[TreasureChestPanel] Showing treasure chest panel");
        }
        
        // Show panel
        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }
        
        // Start animation sequence
        StartCoroutine(ChestRevealSequence());
    }

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
            AudioSource.PlayClipAtPoint(chestOpenSound, Camera.main.transform.position);
        }
        
        // Animate chest opening
        float elapsed = 0f;
        Vector3 startScale = chestImage.transform.localScale;
        
        while (elapsed < openDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / openDuration;
            
            // Bounce effect
            float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.2f;
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
            AudioSource.PlayClipAtPoint(rewardSound, Camera.main.transform.position);
        }
        
        // Camera shake
        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake(0.3f, 0.3f);
        }
        
        if (showDebug)
        {
            Debug.Log("[TreasureChestPanel] Rewards revealed!");
        }
    }

    private void OnContinueClicked()
    {
        if (showDebug)
        {
            Debug.Log("[TreasureChestPanel] Continue button clicked");
        }
        
        // Hide panel
        HidePanel();
        
        // Invoke callback (show level result panel)
        onContinueCallback?.Invoke();
    }

    public void HidePanel()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
        
        isShowing = false;
        
        // Reset for next time
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

    public bool IsShowing()
    {
        return isShowing;
    }

    // Test from Inspector
    [ContextMenu("Test Show Chest")]
    public void TestShowChest()
    {
        ShowTreasureChest(() => {
            Debug.Log("Test: Continue clicked");
        });
    }

    [ContextMenu("Test Hide Chest")]
    public void TestHideChest()
    {
        HidePanel();
    }
}
