using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHeadRotate : MonoBehaviour

{
    [Header("References")]
    [SerializeField] private Transform headTransform;

    [Header("Rotation Settings")]
    [SerializeField] private float minAngle = -60f;
    [SerializeField] private float maxAngle = 60f;
    [SerializeField] private int step = 16;

    private Camera mainCam;
    private float _step;

    void Start()
    {
        _step = 360f / step;
        mainCam = Camera.main;

        if (headTransform == null)
        {
            Debug.LogError("Head Transform이 할당되지 않았습니다.");
        }
    }

    // Update is called once per frame
    void Update()
    {
        RotateHead();
    }

    private void RotateHead()
    {
        if (headTransform == null) return;

        // 마우스 위치를 월드 좌표로 변환
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = mainCam.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, -mainCam.transform.position.z));

        // 방향 및 각도 계산
        Vector2 direction = (Vector2)mouseWorldPos - (Vector2)headTransform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // 각도 보정 
        float absoluteParentScaleX = transform.lossyScale.x;
        if (absoluteParentScaleX < 0)
        {
            // 왼쪽을 보고 있을 때는 각도를 180도 반전
            if (angle > 0) angle = 180f - angle;
            else angle = -180f - angle;
        }

        // 클램핑 및 스냅
        angle = Mathf.Clamp(angle, minAngle, maxAngle);
        float snappedAngle = Mathf.Round(angle / _step) * _step;

        // 머리에만 회전 적용
        headTransform.localRotation = Quaternion.Euler(0, 0, snappedAngle);
    }
}
