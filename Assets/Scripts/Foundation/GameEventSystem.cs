using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Event-based notification system for decoupling components
/// Replaces tight coupling through direct references and Singleton calls
/// Follows Observer pattern and Dependency Inversion Principle
/// </summary>
public class GameEventSystem
{
    private readonly Dictionary<Type, List<Delegate>> eventListeners = new Dictionary<Type, List<Delegate>>();

    /// <summary>
    /// Subscribe to an event
    /// </summary>
    public void Subscribe<TEvent>(Action<TEvent> listener) where TEvent : struct
    {
        Type eventType = typeof(TEvent);

        if (!eventListeners.ContainsKey(eventType))
        {
            eventListeners[eventType] = new List<Delegate>();
        }

        eventListeners[eventType].Add(listener);
    }

    /// <summary>
    /// Unsubscribe from an event
    /// </summary>
    public void Unsubscribe<TEvent>(Action<TEvent> listener) where TEvent : struct
    {
        Type eventType = typeof(TEvent);

        if (eventListeners.ContainsKey(eventType))
        {
            eventListeners[eventType].Remove(listener);
        }
    }

    /// <summary>
    /// Publish an event to all subscribers
    /// </summary>
    public void Publish<TEvent>(TEvent eventData) where TEvent : struct
    {
        Type eventType = typeof(TEvent);

        if (eventListeners.ContainsKey(eventType))
        {
            foreach (Delegate listener in eventListeners[eventType])
            {
                try
                {
                    (listener as Action<TEvent>)?.Invoke(eventData);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error invoking event listener for {eventType.Name}: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Clear all event listeners (useful for cleanup)
    /// </summary>
    public void ClearAllListeners()
    {
        eventListeners.Clear();
    }

    /// <summary>
    /// Clear listeners for a specific event type
    /// </summary>
    public void ClearListeners<TEvent>() where TEvent : struct
    {
        Type eventType = typeof(TEvent);
        if (eventListeners.ContainsKey(eventType))
        {
            eventListeners[eventType].Clear();
        }
    }
}

// ===== GAME EVENTS =====
// All game events as structs (value types for performance)

/// <summary>
/// Event fired when player completes a flip
/// </summary>
public struct FlipCompletedEvent
{
    public int TotalFlipCount;
    public int BonusPoints;
    public Vector3 Position;
}

/// <summary>
/// Event fired when player crashes
/// </summary>
public struct PlayerCrashedEvent
{
    public Vector3 CrashPosition;
    public GameObject CrashObject;
}

/// <summary>
/// Event fired when level is completed
/// </summary>
public struct LevelCompletedEvent
{
    public int FinalScore;
    public int FlipCount;
    public int CollectiblesCollected;
    public float CompletionTime;
}

/// <summary>
/// Event fired when collectible is collected
/// </summary>
public struct CollectibleCollectedEvent
{
    public int CollectedCount;
    public int TotalCount;
    public Vector3 Position;
}

public struct CollectibleProgressEvent
{
    public int CollectedCount;
    public int TotalCount;
}

/// <summary>
/// Event fired when BONUS letter is collected
/// </summary>
public struct BonusLetterCollectedEvent
{
    public int LetterIndex;
    public string LetterName;
    public int TotalCollected;
}

/// <summary>
/// Event fired when BONUS word is completed
/// </summary>
public struct BonusWordCompletedEvent
{
    public int TreasurePoints;
}

/// <summary>
/// Event fired when score changes
/// </summary>
public struct ScoreChangedEvent
{
    public int CurrentScore;
    public int DeltaScore;
}

/// <summary>
/// Event fired when game state changes
/// </summary>
public struct GameStateChangedEvent
{
    public GameState PreviousState;
    public GameState NewState;
}

/// <summary>
/// Event fired when level/track is loaded
/// </summary>
public struct TrackLoadedEvent
{
    public string TrackName;
    public int TrackIndex;
    public Vector3 SpawnPosition;
}

/// <summary>
/// Event fired when spike is hit
/// </summary>
public struct SpikeHitEvent
{
    public Vector3 HitPosition;
    public GameObject SpikeObject;
}

/// <summary>
/// Event fired when boost platform is activated
/// </summary>
public struct BoostActivatedEvent
{
    public Vector3 BoostPosition;
    public Vector2 BoostDirection;
    public float BoostForce;
}

/// <summary>
/// Event fired when player requests level restart
/// </summary>
public struct LevelRestartRequestedEvent
{
}

/// <summary>
/// Event fired when player requests next level
/// </summary>
public struct NextLevelRequestedEvent
{
}

/// <summary>
/// Event fired when treasure chest should be shown
/// </summary>
public struct ShowTreasureChestEvent
{
    public int RewardPoints;
}

/// <summary>
/// Event fired when UI needs to update
/// </summary>
public struct UIRefreshRequestedEvent
{
}
