using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    [SerializeField] private Vector2 multiplier = new(0.3f, 0f);

    public void Move(Vector3 delta)
    {
        Vector3 move = new(
            delta.x * multiplier.x,
            delta.y * multiplier.y,
            0f);

        transform.position += move;
    }
}