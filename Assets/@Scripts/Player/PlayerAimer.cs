using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAimer : MonoBehaviour
{
    [SerializeField] private Transform _gunPivot;
    [SerializeField] private Camera _cam;

    Player _player;
    Rigidbody2D _rb;

    [Header("Aim Assist")]
    [SerializeField] private float _aimAssistRadiusMouse = 1.5f;
    [SerializeField] private float _aimAssistRadiusGamepad = 4f;
    [SerializeField] private float _aimAssistAngleGamepad = 30f;
    [SerializeField] private float _aimAssistStrengthMouse = 0.15f;
    [SerializeField] private float _aimAssistStrengthGamepad = 0.3f;
    [SerializeField] private LayerMask _enemyLayer;
    [SerializeField] private LayerMask _groundLayer;

    [Header("Camera Aim Offset")]
    [SerializeField] CinemachineCamera _vcam;
    [SerializeField] float _aimOffsetStrength = 2f;
    [SerializeField] float _smoothSpeed = 3f;

    CinemachinePositionComposer _composer;
    Vector3 _baseOffset = Vector3.zero;
    Vector3 _defaultBaseOffset = Vector3.zero;

    private Dictionary<CinemachinePositionComposer, Vector3> _originalOffsets = new();

    [Header("Lookahead")]
    [SerializeField] float _lookaheadStrength = 2f;
    [SerializeField] float _lookaheadSmooth = 3f;
    [SerializeField] float _returnDelay = 1.5f;
    [SerializeField] float _returnSmooth = 1.5f;

    Vector2 _lastMoveDir = Vector2.zero;
    Vector2 _lookaheadOffset = Vector2.zero;
    float _stopTimer = 0f;

    public Vector2 AimDirection { get; private set; } = Vector2.right;
    public Transform GunPivot => _gunPivot;
    public bool IsLookingLeft { get; private set; }
    public bool IsUsingGamepad { get; private set; }

    private void Awake()
    {
        if (_cam == null)
            _cam = Camera.main;

        _player = GetComponent<Player>();
        _rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        _composer = _vcam.GetComponent<CinemachinePositionComposer>();
        _defaultBaseOffset = _composer.TargetOffset;
        _baseOffset = _defaultBaseOffset;

        // 기본 카메라 원본 등록
        _originalOffsets[_composer] = _defaultBaseOffset;
    }

    void Update()
    {
        UpdateLookahead();
    }

    private Vector3 GetOriginalOffset(CinemachinePositionComposer composer)
    {
        // 처음 접근할 때만 현재값을 원본으로 저장
        // 이후엔 항상 저장된 원본 반환
        if (!_originalOffsets.ContainsKey(composer))
            _originalOffsets[composer] = composer.TargetOffset;

        return _originalOffsets[composer];
    }

    public void SetComposer(CinemachineCamera vcam)
    {
        var composer = vcam.GetComponent<CinemachinePositionComposer>();
        if (composer == null) return;

        // 나가는 카메라 원상복구
        if (_composer != null)
            _composer.TargetOffset = _baseOffset;

        _composer = composer;
        _baseOffset = GetOriginalOffset(composer); // 항상 원본값 기준
    }

    public void ResetComposer()
    {
        // 나가는 카메라 원상복구
        if (_composer != null)
            _composer.TargetOffset = _baseOffset;

        _composer = _vcam.GetComponent<CinemachinePositionComposer>();
        _baseOffset = _defaultBaseOffset;
        _composer.TargetOffset = _defaultBaseOffset;
    }

    public void HandleLook(InputAction.CallbackContext ctx)
    {
        if (ctx.canceled) return;
        Vector2 input = ctx.ReadValue<Vector2>();
        if (input.sqrMagnitude < 0.01f) return;

        IsUsingGamepad = true;
        AimDirection = input.normalized;

        Vector2 detectCenter = (Vector2)transform.position;
        AimDirection = GetAimAssistDirection(AimDirection, detectCenter, _aimAssistRadiusGamepad, _aimAssistAngleGamepad);
        ApplyRotation();
    }

    public void HandleLookMouse(InputAction.CallbackContext ctx)
    {
        if (ctx.canceled) return;
        IsUsingGamepad = false;

        Vector2 input = ctx.ReadValue<Vector2>();
        Vector2 mouseWorld = _cam.ScreenToWorldPoint(input);
        Vector2 dir = mouseWorld - (Vector2)transform.position;
        if (dir.sqrMagnitude > 0.001f)
            AimDirection = dir.normalized;

        Vector2 detectCenter = mouseWorld;
        AimDirection = GetAimAssistDirection(AimDirection, detectCenter, _aimAssistRadiusMouse, 360f);
        ApplyRotation();
    }

    private void ApplyRotation()
    {
        float angle = Mathf.Atan2(AimDirection.y, AimDirection.x) * Mathf.Rad2Deg;

        IsLookingLeft = angle > 90f || angle < -90f;

        if (angle > 90f)
            angle -= 180f;
        else if (angle < -90f)
            angle += 180f;

        _gunPivot.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    Vector2 GetAimAssistDirection(Vector2 aimDir, Vector2 detectCenter, float radius, float maxAngle)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(detectCenter, radius, _enemyLayer);
        if (hits.Length == 0) return aimDir;

        Collider2D closest = null;
        float closestAngle = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            Vector2 toEnemy = ((Vector2)hit.transform.position - (Vector2)transform.position).normalized;
            float angle = Vector2.Angle(aimDir, toEnemy);

            if (angle > maxAngle) continue;

            float dist = Vector2.Distance(transform.position, hit.transform.position);
            RaycastHit2D wallCheck = Physics2D.Raycast(
                transform.position,
                toEnemy,
                dist,
                _groundLayer
            );
            if (wallCheck.collider != null) continue;

            if (angle < closestAngle)
            {
                closestAngle = angle;
                closest = hit;
            }
        }

        if (closest == null) return aimDir;

        Vector2 toClosest = ((Vector2)closest.transform.position - (Vector2)transform.position).normalized;
        float strength = IsUsingGamepad ? _aimAssistStrengthGamepad : _aimAssistStrengthMouse;
        return Vector2.Lerp(aimDir, toClosest, strength).normalized;
    }

    void UpdateLookahead()
    {
        if (_composer == null) return;

        Vector2 velocity = _rb.linearVelocity;
        bool moving = velocity.sqrMagnitude > 0.1f;

        if (moving)
        {
            _lastMoveDir = velocity.normalized;
            _stopTimer = 0f;

            Vector2 targetLookahead = _lastMoveDir * _lookaheadStrength;
            _lookaheadOffset = Vector2.Lerp(_lookaheadOffset, targetLookahead, Time.deltaTime * _lookaheadSmooth);
        }
        else
        {
            _stopTimer += Time.deltaTime;

            if (_returnDelay > 0f && _stopTimer >= _returnDelay)
                _lookaheadOffset = Vector2.Lerp(_lookaheadOffset, Vector2.zero, Time.deltaTime * _returnSmooth);
        }

        _lookaheadOffset = Vector2.ClampMagnitude(_lookaheadOffset, _lookaheadStrength);

        Vector2 aimOffset = AimDirection * _aimOffsetStrength;
        Vector2 totalOffset = aimOffset + _lookaheadOffset;

        Vector3 targetOffset = _baseOffset + new Vector3(totalOffset.x, totalOffset.y / 2, 0f);

        _composer.TargetOffset = Vector3.Lerp(
            _composer.TargetOffset,
            targetOffset,
            Time.deltaTime * _smoothSpeed
        );
    }
}