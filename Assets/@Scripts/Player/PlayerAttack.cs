using DG.Tweening;
using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using Random = UnityEngine.Random;

public readonly struct DryFireContext
{
    public WeaponInstance Weapon { get; }
    public bool IsShotgun { get; }

    public DryFireContext(WeaponInstance weapon, bool isShotgun)
    {
        Weapon = weapon;
        IsShotgun = isShotgun;
    }
}

public class PlayerAttack : MonoBehaviour
{
    Player _player;
    Rigidbody2D _rb;

    // 샷건
    [SerializeField] private SO_WeaponBase _shotgunData;
    private WeaponInstance _shotgunInstance;
    public WeaponInstance Shotgun => _shotgunInstance;

    // 좌클릭 무기 (교체 가능한.)
    [SerializeField] private SO_WeaponBase currentWeaponData;
    private WeaponInstance _currentWeaponInstance;
    public WeaponInstance Current => _currentWeaponInstance;

    // 머리 쿵 관련
    [SerializeField] private Transform _ceilingCheck;
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private float _ceilingCheckRadius = 0.1f;

    // 총알 스폰 위치
    [SerializeField] private Transform _gunMuzzle;      
    [SerializeField] private Transform _shotgunMuzzle;

    // 샷건 위치 조정
    [SerializeField] private Transform _shotgunPivot;
    [SerializeField] private float _shotgunIdleAngle = 270f; // 평소 위로 든 각도

    private PoolManager _poolManager;
    private HapticManager _hapticManager;

    // 카메라 impulse
    [SerializeField] private CinemachineImpulseSource _impulseSource;


    // Temp: 둘 다 끝났는지 추적
    bool _gravityDone = false;
    bool _dampingDone = false;

    // 총알없음 액션
    public event Action<DryFireContext> OnDryFire;

    private void Awake()
    {
        _player = GetComponent<Player>();
        _rb = GetComponent<Rigidbody2D>();
        _shotgunInstance = new WeaponInstance(_shotgunData);
        _currentWeaponInstance = new WeaponInstance(currentWeaponData);
    }

    private void Start()
    {
        // 풀매니저 세팅
        if (!ManagerRegistry.TryGet<PoolManager>(out _poolManager))
            _poolManager = null;

        if (!ManagerRegistry.TryGet<HapticManager>(out _hapticManager))
            _hapticManager = null;

        _player.OnLocomotionChanged += HandleLocomotionChanged;
    }

    void OnDestroy()
    {
        _player.OnLocomotionChanged -= HandleLocomotionChanged;
    }



    public void FireShotgun()
    {
        if (!TryFireWeapon(_shotgunInstance, true))
            return;

        Fire(_shotgunData);

        //_hapticManager?.PlayShotgunShot();
        float angle = Mathf.Atan2(_player.playerAimer.AimDirection.y, _player.playerAimer.AimDirection.x) * Mathf.Rad2Deg + 180f;
        _shotgunPivot.DORotate(new Vector3(0f, 0f, angle), 0f); // 0f = 즉시 회전

    }


    public void FireCurrentWeapon()
    {
        if (_player.deadeyeSkill.IsDeadeyeActive) return;
        if (currentWeaponData == null) return;
        if (!TryFireWeapon(_currentWeaponInstance, false)) return;

        Fire(currentWeaponData);
    }

    void Fire(SO_WeaponBase data)
    {
        Vector2 aimDir = _player.playerAimer.AimDirection;

        // 총알 스폰
        SpawnBullets(data, aimDir); // 총알은 정확한 마우스 방향으로

        // 반동
        Vector2 shootDir = SnapTo8Direction(aimDir); // 반동만 8방향 스냅

        // X만 초기화, Y는 보존 (점프 중 샷건 쏴도 Y속도 안 날아감)
        _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
        _rb.AddForce(-shootDir * data.recoilForce, ForceMode2D.Impulse);

        _hapticManager?.PlayOneShot(data.lowFrequency, data.highFrequency, data.duration);

        // 카메라 쉐이크 - 땅/공중 분기
        float shakeForce = _player.IsGrounded ? data.groundCameraShakeForce : data.airCameraShakeForce;
        _impulseSource.GenerateImpulse(-shootDir * shakeForce);

        TriggerRecoilRoutines(shootDir);
    }

    private bool TryFireWeapon(WeaponInstance instance, bool isShotgun)
    {
        var result = instance.TryConsumeDetailed();

        if (result == WeaponConsumeResult.Success)
            return true;

        if (result == WeaponConsumeResult.NoAmmo)
        {
            var context = new DryFireContext(instance, isShotgun);
            OnDryFire?.Invoke(context);
        }

        return false;
    }

    Vector2 SnapTo8Direction(Vector2 dir)
    {
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // 45도 단위로 반올림
        float snapped = Mathf.Round(angle / 45f) * 45f;

        float rad = snapped * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }

    void SpawnBullets(SO_WeaponBase data, Vector2 aimDir)
    {
        float baseAngle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;

        for (int i = 0; i < data.pelletCount; i++)
        {
            float spread = Random.Range(-data.spreadAngle, data.spreadAngle);
            float rad = (baseAngle + spread) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            SpawnBullet(data, dir);
        }
    }

    void SpawnBullet(SO_WeaponBase data, Vector2 dir)
    {
        // 무기에 따라 스폰 위치 결정
        Transform muzzle = data == _shotgunData ? _shotgunMuzzle : _gunMuzzle;
        Vector3 spawnPos = muzzle != null ? muzzle.position : _player.transform.position;

        GameObject bullet = _poolManager != null
            ? _poolManager.Get(data.bulletPrefab, spawnPos, Quaternion.identity)
            : Instantiate(data.bulletPrefab, spawnPos, Quaternion.identity);

        if (bullet.TryGetComponent<Rigidbody2D>(out var bulletRb))
            bulletRb.linearVelocity = dir * data.bulletSpeed;
    }

    void TriggerRecoilRoutines(Vector2 shootDir)
    {
        _player.SetRecoilState(RecoilState.Recoiling);

        _gravityDone = false;
        _dampingDone = false;

        StopCoroutine(nameof(GravityRoutine));
        StopCoroutine(nameof(DampingRoutine));
        StartCoroutine(nameof(GravityRoutine));
        StartCoroutine(nameof(DampingRoutine));

    }

    IEnumerator GravityRoutine()
    {
        _rb.gravityScale = 0f;

        float elapsed = 0f;
        while (elapsed < _player.gravityOffDuration)
        {
            // 천장 감지 시 즉시 중단 (자연스럽게 떨어지기 위함)
            if (Physics2D.OverlapCircle(_ceilingCheck.position, _ceilingCheckRadius, _groundLayer))
            {
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0f);
                break;
            }
            elapsed += Time.deltaTime;
            yield return null; // 매 프레임 체크
        }
        _rb.gravityScale = _player.OriginalGravity;
        _gravityDone = true;
        TryExitRecoiling();

    }

    IEnumerator DampingRoutine()
    {
        _rb.linearDamping = _player.dampingValue;
        yield return new WaitForSeconds(_player.dampingDuration);
        _rb.linearDamping = 0f;

        _dampingDone = true;
        TryExitRecoiling();
    }

    void TryExitRecoiling()
    {
        if (!_gravityDone || !_dampingDone) return; // 둘 다 끝나야 해제

        _player.SetRecoilState(RecoilState.None);

        if (!_player.IsGrounded)
            _player.CanJump = false;
    }

    public void ReloadAll()
    {
        _shotgunInstance.Reload();
        _currentWeaponInstance?.Reload();
    }

    // 무기 추가 시 필요.
    public void SwapWeapon(SO_WeaponBase newWeapon)
    {
        currentWeaponData = newWeapon;
        _currentWeaponInstance = new WeaponInstance(newWeapon); // 교체 시 인스턴스도 새로 생성 (기존꺼는 자동으로 GC가 해결.)
    }


    // 지워야 할 코드
    void HandleLocomotionChanged(LocomotionState state)
    {
        if (state == LocomotionState.Land)
        {
            // 착지 시 샷건 원래 자세로 복귀
            _shotgunPivot.DORotate(new Vector3(0f, 0f, _shotgunIdleAngle), 0.2f);
        }
    }

    // 상태 다 종료
    public void ResetState()
    {
        StopCoroutine(nameof(GravityRoutine));
        StopCoroutine(nameof(DampingRoutine));
        _rb.gravityScale = _player.OriginalGravity;
        _rb.linearDamping = 0f;
        _gravityDone = false;
        _dampingDone = false;
    }

    //컷씬용 메서드
    public void CutsceneFireRight()
    {
        SO_WeaponBase data = _shotgunData;
        Vector2 aimDir = Vector2.right;

        // 총알 스폰
        SpawnBullets(data, aimDir); // 총알은 정확한 마우스 방향으로

        // 반동
        Vector2 shootDir = SnapTo8Direction(aimDir); // 반동만 8방향 스냅

        // X만 초기화, Y는 보존 (점프 중 샷건 쏴도 Y속도 안 날아감)
        _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
        _rb.AddForce(-shootDir * data.recoilForce, ForceMode2D.Impulse);

        _hapticManager?.PlayOneShot(data.lowFrequency, data.highFrequency, data.duration);

        TriggerRecoilRoutines(shootDir);
    }

    public void CutsceneFireLeft()
    {
        SO_WeaponBase data = _shotgunData;
        Vector2 aimDir = Vector2.left;

        // 총알 스폰
        SpawnBullets(data, aimDir); // 총알은 정확한 마우스 방향으로

        // 반동
        Vector2 shootDir = SnapTo8Direction(aimDir); // 반동만 8방향 스냅

        // X만 초기화, Y는 보존 (점프 중 샷건 쏴도 Y속도 안 날아감)
        _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
        _rb.AddForce(-shootDir * data.recoilForce, ForceMode2D.Impulse);

        _hapticManager?.PlayOneShot(data.lowFrequency, data.highFrequency, data.duration);

        TriggerRecoilRoutines(shootDir);
    }

}