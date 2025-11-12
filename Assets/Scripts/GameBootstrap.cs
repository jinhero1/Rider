using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    void Awake()
    {
        // Register Event System
        var eventSystem = new GameEventSystem();
        ServiceLocator.Instance.Register(eventSystem);
        
        // Register Core Services
        var scoreService = new ScoreService(eventSystem);
        ServiceLocator.Instance.Register<IScoreService>(scoreService);
        
        var stateService = new GameStateService(eventSystem);
        ServiceLocator.Instance.Register<IGameStateService>(stateService);
        
        var inputService = new InputService();
        ServiceLocator.Instance.Register<IInputService>(inputService);
        
        var saveService = new SaveService();
        ServiceLocator.Instance.Register<ISaveService>(saveService);
        
        var effectService = new EffectService();
        ServiceLocator.Instance.Register<IEffectService>(effectService);
        
        var audioService = new AudioService();
        ServiceLocator.Instance.Register<IAudioService>(audioService);
        
        Debug.Log("[GameBootstrap] All services registered!");
    }
}
