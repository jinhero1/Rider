using UnityEngine;

/// <summary>
/// Detects when player reaches the finish line
/// </summary>
public class FinishLine : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.Instance?.OnLevelComplete();
        }
    }
}