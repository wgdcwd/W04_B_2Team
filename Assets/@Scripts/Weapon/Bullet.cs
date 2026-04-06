using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Bullet : MonoBehaviour
{
    [SerializeField] private int _damage = 1;
    [SerializeField] private float _lifetime = 1f;
    [SerializeField] private bool _isPiercing = false; // 관통 여부
    [SerializeField] private bool _giveGauge = true;

    // Hit 효과
    [SerializeField] private GameObject _hitParticlePrefab;
    [SerializeField] private Light2D _light;

    private Coroutine _lifeRoutine;
    private Rigidbody2D _rb;
    private PoolManager _pool;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        ManagerRegistry.TryGet(out _pool);
    }

    private void OnEnable()
    {
        if (_lifeRoutine != null)
            StopCoroutine(_lifeRoutine);

        _lifeRoutine = StartCoroutine(LifeReturnRoutine(_pool));
    }

    private void OnDisable()
    {
        if (_lifeRoutine != null)
        {
            StopCoroutine(_lifeRoutine);
            _lifeRoutine = null;
        }

        if (_rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
        }
    }

    #region Pool Return 코루틴
    private IEnumerator LifeReturnRoutine(PoolManager pool)
    {
        yield return new WaitForSeconds(_lifetime);

        if (pool != null)
            pool.Return(gameObject);

        else
            Destroy(gameObject);
    }
    #endregion

    private void OnTriggerEnter2D(Collider2D other)
    {
        int otherLayer = other.gameObject.layer;
        if (other.GetComponent<BreakablePlatform>() )
        {
            return;
        }

        if (otherLayer == LayerMask.NameToLayer("Ground") || otherLayer == LayerMask.NameToLayer("Obstacle"))
        {
            if (_isPiercing) return; // 관통이면 무시
            SpawnHitParticle();
            ReturnToPool();
            return;
        }

        if (otherLayer == LayerMask.NameToLayer("Enemy") || other.CompareTag("Enemy")) //보스 에임 보정 관련때문에 CompareTag 추가
        {
            if (other.TryGetComponent<EnemyBase>(out var damageable))
                damageable.TakeDamage(_damage, _giveGauge);
            SpawnHitParticle();
            ReturnToPool();
        }
    }

    // Study
    private void SpawnHitParticle()
    {
        if (_hitParticlePrefab == null) return;

        // 날라온 방향의 반대로 날림.
        //Vector2 dir = _rb.linearVelocity.normalized;
        //float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg -90f;
        //Quaternion rotation = Quaternion.Euler(angle, 90f, 0f);

        Vector2 dir = -_rb.linearVelocity.normalized;

        // forward에서 dir 방향으로 회전
        Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, new Vector3(dir.x, dir.y, 0f));


        if (_pool != null)
            _pool.Get(_hitParticlePrefab, transform.position, rotation);
        else
            Instantiate(_hitParticlePrefab, transform.position, rotation);
    }


    //Helper
    private void ReturnToPool()
    {
        if (ManagerRegistry.TryGet<PoolManager>(out var pool))
            pool.Return(gameObject);
        else
            Destroy(gameObject);
    }
}
