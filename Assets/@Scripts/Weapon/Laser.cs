using UnityEngine;
using UnityEngine.InputSystem;

public class Laser : MonoBehaviour
{
    [SerializeField] private Transform _muzzle;     // 총구 위치
    [SerializeField] private LayerMask _blockLayer; // 레이저 막을 레이어 (Ground, Enemy 등)
    [SerializeField] private float _laserRange = 20f;
    [SerializeField] private RectTransform _cursorDot;
    [SerializeField] private float _cursorVisibleRange = 7f;


    LineRenderer _laser;
    Player _player;
    Camera _cam;
    GameStateManager _gameStateManager;

    void Start()
    {
        _laser = GetComponent<LineRenderer>();
        _player = GetComponentInParent<Player>();
        _cam = Camera.main;
        Cursor.visible = false;

        _gameStateManager = ManagerRegistry.Get<GameStateManager>();
        if (_gameStateManager != null)
            _gameStateManager.OnStateChanged += HandleStateChanged;

    }
    void OnDestroy()
    {
        if (_gameStateManager != null)
            _gameStateManager.OnStateChanged -= HandleStateChanged;
    }

    void HandleStateChanged(GameState state)
    {
        if (_cursorDot == null) return;

        bool isPlaying = state == GameState.Playing;
        _cursorDot.gameObject.SetActive(isPlaying);
    }

    void Update()
    {
        Vector2 aimDir = _player.playerAimer.AimDirection;
        _laser.SetPosition(0, _muzzle.position);

        RaycastHit2D hit = Physics2D.Raycast(_muzzle.position, aimDir, _laserRange, _blockLayer);

        if (hit.collider != null)
            _laser.SetPosition(1, hit.point);
        else
            _laser.SetPosition(1, (Vector2)_muzzle.position + aimDir * _laserRange);

        if (_cursorDot != null)
        {
            Vector2 laserStart = _muzzle.position;
            Vector2 laserEnd = laserStart + aimDir * _laserRange;

            Vector2 mouseWorld = _cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());

            // 마우스를 레이저 선에 투영
            float t = Vector2.Dot(mouseWorld - laserStart, aimDir);
            t = Mathf.Clamp(t, 0f, _cursorVisibleRange); // 0 ~ 20m 사이로 클램프

            Vector2 dotPosition = laserStart + aimDir * t;
            _cursorDot.position = _cam.WorldToScreenPoint(dotPosition);
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        Cursor.visible = !hasFocus;
    }
}
