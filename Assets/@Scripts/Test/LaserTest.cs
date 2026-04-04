using UnityEngine;
using DG.Tweening;

public class LaserTest : MonoBehaviour
{
    public float duration = 0.5f;
    public float maxScaleX = 2f;
    public Ease easeType = Ease.Linear;

    [ContextMenu("Fire Laser")]
    public void FireLaser()
    {
        // 실행 중인 트윈 취소
        transform.DOKill();

        // X 스케일을 0으로 초기화
        Vector3 scale = transform.localScale;
        scale.x = 0f;
        transform.localScale = scale;

        // 트윈 실행
        transform.DOScaleX(maxScaleX, duration).SetEase(easeType);
    }
}