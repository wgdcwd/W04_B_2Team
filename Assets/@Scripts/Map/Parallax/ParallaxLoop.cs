using UnityEngine;

public class ParallaxLoop : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float spriteWidth = 20f;

    private void Awake()
    {
        if (target == null && Camera.main != null)
            target = Camera.main.transform;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        float distance = target.position.x - transform.position.x;

        if (distance > spriteWidth)
            transform.position += Vector3.right * spriteWidth;

        else if (distance < -spriteWidth)
            transform.position += Vector3.left * spriteWidth;
    }
}
