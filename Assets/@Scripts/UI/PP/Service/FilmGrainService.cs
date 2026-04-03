using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class FilmGrainService : MonoBehaviour
{
    [SerializeField] private Volume _volume;

    private FilmGrain _filmGrain;
    private Tween _intensityTween;

    private void Start()
    {
        if (_volume != null && _volume.profile.TryGet(out _filmGrain))
            _filmGrain.intensity.value = 0f;
    }

    private void OnDisable()
    {
        StopEffect();
    }

    public void Play(FilmGrainEffectSettings settings)
    {
        if (_filmGrain == null)
            return;

        StopEffect();
        _filmGrain.response.value = settings.response;
        _filmGrain.intensity.value = settings.startIntensity;
        _intensityTween = DOTween.To(
            () => _filmGrain.intensity.value,
            x => _filmGrain.intensity.value = x,
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
