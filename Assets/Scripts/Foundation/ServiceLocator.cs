using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Service Locator pattern for dependency injection
/// Replaces Singleton anti-pattern throughout the codebase
/// Follows Dependency Inversion and Single Responsibility principles
/// </summary>
public class ServiceLocator
{
    private static ServiceLocator instance;
    private readonly Dictionary<Type, object> services = new Dictionary<Type, object>();

    /// <summary>
    /// Global access point (only for bootstrapping)
    /// In production code, inject dependencies through constructors
    /// </summary>
    public static ServiceLocator Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new ServiceLocator();
            }
            return instance;
        }
    }

    /// <summary>
    /// Register a service implementation
    /// </summary>
    public void Register<TService>(TService implementation) where TService : class
    {
        Type serviceType = typeof(TService);

        if (services.ContainsKey(serviceType))
        {
            Debug.LogWarning($"Service {serviceType.Name} is already registered. Overwriting.");
        }

        services[serviceType] = implementation;
        Debug.Log($"[ServiceLocator] Registered: {serviceType.Name}");
    }

    /// <summary>
    /// Register a service with interface
    /// </summary>
    public void Register<TInterface, TImplementation>(TImplementation implementation) 
        where TInterface : class 
        where TImplementation : class, TInterface
    {
        Type serviceType = typeof(TInterface);

        if (services.ContainsKey(serviceType))
        {
            Debug.LogWarning($"Service {serviceType.Name} is already registered. Overwriting.");
        }

        services[serviceType] = implementation;
        Debug.Log($"[ServiceLocator] Registered: {serviceType.Name} -> {typeof(TImplementation).Name}");
    }

    /// <summary>
    /// Unregister a service
    /// </summary>
    public void Unregister<TService>() where TService : class
    {
        Type serviceType = typeof(TService);

        if (services.ContainsKey(serviceType))
        {
            services.Remove(serviceType);
            Debug.Log($"[ServiceLocator] Unregistered: {serviceType.Name}");
        }
    }

    /// <summary>
    /// Get a registered service
    /// </summary>
    public TService Get<TService>() where TService : class
    {
        Type serviceType = typeof(TService);

        if (services.TryGetValue(serviceType, out object service))
        {
            return service as TService;
        }

        Debug.LogError($"[ServiceLocator] Service not found: {serviceType.Name}");
        return null;
    }

    /// <summary>
    /// Try to get a service (returns false if not found)
    /// </summary>
    public bool TryGet<TService>(out TService service) where TService : class
    {
        Type serviceType = typeof(TService);

        if (services.TryGetValue(serviceType, out object serviceObj))
        {
            service = serviceObj as TService;
            return service != null;
        }

        service = null;
        return false;
    }

    /// <summary>
    /// Check if a service is registered
    /// </summary>
    public bool IsRegistered<TService>() where TService : class
    {
        return services.ContainsKey(typeof(TService));
    }

    /// <summary>
    /// Clear all services (useful for cleanup/testing)
    /// </summary>
    public void Clear()
    {
        services.Clear();
        Debug.Log("[ServiceLocator] All services cleared");
    }

    /// <summary>
    /// Reset the service locator instance
    /// </summary>
    public static void Reset()
    {
        instance?.Clear();
        instance = null;
    }
}

/// <summary>
/// MonoBehaviour wrapper for ServiceLocator initialization
/// Place this on a GameObject in your scene to bootstrap services
/// </summary>
public class ServiceLocatorBootstrap : MonoBehaviour
{
    [Header("Auto-Register Services")]
    [SerializeField] private bool autoRegisterOnAwake = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private void Awake()
    {
        if (autoRegisterOnAwake)
        {
            RegisterCoreServices();
        }
    }

    /// <summary>
    /// Register all core game services
    /// Called during bootstrap
    /// </summary>
    private void RegisterCoreServices()
    {
        if (showDebugLogs)
        {
            Debug.Log("[ServiceLocatorBootstrap] Registering core services...");
        }

        // Register Event System
        GameEventSystem eventSystem = new GameEventSystem();
        ServiceLocator.Instance.Register(eventSystem);

        // Register Score Service
        IScoreService scoreService = new ScoreService(eventSystem);
        ServiceLocator.Instance.Register<IScoreService>(scoreService);

        // Register Game State Service
        IGameStateService gameStateService = new GameStateService(eventSystem);
        ServiceLocator.Instance.Register<IGameStateService>(gameStateService);

        if (showDebugLogs)
        {
            Debug.Log("[ServiceLocatorBootstrap] Core services registered successfully");
        }
    }

    private void OnDestroy()
    {
        if (showDebugLogs)
        {
            Debug.Log("[ServiceLocatorBootstrap] Cleaning up services...");
        }

        // Cleanup
        ServiceLocator.Instance.Clear();
    }

    /// <summary>
    /// Manual registration (call from Inspector or code)
    /// </summary>
    [ContextMenu("Register Services")]
    public void ManualRegisterServices()
    {
        RegisterCoreServices();
    }

    /// <summary>
    /// Clear all services
    /// </summary>
    [ContextMenu("Clear Services")]
    public void ClearServices()
    {
        ServiceLocator.Instance.Clear();
    }
}
