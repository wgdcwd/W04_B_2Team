using UnityEngine;

public class BossLaserImpactEffect : MonoBehaviour
{
    [SerializeField] private float _lifeTime = 0.25f;

    void OnEnable()
    {
        CancelInvoke(nameof(DestroySelf));
        Invoke(nameof(DestroySelf), _lifeTime);
    }

    void DestroySelf()
    {
        Destroy(gameObject);
    }
}
