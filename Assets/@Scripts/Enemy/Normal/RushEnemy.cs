using System.Collections;
using UnityEngine;

public class RushEnemy : NormalEnemyBase
{
    [SerializeField] private float _rushSpeed = 100f;
    [SerializeField] private float _rushDuration = 0.5f;
    [SerializeField] private float _rushWindupTime = 0.5f;
    [SerializeField] private ParticleSystem _rushParticle;

    private bool _isRushing = false;
    private Coroutine _rushCoroutine;

    protected override void Start()
    {
        base.Start();
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (_spriteRenderer != null)
            _originalColor = _spriteRenderer.color;
    }

    // Jaein 추가
    protected override void OnEnable()
    {
        base.OnEnable();

        if (_spriteRenderer == null)
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (_spriteRenderer != null)
        {
            _originalColor = _spriteRenderer.color;
            _spriteRenderer.color = _originalColor;
        }

        _isRushing = false;
        _rushCoroutine = null;

        if (_rushParticle != null)
            _rushParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    protected override void Update()
    {
        if (_isDead || !CanAct()) return;

        // Jaein 추가
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

            if (!_isRushing)
                MoveToward(_player.position);

            if (IsInAttackRange() && _canAttack)
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
        yield return StartCoroutine(RushRoutine());
        yield return new WaitForSeconds(_attackCooldown);
        _canAttack = true;
    }

    protected override void DoAttack() { }

    IEnumerator RushRoutine()
    {
        _rb.linearVelocity = Vector2.zero;

        if (_rushParticle != null)
            _rushParticle.Play();

        yield return StartCoroutine(WindupEffectRoutine());

        if (_spriteRenderer != null)
            _spriteRenderer.color = _originalColor;

        // Jaein 추가
        if (!TryFindPlayer())
        {
            _isRushing = false;
            yield break;
        }

        _isRushing = true;
        Vector2 rushDir = ((Vector2)_player.position - (Vector2)transform.position).normalized;

        float t = 0f;
        while (t < _rushDuration)
        {
            t += Time.deltaTime;
            _rb.linearVelocity = rushDir * _rushSpeed;
            yield return null;
        }

        _isRushing = false;
        _rb.linearVelocity = Vector2.zero;
    }

    IEnumerator WindupEffectRoutine()
    {
        if (_spriteRenderer == null)
            yield break;

        float t = 0f;
        while (t < _rushWindupTime)
        {
            t += Time.deltaTime;
            float ratio = t / _rushWindupTime;
            _spriteRenderer.color = Color.Lerp(_originalColor, Color.black, ratio);
            yield return null;
        }
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (!col.gameObject.CompareTag("Player")) return;
        col.gameObject.GetComponent<IDamageable>()?.TakeDamage(1);

        if (_isRushing)
        {
            _isRushing = false;
            _rb.linearVelocity = Vector2.zero;

            if (_spriteRenderer != null)
                _spriteRenderer.color = _originalColor;

            StopCoroutine(nameof(RushRoutine));
        }
    }

    protected override IEnumerator OnDieRoutine()
    {
        yield return new WaitForSeconds(0.2f);
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
    }
}
