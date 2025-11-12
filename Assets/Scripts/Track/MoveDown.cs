using UnityEngine;

public class MoveDown : MonoBehaviour
{
    public float speed = 1f;

    void Update()
    {
        transform.position += Vector3.down * speed * Time.deltaTime;
    }
}
