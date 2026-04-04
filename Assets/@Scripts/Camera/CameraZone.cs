using Unity.Cinemachine;
using UnityEngine;

public class CameraZone : MonoBehaviour
{
    [SerializeField] private CinemachineCamera _zoneCam;

    // 중첩 존 진입 카운터 - 동일 존에 여러 콜라이더가 있거나
    // 두 존 경계에 걸쳐있을 때 잘못된 Reset 방지
    int _overlapCount = 0;

    PlayerAimer _aimer;

    private void Awake()
    {
        // Zone이 Player 자식이 아니므로 태그로 검색하는 것보다
        // PlayerAimer가 씬에 하나뿐임을 보장할 수 있으면 이 방식이 가장 안전
        _aimer = FindFirstObjectByType<PlayerAimer>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (_zoneCam.Target.TrackingTarget == null) _zoneCam.Target.TrackingTarget = other.transform;
        

        _overlapCount++;
        if (_overlapCount == 1)
        {
            _zoneCam.Priority = 20;
            _aimer?.SetComposer(_zoneCam);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        _overlapCount = Mathf.Max(0, _overlapCount - 1);
        if (_overlapCount == 0)
        {
            _zoneCam.Priority = 0;
            _aimer?.ResetComposer();
        }
    }
}