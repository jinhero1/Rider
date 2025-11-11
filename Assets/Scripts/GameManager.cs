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
    
    [Header("References")]
    public BikeController bikeController;
    public UIManager uiManager;
    public Vector3 startPosition;
    
    [Header("Effects")]
    public GameObject crashEffectPrefab;
    public GameObject flipEffectPrefab;

    [Header("Score System")]
    public int highScore = 0;
    public int flipBonusPoints = 1;

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
        
        highScore = PlayerPrefs.GetInt("HighScore", 0);
        Debug.Log($"[GameManager] High Score: {highScore}");
    }

    void Update()
    {
        // Update timer
        if (currentState == GameState.Playing && levelStarted)
        {
            uiManager?.UpdateScore(GetCurrentScore());
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

        bikeController.AddFlipBonus(flipBonusPoints);
        
        uiManager?.ShowFlipMessage(flipBonusPoints);

        Debug.Log($"[GameManager] Flip completed! Bonus: +{flipBonusPoints}");
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

        uiManager?.ShowLevelResult(highScore);
    }
    
    private int GetCurrentScore()
    {
        return bikeController.GetScore();
    }

    public void OnLevelComplete()
    {
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
        
        // Show completion UI
        if (uiManager != null)
        {
            int currentScore = GetCurrentScore();
            if (currentScore > highScore)
            {
                highScore = currentScore;
                PlayerPrefs.SetInt("HighScore", highScore);
                PlayerPrefs.Save();
            }

            Debug.Log("[GameManager] Calling UIManager.ShowLevelComplete()...");
            uiManager.ShowLevelResult(highScore);
            Debug.Log("[GameManager] ✓ Level complete UI should be visible now");
        }
        else
        {
            Debug.LogError("[GameManager] UIManager is NULL! Cannot show completion screen");
            Debug.LogError("  → Assign UIManager in GameManager Inspector");
        }
    }

    public void RestartLevel()
    {
        CancelInvoke();
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