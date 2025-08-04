using UnityEngine;

public class MovingBlock : MonoBehaviour
{
    public Vector3 velocity;

    void Update()
    {
        transform.Translate(velocity * Time.deltaTime);
    }
}