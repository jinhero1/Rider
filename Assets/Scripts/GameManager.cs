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
        
        // Screen shake
        CameraShake.Instance?.Shake(0.3f, 0.3f);
        
        // Delayed restart
        Invoke(nameof(RestartLevel), 0.5f);
    }

    public void OnLevelComplete()
    {
        if (currentState == GameState.Completed) return;
        
        currentState = GameState.Completed;
        
        // Calculate stars
        int stars = CalculateStars();
        
        // Show completion UI
        uiManager?.ShowLevelComplete(levelTime, stars);
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
        uiManager?.HideLevelComplete();
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