using UnityEngine;

public class AutoDestroy : MonoBehaviour
{
    [SerializeField] private float lifetime = 1f;
    
    void Start()
    {
        Destroy(gameObject, lifetime);
    }
}