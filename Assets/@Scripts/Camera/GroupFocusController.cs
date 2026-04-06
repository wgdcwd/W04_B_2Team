using Unity.Cinemachine;
using UnityEngine;

public class GroupFocusController : MonoBehaviour
{
    [Header("References")]
    public CinemachineTargetGroup targetGroup;
    public Transform player;
    public Transform target;

    [Header("Settings")]
    public float focusDistance = 15f;   // 이 거리 이상이면 플레이어에 집중
    public float transitionSpeed = 2f;  // 전환 부드러움

    // TargetGroup 내 인덱스 (추가한 순서 기준)
    // 0 = player, 1 = target 이라고 가정
    private const int TargetIndex = 1;

    void Update()
    {
        float distance = Vector3.Distance(player.position, target.position);

        // 거리가 멀수록 weight가 0에 가까워짐
        float t = Mathf.InverseLerp(focusDistance * 0.5f, focusDistance, distance);
        float desiredWeight = Mathf.Lerp(1f, 0f, t);

        // 부드럽게 전환
        var member = targetGroup.Targets[TargetIndex];
        member.Weight = Mathf.Lerp(member.Weight, desiredWeight, Time.deltaTime * transitionSpeed);
        targetGroup.Targets[TargetIndex] = member;
    }
}
