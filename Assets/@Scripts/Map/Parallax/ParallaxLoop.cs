using UnityEngine;

public class ParallaxLoop : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float spriteWidth = 20f;
    [SerializeField] private int pieceCount = 3;

    private float _loopWidth;
    private float _halfLoopWidth;

    private void Awake()
    {
        if (target == null && Camera.main != null)
            target = Camera.main.transform;

        if (transform.parent != null)
            pieceCount = Mathf.Max(2, transform.parent.childCount);

        _loopWidth = spriteWidth * pieceCount;
        _halfLoopWidth = _loopWidth * 0.5f;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        float distance = target.position.x - transform.position.x;

        if (distance > _halfLoopWidth)
            transform.position += Vector3.right * _loopWidth;

        else if (distance < -_halfLoopWidth)
            transform.position += Vector3.left * _loopWidth;
    }

    private void OnValidate()
    {
        if (pieceCount < 2)
            pieceCount = 2;
    }
}
