using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;

public class PlayerHealth : EntityBase
{

    [Header("Invincibility")]
    [SerializeField] private float _invincibleDuration = 1.5f;
    private bool _isInvincible = false;
    private HapticManager _hapticManager;

    public bool IsInvincible => _isInvincible;

    public event Action<int> OnHit; // Amount   
    public event Action<int> OnHeal;
    public event Action OnDie;

    private Color _originalColor;

    private void Awake()
    {
        if (!ManagerRegistry.TryGet<HapticManager>(out _hapticManager))
            _hapticManager = null;
        _originalColor = _spriteRenderer.color; // 원래 색깔 저장
    }

    public override void TakeDamage(int damage)
    {
        if (_isInvincible) return;
        if (_currentHp <= 0f) return;

        base.TakeDamage(damage);

        Debug.Log("currentHp: " + _currentHp + ", damage: " + damage);
        //_hapticManager?.RumbleAttack();
        OnHit?.Invoke(damage);

        if (_currentHp > 0)
            StartCoroutine(InvincibleRoutine());
    }

    public void Heal(int amount = 1)
    {
        if (_currentHp <= 0f) return;

        int actual = Mathf.Min(amount, _maxHp - _currentHp);
        if (actual <= 0) return; // 이미 풀피

        _currentHp += actual;
        OnHeal?.Invoke(actual);
    }

    // Jaein 추가: 풀피 회복 (리스폰 시 필요)
    public void ResetHP()
    {
        StopAllCoroutines();
        _isInvincible = false;

        int restored = _maxHp - _currentHp;
        _currentHp = _maxHp;

        if (restored > 0)
            OnHeal?.Invoke(restored);
        StopVisual();
    }

    public override void Die()
    {
        _isInvincible = false;
        StopAllCoroutines();
        StopVisual();
        OnDie?.Invoke();
        base.Die();
    }

    private IEnumerator InvincibleRoutine()
    {
        _isInvincible = true;
        StartCoroutine(HitVisualRoutine());
        yield return new WaitForSeconds(_invincibleDuration);
        _isInvincible = false;
    }


    #region Visual
    [Header("Visual")]
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private Color _hitColor = Color.red;
    [SerializeField] private float _hitFlashDuration = 0.1f;
    [SerializeField] private float _blinkInterval = 0.1f;

    private Coroutine _visualRoutine;

    private IEnumerator HitVisualRoutine()
    {
        if (_visualRoutine != null)
        {
            StopCoroutine(_visualRoutine);
            _spriteRenderer.DOKill();
            _spriteRenderer.color = _originalColor;
            _spriteRenderer.enabled = true;
        }
        _visualRoutine = StartCoroutine(RunVisual());
        yield return null;
    }

    private IEnumerator RunVisual()
    {
        // 빨간 번쩍 후 흰색으로 복귀
        _spriteRenderer.DOColor(_hitColor, 0f);
        _spriteRenderer.DOColor(_originalColor, _hitFlashDuration);

        yield return new WaitForSeconds(_hitFlashDuration);

        // 깜빡임
        while (_isInvincible)
        {
            _spriteRenderer.DOFade(0f, _blinkInterval);
            yield return new WaitForSeconds(_blinkInterval);
            _spriteRenderer.DOFade(1f, _blinkInterval);
            yield return new WaitForSeconds(_blinkInterval);
        }

        // 원래 상태로 복귀
        _spriteRenderer.DOFade(1f, 0f);
        _spriteRenderer.DOColor(_originalColor, 0f);
        _visualRoutine = null;
    }

    private void StopVisual()
    {
        if (_visualRoutine != null)
        {
            StopCoroutine(_visualRoutine);
            _visualRoutine = null;
        }
        _spriteRenderer.DOKill();
        _spriteRenderer.color = _originalColor;
        _spriteRenderer.enabled = true;
    }
    #endregion

}
