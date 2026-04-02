using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class LensDistortionService : MonoBehaviour
{
    [SerializeField] private Volume _volume;

    private LensDistortion _lensDistortion;
    private Tween _intensityTween;

    private void Start()
    {
        if (_volume != null && _volume.profile.TryGet(out _lensDistortion))
            _lensDistortion.intensity.value = 0f;
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

    private void StopEffect()
    {
        _intensityTween?.Kill();
        _intensityTween = null;
    }
}
