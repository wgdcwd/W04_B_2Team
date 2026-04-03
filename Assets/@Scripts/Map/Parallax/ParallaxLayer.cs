using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    [SerializeField] private Vector2 multiplier = new(0.3f, 0.05f);
    [SerializeField] private float minYOffset = -0.5f;
    [SerializeField] private float maxYOffset = 1.5f;

    private float _startY;
    private float _startTargetY;

    public void Initialize(float startTargetY)
    {
        _startY = transform.position.y;
        _startTargetY = startTargetY;
    }

    public void Move(float deltaX, float targetY)
    {
        Vector3 position = transform.position;

        position.x += deltaX * multiplier.x;

        float yOffset = (targetY - _startTargetY) * multiplier.y;
        yOffset = Mathf.Clamp(yOffset, minYOffset, maxYOffset);
        position.y = _startY + yOffset;

        transform.position = position;
    }
}
