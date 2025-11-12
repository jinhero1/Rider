using UnityEngine;

public enum GameState
{
    Playing,
    Crashed,
    Completed
}

/// <summary>
/// Interface for score management
/// Follows Interface Segregation Principle - focused on score operations only
/// </summary>
public interface IScoreService
{
    int CurrentScore { get; }
    int HighScore { get; }

    void AddScore(int points);
    void ResetScore();
    void SaveHighScore();
    void LoadHighScore();
}

/// <summary>
/// Interface for game state management
/// Follows Single Responsibility Principle
/// </summary>
public interface IGameStateService
{
    GameState CurrentState { get; }
    
    void ChangeState(GameState newState);
    bool IsState(GameState state);
}

/// <summary>
/// Interface for collectible management
/// </summary>
public interface ICollectibleService
{
    int CollectedCount { get; }
    int TotalCount { get; }
    float CollectionProgress { get; }
    
    void RegisterCollectible(ICollectible collectible);
    void OnCollectibleCollected(ICollectible collectible);
    void ResetCollectibles();
    void RefreshCollectibles();
}

/// <summary>
/// Interface for individual collectible items
/// </summary>
public interface ICollectible
{
    bool IsCollected { get; }
    Vector3 Position { get; }
    
    void Collect();
    void Reset();
}

/// <summary>
/// Interface for bonus letter management
/// </summary>
public interface IBonusLetterService
{
    bool[] CollectedLetters { get; }
    int TotalCollected { get; }
    bool IsWordComplete { get; }
    
    void OnLetterCollected(int letterIndex);
    void ResetLetters();
    void RefreshLetters();
}

/// <summary>
/// Interface for player control
/// </summary>
public interface IPlayerController
{
    bool IsCrashed { get; }
    int FlipCount { get; }
    float CurrentSpeed { get; }
    Vector3 Position { get; }
    
    void Crash();
    void ResetPlayer();
    void StopPlayer();
}

/// <summary>
/// Interface for track/level management
/// </summary>
public interface ITrackService
{
    int CurrentTrackIndex { get; }
    int TotalTracks { get; }
    
    void LoadTrack(int trackIndex);
    void LoadNextTrack();
    void LoadPreviousTrack();
}

/// <summary>
/// Interface for UI updates (reactive pattern)
/// </summary>
public interface IUIService
{
    void ShowScore(int score);
    void ShowFlipMessage(int flipCount);
    void ShowLevelResult(int finalScore, int highScore);
    void HideLevelResult();
    void UpdateCollectibles(int collected, int total);
    void UpdateBonusLetters(bool[] collectedLetters);
    void ShowTreasureChest(System.Action onContinue);
}

/// <summary>
/// Interface for audio management
/// </summary>
public interface IAudioService
{
    void PlaySound(AudioClip clip, Vector3 position, float volume = 1f);
    void PlayMusic(AudioClip clip, bool loop = true);
    void StopMusic();
    void SetMusicVolume(float volume);
    void SetSFXVolume(float volume);
}

/// <summary>
/// Interface for camera control
/// </summary>
public interface ICameraService
{
    void SetTarget(Transform target);
    void Shake(float duration, float intensity);
    void ResetCamera();
}

/// <summary>
/// Interface for effect spawning
/// </summary>
public interface IEffectService
{
    void SpawnEffect(GameObject effectPrefab, Vector3 position, Quaternion rotation);
    void SpawnEffect(GameObject effectPrefab, Vector3 position, float destroyAfter = 2f);
}

/// <summary>
/// Interface for input handling
/// </summary>
public interface IInputService
{
    bool IsAccelerating { get; }
    bool IsRestartPressed { get; }
    bool IsNextLevelPressed { get; }
    bool IsPreviousLevelPressed { get; }
}

/// <summary>
/// Interface for save/load management
/// </summary>
public interface ISaveService
{
    void SaveInt(string key, int value);
    int LoadInt(string key, int defaultValue = 0);
    void SaveFloat(string key, float value);
    float LoadFloat(string key, float defaultValue = 0f);
    void SaveBool(string key, bool value);
    bool LoadBool(string key, bool defaultValue = false);
    void DeleteKey(string key);
    void DeleteAll();
}
