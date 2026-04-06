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
        if (transform.parent != null)
        {
            int siblingLoopCount = 0;

            foreach (Transform child in transform.parent)
            {
                if (child.GetComponent<ParallaxLoop>() != null)
                    siblingLoopCount++;
            }

            pieceCount = Mathf.Max(2, siblingLoopCount);
        }

        RecalculateLoopMetrics();
    }

    private void LateUpdate()
    {
        if (!TryResolveTarget() || _loopWidth <= 0f)
            return;

        float distance = target.position.x - transform.position.x;

        while (distance > _halfLoopWidth)
        {
            transform.position += Vector3.right * _loopWidth;
            distance = target.position.x - transform.position.x;
        }

        while (distance < -_halfLoopWidth)
        {
            transform.position += Vector3.left * _loopWidth;
            distance = target.position.x - transform.position.x;
        }
    }

    private void OnValidate()
    {
        if (pieceCount < 2)
            pieceCount = 2;

        RecalculateLoopMetrics();
    }

    private void RecalculateLoopMetrics()
    {
        _loopWidth = spriteWidth * pieceCount;
        _halfLoopWidth = _loopWidth * 0.5f;
    }

    private bool TryResolveTarget()
    {
        if (target != null)
            return true;

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            return false;

        target = mainCamera.transform;
        return true;
    }
}
