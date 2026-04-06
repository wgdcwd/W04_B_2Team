using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    [SerializeField] private Vector2 multiplier = new(0.3f, 0.05f);
    [SerializeField] private float minYOffset = -0.5f;
    [SerializeField] private float maxYOffset = 1.5f;

    private Vector3 _startLocalPosition;
    private Vector3 _startTargetPosition;

    public void Initialize(Vector3 startTargetPosition)
    {
        _startLocalPosition = transform.localPosition;
        _startTargetPosition = startTargetPosition;
    }

    public void Move(Vector3 targetPosition)
    {
        Vector3 localPosition = _startLocalPosition;
        Vector3 targetDelta = targetPosition - _startTargetPosition;

        localPosition.x += targetDelta.x * multiplier.x;

        float yOffset = targetDelta.y * multiplier.y;
        yOffset = Mathf.Clamp(yOffset, minYOffset, maxYOffset);
        localPosition.y += yOffset;

        transform.localPosition = localPosition;
    }
}
