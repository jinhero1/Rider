using UnityEngine;

/// <summary>
/// Scriptable Object to store individual level data
/// </summary>
[CreateAssetMenu(fileName = "New Level", menuName = "Game/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Level Info")]
    [SerializeField] private string levelName = "Level 1";
    [SerializeField] private string levelTitle = "Basic Movement";
    [SerializeField] [TextArea(2, 4)] private string levelDescription = "Learn to ride your bike!";
    
    [Header("Level Scene")]
    [SerializeField] private string sceneName = "";
    [SerializeField] private int sceneIndex = 0;
    [SerializeField] private bool useSceneName = true;
    
    [Header("Feature Tags")]
    [SerializeField] private string[] featureTags = new string[] { };
    
    [Header("Visual")]
    [SerializeField] private Sprite levelIcon;
    [SerializeField] private Color levelColor = Color.white;
    
    // Properties
    public string LevelName => levelName;
    public string LevelTitle => levelTitle;
    public string LevelDescription => levelDescription;
    public string SceneName => sceneName;
    public int SceneIndex => sceneIndex;
    public bool UseSceneName => useSceneName;
    public string[] FeatureTags => featureTags;
    public Sprite LevelIcon => levelIcon;
    public Color LevelColor => levelColor;
    
    /// <summary>
    /// Get the scene identifier (name or index)
    /// </summary>
    public string GetSceneIdentifier()
    {
        return useSceneName ? sceneName : sceneIndex.ToString();
    }
}
