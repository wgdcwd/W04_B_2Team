using UnityEngine;

public class Explosives : MonoBehaviour
{
    [SerializeField] LayerMask _interactionMask;
    [SerializeField] float _explosionRadius;
    [SerializeField] int _explosionDamage;

    [Header("Effect")]
    [SerializeField] private GameObject _explosionParticlePrefab;

    private TNTSpawner _spawner;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Bullet bullet = collision.GetComponent<Bullet>();
        if (bullet != null)
        {
            Explosion();
            Destroy(gameObject);
        }
    }

    public void SetSpawner(TNTSpawner spawner)
    {
        _spawner = spawner;
    }

    public void Explosion()
    {
        _spawner?.OnTNTExploded(); // Destroy 전에 먼저 호출
        SpawnExplosionParticle();

        Collider2D[] _hits = Physics2D.OverlapCircleAll(transform.position, _explosionRadius, _interactionMask);
        foreach (Collider2D _hit in _hits)
        {
            // 버려야 할 코드.
            if (_hit.TryGetComponent<PlayerAttack>(out var playerAttack))
            {
                Vector2 dir = ((Vector2)(_hit.transform.position - transform.position)).normalized;
                playerAttack.ReceiveExplosionForce(dir, _explosionDamage);
            }
        }
    }

    private void SpawnExplosionParticle()
    {
        if (_explosionParticlePrefab == null) return;

        GameObject go = Instantiate(_explosionParticlePrefab, transform.position, Quaternion.identity);

        // 폭발 반경에 맞게 스케일 조절
        float scale = _explosionRadius * 0.2f;
        go.transform.localScale = new Vector3(scale, scale, scale);
    }

    public void Test()
    {
        Explosion();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _explosionRadius);
    }
}
