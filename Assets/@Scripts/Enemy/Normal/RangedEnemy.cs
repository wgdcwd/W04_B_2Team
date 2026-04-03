using System.Collections;
using UnityEngine;

public class RangedEnemy : NormalEnemyBase
{
    [SerializeField] protected GameObject _projectilePrefab;
    [SerializeField] protected float _projectileSpeed = 8f;
    [SerializeField] protected float _retreatRange = 2f;
    [SerializeField] protected float _preferredRange = 4f;
    [SerializeField] protected Transform _gunPivot;

    [Header("연사 설정")]
    [SerializeField] private int _burstCount = 1;
    [SerializeField] private float _burstInterval = 0.15f;

    protected PoolManager _pool;
    private bool _isBursting = false;

    protected override void Start()
    {
        base.Start();
        ManagerRegistry.TryGet(out _pool);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        _isBursting = false;
    }

    protected override void Update()
    {
        if (_isDead || !CanAct()) return;

        if (!TryFindPlayer())
        {
            _wasDetecting = false;
            _rb.linearVelocity = Vector2.zero;
            Patrol();
            return;
        }

        bool detecting = DetectPlayer();

        if (detecting)
        {
            if (!_wasDetecting)
                RaiseAlerted();

            _wasDetecting = true;

            float dist = Vector2.Distance(transform.position, _player.position);

            // 총구 방향 (발사 중 고정)
            if (_gunPivot != null && !_isBursting)
            {
                Vector2 aimDir = ((Vector2)_player.position - (Vector2)transform.position).normalized;
                _gunPivot.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg);
            }

            if (!_isBursting)
            {
                if (dist <= _retreatRange)
                {
                    Vector2 retreatDir = ((Vector2)transform.position - (Vector2)_player.position).normalized;
                    MoveToward((Vector2)transform.position + retreatDir);
                }
                else if (dist <= _attackRange)
                {
                    _rb.linearVelocity = Vector2.zero;
                }
                else if (dist <= _preferredRange)
                {
                    _rb.linearVelocity = Vector2.zero;
                }
                else
                {
                    MoveToward(_player.position);
                }
            }
            else
            {
                _rb.linearVelocity = Vector2.zero;
            }

            if (dist <= _attackRange && _canAttack)
                StartCoroutine(AttackRoutine());
        }
        else
        {
            if (_wasDetecting)
            {
                _wasDetecting = false;
                _originalPos = transform.position;
                _patrolTarget = GetRandomPatrolTarget();
            }

            Patrol();
        }
    }

    protected override IEnumerator AttackRoutine()
    {
        _canAttack = false;
        yield return StartCoroutine(BurstRoutine());
        yield return new WaitForSeconds(_attackCooldown);
        _canAttack = true;
    }

    protected override void DoAttack() { }

    IEnumerator BurstRoutine()
    {
        if (!TryFindPlayer()) yield break;
        if (_projectilePrefab == null)
        {
            Debug.LogWarning($"{gameObject.name}: 투사체가 없습니다.");
            yield break;
        }

        _isBursting = true;
        _rb.linearVelocity = Vector2.zero;

        Vector2 dir = ((Vector2)_player.position - (Vector2)transform.position).normalized;
        Quaternion rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

        for (int i = 0; i < _burstCount; i++)
        {
            GameObject projectile = _pool != null
                ? _pool.Get(_projectilePrefab, transform.position, rotation)
                : Instantiate(_projectilePrefab, transform.position, rotation);

            projectile.GetComponent<EnemyProjectile>()?.Initialize(_projectileSpeed, _attackDamage);

            if (i < _burstCount - 1)
                yield return new WaitForSeconds(_burstInterval);
        }

        _isBursting = false;
    }

    protected override IEnumerator OnDieRoutine()
    {
        yield return new WaitForSeconds(0.3f);
    }
}
