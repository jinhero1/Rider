using UnityEngine;

public class CrashDetector : MonoBehaviour
{
    [SerializeField] private BikeController bikeController;
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Ground"))
        {
            Debug.Log("[CrashDetector] Head hit ground - CRASH!");
            
            if (bikeController != null)
            {
                bikeController.Crash();
            }
        }
    }
}