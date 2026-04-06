using DG.Tweening;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.U2D;

public class StageStartAction : MonoBehaviour
{
    public float ZoomOutDuration = 2f;
    CinemachineCamera _vcam;

    void Start()
    {
        _vcam = GetComponent<CinemachineCamera>();

        CinemachineBrain brain = Camera.main.GetComponent<CinemachineBrain>();
        var originalBlend = brain.DefaultBlend;
        brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);
        _vcam.Priority = 100;
        float defaultSize = _vcam.Lens.OrthographicSize;
        _vcam.Lens.OrthographicSize = 0.1f;

        StartCoroutine(RestoreBlend(brain, originalBlend));

        // PixelPerfectCamera 끄기
        PixelPerfectCamera pixelPerfect = Camera.main.GetComponent<PixelPerfectCamera>();
        if (pixelPerfect != null) pixelPerfect.enabled = false;

        DOTween.To(
            () => _vcam.Lens.OrthographicSize,
            x => _vcam.Lens.OrthographicSize = x,
            defaultSize,
            ZoomOutDuration
        ).SetEase(Ease.OutCubic)
        .OnComplete(() =>
        {
            // 줌 끝나면 다시 켜기
            if (pixelPerfect != null) pixelPerfect.enabled = true;

            _vcam.Priority = 0;
            gameObject.SetActive(false);
        });
    }

    IEnumerator RestoreBlend(CinemachineBrain brain, CinemachineBlendDefinition original)
    {
        yield return null;
        brain.DefaultBlend = original;
    }
}