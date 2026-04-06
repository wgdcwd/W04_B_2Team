using System.Collections;
using DG.Tweening;
using UnityEngine;

public class BossEye : EnemyBase
{

    [SerializeField] private GameObject _aimTarget;
    GameObject _player;
    public enum EyeState { Idle, Laser, Dead }

    [Header("Eye 설정")]
    public float colorTransitionTime = 1f;
    public float blinkInterval = 0.1f;
    public int blinkCount = 4;

    [Header("레이저 오브젝트")]
    [SerializeField] private GameObject _warningLaser;
    [SerializeField] private GameObject _fireLaser;

    [Header("레이저 설정")]
    public float warningDuration = 0.8f;
    public float laserExpandTime = 0.15f;

    [Header("소환 설정")]
    [SerializeField] private GameObject[] _minionPrefabs;
    [SerializeField] private Vector3 _spawnOffset = new Vector3(0f, 0.6f, 0f);
    public float spawnInterval = 20f;

    public EyeState EyeCurrentState { get; private set; } = EyeState.Idle;
    public bool IsDead => EyeCurrentState == EyeState.Dead;
    public bool CanBeginLaser => EyeCurrentState == EyeState.Idle && !_isTransitioning;
    public bool IsLaserFinished { get; private set; } = true;
    public float CurrentHp => _currentHp;

    private static readonly Color ColorIdle = Color.white;
    private static readonly Color ColorLaser = Color.red;
    private static readonly Color ColorDead = Color.black;
    private static readonly Color ColorHit = Color.yellow;

    private Renderer _rend;
    private bool _isTransitioning;
    private bool _isBlinking;
    private Coroutine _transitionCoroutine;
    private Vector3 _fireLaserOriginalScale;
    private Tween _laserExpandTween;

    // =====================
    // 초기화
    // =====================
    void Awake()
    {
        _rend = GetComponentInChildren<Renderer>();
        _currentHp = _maxHp;
        _rend.material.color = ColorIdle;

        _warningLaser.SetActive(false);
        _fireLaser.SetActive(false);
        _fireLaser.transform.DOKill();
        _fireLaserOriginalScale = _fireLaser.transform.localScale;
    }

    void Start()
    {
        base.Start();
        _player = GameObject.FindWithTag("Player");
    }


    // =====================
    // TakeDamage
    // =====================
    void ProcessDamage(int damage)
    {
        if (IsDead) return;
        if (EyeCurrentState == EyeState.Laser) return;
        if (_isTransitioning) return;
        if (EyeCurrentState != EyeState.Idle) return;

        _player.GetComponent<DeadeyeSkill>().AddGauge(0.5f);

        _currentHp -= damage;

        if (_currentHp <= 0)
        {
            _currentHp = 0;
            EyeDie();
            return;
        }

        if (!_isBlinking)
            StartCoroutine(HitBlinkRoutine());
    }

    public override void TakeDamage(int damage) => ProcessDamage(damage);
    public override void TakeDamage(int damage, bool isAddGauge = false) => ProcessDamage(damage);
    public override void Die() => EyeDie();

    // =====================
    // Manager가 호출
    // =====================
    public void BeginLaser(float duration)
    {
        if (!CanBeginLaser) return;
        IsLaserFinished = false;
        StartEyeTransition(EyeState.Laser, ColorLaser, () =>
        {
            StartCoroutine(LaserRoutine(duration));
        });
    }

    // =====================
    // 레이저 루틴
    // =====================
    IEnumerator LaserRoutine(float duration)
    {
        if (IsDead) { IsLaserFinished = true; yield break; }

        // 1단계 : 예고
        _warningLaser.SetActive(true);

        float elapsed = 0f;
        while (elapsed < warningDuration)
        {
            if (IsDead)
            {
                _warningLaser.SetActive(false);
                IsLaserFinished = true;
                yield break;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 2단계 : 레이저 펼치기
        _warningLaser.SetActive(false);
        _fireLaser.transform.DOKill();
        _fireLaser.transform.localScale = new Vector3(0f, _fireLaserOriginalScale.y, _fireLaserOriginalScale.z);
        _fireLaser.SetActive(true);

        _laserExpandTween = _fireLaser.transform
            .DOScaleX(_fireLaserOriginalScale.x, laserExpandTime)
            .SetEase(Ease.Linear);

        while (_laserExpandTween != null && _laserExpandTween.IsActive() && !_laserExpandTween.IsComplete())
        {
            if (IsDead)
            {
                _fireLaser.transform.DOKill();
                _laserExpandTween = null;
                _fireLaser.SetActive(false);
                _fireLaser.transform.localScale = _fireLaserOriginalScale;
                IsLaserFinished = true;
                yield break;
            }
            yield return null;
        }
        _laserExpandTween = null;
        _fireLaser.transform.localScale = _fireLaserOriginalScale;

        // 3단계 : 레이저 지속
        elapsed = 0f;
        while (elapsed < duration)
        {
            if (IsDead)
            {
                _fireLaser.transform.DOKill();
                _laserExpandTween = null;
                _fireLaser.SetActive(false);
                _fireLaser.transform.localScale = _fireLaserOriginalScale;
                IsLaserFinished = true;
                yield break;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        _fireLaser.SetActive(false);
        _fireLaser.transform.localScale = _fireLaserOriginalScale;

        if (!IsDead)
        {
            StartEyeTransition(EyeState.Idle, ColorIdle, () =>
            {
                IsLaserFinished = true;
            });
        }
        else
        {
            IsLaserFinished = true;
        }
    }

    // =====================
    // 내부 루틴
    // =====================
    void EyeDie()
    {
        if (IsDead) return;

        Destroy(_aimTarget);

        EyeCurrentState = EyeState.Dead;
        IsLaserFinished = true;
        StopAllCoroutines();

        _warningLaser.SetActive(false);
        _fireLaser.transform.DOKill();
        _laserExpandTween = null;
        _fireLaser.SetActive(false);
        _fireLaser.transform.localScale = _fireLaserOriginalScale;
        GetComponent<Collider2D>().enabled = false;

        _transitionCoroutine = StartCoroutine(TransitionRoutine(EyeState.Dead, ColorDead, () =>
        {
            if (_minionPrefabs.Length > 0)
                StartCoroutine(SummonRoutine());
        }));
    }

    IEnumerator SummonRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            GameObject prefab = _minionPrefabs[Random.Range(0, _minionPrefabs.Length)];
            Vector3 spawnPos = transform.TransformPoint(_spawnOffset);
            Instantiate(prefab, spawnPos, Quaternion.identity);
        }
    }

    void StartEyeTransition(EyeState next, Color target, System.Action onComplete = null)
    {
        if (_transitionCoroutine != null) StopCoroutine(_transitionCoroutine);
        _transitionCoroutine = StartCoroutine(TransitionRoutine(next, target, onComplete));
    }

    IEnumerator TransitionRoutine(EyeState next, Color target, System.Action onComplete)
    {
        _isTransitioning = true;
        Color from = _rend.material.color;
        float elapsed = 0f;

        while (elapsed < colorTransitionTime)
        {
            elapsed += Time.deltaTime;
            _rend.material.color = Color.Lerp(from, target, elapsed / colorTransitionTime);
            yield return null;
        }

        _rend.material.color = target;
        EyeCurrentState = next;
        _isTransitioning = false;
        onComplete?.Invoke();
    }

    IEnumerator HitBlinkRoutine()
    {
        _isBlinking = true;
        Color original = _rend.material.color;

        for (int i = 0; i < blinkCount; i++)
        {
            if (IsDead) { _isBlinking = false; yield break; }
            _rend.material.color = ColorHit;
            yield return new WaitForSeconds(blinkInterval);
            _rend.material.color = original;
            yield return new WaitForSeconds(blinkInterval);
        }

        _isBlinking = false;
    }
}
