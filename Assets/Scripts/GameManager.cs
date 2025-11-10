using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages game state, timer, and level completion logic
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    
    [Header("Game State")]
    public GameState currentState = GameState.Playing;
    
    [Header("Timer")]
    public float levelTime = 0f;
    public float threeStarTime = 12f;  // 3-star time threshold
    public float twoStarTime = 20f;    // 2-star time threshold
    
    [Header("References")]
    public BikeController bikeController;
    public UIManager uiManager;
    public Vector3 startPosition;
    
    [Header("Effects")]
    public GameObject crashEffectPrefab;
    public GameObject flipEffectPrefab;
    
    private bool levelStarted = false;

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
        }
    }

    void Start()
    {
        startPosition = bikeController.transform.position;
        currentState = GameState.Playing;
        levelStarted = true;
    }

    void Update()
    {
        // Update timer
        if (currentState == GameState.Playing && levelStarted)
        {
            levelTime += Time.deltaTime;
            uiManager?.UpdateTimer(levelTime);
        }
        
        // Restart hotkey
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartLevel();
        }
    }

    public void OnFlipCompleted()
    {
        // Flip completion effect
        if (flipEffectPrefab != null)
        {
            Instantiate(flipEffectPrefab, bikeController.transform.position, Quaternion.identity);
        }
        
        uiManager?.ShowFlipMessage();
    }

    public void OnCrash()
    {
        currentState = GameState.Crashed;

        // Play crash effect
        if (crashEffectPrefab != null)
        {
            Instantiate(crashEffectPrefab, bikeController.transform.position, Quaternion.identity);
        }

        bikeController.HideBike();

        // Screen shake
        CameraShake.Instance?.Shake(0.3f, 0.3f);
        
        uiManager?.ShowLevelFail(levelTime);
    }

    public void OnLevelComplete()
    {
        Debug.Log($"[GameManager] >>> OnLevelComplete() called at time: {levelTime:F2}s");
        Debug.Log($"[GameManager]     Current State: {currentState}");
        
        if (currentState == GameState.Completed)
        {
            Debug.Log("[GameManager] Already completed, returning");
            return;
        }
        
        Debug.Log("[GameManager] Setting state to Completed...");
        currentState = GameState.Completed;
        Debug.Log($"[GameManager] State changed to: {currentState}");
        
        // Stop bike immediately
        if (bikeController != null)
        {
            bikeController.StopBike();
            Debug.Log("[GameManager] Bike stopped");
        }
        
        // Calculate stars
        int stars = CalculateStars();
        Debug.Log($"[GameManager] Stars calculated: {stars} (Time: {levelTime:F2}s)");
        
        // Show completion UI
        if (uiManager != null)
        {
            Debug.Log("[GameManager] Calling UIManager.ShowLevelComplete()...");
            uiManager.ShowLevelComplete(levelTime, stars);
            Debug.Log("[GameManager] ✓ Level complete UI should be visible now");
        }
        else
        {
            Debug.LogError("[GameManager] UIManager is NULL! Cannot show completion screen");
            Debug.LogError("  → Assign UIManager in GameManager Inspector");
        }
    }

    int CalculateStars()
    {
        if (levelTime <= threeStarTime)
            return 3;
        else if (levelTime <= twoStarTime)
            return 2;
        else
            return 1;
    }

    public void RestartLevel()
    {
        CancelInvoke();
        levelTime = 0f;
        currentState = GameState.Playing;
        bikeController.ResetBike();
        uiManager?.HideLevelResult();
    }

    public void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}

public enum GameState
{
    Playing,
    Crashed,
    Completed
}