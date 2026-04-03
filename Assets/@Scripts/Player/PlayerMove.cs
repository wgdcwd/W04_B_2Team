using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{
    Rigidbody2D _rb;
    Player _player;
    Vector2 _dir;

    // 애니메이션과 머리 제어용 참조
    PlayerAnimationController _playerAnimationController;
    PlayerHeadRotate _playerHeadRotate;

    [SerializeField] private Transform _groundCheck;
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private float _groundCheckRadius = 0.1f;

    [Tooltip("공중 반동 아닐 시의 좌우 이동 저항")]
    [SerializeField] private float _recoilMoveInfluence = 0.3f;
    [Tooltip("공중 반동일 시의 좌우 이동 저항")]
    [SerializeField] private float _airRecoilMoveInfluence = 0.1f;

    private float _landTimer = 0f;
    private Vector2 mouseScreenPos = Vector2.zero;
    private Vector3 mouseWorldPos = Vector3.zero;
    private bool _isLookingLeft => mouseWorldPos.x < transform.position.x;

    void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        _player = GetComponent<Player>();
        _player.OnRecoilStateChanged += HandleRecoilStateChanged;
        _playerAnimationController = GetComponent<PlayerAnimationController>();
        _playerHeadRotate = GetComponent<PlayerHeadRotate>();
    }

    void OnDestroy()
    {
        _player.OnRecoilStateChanged -= HandleRecoilStateChanged;
    }

    void HandleRecoilStateChanged(RecoilState state)
    {
        // 반동이 끝났는데 여전히 땅이면 재장전
        if (state == RecoilState.None && _player.IsGrounded)
            _player.playerAttack.ReloadAll();
    }

    void FixedUpdate()
    {
        if (_player.CurrentRecoil == RecoilState.Recoiling && !_player.IsGrounded)
        {
            float newX = _rb.linearVelocity.x + _player.moveSpeed * _dir.x * _airRecoilMoveInfluence * Time.fixedDeltaTime;
            _rb.linearVelocity = new Vector2(newX, _rb.linearVelocityY);
            return;
        }

        if (_player.CurrentRecoil == RecoilState.Recoiling && _player.IsGrounded)
        {
            float newX = _rb.linearVelocity.x + _player.moveSpeed * _dir.x * _recoilMoveInfluence * Time.fixedDeltaTime;
            _rb.linearVelocity = new Vector2(newX, _rb.linearVelocityY);
            return;
        }

        _rb.linearVelocity = new Vector2(_player.moveSpeed * _dir.x, _rb.linearVelocityY);
    }

    private void ReadMousePosition()
    {
        mouseScreenPos = Mouse.current.position.ReadValue();
        mouseWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, -Camera.main.transform.position.z));
    }

    void Update()
    {
        bool isGrounded = Physics2D.OverlapCircle(_groundCheck.position, _groundCheckRadius, _groundLayer);
        ReadMousePosition();

        _playerAnimationController.GetGoundCheck(isGrounded);

        _playerHeadRotate.RotateHead(mouseWorldPos);
        _playerAnimationController.FlipSprite(_isLookingLeft);
        _playerHeadRotate.FlipHead(_isLookingLeft);

        _playerAnimationController.SetWalkByMovment(_rb.linearVelocityX);

        // 공중 -> 착지 순간 감지
        if (isGrounded && !_player.IsGrounded)
        {
            _player.playerAttack.ReloadAll(); // 무기 전체 재장전
            _player.CanJump = true;
            _player.SetLocomotionState(LocomotionState.Land);
            _landTimer = 0f;
        }

        // Land 타이머
        if (_player.CurrentLocomotion == LocomotionState.Land)
        {
            _landTimer += Time.deltaTime;
            if (_landTimer >= _player.landDuration)
            {
                _player.SetLocomotionState(LocomotionState.Idle);
            }
            return; // Land 중엔 아래 로직 스킵
        }

        // LocomotionState 세팅
        if (isGrounded)
        {
            _player.SetLocomotionState(LocomotionState.Idle);
        }
        else
        {
            if (_rb.linearVelocity.y > 0.01f)
                _player.SetLocomotionState(LocomotionState.Jumping);
            else
                _player.SetLocomotionState(LocomotionState.Falling);
        }
    }

    public void CanMove(Vector2 input)
    {
        _dir = input;
    }
}