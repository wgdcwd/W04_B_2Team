using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ChromaticAberrationService : MonoBehaviour
{
    [SerializeField] private float _value;
    [SerializeField] private Volume _volume;

    private ChromaticAberration _chromaticAberration;

    private void Start()
    {
        if (_volume.profile.TryGet(out _chromaticAberration))
        {
            _chromaticAberration.intensity.value = 0f;
        }
    }

    public void DoTweenPlay(float duration)
    {
        Debug.Log("ChromaticAberrationService 호출");
        //      => 현재값, x
        //      => 세팅할 값, 목표값, 지속시간
        DOTween.To(() => _chromaticAberration.intensity.value, x
                        => _chromaticAberration.intensity.value = x, 0f, duration).From(_value);
    }
}
