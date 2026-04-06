using System.Collections;
using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [SerializeField] private float _lifetime = 5f;

    [SerializeField] private float _speed = 12f;
    private int _damage;

    private Coroutine _lifeRoutine;
    private PoolManager _pool;

    private void Awake()
    {
        ManagerRegistry.TryGet(out _pool);
    }

    public void Initialize(float speed, int damage)
    {
        //_speed = speed;
        _damage = damage;
    }

    private void OnEnable()
    {
        if (_lifeRoutine != null)
        {
            StopCoroutine(_lifeRoutine);
        }

        _lifeRoutine = StartCoroutine(LifeReturnRoutine(_pool));
    }

    private void OnDisable()
    {
        if (_lifeRoutine != null)
        {
            StopCoroutine(_lifeRoutine);
            _lifeRoutine = null;
        }
    }

    private IEnumerator LifeReturnRoutine(PoolManager pool)
    {
        yield return new WaitForSeconds(_lifetime);

        if (pool != null)
        {
            pool.Return(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        transform.Translate(Vector2.right * _speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Player"))
        {
            col.GetComponent<IDamageable>()?.TakeDamage(_damage);
            ReturnToPool();
            return;
        }

        if (col.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            ReturnToPool();
            return;
        }
    }

    private void ReturnToPool()
    {
        if (ManagerRegistry.TryGet<PoolManager>(out var pool))
            pool.Return(gameObject);
        else
            Destroy(gameObject);
    }
}