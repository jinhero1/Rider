using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CrashDetector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BikeController bikeController;
    
    [Header("Debug")]
    [SerializeField] private bool showDebug = true;

    private void Awake()
    {
        ValidateSetup();
    }

    private void ValidateSetup()
    {
        if (bikeController == null)
        {
            bikeController = GetComponentInParent<BikeController>();
            
            if (bikeController == null)
            {
                Debug.LogError("[CrashDetector] BikeController not found! Assign it in Inspector or ensure it's in parent.");
            }
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            Debug.LogError("[CrashDetector] No Collider2D found! Add one to detect crashes.");
        }
        else if (!col.isTrigger)
        {
            Debug.LogWarning("[CrashDetector] Collider should be a trigger for proper detection.");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(GameConstants.Tags.GROUND))
        {
            TriggerCrash();
        }
    }

    private void TriggerCrash()
    {
        if (showDebug)
        {
            Debug.Log("[CrashDetector] Head hit ground - CRASH!");
        }
        
        if (bikeController != null)
        {
            bikeController.Crash();
        }
        else
        {
            Debug.LogError("[CrashDetector] Cannot crash - BikeController is null!");
        }
    }
}
