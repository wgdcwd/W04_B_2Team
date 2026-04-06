using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class BossController : MonoBehaviour
{
    [Header("Eye 연결")]
    public BossEye[] eyes;

    [Header("회전 설정")]
    public float rotationSpeedMin = 30f;
    public float rotationSpeedMax = 60f;

    [Header("레이저 페이즈 설정")]
    public float laserDuration = 5f;
    public float idleDuration = 5f;

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
    [SerializeField] float _bossIntro = 5;

    public float TotalHp => CalculateTotalHp();

    private float _currentRotationSpeed;
    private bool _isDead = false;
    private int _prevDeadCount = 0;
    private Transform _player;
    private Vector3 _originPos;
    private Tween _moveTween;

    private void OnEnable()
    {
    }

    private void OnDisable()
    {
        KillMoveTween();
    }

    void Start()
    {
        _originPos = transform.position;
        _currentRotationSpeed = rotationSpeedMin;

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) _player = playerObj.transform;
        StartBoss();
    }

    void Update()
    {
        if (_isDead) return;
        transform.Rotate(0f, 0f, -_currentRotationSpeed * Time.deltaTime);
    }

    // =====================
    // 회전 속도 갱신
    // =====================
    void UpdateRotationSpeed()
    {
        float t = (float)DeadCount() / eyes.Length;
        _currentRotationSpeed = Mathf.Lerp(rotationSpeedMin, rotationSpeedMax, t);
    }

    // =====================
    // 패턴 사이클
    // =====================
    IEnumerator PatternCycleRoutine()
    {
        while (!_isDead)
        {
            int pattern = Random.Range(0, 2);

            if (pattern == 0)
                yield return StartCoroutine(LaserPattern());
            else
                yield return StartCoroutine(DashPattern());

            yield return new WaitForSeconds(idleDuration);
        }
    }

    // =====================
    // 레이저 패턴
    // =====================
    IEnumerator LaserPattern()
    {
        BossEye[] ready = GetReadyEyes();
        if (ready.Length == 0) yield break;

        int count = Mathf.Min(GetLaserShotCountByDeadEyes(), ready.Length);
        List<BossEye> targets = PickRandom(ready, count);
        foreach (var eye in targets)
            eye.BeginLaser(laserDuration);

        yield return new WaitForSeconds(laserDuration);
    }

    // =====================
    // 돌진 패턴
    // =====================
    IEnumerator DashPattern()
    {
        if (_player == null) yield break;

        float savedRotSpeed = _currentRotationSpeed;
        _currentRotationSpeed = 0f;

        Vector3 startPos = transform.position;
        Vector3 toPlayer = (_player.position - transform.position).normalized;

        Vector3 backTarget = startPos + (-toPlayer * dashBackDistance);
        yield return StartCoroutine(MoveToPositionTween(backTarget, dashBackSpeed, dashBackEase));

        Vector3 dashTarget = startPos + (toPlayer * dashDistance);
        yield return StartCoroutine(MoveToPositionTween(dashTarget, dashSpeed, dashEase));
        yield return StartCoroutine(ApplyDashOvershoot(dashTarget, toPlayer));

        yield return new WaitForSeconds(dashCooldown);

        yield return StartCoroutine(MoveToPositionTween(_originPos, returnSpeed, returnEase));

        _currentRotationSpeed = savedRotSpeed;
    }

    IEnumerator MoveToPositionTween(Vector3 target, float speed, Ease ease)
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
        yield return StartCoroutine(MoveToPositionTween(overshootTarget, dashOvershootReturnSpeed, dashOvershootEase));
        yield return StartCoroutine(MoveToPositionTween(dashTarget, dashOvershootReturnSpeed, dashOvershootEase));
    }

    void KillMoveTween()
    {
        if (_moveTween == null || !_moveTween.IsActive())
            return;

        _moveTween.Kill();
        _moveTween = null;
    }

    // =====================
    // 체력
    // =====================
    float CalculateTotalHp()
    {
        float total = 0f;
        foreach (var eye in eyes)
            if (!eye.IsDead)
                total += eye.CurrentHp;
        return total;
    }

    BossEye[] GetReadyEyes()
    {
        return System.Array.FindAll(eyes, e => e.CanBeginLaser);
    }

    int DeadCount()
    {
        return System.Array.FindAll(eyes, e => e.IsDead).Length;
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

    // =====================
    // 사망 감지
    // =====================
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
        if (_isDead) return;
        _isDead = true;
        KillMoveTween();
        StopAllCoroutines();

        foreach (var eye in eyes)
        {
            if (eye != null)
                Destroy(eye.gameObject);
        }

        gameObject.GetComponent<BossPhase2>().SetPhase2();
        Destroy(this);
    }

    void StartBoss()
    {
        StartCoroutine(PatternCycleRoutine());
        StartCoroutine(DeathCheckRoutine());
    }
}
