using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VignetteService : MonoBehaviour
{
    [SerializeField] private Volume _volume;

    private Vignette _vignette;
    private Tween _intensityTween;
    private Tween _colorTween;
    private float _defaultIntensity;
    private Color _defaultColor;
    private bool _hasDefaultState;

    private void Start()
    {
        if (_volume != null)
            _volume.profile.TryGet(out _vignette);

        if (_vignette == null)
            return;

        _defaultIntensity = _vignette.intensity.value;
        _defaultColor = _vignette.color.value;
        _hasDefaultState = true;
    }

    private void OnDisable()
    {
        StopEffect();
    }

    public void Play(VignetteEffectSettings settings)
    {
        if (_vignette == null)
            return;

        StopEffect();
        SetState(settings.startIntensity, settings.startColor);

        _intensityTween = DOTween.To(
            () => _vignette.intensity.value,
            x => _vignette.intensity.value = x,
            settings.endIntensity,
            settings.duration
        );

        _colorTween = DOTween.To(
            () => _vignette.color.value,
            x => _vignette.color.value = x,
            settings.endColor,
            settings.duration
        );
    }

    public void RestoreDefault()
    {
        if (_vignette == null || !_hasDefaultState)
            return;

        StopEffect();
        SetState(_defaultIntensity, _defaultColor);
    }

    public void SetImmediate(float intensity, Color color)
    {
        if (_vignette == null)
            return;

        StopEffect();
        SetState(intensity, color);
    }

    private void SetState(float intensity, Color color)
    {
        _vignette.intensity.value = intensity;
        _vignette.color.value = color;
    }

    private void StopEffect()
    {
        _intensityTween?.Kill();
        _colorTween?.Kill();
        _intensityTween = null;
        _colorTween = null;
    }
}
