using DG.Tweening;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class StageStartAction : MonoBehaviour
{
    public float ZoomOutDuration = 2f;
    CinemachineCamera _vcam;

    void Start()
    {
        _vcam = GetComponent<CinemachineCamera>();

        // 블렌드 없이 즉시 전환
        CinemachineBrain brain = Camera.main.GetComponent<CinemachineBrain>();
        var originalBlend = brain.DefaultBlend;
        brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);

        _vcam.Priority = 100;
        float defaultSize = _vcam.Lens.OrthographicSize;
        _vcam.Lens.OrthographicSize = 0.1f;

        // 다음 프레임에 블렌드 복구 (즉시 복구하면 전환 전에 복구될 수 있음)
        StartCoroutine(RestoreBlend(brain, originalBlend));

        DOTween.To(
            () => _vcam.Lens.OrthographicSize,
            x => _vcam.Lens.OrthographicSize = x,
            defaultSize,
            ZoomOutDuration
        ).SetEase(Ease.OutCubic)
        .OnComplete(() =>
        {
            _vcam.Priority = 0;
            gameObject.SetActive(false);
        });
    }

    IEnumerator RestoreBlend(CinemachineBrain brain, CinemachineBlendDefinition original)
    {
        yield return null; // 한 프레임 대기
        brain.DefaultBlend = original;
    }
}