using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class BossController : MonoBehaviour
{
    enum BossPattern
    {
        Laser,
        Dash
    }

    enum DebugPatternMode
    {
        Random,
        Laser,
        Dash
    }

    [Header("Eye 연결")]
    public BossEye[] eyes;

    [Header("회전 설정")]
    public float rotationSpeedMin = 30f;
    public float rotationSpeedMax = 60f;

    [Header("레이저 페이즈 설정")]
    public float laserDuration = 5f;
    public float idleDuration = 5f;
    public float patternRecoveryDuration = 0.2f;

    [Header("레이저 선딜")]
    public float laserTelegraphDuration = 0.25f;
    public float laserTelegraphRotateAngle = 180f;
    [Range(2, 6)] public int laserTelegraphTickCount = 3;
    public float laserTelegraphClockwiseKickAngle = 15f;
    public Ease laserTelegraphEase = Ease.OutQuad;

    [Header("돌진 설정")]
    public float dashBackDistance = 1.5f;
    public float dashBackSpeed = 2f;
    public float dashSpeed = 15f;
    public float dashDistance = 8f;
    public float returnSpeed = 5f;
    public float dashCooldown = 1f;
    [Range(0f, 0.2f)] public float dashOvershootRatio = 0.08f;
    public float dashOvershootReturnSpeed = 18f;
    public Ease dashBackEase = Ease.OutSine;
    public Ease dashEase = Ease.InExpo;
    public Ease dashOvershootEase = Ease.OutQuad;
    public Ease returnEase = Ease.OutQuad;

    [Header("보스 인트로")]
    [SerializeField] float _bossIntro = 5f;

    [Header("Debug")]
    [SerializeField] private bool _startFromPhase2;
    [SerializeField] private DebugPatternMode _debugPatternMode = DebugPatternMode.Random;

    public float TotalHp => CalculateTotalHp();

    private float _currentRotationSpeed;
    private bool _isDead;
    private int _prevDeadCount;
    private Transform _player;
    private Vector3 _originPos;
    private Tween _moveTween;
    private Tween _laserTelegraphTween;
    private BoseDamageZone[] _damageZones;
    private BossPattern _lastPattern;
    private int _samePatternStreak;

    void OnDisable()
    {
        KillMoveTween();
        KillLaserTelegraphTween();
    }

    void Start()
    {
        _originPos = transform.position;
        _currentRotationSpeed = rotationSpeedMin;
        _damageZones = GetComponentsInChildren<BoseDamageZone>(true);

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            _player = playerObj.transform;

        if (_startFromPhase2)
        {
            StartPhase2Debug();
            return;
        }

        StartCoroutine(BeginAfterIntro());
    }

    void Update()
    {
        if (_isDead)
            return;

        transform.Rotate(0f, 0f, -_currentRotationSpeed * Time.deltaTime);
    }

    void UpdateRotationSpeed()
    {
        float t = (float)DeadCount() / eyes.Length;
        _currentRotationSpeed = Mathf.Lerp(rotationSpeedMin, rotationSpeedMax, t);
    }

    IEnumerator PatternCycleRoutine()
    {
        while (!_isDead)
        {
            yield return StartCoroutine(ExecuteNextPattern());
            yield return new WaitForSeconds(idleDuration);
        }
    }

    IEnumerator ExecuteNextPattern()
    {
        BossPattern nextPattern = ChooseNextPattern();
        TrackPatternUsage(nextPattern);

        if (nextPattern == BossPattern.Laser)
            yield return StartCoroutine(ExecuteLaserPattern());
        else
            yield return StartCoroutine(ExecuteDashPattern());
    }

    IEnumerator ExecuteLaserPattern()
    {
        BossEye[] readyEyes = GetReadyEyes();
        if (readyEyes.Length == 0)
            yield break;

        int laserShotCount = Mathf.Min(GetLaserShotCountByDeadEyes(), readyEyes.Length);
        List<BossEye> targets = PickRandom(readyEyes, laserShotCount);

        foreach (BossEye eye in targets)
            eye.BeginLaser(laserDuration, laserTelegraphDuration);

        yield return StartCoroutine(WaitForLaserTelegraphReady(targets));
        yield return StartCoroutine(PlayLaserTelegraph());

        yield return StartCoroutine(WaitForLaserTargets(targets));
        yield return StartCoroutine(WaitForPatternRecovery());
    }

    IEnumerator ExecuteDashPattern()
    {
        if (_player == null)
            yield break;

        float savedRotationSpeed = _currentRotationSpeed;
        _currentRotationSpeed = 0f;

        Vector3 startPos = transform.position;
        Vector3 dashDirection = (_player.position - transform.position).normalized;

        Vector3 backTarget = startPos + (-dashDirection * dashBackDistance);
        yield return StartCoroutine(MoveToTarget(backTarget, dashBackSpeed, dashBackEase));

        Vector3 dashTarget = startPos + (dashDirection * dashDistance);
        yield return StartCoroutine(MoveToTarget(dashTarget, dashSpeed, dashEase));
        yield return StartCoroutine(ApplyDashOvershoot(dashTarget, dashDirection));

        yield return new WaitForSeconds(dashCooldown);
        SetBodyContactDamageEnabled(false);
        yield return StartCoroutine(MoveToTarget(_originPos, returnSpeed, returnEase));
        SetBodyContactDamageEnabled(true);
        yield return StartCoroutine(WaitForPatternRecovery());

        _currentRotationSpeed = savedRotationSpeed;
    }

    IEnumerator MoveToTarget(Vector3 target, float speed, Ease ease)
    {
        float distance = Vector3.Distance(transform.position, target);
        if (distance <= 0.05f || speed <= Mathf.Epsilon)
        {
            transform.position = target;
            yield break;
        }

        float duration = distance / speed;

        KillMoveTween();
        _moveTween = transform.DOMove(target, duration).SetEase(ease);

        yield return _moveTween.WaitForCompletion();

        transform.position = target;
        _moveTween = null;
    }

    IEnumerator ApplyDashOvershoot(Vector3 dashTarget, Vector3 dashDirection)
    {
        float overshootDistance = dashDistance * dashOvershootRatio;
        if (overshootDistance <= 0f)
            yield break;

        Vector3 overshootTarget = dashTarget + (dashDirection.normalized * overshootDistance);
        yield return StartCoroutine(MoveToTarget(overshootTarget, dashOvershootReturnSpeed, dashOvershootEase));
        yield return StartCoroutine(MoveToTarget(dashTarget, dashOvershootReturnSpeed, dashOvershootEase));
    }

    IEnumerator WaitForLaserTargets(List<BossEye> targets)
    {
        while (true)
        {
            bool allFinished = true;

            for (int i = 0; i < targets.Count; i++)
            {
                BossEye eye = targets[i];
                if (eye == null || eye.IsDead)
                    continue;

                if (!eye.IsLaserFinished)
                {
                    allFinished = false;
                    break;
                }
            }

            if (allFinished)
                yield break;

            yield return null;
        }
    }

    IEnumerator WaitForLaserTelegraphReady(List<BossEye> targets)
    {
        while (true)
        {
            bool allReady = true;

            for (int i = 0; i < targets.Count; i++)
            {
                BossEye eye = targets[i];
                if (eye == null || eye.IsDead)
                    continue;

                if (eye.EyeCurrentState != BossEye.EyeState.Laser)
                {
                    allReady = false;
                    break;
                }
            }

            if (allReady)
                yield break;

            yield return null;
        }
    }

    IEnumerator WaitForPatternRecovery()
    {
        if (patternRecoveryDuration <= 0f)
            yield break;

        yield return new WaitForSeconds(patternRecoveryDuration);
    }

    void KillMoveTween()
    {
        if (_moveTween == null || !_moveTween.IsActive())
            return;

        _moveTween.Kill();
        _moveTween = null;
    }

    IEnumerator PlayLaserTelegraph()
    {
        if (laserTelegraphDuration <= 0f || Mathf.Abs(laserTelegraphRotateAngle) <= Mathf.Epsilon)
            yield break;

        KillLaserTelegraphTween();
        _laserTelegraphTween = BuildLaserTelegraphSequence();

        yield return _laserTelegraphTween.WaitForCompletion();
        _laserTelegraphTween = null;
    }

    Tween BuildLaserTelegraphSequence()
    {
        Sequence sequence = DOTween.Sequence();

        int tickCount = Mathf.Max(1, laserTelegraphTickCount);
        float tickDuration = laserTelegraphDuration / (tickCount * 2f);
        float currentZ = transform.localEulerAngles.z;
        float stepAngle = laserTelegraphRotateAngle / tickCount;

        for (int i = 0; i < tickCount; i++)
        {
            bool shouldKickClockwise = Random.value > 0.5f;
            float kickTarget = shouldKickClockwise
                ? currentZ - laserTelegraphClockwiseKickAngle
                : currentZ;
            float settleTarget = currentZ + stepAngle;

            sequence.Append(
                transform.DOLocalRotate(
                    new Vector3(0f, 0f, kickTarget),
                    tickDuration,
                    RotateMode.Fast));

            sequence.Append(
                transform.DOLocalRotate(
                    new Vector3(0f, 0f, settleTarget),
                    tickDuration,
                    RotateMode.FastBeyond360)
                .SetEase(laserTelegraphEase));

            currentZ = settleTarget;
        }

        return sequence;
    }

    void KillLaserTelegraphTween()
    {
        if (_laserTelegraphTween == null || !_laserTelegraphTween.IsActive())
            return;

        _laserTelegraphTween.Kill();
        _laserTelegraphTween = null;
    }

    void SetBodyContactDamageEnabled(bool isEnabled)
    {
        if (_damageZones == null)
            return;

        for (int i = 0; i < _damageZones.Length; i++)
        {
            if (_damageZones[i] != null)
                _damageZones[i].SetDamageEnabled(isEnabled);
        }
    }

    float CalculateTotalHp()
    {
        float total = 0f;

        foreach (BossEye eye in eyes)
        {
            if (!eye.IsDead)
                total += eye.CurrentHp;
        }

        return total;
    }

    BossEye[] GetReadyEyes()
    {
        return System.Array.FindAll(eyes, eye => eye.CanBeginLaser);
    }

    int DeadCount()
    {
        return System.Array.FindAll(eyes, eye => eye.IsDead).Length;
    }

    int GetLaserShotCountByDeadEyes()
    {
        int deadCount = DeadCount();

        if (deadCount <= 1)
            return 2;

        if (deadCount <= 3)
            return 3;

        return eyes.Length - deadCount;
    }

    BossPattern ChooseNextPattern()
    {
        if (_debugPatternMode == DebugPatternMode.Laser)
            return BossPattern.Laser;

        if (_debugPatternMode == DebugPatternMode.Dash)
            return BossPattern.Dash;

        bool canUseLaser = CanUseLaserPattern();
        bool canUseDash = CanUseDashPattern();

        if (!canUseLaser && !canUseDash)
            return BossPattern.Laser;

        if (!canUseLaser)
            return BossPattern.Dash;

        if (!canUseDash)
            return BossPattern.Laser;

        BossPattern randomPattern = (BossPattern)Random.Range(0, 2);
        if (ShouldForceAlternatePattern(randomPattern))
            return GetAlternatePattern(randomPattern);

        return randomPattern;
    }

    bool CanUseLaserPattern()
    {
        return GetReadyEyes().Length > 0;
    }

    bool CanUseDashPattern()
    {
        return _player != null;
    }

    bool ShouldForceAlternatePattern(BossPattern nextPattern)
    {
        return _samePatternStreak >= 2 && _lastPattern == nextPattern;
    }

    BossPattern GetAlternatePattern(BossPattern currentPattern)
    {
        return currentPattern == BossPattern.Laser ? BossPattern.Dash : BossPattern.Laser;
    }

    void TrackPatternUsage(BossPattern usedPattern)
    {
        if (_samePatternStreak == 0 || _lastPattern != usedPattern)
        {
            _lastPattern = usedPattern;
            _samePatternStreak = 1;
            return;
        }

        _samePatternStreak++;
    }

    List<BossEye> PickRandom(BossEye[] pool, int count)
    {
        List<BossEye> list = new List<BossEye>(pool);

        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }

        return list.GetRange(0, count);
    }

    IEnumerator DeathCheckRoutine()
    {
        while (!_isDead)
        {
            int deadNow = DeadCount();

            if (deadNow > _prevDeadCount)
            {
                _prevDeadCount = deadNow;
                UpdateRotationSpeed();
            }

            if (deadNow >= eyes.Length)
            {
                Die();
                yield break;
            }

            yield return new WaitForSeconds(0.2f);
        }
    }

    void Die()
    {
        if (_isDead)
            return;

        _isDead = true;
        CleanupPhase1State();

        foreach (BossEye eye in eyes)
        {
            if (eye != null)
            {
                eye.ForceStopPattern();
                Destroy(eye.gameObject);
            }
        }

        GetComponent<BossPhase2>().SetPhase2();
        Destroy(this);
    }

    void StartPhase2Debug()
    {
        _isDead = true;
        CleanupPhase1State();

        foreach (BossEye eye in eyes)
        {
            if (eye != null)
            {
                eye.ForceStopPattern();
                Destroy(eye.gameObject);
            }
        }

        GetComponent<BossPhase2>().SetPhase2();
        Destroy(this);
    }

    void CleanupPhase1State()
    {
        KillMoveTween();
        KillLaserTelegraphTween();
        StopAllCoroutines();
        SetBodyContactDamageEnabled(false);
    }

    void StartBoss()
    {
        StartCoroutine(PatternCycleRoutine());
        StartCoroutine(DeathCheckRoutine());
    }

    IEnumerator BeginAfterIntro()
    {
        if (_bossIntro > 0f)
            yield return new WaitForSeconds(_bossIntro);

        if (_isDead)
            yield break;

        StartBoss();
    }
}
