using UnityEngine;

/// <summary>
/// Score service implementation
/// Follows Single Responsibility Principle - only manages score
/// </summary>
public class ScoreService : IScoreService
{
    private readonly GameEventSystem eventSystem;
    private int currentScore;
    private int highScore;

    public int CurrentScore => currentScore;
    public int HighScore => highScore;

    public ScoreService(GameEventSystem eventSystem)
    {
        this.eventSystem = eventSystem;
        LoadHighScore();
    }

    public void AddScore(int points)
    {
        int previousScore = currentScore;
        currentScore += points;

        // Publish event
        eventSystem.Publish(new ScoreChangedEvent
        {
            CurrentScore = currentScore,
            DeltaScore = points
        });

        //Debug.Log($"[ScoreService] Score: {previousScore} -> {currentScore} (+{points})");
    }

    public void ResetScore()
    {
        int previousScore = currentScore;
        currentScore = 0;

        eventSystem.Publish(new ScoreChangedEvent
        {
            CurrentScore = currentScore,
            DeltaScore = -previousScore
        });

        Debug.Log("[ScoreService] Score reset to 0");
    }

    public void SaveHighScore()
    {
        if (currentScore > highScore)
        {
            highScore = currentScore;
            PlayerPrefs.SetInt(GameConstants.Score.HIGH_SCORE_PLAYER_PREF_KEY, highScore);
            PlayerPrefs.Save();
            Debug.Log($"[ScoreService] New high score saved: {highScore}");
        }
    }

    public void LoadHighScore()
    {
        highScore = PlayerPrefs.GetInt(GameConstants.Score.HIGH_SCORE_PLAYER_PREF_KEY, 0);
        Debug.Log($"[ScoreService] High score loaded: {highScore}");
    }
}

/// <summary>
/// Game state service implementation
/// Manages game state transitions with event notifications
/// </summary>
public class GameStateService : IGameStateService
{
    private readonly GameEventSystem eventSystem;
    private GameState currentState;

    public GameState CurrentState => currentState;

    public GameStateService(GameEventSystem eventSystem)
    {
        this.eventSystem = eventSystem;
        currentState = GameState.Playing;
    }

    public void ChangeState(GameState newState)
    {
        if (currentState == newState)
        {
            //Debug.LogWarning($"[GameStateService] Already in state: {newState}");
            return;
        }

        GameState previousState = currentState;
        currentState = newState;

        // Publish event
        eventSystem.Publish(new GameStateChangedEvent
        {
            PreviousState = previousState,
            NewState = newState
        });

        Debug.Log($"[GameStateService] State changed: {previousState} -> {newState}");
    }

    public bool IsState(GameState state)
    {
        return currentState == state;
    }
}

/// <summary>
/// Input service implementation
/// Centralizes all input handling
/// </summary>
public class InputService : IInputService
{
    public bool IsAccelerating => Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space);
    public bool IsRestartPressed => Input.GetKeyDown(KeyCode.R);
    public bool IsNextLevelPressed => Input.GetKeyDown(KeyCode.N);
    public bool IsPreviousLevelPressed => Input.GetKeyDown(KeyCode.P);
}

/// <summary>
/// Save service implementation
/// Wraps PlayerPrefs for easier testing and modification
/// </summary>
public class SaveService : ISaveService
{
    public void SaveInt(string key, int value)
    {
        PlayerPrefs.SetInt(key, value);
        PlayerPrefs.Save();
    }

    public int LoadInt(string key, int defaultValue = 0)
    {
        return PlayerPrefs.GetInt(key, defaultValue);
    }

    public void SaveFloat(string key, float value)
    {
        PlayerPrefs.SetFloat(key, value);
        PlayerPrefs.Save();
    }

    public float LoadFloat(string key, float defaultValue = 0f)
    {
        return PlayerPrefs.GetFloat(key, defaultValue);
    }

    public void SaveBool(string key, bool value)
    {
        PlayerPrefs.SetInt(key, value ? 1 : 0);
        PlayerPrefs.Save();
    }

    public bool LoadBool(string key, bool defaultValue = false)
    {
        return PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) == 1;
    }

    public void DeleteKey(string key)
    {
        PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
    }

    public void DeleteAll()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
    }
}

/// <summary>
/// Effect service implementation
/// Manages particle effects and visual feedback
/// </summary>
public class EffectService : IEffectService
{
    public void SpawnEffect(GameObject effectPrefab, Vector3 position, Quaternion rotation)
    {
        if (effectPrefab == null)
        {
            Debug.LogWarning("[EffectService] Effect prefab is null");
            return;
        }

        GameObject effect = Object.Instantiate(effectPrefab, position, rotation);
        Debug.Log($"[EffectService] Spawned effect at {position}");
    }

    public void SpawnEffect(GameObject effectPrefab, Vector3 position, float destroyAfter = 2f)
    {
        if (effectPrefab == null)
        {
            Debug.LogWarning("[EffectService] Effect prefab is null");
            return;
        }

        GameObject effect = Object.Instantiate(effectPrefab, position, Quaternion.identity);
        Object.Destroy(effect, destroyAfter);
        Debug.Log($"[EffectService] Spawned effect at {position}, destroying after {destroyAfter}s");
    }
}

/// <summary>
/// Audio service implementation
/// Centralizes audio playback
/// </summary>
public class AudioService : IAudioService
{
    private AudioSource musicSource;
    private float musicVolume = 1f;
    private float sfxVolume = 1f;

    public AudioService()
    {
        // Create music source
        GameObject musicObject = new GameObject("MusicSource");
        Object.DontDestroyOnLoad(musicObject);
        musicSource = musicObject.AddComponent<AudioSource>();
        musicSource.loop = true;
    }

    public void PlaySound(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null)
        {
            Debug.LogWarning("[AudioService] Audio clip is null");
            return;
        }

        AudioSource.PlayClipAtPoint(clip, position, volume * sfxVolume);
    }

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (clip == null)
        {
            Debug.LogWarning("[AudioService] Music clip is null");
            return;
        }

        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        musicSource.volume = musicVolume;
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
    }
}
