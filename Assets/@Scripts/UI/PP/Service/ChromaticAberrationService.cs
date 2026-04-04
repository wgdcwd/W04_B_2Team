using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ChromaticAberrationService : MonoBehaviour
{
    [SerializeField] private Volume _volume;

    private ChromaticAberration _chromaticAberration;
    private Tween _intensityTween;
    private float _defaultIntensity;
    private bool _hasDefaultState;

    private void Start()
    {
        if (_volume != null && _volume.profile.TryGet(out _chromaticAberration))
        {
            _defaultIntensity = _chromaticAberration.intensity.value;
            _hasDefaultState = true;
            _chromaticAberration.intensity.value = 0f;
        }
    }

    private void OnDisable()
    {
        StopEffect();
    }

    public void Play(ChromaticAberrationEffectSettings settings)
    {
        if (_chromaticAberration == null)
            return;

        StopEffect();
        _chromaticAberration.intensity.value = settings.startIntensity;
        _intensityTween = DOTween.To(
            () => _chromaticAberration.intensity.value,
            x => _chromaticAberration.intensity.value = x,
            settings.endIntensity,
            settings.duration
        );
    }

    public void SetState(float intensity)
    {
        if (_chromaticAberration == null)
            return;

        StopEffect();
        _chromaticAberration.intensity.value = intensity;
    }

    public void RestoreDefault()
    {
        if (_chromaticAberration == null)
            return;

        StopEffect();

        if (_hasDefaultState)
        {
            _chromaticAberration.intensity.value = _defaultIntensity;
            return;
        }

        _chromaticAberration.intensity.value = 0f;
    }

    private void StopEffect()
    {
        _intensityTween?.Kill();
        _intensityTween = null;
    }
}
