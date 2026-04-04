using UnityEngine;

public class MetaUIJumpHoldBinder : MonoBehaviour
{
    [SerializeField] private LensDistortionService _lensDistortionService;
    [SerializeField] private float _activeIntensity = -0.35f;
    [SerializeField] private float _xMultiplier = 1f;
    [SerializeField] private float _yMultiplier = 1f;
    [SerializeField] private float _enterSpeed = 3.5f;
    [SerializeField] private float _exitSpeed = 6f;

    private Player _player;
    private bool _isActive;
    private float _currentIntensity;

    public void Bind(Player player)
    {
        if (_player == player)
        {
            RefreshState();
            return;
        }

        Unbind();
        _player = player;

        if (_player == null)
        {
            _isActive = false;
            _currentIntensity = 0f;
            _lensDistortionService?.SetState(0f, _xMultiplier, _yMultiplier);
            return;
        }

        _player.OnLocomotionChanged += HandleLocomotionChanged;
        _player.OnSkillStateChanged += HandleSkillStateChanged;

        _currentIntensity = 0f;
        RefreshState();
    }

    private void Update()
    {
        if (_lensDistortionService == null)
            return;

        float targetIntensity = _isActive ? _activeIntensity : 0f;
        float speed = _isActive ? _enterSpeed : _exitSpeed;

        _currentIntensity = Mathf.MoveTowards(
            _currentIntensity,
            targetIntensity,
            speed * Time.unscaledDeltaTime
        );

        _lensDistortionService.SetState(_currentIntensity, _xMultiplier, _yMultiplier);
    }

    private void OnDisable()
    {
        _currentIntensity = 0f;
        _isActive = false;
        _lensDistortionService?.RestoreDefault();
    }

    private void OnDestroy()
    {
        Unbind();
    }

    private void HandleLocomotionChanged(LocomotionState state)
    {
        RefreshState();
    }

    private void HandleSkillStateChanged(SkillState state)
    {
        RefreshState();
    }

    private void RefreshState()
    {
        _isActive = _player != null && _player.IsSlowAirborne;
    }

    private void Unbind()
    {
        if (_player == null)
            return;

        _player.OnLocomotionChanged -= HandleLocomotionChanged;
        _player.OnSkillStateChanged -= HandleSkillStateChanged;
        _player = null;
    }
}
