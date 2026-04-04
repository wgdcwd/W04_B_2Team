using DG.Tweening;
using System;
using Unity.Cinemachine;
using UnityEngine;

public class StageStartAction : MonoBehaviour
{
    public float ZoomOutDuration = 2f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        CinemachineCamera _vcam = GetComponent<CinemachineCamera>();
        float defaultSize = _vcam.Lens.OrthographicSize;
        _vcam.Lens.OrthographicSize = 1f;
        // 시작할때 줌 아웃
        DOTween.To(
            () => _vcam.Lens.OrthographicSize,
            x => _vcam.Lens.OrthographicSize = x,
            defaultSize, // 목표 크기
            ZoomOutDuration  // 지속 시간
        ).SetEase(Ease.OutCubic);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
