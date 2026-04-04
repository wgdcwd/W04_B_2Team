using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class LensDistortionService : MonoBehaviour
{
    [SerializeField] private Volume _volume;

    private LensDistortion _lensDistortion;
    private Tween _intensityTween;
    private float _defaultIntensity;
    private float _defaultXMultiplier;
    private float _defaultYMultiplier;
    private bool _hasDefaultState;

    private void Start()
    {
        if (_volume != null && _volume.profile.TryGet(out _lensDistortion))
        {
            _defaultIntensity = _lensDistortion.intensity.value;
            _defaultXMultiplier = _lensDistortion.xMultiplier.value;
            _defaultYMultiplier = _lensDistortion.yMultiplier.value;
            _hasDefaultState = true;
            _lensDistortion.intensity.value = 0f;
        }
    }

    private void OnDisable()
    {
        StopEffect();
    }

    public void Play(LensDistortionEffectSettings settings)
    {
        if (_lensDistortion == null)
            return;

        StopEffect();
        _lensDistortion.xMultiplier.value = settings.xMultiplier;
        _lensDistortion.yMultiplier.value = settings.yMultiplier;
        _lensDistortion.intensity.value = settings.startIntensity;
        _intensityTween = DOTween.To(
            () => _lensDistortion.intensity.value,
            x => _lensDistortion.intensity.value = x,
            settings.endIntensity,
            settings.duration
        );
    }

    public void SetState(float intensity, float xMultiplier, float yMultiplier)
    {
        if (_lensDistortion == null)
            return;

        StopEffect();
        _lensDistortion.intensity.value = intensity;
        _lensDistortion.xMultiplier.value = xMultiplier;
        _lensDistortion.yMultiplier.value = yMultiplier;
    }

    public void RestoreDefault()
    {
        if (_lensDistortion == null)
            return;

        StopEffect();

        if (_hasDefaultState)
        {
            _lensDistortion.intensity.value = _defaultIntensity;
            _lensDistortion.xMultiplier.value = _defaultXMultiplier;
            _lensDistortion.yMultiplier.value = _defaultYMultiplier;
            return;
        }

        _lensDistortion.intensity.value = 0f;
    }

    private void StopEffect()
    {
        _intensityTween?.Kill();
        _intensityTween = null;
    }
}
