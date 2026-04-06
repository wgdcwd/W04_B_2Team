using System.Collections;
using DG.Tweening;
using UnityEngine;

public class BossPhase2 : EnemyBase
{
    enum BossPhase2Pattern
    {
        Bounce,
        BorderRush,
        Dash
    }

    enum DebugPatternMode
    {
        Random,
        Bounce,
        BorderRush,
        Dash
    }

    [Header("맵 경계")]
    public Vector2 mapMin = new Vector2(-10f, -6f);
    public Vector2 mapMax = new Vector2(10f, 6f);

    [Header("회전 설정")]
    public float rotationSpeed = 30f;

    [Header("튀기 패턴")]
    public float bounceSpeed = 8f;
    public float bounceDuration = 6f;
    [Range(0.1f, 1f)] public float bounceImpactSpeedMultiplier = 0.65f;
    public float bounceImpactRecoveryRate = 10f;
    public float bounceImpactPauseDuration = 0.05f;
    public float bounceImpactRecoveryDelay = 0.08f;
    public float bounceTurnMinAngle = 15f;
    public float bounceTurnMaxAngle = 35f;
    public float bounceTurnHeadOnThreshold = 150f;
    public float bounceTurnHeadOnAngle = 55f;
    public float bounceTurnDuration = 0.08f;
    public float bounceTelegraphAngle = 18f;
    public float bounceTelegraphOvershootAngle = 6f;
    public float bounceTelegraphOutDuration = 0.45f;
    public float bounceTelegraphHoldDuration = 0.06f;
    public float bounceTelegraphReturnDuration = 0.09f;
    [Range(1, 8)] public int bounceTelegraphStepCount = 4;
    [Range(0f, 1f)] public float bounceTelegraphLaunchPoint = 0.5f;
    public Ease bounceTelegraphOutEase = Ease.OutSine;
    public Ease bounceTelegraphReturnEase = Ease.OutExpo;

    [Header("테두리 질주 패턴")]
    public float borderSpeed = 15f;
    public float borderLapCount = 1f;
    public float borderRushStartRotationSpeed = 240f;
    public float borderRushRotationSpeed = 720f;
    [Range(0.1f, 1f)] public float borderRushFinalSpeedMultiplier = 0.65f;
    public float borderRushSpinDownDuration = 0.3f;
    public float borderRushRecoverDuration = 0.35f;
    public Ease borderRushSpinUpEase = Ease.InQuad;
    public Ease borderRushFinalMoveEase = Ease.OutQuad;
    public Ease borderRushSpinDownEase = Ease.OutQuad;
    public Ease borderRushRecoverEase = Ease.InOutSine;

    [Header("돌진 패턴")]
    public float dashBackDistance = 1.5f;
    public float dashBackSpeed = 2f;
    public float dashSpeed = 15f;
    public float dashDistance = 8f;
    public float dashCooldown = 1f;
    [Range(0f, 0.2f)] public float dashOvershootRatio = 0.08f;
    public float dashOvershootReturnSpeed = 18f;
    public Ease dashBackEase = Ease.OutSine;
    public Ease dashEase = Ease.InExpo;
    public Ease dashOvershootEase = Ease.OutQuad;

    [Header("패턴 설정")]
    public float idleDuration = 2f;
    public float returnSpeed = 6f;
    public float phaseStartDelay = 1.5f;
    public Ease returnEase = Ease.OutQuad;

    [Header("다음 스테이지 트리거")]
    [SerializeField] private GameObject _nextStageDoor;

    [Header("피격 연출")]
    [SerializeField] private int _hitFlashCount = 3;
    [SerializeField] private float _hitFlashInterval = 0.08f;
    [SerializeField] private ParticleSystem _sparkParticle;
    [SerializeField] private float _bounceSparkDuration = 0.08f;

    [Header("Debug")]
    [SerializeField] private DebugPatternMode _debugPatternMode = DebugPatternMode.Random;

    private Vector2 _velocity;
    private bool _isActive = false;
    private Transform _player;

    private SpriteRenderer _spriteRenderer;
    private Color _originalColor;
    private Coroutine _flashCoroutine;
    private Coroutine _bounceSparkCoroutine;
    private Tween _moveTween;
    private Tween _rotationSpeedTween;
    private Tween _bounceTurnTween;
    private Tween _bounceTelegraphTween;
    private float _defaultRotationSpeed;
    private BoseDamageZone[] _damageZones;
    private BossPhase2Pattern _lastPattern;
    private int _samePatternStreak;

    // =====================
    // 생명주기
    // =====================
    protected override void Start()
    {
        base.Start();
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        _defaultRotationSpeed = rotationSpeed;
        _damageZones = GetComponentsInChildren<BoseDamageZone>(true);
        CacheEffects();
        if (_spriteRenderer != null)
            _originalColor = _spriteRenderer.color;
    }

    private void OnEnable()
    {
        _isActive = false;
        _velocity = Vector2.zero;
        _player = null;
        if (_defaultRotationSpeed <= 0f)
            _defaultRotationSpeed = rotationSpeed;

        rotationSpeed = _defaultRotationSpeed;
        CacheEffects();
        KillTweens();

        if (_spriteRenderer != null)
            _spriteRenderer.color = _originalColor;

        SetSparkEffect(false);
    }

    void Update()
    {
        if (!_isActive) return;
        transform.Rotate(0f, 0f, -rotationSpeed * Time.deltaTime);
    }

    // =====================
    // 피격
    // =====================
    public override void TakeDamage(int damage, bool isAddGauge = false)
    {
        if (!_isActive) return;
        if (!gameObject.activeInHierarchy) return;

        if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
        if (gameObject.activeInHierarchy)
            _flashCoroutine = StartCoroutine(HitFlashRoutine());

        base.TakeDamage(damage, isAddGauge);
    }

    private IEnumerator HitFlashRoutine()
    {
        for (int i = 0; i < _hitFlashCount; i++)
        {
            _spriteRenderer.color = Color.black;
            yield return new WaitForSeconds(_hitFlashInterval);
            _spriteRenderer.color = _originalColor;
            yield return new WaitForSeconds(_hitFlashInterval);
        }
    }

    public override void Die() => Phase2Die();

    // =====================
    // 페이즈2 시작
    // =====================
    public void SetPhase2()
    {
        StopAllCoroutines();
        KillTweens();
        _currentHp = _maxHp;
        _isActive = true;
        rotationSpeed = _defaultRotationSpeed;
        _samePatternStreak = 0;

        gameObject.tag = "Enemy";
        gameObject.layer = LayerMask.NameToLayer("Enemy");

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) _player = playerObj.transform;

        SetBodyContactDamageEnabled(false);
        StartCoroutine(DelayedStart());
    }

    IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(phaseStartDelay);
        SetBodyContactDamageEnabled(true);
        StartCoroutine(PatternCycleRoutine());
    }

    void Phase2Die()
    {
        if (!_isActive) return;
        _isActive = false;
        KillTweens();
        StopAllCoroutines();

        if (_spriteRenderer != null)
            _spriteRenderer.color = _originalColor;

        SetSparkEffect(false);

        Debug.Log("보스 완전 사망");
        _nextStageDoor.SetActive(true);
        gameObject.SetActive(false);
    }

    // =====================
    // 패턴 사이클
    // =====================
    IEnumerator PatternCycleRoutine()
    {
        while (_isActive)
        {
            BossPhase2Pattern pattern = ChooseNextPattern();
            TrackPatternUsage(pattern);

            if (pattern == BossPhase2Pattern.Bounce)
                yield return StartCoroutine(BouncePattern());
            else if (pattern == BossPhase2Pattern.BorderRush)
                yield return StartCoroutine(BorderRushPattern());
            else
                yield return StartCoroutine(DashPattern());

            yield return StartCoroutine(ReturnToCenter());
            yield return new WaitForSeconds(idleDuration);
        }
    }

    // =====================
    // 중앙 복귀
    // =====================
    IEnumerator ReturnToCenter()
    {
        Vector2 center = (mapMin + mapMax) * 0.5f;
        SetBodyContactDamageEnabled(false);
        yield return StartCoroutine(MoveToTarget(center, returnSpeed, returnEase));
        SetBodyContactDamageEnabled(true);
    }

    // =====================
    // 튀기 패턴
    // =====================
    IEnumerator BouncePattern()
    {
        float savedRotationSpeed = rotationSpeed;
        rotationSpeed = 0f;
        SetBodyContactDamageEnabled(false);
        Vector2 launchVelocity = GetSafeDirectionFromWall() * bounceSpeed;
        bool hasLaunched = false;
        StartCoroutine(PlayBounceTelegraph(savedRotationSpeed, () =>
        {
            _velocity = launchVelocity;
            SetBodyContactDamageEnabled(true);
            hasLaunched = true;
        }));

        while (!hasLaunched)
            yield return null;

        float recoveryDelayRemaining = 0f;

        float elapsed = 0f;
        while (elapsed < bounceDuration)
        {
            bool hitWall = false;
            Vector2 previousVelocity = _velocity;
            if (recoveryDelayRemaining > 0f)
            {
                recoveryDelayRemaining -= Time.deltaTime;
            }
            else
            {
                float recoveredSpeed = Mathf.MoveTowards(
                    _velocity.magnitude,
                    bounceSpeed,
                    bounceImpactRecoveryRate * Time.deltaTime);
                _velocity = _velocity.normalized * recoveredSpeed;
            }

            Vector3 next = transform.position + (Vector3)_velocity * Time.deltaTime;

            if (next.x <= mapMin.x || next.x >= mapMax.x)
            {
                _velocity.x *= -1f;
                next.x = Mathf.Clamp(next.x, mapMin.x, mapMax.x);
                ApplyBounceImpactSpeed();
                recoveryDelayRemaining = bounceImpactRecoveryDelay;
                hitWall = true;
            }

            if (next.y <= mapMin.y || next.y >= mapMax.y)
            {
                _velocity.y *= -1f;
                next.y = Mathf.Clamp(next.y, mapMin.y, mapMax.y);
                ApplyBounceImpactSpeed();
                recoveryDelayRemaining = bounceImpactRecoveryDelay;
                hitWall = true;
            }

            transform.position = next;
            if (hitWall)
            {
                ApplyBounceTurn(previousVelocity, _velocity);
                PlayBounceSpark();
                if (bounceImpactPauseDuration > 0f)
                    yield return new WaitForSeconds(bounceImpactPauseDuration);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        rotationSpeed = savedRotationSpeed;
    }

    // =====================
    // 테두리 질주 패턴
    // =====================
    IEnumerator BorderRushPattern()
    {
        bool clockwise = Random.value > 0.5f;
        float savedRotationSpeed = rotationSpeed;
        float signedStartRotationSpeed = clockwise
            ? Mathf.Abs(borderRushStartRotationSpeed)
            : -Mathf.Abs(borderRushStartRotationSpeed);
        float signedRushRotationSpeed = clockwise
            ? Mathf.Abs(borderRushRotationSpeed)
            : -Mathf.Abs(borderRushRotationSpeed);

        Vector2 dir = GetSafeRandomDirection();
        Vector2 wallTarget = GetWallPoint(dir);
        rotationSpeed = signedStartRotationSpeed;
        StartRotationSpeedTween(
            signedRushRotationSpeed,
            GetMoveDuration(wallTarget, borderSpeed),
            borderRushSpinUpEase);
        yield return StartCoroutine(MoveToTarget(wallTarget, borderSpeed, Ease.Linear));
        SetSparkEffect(true);

        Vector2[] corners = clockwise
            ? new Vector2[]
            {
                new Vector2(mapMin.x, mapMin.y),
                new Vector2(mapMin.x, mapMax.y),
                new Vector2(mapMax.x, mapMax.y),
                new Vector2(mapMax.x, mapMin.y),
            }
            : new Vector2[]
            {
                new Vector2(mapMin.x, mapMin.y),
                new Vector2(mapMax.x, mapMin.y),
                new Vector2(mapMax.x, mapMax.y),
                new Vector2(mapMin.x, mapMax.y),
            };

        int startIndex = 0;
        float minDist = float.MaxValue;
        for (int i = 0; i < corners.Length; i++)
        {
            float dist = Vector2.Distance(transform.position, corners[i]);
            if (dist < minDist)
            {
                minDist = dist;
                startIndex = i;
            }
        }

        int totalSteps = Mathf.RoundToInt(borderLapCount * corners.Length);
        for (int step = 0; step < totalSteps; step++)
        {
            int idx = (startIndex + step) % corners.Length;
            bool isLastStep = step == totalSteps - 1;
            float stepSpeed = borderSpeed;
            Ease stepEase = Ease.Linear;
            if (isLastStep)
            {
                SetSparkEffect(false);
                stepSpeed *= Mathf.Clamp(borderRushFinalSpeedMultiplier, 0.1f, 1f);
                float spinDownDuration = Mathf.Max(
                    GetMoveDuration(corners[idx], stepSpeed),
                    borderRushSpinDownDuration);
                StartRotationSpeedTween(
                    0f,
                    spinDownDuration,
                    borderRushSpinDownEase);
                stepEase = borderRushFinalMoveEase;
            }

            yield return StartCoroutine(MoveToTarget(corners[idx], stepSpeed, stepEase));
        }

        yield return StartCoroutine(TweenRotationSpeed(savedRotationSpeed, borderRushRecoverDuration, borderRushRecoverEase));
    }

    // =====================
    // 돌진 패턴
    // =====================
    IEnumerator DashPattern()
    {
        if (_player == null) yield break;

        float savedRotSpeed = rotationSpeed;
        rotationSpeed = 0f;

        Vector3 startPos = transform.position;
        Vector3 toPlayer = (_player.position - transform.position).normalized;

        Vector3 backTarget = startPos + (-toPlayer * dashBackDistance);
        yield return StartCoroutine(MoveToTarget(backTarget, dashBackSpeed, dashBackEase));

        Vector3 dashTarget = startPos + (toPlayer * dashDistance);
        yield return StartCoroutine(MoveToTarget(dashTarget, dashSpeed, dashEase));
        yield return StartCoroutine(ApplyDashOvershoot(dashTarget, toPlayer));

        yield return new WaitForSeconds(dashCooldown);

        rotationSpeed = savedRotSpeed;
    }

    // =====================
    // 방향 유틸 - 플레이어 방향 ±30도 피하기
    // =====================
    private Vector2 GetSafeRandomDirection()
    {
        if (_player == null)
        {
            float r = Random.Range(0f, 360f);
            return new Vector2(Mathf.Cos(r * Mathf.Deg2Rad), Mathf.Sin(r * Mathf.Deg2Rad));
        }

        Vector2 toPlayer = (_player.position - transform.position).normalized;
        float playerAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
        float excludeAngle = 30f;

        float angle;
        int maxTry = 100;
        do
        {
            angle = Random.Range(0f, 360f);
            float diff = Mathf.Abs(Mathf.DeltaAngle(angle, playerAngle));
            if (diff > excludeAngle) break;
        } while (--maxTry > 0);

        return new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
    }

    // =====================
    // 방향 유틸 - 벽 방향 피하기
    // =====================
    private Vector2 GetSafeDirectionFromWall()
    {
        Vector2 pos = transform.position;
        Vector2 center = (mapMin + mapMax) * 0.5f;
        Vector2 toCenter = (center - pos).normalized;
        float centerAngle = Mathf.Atan2(toCenter.y, toCenter.x) * Mathf.Rad2Deg;

        float marginX = (mapMax.x - mapMin.x) * 0.2f;
        float marginY = (mapMax.y - mapMin.y) * 0.2f;
        bool nearWall = pos.x < mapMin.x + marginX || pos.x > mapMax.x - marginX
                     || pos.y < mapMin.y + marginY || pos.y > mapMax.y - marginY;

        if (nearWall)
        {
            float angle = centerAngle + Random.Range(-60f, 60f);
            return new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
        }

        return GetSafeRandomDirection();
    }

    // =====================
    // 유틸
    // =====================
    Vector2 GetWallPoint(Vector2 dir)
    {
        Vector2 pos = transform.position;
        float tMin = float.MaxValue;

        if (dir.x > 0) tMin = Mathf.Min(tMin, (mapMax.x - pos.x) / dir.x);
        else if (dir.x < 0) tMin = Mathf.Min(tMin, (mapMin.x - pos.x) / dir.x);

        if (dir.y > 0) tMin = Mathf.Min(tMin, (mapMax.y - pos.y) / dir.y);
        else if (dir.y < 0) tMin = Mathf.Min(tMin, (mapMin.y - pos.y) / dir.y);

        return pos + dir * tMin;
    }

    IEnumerator MoveToTarget(Vector2 target, float speed, Ease ease)
    {
        float distance = Vector2.Distance(transform.position, target);
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

    float GetMoveDuration(Vector2 target, float speed)
    {
        if (speed <= Mathf.Epsilon)
            return 0f;

        return Vector2.Distance(transform.position, target) / speed;
    }

    BossPhase2Pattern ChooseNextPattern()
    {
        if (_debugPatternMode == DebugPatternMode.Bounce)
            return BossPhase2Pattern.Bounce;

        if (_debugPatternMode == DebugPatternMode.BorderRush)
            return BossPhase2Pattern.BorderRush;

        if (_debugPatternMode == DebugPatternMode.Dash)
            return BossPhase2Pattern.Dash;

        BossPhase2Pattern nextPattern = (BossPhase2Pattern)Random.Range(0, 3);
        if (_samePatternStreak >= 1 && _lastPattern == nextPattern)
            return GetAlternatePattern(nextPattern);

        return nextPattern;
    }

    BossPhase2Pattern GetAlternatePattern(BossPhase2Pattern currentPattern)
    {
        BossPhase2Pattern alternatePattern = (BossPhase2Pattern)Random.Range(0, 3);
        if (alternatePattern == currentPattern)
            alternatePattern = (BossPhase2Pattern)(((int)currentPattern + 1) % 3);

        return alternatePattern;
    }

    void TrackPatternUsage(BossPhase2Pattern usedPattern)
    {
        if (_samePatternStreak == 0 || _lastPattern != usedPattern)
        {
            _lastPattern = usedPattern;
            _samePatternStreak = 1;
            return;
        }

        _samePatternStreak++;
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

    void ApplyBounceImpactSpeed()
    {
        if (_velocity.sqrMagnitude <= Mathf.Epsilon)
            return;

        float currentSpeed = _velocity.magnitude;
        float slowedSpeed = currentSpeed * Mathf.Clamp(bounceImpactSpeedMultiplier, 0.1f, 1f);
        _velocity = _velocity.normalized * slowedSpeed;
    }

    void ApplyBounceTurn(Vector2 previousVelocity, Vector2 currentVelocity)
    {
        float minAngle = Mathf.Abs(bounceTurnMinAngle);
        float maxAngle = Mathf.Abs(bounceTurnMaxAngle);
        if (maxAngle <= Mathf.Epsilon)
            return;

        if (minAngle > maxAngle)
            (minAngle, maxAngle) = (maxAngle, minAngle);

        float signedTurn = Vector2.SignedAngle(previousVelocity, currentVelocity);
        float turnDirection = Mathf.Approximately(signedTurn, 0f) ? 1f : Mathf.Sign(signedTurn);
        float absoluteTurn = Mathf.Abs(signedTurn);
        float clampedTurnMagnitude = Mathf.Clamp(absoluteTurn, minAngle, maxAngle);

        if (absoluteTurn >= bounceTurnHeadOnThreshold)
            clampedTurnMagnitude = Mathf.Max(clampedTurnMagnitude, Mathf.Abs(bounceTurnHeadOnAngle));

        float targetZ = transform.eulerAngles.z + (turnDirection * clampedTurnMagnitude);

        if (_bounceTurnTween != null && _bounceTurnTween.IsActive())
            _bounceTurnTween.Kill();

        if (bounceTurnDuration <= 0f)
        {
            transform.rotation = Quaternion.Euler(0f, 0f, targetZ);
            return;
        }

        _bounceTurnTween = transform
            .DORotate(new Vector3(0f, 0f, targetZ), bounceTurnDuration, RotateMode.Fast)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => _bounceTurnTween = null);
    }

    IEnumerator PlayBounceTelegraph(float currentRotationSpeed, System.Action onLaunch)
    {
        if (Mathf.Abs(bounceTelegraphAngle) <= Mathf.Epsilon || bounceTelegraphOutDuration <= 0f)
        {
            onLaunch?.Invoke();
            yield break;
        }

        float startZ = transform.eulerAngles.z;
        float direction = GetBounceTelegraphDirection(currentRotationSpeed);
        float telegraphTargetZ = startZ + (direction * bounceTelegraphAngle);
        float overshootTargetZ = telegraphTargetZ + (direction * bounceTelegraphOvershootAngle);
        float launchTime = bounceTelegraphOutDuration +
            bounceTelegraphHoldDuration +
            (bounceTelegraphReturnDuration * Mathf.Clamp01(bounceTelegraphLaunchPoint));
        bool launched = false;
        int stepCount = Mathf.Max(1, bounceTelegraphStepCount);
        float stepDuration = bounceTelegraphOutDuration / stepCount;

        KillBounceTelegraphTween();
        Sequence sequence = DOTween.Sequence();
        for (int step = 0; step < stepCount; step++)
        {
            float progress = (step + 1f) / stepCount;
            float stepTargetZ = Mathf.Lerp(startZ, overshootTargetZ, progress);
            sequence.Append(
                transform.DORotate(
                    new Vector3(0f, 0f, stepTargetZ),
                    stepDuration,
                    RotateMode.Fast)
                .SetEase(bounceTelegraphOutEase));
        }

        if (bounceTelegraphHoldDuration > 0f)
            sequence.AppendInterval(bounceTelegraphHoldDuration);

        sequence.Append(
            transform.DORotate(
                new Vector3(0f, 0f, startZ),
                bounceTelegraphReturnDuration,
                RotateMode.Fast)
            .SetEase(bounceTelegraphReturnEase));
        sequence.InsertCallback(launchTime, () =>
        {
            if (launched)
                return;

            launched = true;
            onLaunch?.Invoke();
        });

        _bounceTelegraphTween = sequence;
        yield return _bounceTelegraphTween.WaitForCompletion();
        if (!launched)
            onLaunch?.Invoke();

        transform.rotation = Quaternion.Euler(0f, 0f, startZ);
        _bounceTelegraphTween = null;
    }

    float GetBounceTelegraphDirection(float currentRotationSpeed)
    {
        if (Mathf.Approximately(currentRotationSpeed, 0f))
            return 1f;

        // Update() rotates by -rotationSpeed, so the opposite wind-up direction
        // matches the sign of the current rotationSpeed.
        return Mathf.Sign(currentRotationSpeed);
    }

    void CacheEffects()
    {
        if (_sparkParticle == null)
            _sparkParticle = GetComponentInChildren<ParticleSystem>(true);
    }

    void PlayBounceSpark()
    {
        if (_sparkParticle == null)
            return;

        if (_bounceSparkCoroutine != null)
            StopCoroutine(_bounceSparkCoroutine);

        _bounceSparkCoroutine = StartCoroutine(BounceSparkRoutine());
    }

    IEnumerator BounceSparkRoutine()
    {
        _sparkParticle.Play();
        yield return new WaitForSeconds(_bounceSparkDuration);

        if (_sparkParticle != null && _sparkParticle.isPlaying)
            _sparkParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        _bounceSparkCoroutine = null;
    }

    void StartRotationSpeedTween(float target, float duration, Ease ease)
    {
        if (duration <= 0f)
        {
            rotationSpeed = target;
            return;
        }

        KillRotationSpeedTween();
        _rotationSpeedTween = DOTween
            .To(() => rotationSpeed, value => rotationSpeed = value, target, duration)
            .SetEase(ease);
    }

    IEnumerator TweenRotationSpeed(float target, float duration, Ease ease)
    {
        if (duration <= 0f)
        {
            rotationSpeed = target;
            yield break;
        }

        StartRotationSpeedTween(target, duration, ease);
        yield return _rotationSpeedTween.WaitForCompletion();
        rotationSpeed = target;
        _rotationSpeedTween = null;
    }

    void KillMoveTween()
    {
        if (_moveTween == null || !_moveTween.IsActive())
            return;

        _moveTween.Kill();
        _moveTween = null;
    }

    void SetSparkEffect(bool enabled)
    {
        if (_sparkParticle == null)
            return;

        if (enabled)
        {
            if (!_sparkParticle.isPlaying)
                _sparkParticle.Play();

            return;
        }

        if (_sparkParticle.isPlaying)
            _sparkParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
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

    void KillRotationSpeedTween()
    {
        if (_rotationSpeedTween == null || !_rotationSpeedTween.IsActive())
            return;

        _rotationSpeedTween.Kill();
        _rotationSpeedTween = null;
    }

    void KillBounceTurnTween()
    {
        if (_bounceTurnTween == null || !_bounceTurnTween.IsActive())
            return;

        _bounceTurnTween.Kill();
        _bounceTurnTween = null;
    }

    void KillBounceTelegraphTween()
    {
        if (_bounceTelegraphTween == null || !_bounceTelegraphTween.IsActive())
            return;

        _bounceTelegraphTween.Kill();
        _bounceTelegraphTween = null;
    }

    void KillTweens()
    {
        SetSparkEffect(false);
        if (_bounceSparkCoroutine != null)
        {
            StopCoroutine(_bounceSparkCoroutine);
            _bounceSparkCoroutine = null;
        }

        if (_sparkParticle != null && _sparkParticle.isPlaying)
            _sparkParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        KillMoveTween();
        KillRotationSpeedTween();
        KillBounceTurnTween();
        KillBounceTelegraphTween();
    }
}
