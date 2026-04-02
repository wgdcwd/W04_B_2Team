using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VignetteService : MonoBehaviour
{
    [SerializeField] private Color _baseColor;
    [SerializeField] private Color _setColor;
    [SerializeField] private Volume _volume;
    [SerializeField] private float _defaultValue;
    [SerializeField] private float _activeValue;
    [SerializeField] private float _duration = 0.5f;

    private Vignette _vignette;
    private Tween _intensityTween;
    private Tween _colorTween;

    private void Start()
    {
        if (_volume.profile.TryGet(out _vignette))
        {
            SetDefaultState();
        }
    }

    private void OnDisable()
    {
        StopEffect();
    }

    public void DoTweenPlay()
    {
        StopEffect();
        Debug.Log("VignetteService 호출");
        SetActiveState();

        _intensityTween = DOTween.To(
            () => _vignette.intensity.value,
            x => _vignette.intensity.value = x,
            _defaultValue,
            _duration
        );

        _colorTween = DOTween.To(
            () => _vignette.color.value,
            x => _vignette.color.value = x,
            _baseColor,
            _duration
        );
    }
    private void SetActiveState()
    {
        _vignette.intensity.value = _activeValue;
        _vignette.color.value = _setColor;
    }

    private void SetDefaultState()
    {
        _vignette.intensity.value = _defaultValue;
        _vignette.color.value = _baseColor;
    }

    private void StopEffect()
    {
        _intensityTween?.Kill();
        _colorTween?.Kill();
        _intensityTween = null;
        _colorTween = null;
    }
}
