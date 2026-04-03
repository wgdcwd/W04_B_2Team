using System.Collections;
using UnityEngine;

public abstract class NormalEnemyBase : EnemyBase
{
    // =====================
    // 스탯
    // =====================
    [SerializeField] protected float _moveSpeed = 5f;
    [SerializeField] protected float _attackRange = 1.5f;
    [SerializeField] protected float _detectionRange = 5f;
    [SerializeField] protected float _attackCooldown = 1.5f;
    [SerializeField] protected bool _isFlying = false;
    [SerializeField] protected bool _isAmmoEnemy = false;
    // =====================
    // 순찰
    // =====================
    [SerializeField] protected float _patrolRadius = 3f;
    [SerializeField] protected LayerMask _wallLayer;
    [SerializeField] protected LayerMask _groundLayer;
    [SerializeField] protected float _edgeCheckDistance = 0.5f;
    [SerializeField] protected float _edgeCheckDepth = 1f;

    protected Vector2 _originalPos;
    protected Vector2 _patrolTarget;
    protected bool _isDead = false;
    protected bool _canAttack = true;
    protected bool _isAddGauge = false;
    protected bool _wasDetecting = false;
    protected bool _isBlockedByEdge = false;

    protected Transform _player;

    // =====================
    // 피격 연출
    // =====================
    [Header("Hit Effect")]
    [SerializeField] private ParticleSystem _hitParticle;
    [SerializeField] private int _hitFlashCount = 3;
    [SerializeField] private float _hitFlashInterval = 0.08f;
    [SerializeField] private Color _hitFlashColor = Color.black;

    protected SpriteRenderer _spriteRenderer;
    protected Color _originalColor;
    private Coroutine _flashCoroutine;

    // =====================
    // 생명주기
    // =====================
    protected override void Start()
    {
        base.Start();
        Initialize();

        TryFindPlayer();
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        _originalColor = _spriteRenderer.color;

        _originalPos = transform.position;
        _patrolTarget = GetRandomPatrolTarget();
    }

    protected virtual void OnEnable()
    {
        if (_rb == null)
            _rb = GetComponent<Rigidbody2D>();

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = true;

        if (_rb != null)
            _rb.linearVelocity = Vector2.zero;

        _isDead = false;
        _canAttack = true;
        _isAddGauge = false;
        _wasDetecting = false;
        _isBlockedByEdge = false;
        _currentHp = _maxHp;

        _player = null;
        TryFindPlayer();

        _originalPos = transform.position;
        _patrolTarget = GetRandomPatrolTarget();

        ShowMark(false);
    }

    protected virtual void OnDisable()
    {
        _player = null;
        StopAllCoroutines();

        if (_rb != null)
            _rb.linearVelocity = Vector2.zero;
    }

    protected bool TryFindPlayer()
    {
        if (_player != null) return true;

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj == null) return false;

        _player = playerObj.transform;
        return _player != null;
    }

    protected virtual void Update()
    {
        if (_isDead) return;

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

            if (IsInAttackRange())
            {
                _rb.linearVelocity = Vector2.zero;
                if (_canAttack && CanAct())
                    StartCoroutine(AttackRoutine());
            }
            else
            {
                // 절벽 막힌 상태여도 MoveToward 호출해서 방향 재체크
                MoveToward(_player.position);

                // 절벽 앞에 막혀있으면 공격도 시도
                if (_isBlockedByEdge && _canAttack && CanAct())
                    StartCoroutine(AttackRoutine());
            }
        }
        else
        {
            _isBlockedByEdge = false;

            if (_wasDetecting)
            {
                _wasDetecting = false;
                _originalPos = transform.position;
                _patrolTarget = GetRandomPatrolTarget();
            }

            Patrol();
        }
    }

    protected override void Initialize()
    {
        base.Initialize();
        ShowMark(false);
    }

    // =====================
    // 순찰
    // =====================
    protected void Patrol()
    {
        MoveToward(_patrolTarget);

        float dist = _isFlying
            ? Vector2.Distance(transform.position, _patrolTarget)
            : Mathf.Abs(transform.position.x - _patrolTarget.x);

        if (dist < 0.2f)
            _patrolTarget = GetRandomPatrolTarget();
    }

    protected Vector2 GetRandomPatrolTarget()
    {
        float randomX = Random.Range(-_patrolRadius, _patrolRadius);
        return new Vector2(_originalPos.x + randomX, _originalPos.y);
    }

    // =====================
    // 낭떠러지 감지
    // =====================
    protected bool IsEdgeAhead(Vector2 moveDir)
    {
        if (_isFlying) return false;
        if (_groundLayer == 0) return false;

        float scaledDistance = _edgeCheckDistance * Mathf.Abs(transform.localScale.x);
        float scaledDepth = _edgeCheckDepth * Mathf.Abs(transform.localScale.y);

        Vector2 ahead = (Vector2)transform.position + new Vector2(moveDir.x * scaledDistance, 0f);
        RaycastHit2D hit = Physics2D.Raycast(ahead, Vector2.down, scaledDepth, _groundLayer);

        return hit.collider == null;
    }

    // =====================
    // 이동
    // =====================
    protected virtual void MoveToward(Vector2 target)
    {
        Vector2 dir = (target - (Vector2)transform.position).normalized;

        if (!_isFlying && IsEdgeAhead(dir))
        {
            _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
            _isBlockedByEdge = true;
            return;
        }

        // 절벽 없으면 플래그 해제 후 정상 이동
        _isBlockedByEdge = false;

        if (_isFlying)
            _rb.linearVelocity = dir * _moveSpeed;
        else
            _rb.linearVelocity = new Vector2(dir.x * _moveSpeed, _rb.linearVelocity.y);
    }

    // =====================
    // 감지
    // =====================
    protected virtual bool DetectPlayer()
    {
        if (!TryFindPlayer()) return false;

        float dist = Vector2.Distance(transform.position, _player.position);
        if (dist > _detectionRange) return false;

        if (_wallLayer == 0) return true;

        Vector2 dir = ((Vector2)_player.position - (Vector2)transform.position).normalized;
        Vector2 origin = (Vector2)transform.position + Vector2.up * 0.2f;
        RaycastHit2D hit = Physics2D.Raycast(origin, dir, dist, _wallLayer);
        return hit.collider == null;
    }

    protected bool IsInAttackRange()
    {
        if (!TryFindPlayer()) return false;
        return Vector2.Distance(transform.position, _player.position) <= _attackRange;
    }

    // =====================
    // 전투
    // =====================
    

    protected abstract void DoAttack();

    protected virtual IEnumerator AttackRoutine()
    {
        _canAttack = false;
        DoAttack();
        yield return new WaitForSeconds(_attackCooldown);
        _canAttack = true;
    }

    // =====================
    // 디버그
    // =====================
    protected virtual void OnDrawGizmosSelected()
    {
        Vector2 origin = Application.isPlaying ? _originalPos : (Vector2)transform.position;

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(origin, new Vector3(_patrolRadius * 2f, 0.2f, 0f));

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _attackRange);

        if (_player != null)
        {
            Vector2 dir = ((Vector2)_player.position - (Vector2)transform.position).normalized;
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, (Vector2)transform.position + dir * _detectionRange);
        }

        Gizmos.color = Color.cyan;
        Vector2 aheadR = (Vector2)transform.position + new Vector2(_edgeCheckDistance, 0f);
        Vector2 aheadL = (Vector2)transform.position + new Vector2(-_edgeCheckDistance, 0f);
        Gizmos.DrawLine(aheadR, aheadR + Vector2.down * _edgeCheckDepth);
        Gizmos.DrawLine(aheadL, aheadL + Vector2.down * _edgeCheckDepth);
    }

    public override void TakeDamage(int damage, bool isAddGauge = false)
    {
        if (_hitParticle != null) _hitParticle.Play();
        if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
        _flashCoroutine = StartCoroutine(HitFlashRoutine());
        _isAddGauge = isAddGauge;
        if (_isAmmoEnemy)
        {
            _player.GetComponent<PlayerAttack>().AddAmmo();
            return;
        }
        base.TakeDamage(damage);
    }

    private IEnumerator HitFlashRoutine()
    {
        for (int i = 0; i < _hitFlashCount; i++)
        {
            _spriteRenderer.color = _hitFlashColor;
            yield return new WaitForSeconds(_hitFlashInterval);
            _spriteRenderer.color = _originalColor;
            yield return new WaitForSeconds(_hitFlashInterval);
        }
    }


    public override void Die()
    {
        if (_isDead) return;
        _isDead = true;
        RaiseDeath();
        _rb.linearVelocity = Vector2.zero;

        if (_isAddGauge)
            _player.GetComponent<DeadeyeSkill>().AddGauge(15);

        if (_isAmmoEnemy)
            _player.GetComponent<PlayerAttack>().AddAmmo();

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;

        ShowMark(false);
        StartCoroutine(DieRoutine());
    }



}
