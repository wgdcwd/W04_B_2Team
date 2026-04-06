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
    
    [Header("파티클 레퍼런스")]
    [SerializeField] private ParticleSystem _bulletShellParticle;
    [SerializeField] private ParticleSystem _pistolShellParticle;

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
    private Transform _pistolPivot;
    [SerializeField] private Transform _shotgunPivot;
    [SerializeField] private float _shotgunIdleAngle = 90f; // 평소 위로 든 각도

    [Header("Screen Shake")]
    [SerializeField] CinemachineImpulseSource _impulseSource;

    private PoolManager _poolManager;
    private HapticManager _hapticManager;

    // Temp: 둘 다 끝났는지 추적
    bool _gravityDone = false;
    bool _dampingDone = false;

    // 총알없음 액션
    public event Action<DryFireContext> OnDryFire;

    // 총 flip용 참조
    private SpriteRenderer _pistolRenderer;
    private SpriteRenderer _shotgunRenderer;
    private int _pistolBaseSortingOrder;
    private int _shotgunBaseSortingOrder;
    private Vector3 _pistolBaseScale;
    private Vector3 _shotgunBaseScale;
    private bool _lastLookingLeft;
    private bool _isShotgunAngleLocked;
    private float _lockedShotgunAngle = 90f;
    private bool? _forcedShotgunLookLeft;
    private int _cutsceneShotgunShotCount;

    // 몬스터가 총알 채워주는 경우 쿨타임
    private bool _canAddAmmo = true;
    private float _addAmmoCooldown = 0.1f;


    private void Awake()
    {
        _player = GetComponent<Player>();
        _rb = GetComponent<Rigidbody2D>();
        _shotgunInstance = new WeaponInstance(_shotgunData);
        _currentWeaponInstance = new WeaponInstance(currentWeaponData);
        _bulletShellParticle.gameObject.SetActive(true);
    }

    private void Start()
    {
        // 풀매니저 세팅
        if (!ManagerRegistry.TryGet<PoolManager>(out _poolManager))
            _poolManager = null;

        if (!ManagerRegistry.TryGet<HapticManager>(out _hapticManager))
            _hapticManager = null;

        if (_player != null && _player.playerAimer != null)
        {
            _pistolPivot = _player.playerAimer.GunPivot;
        }

        Debug.Log($"PlayerAttack Awake: pistolPivot={_pistolPivot}, shotgunPivot={_shotgunPivot}");

        if (_pistolPivot != null)
        {
            _pistolBaseScale = _pistolPivot.localScale;
            _pistolRenderer = _pistolPivot.GetComponentInChildren<SpriteRenderer>();
            if (_pistolRenderer != null)
                _pistolBaseSortingOrder = _pistolRenderer.sortingOrder;
        }

        if (_shotgunPivot != null)
        {
            _shotgunBaseScale = _shotgunPivot.localScale;
            _shotgunRenderer = _shotgunPivot.GetComponentInChildren<SpriteRenderer>();
            if (_shotgunRenderer != null)
                _shotgunBaseSortingOrder = _shotgunRenderer.sortingOrder;
        }
    }

    public void FireShotgun()
    {
        if (!TryFireWeapon(_shotgunInstance, true))
            return;

        Fire(_shotgunData);

        if (_bulletShellParticle != null)
        {
            _bulletShellParticle.Emit(1);
        }

        //_hapticManager?.PlayShotgunShot();
        ApplyShotgunRotation();
    }


    public void FireCurrentWeapon()
    {
        // if (_player.deadeyeSkill.IsDeadeyeActive)
        //     return;

        if (currentWeaponData == null)
            return;

        if (!TryFireWeapon(_currentWeaponInstance, false))
            return;
        
        if (_pistolShellParticle != null)
        {
            _pistolShellParticle.Emit(1);
        }

        //SoundManager.instance.HandlePistolSFX();
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

        // 땅/공중에 따라 흔들림 세기 결정
        float shakeForce = _player.IsGrounded ? data.groundCameraShakeForce : data.airCameraShakeForce;
        _impulseSource.GenerateImpulseWithVelocity(-shootDir * shakeForce);

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
        RaiseShotgun();
    }

    // 무기 추가 시 필요.
    public void SwapWeapon(SO_WeaponBase newWeapon)
    {
        currentWeaponData = newWeapon;
        _currentWeaponInstance = new WeaponInstance(newWeapon); // 교체 시 인스턴스도 새로 생성 (기존꺼는 자동으로 GC가 해결.)
    }

    public void RaiseShotgun()
    {
        if (_shotgunPivot == null)
            return;

        float targetAngle = _isShotgunAngleLocked ? _lockedShotgunAngle : _shotgunIdleAngle;
        _shotgunPivot.DOKill();
        _shotgunPivot.localRotation = Quaternion.Euler(0f, 0f, targetAngle);
    }

    void ApplyShotgunRotation()
    {
        if (_shotgunPivot == null)
            return;

        float angle = _isShotgunAngleLocked
            ? _lockedShotgunAngle
            : Mathf.Atan2(_player.playerAimer.AimDirection.y, _player.playerAimer.AimDirection.x) * Mathf.Rad2Deg;

        _shotgunPivot.DOKill();
        _shotgunPivot.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    public void UpdateWeaponFlip()
    {
        bool isLookingLeft = _forcedShotgunLookLeft ?? _player.playerAimer.IsLookingLeft;

        if (_lastLookingLeft == isLookingLeft)
            return;

        _lastLookingLeft = isLookingLeft;

        if (_pistolRenderer != null)
        {
            _pistolRenderer.flipX = isLookingLeft;
        }

        if (_shotgunRenderer != null)
        {
            _shotgunRenderer.flipY = isLookingLeft;
        }

        if (_pistolRenderer != null && _shotgunRenderer != null)
        {
            if (isLookingLeft)
            {
                _pistolRenderer.sortingOrder = _shotgunBaseSortingOrder;
                _shotgunRenderer.sortingOrder = _pistolBaseSortingOrder;
            }
            else
            {
                _pistolRenderer.sortingOrder = _pistolBaseSortingOrder;
                _shotgunRenderer.sortingOrder = _shotgunBaseSortingOrder;
            }
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

    public void AddAmmo()
    {
        if (!_canAddAmmo) return;
        _canAddAmmo = false;
        StartCoroutine(nameof(AddAmmoCooldown));
        _shotgunInstance.AddAmmo(1);
        //_currentWeaponInstance.AddAmmo(1);
    }

    IEnumerator AddAmmoCooldown()
    {
        yield return new WaitForSeconds(_addAmmoCooldown);
        _canAddAmmo = true;

    }


    #region Ryeol

    // 폭탄 관련 로직
    // 버려야할 코드.
    public void ReceiveExplosionForce(Vector2 forceDir, float forceMagnitude)
    {
        _rb.AddForce(forceDir * forceMagnitude, ForceMode2D.Impulse);
        TriggerRecoilRoutines(forceDir); // 기존 반동 루틴 재활용
    }

    #endregion

    public void CutsceneFireLeft()
    {
        LockCutsceneShotgunLeft();

        SO_WeaponBase data = _shotgunData;
        Vector2 aimDir = Vector2.left;

        if (_bulletShellParticle != null)
            _bulletShellParticle.Emit(1);

        // 총알 스폰
        SpawnBullets(data, aimDir); // 총알은 정확한 마우스 방향으로

        // 반동
        Vector2 shootDir = SnapTo8Direction(aimDir); // 반동만 8방향 스냅

        // X만 초기화, Y는 보존 (점프 중 샷건 쏴도 Y속도 안 날아감)
        _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
        _rb.AddForce(-shootDir * data.recoilForce, ForceMode2D.Impulse);

        _hapticManager?.PlayOneShot(data.lowFrequency, data.highFrequency, data.duration);
        ApplyShotgunRotation();

        TriggerRecoilRoutines(shootDir);

        _cutsceneShotgunShotCount++;
        if (_cutsceneShotgunShotCount >= 4)
        {
            _cutsceneShotgunShotCount = 0;
            UnlockCutsceneShotgun();
        }
    }

    public void LockCutsceneShotgunLeft()
    {
        _isShotgunAngleLocked = true;
        _lockedShotgunAngle = 180f;
        _forcedShotgunLookLeft = true;
        _lastLookingLeft = !(_forcedShotgunLookLeft ?? _player.playerAimer.IsLookingLeft);
        UpdateWeaponFlip();
        ApplyShotgunRotation();
    }

    public void UnlockCutsceneShotgun()
    {
        _isShotgunAngleLocked = false;
        _forcedShotgunLookLeft = null;
        _lastLookingLeft = !(_forcedShotgunLookLeft ?? _player.playerAimer.IsLookingLeft);
        UpdateWeaponFlip();
        RaiseShotgun();
    }

    public void ResetCutsceneShotgunSequence()
    {
        _cutsceneShotgunShotCount = 0;
        UnlockCutsceneShotgun();
    }
}
