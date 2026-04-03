using UnityEngine;

public class PlayerHeadRotate : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform headTransform;

    [Header("Rotation Settings")]
    [SerializeField] private float minAngle = -60f;
    [SerializeField] private float maxAngle = 60f;
    [SerializeField] private int step = 16;
    [SerializeField] private float angleOffset = 0f;

    private float _step;
    private bool _isLookingLeft;

    void Start()
    {
        _step = step > 0 ? 360f / step : 360f;

        if (headTransform == null)
        {
            Debug.LogError("Head Transform이 할당되지 않았습니다.");
        }
    }

    public void RotateHead(Vector3 mouseWorldPos)
    {
        if (headTransform == null) return;

        // 머리 위치 기준으로 마우스 방향 계산
        Vector2 direction = (Vector2)mouseWorldPos - (Vector2)headTransform.position;
        if (direction.sqrMagnitude < 0.0001f) return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // 현재 바라보는 방향을 기준으로 각도 보정
        float relativeAngle = Mathf.DeltaAngle(_isLookingLeft ? 180f : 0f, angle);

        // 머리 회전 범위 제한
        relativeAngle = Mathf.Clamp(relativeAngle, minAngle, maxAngle);

        // 스텝 단위로 각도 정리
        float snappedAngle = Mathf.Round((relativeAngle + angleOffset) / _step) * _step;

        // 최종 머리 회전 적용
        headTransform.localRotation = Quaternion.Euler(0, 0, snappedAngle);
    }

    public void FlipHead(bool isLookingLeft)
    {
        if (headTransform == null) return;

        _isLookingLeft = isLookingLeft;

        // 머리 스프라이트만 따로 뒤집기
        Vector3 localScale = headTransform.localScale;
        localScale.x = Mathf.Abs(localScale.x) * (_isLookingLeft ? -1f : 1f);
        headTransform.localScale = localScale;
    }
}