using UnityEngine;

public class MetaUIJumpHoldBinder : MonoBehaviour
{
    [SerializeField] private LensDistortionService _lensDistortionService;
    [SerializeField] private ChromaticAberrationService _chromaticAberrationService;

    [Header("Lens")]
    [SerializeField] private float _lensIntensity = -0.35f;
    [SerializeField] private float _lensXMultiplier = 1f;
    [SerializeField] private float _lensYMultiplier = 1f;
    [SerializeField] private float _lensSpeed = 3.5f;

    [Header("Chromatic")]
    [SerializeField] private float _chromaticIntensity = 0.35f;
    [SerializeField] private float _chromaticSpeed = 3.5f;

    private Player _player;
    private bool _isActive;
    private bool _wasActive;
    private float _currentLens;
    private float _currentChromatic;

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
            ClearEffects();
            return;
        }

        _player.OnLocomotionChanged += HandleLocomotionChanged;
        _player.OnSkillStateChanged += HandleSkillStateChanged;

        RefreshState();
    }

    private void Update()
    {
        if (_isActive)
        {
            UpdateEffects();
            _wasActive = true;
            return;
        }

        if (_wasActive)
        {
            ClearEffects();
            _wasActive = false;
        }
    }

    private void OnDisable()
    {
        ClearEffects();
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
        _isActive = _player != null && _player.IsMetaLensActive;
    }

    private void UpdateEffects()
    {
        _currentLens = Mathf.MoveTowards(
            _currentLens,
            _lensIntensity,
            _lensSpeed * Time.unscaledDeltaTime
        );

        _currentChromatic = Mathf.MoveTowards(
            _currentChromatic,
            _chromaticIntensity,
            _chromaticSpeed * Time.unscaledDeltaTime
        );

        _lensDistortionService?.SetState(_currentLens, _lensXMultiplier, _lensYMultiplier);
        _chromaticAberrationService?.SetState(_currentChromatic);
    }

    private void ClearEffects()
    {
        _isActive = false;
        _currentLens = 0f;
        _currentChromatic = 0f;

        _lensDistortionService?.RestoreDefault();
        _chromaticAberrationService?.RestoreDefault();
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
