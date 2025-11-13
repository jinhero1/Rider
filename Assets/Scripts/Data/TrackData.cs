using UnityEngine;

/// <summary>
/// Scriptable Object to store individual track/level data (Prefab-based)
/// </summary>
[CreateAssetMenu(fileName = "New Track", menuName = "Game/Track Data")]
public class TrackData : ScriptableObject
{
    [Header("Track Info")]
    [SerializeField] private string trackName = "Track 1";
    [SerializeField] private string trackTitle = "Basic Movement";
    
    [Header("Track Prefab")]
    [SerializeField] private GameObject trackPrefab;
    
    [Header("Track Settings")]
    [SerializeField] private Vector3 spawnPosition = Vector3.zero;
    
    // Properties
    public string TrackName => trackName;
    public string TrackTitle => trackTitle;
    public GameObject TrackPrefab => trackPrefab;
    public Vector3 SpawnPosition => spawnPosition;
    
    /// <summary>
    /// Validate that prefab is assigned
    /// </summary>
    public bool IsValid()
    {
        if (trackPrefab == null)
        {
            Debug.LogError($"[TrackData] {trackName} has no Track Prefab assigned!");
            return false;
        }
        return true;
    }
}
