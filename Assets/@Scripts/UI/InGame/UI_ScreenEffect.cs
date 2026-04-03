using UnityEngine;
using UnityEngine.UI; // Image 컴포넌트를 사용하기 위해 추가
using DG.Tweening;

public class UI_ScreenEffect : MonoBehaviour
{
    private static readonly int DistortionStrengthId = Shader.PropertyToID("_DistortionStrength");

    // Material을 직접 받는 대신, 머티리얼이 적용된 UI Image를 받는 것이 안전합니다.
    [SerializeField] private Image _distortionImage;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private CanvasGroup _dimOverlayCanvasgroup;
    [SerializeField] private float _duration = 0.5f;

    private Sequence _sequence;

    private void OnEnable()
    {
        _sequence?.Kill();

        // 1. 머티리얼 값 초기화 (Image의 매터리얼 인스턴스 사용)
        _distortionImage.material.SetFloat(DistortionStrengthId, 0f);
        _dimOverlayCanvasgroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;

        _sequence = DOTween.Sequence();

        // 2. SetUpdate(true)를 추가하여 게임이 일시정지되어도 UI 애니메이션이 재생되도록 보장
        _sequence.SetUpdate(true);

        // 3. DOTween.To 대신 머티리얼 전용 확장 메서드 DOFloat 사용
        _sequence.Join(_distortionImage.material.DOFloat(1f, DistortionStrengthId, _duration));
        _sequence.Join(_dimOverlayCanvasgroup.DOFade(1f, _duration));

        _sequence.OnComplete(() =>
        {
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
            _sequence = null;
        });
    }

    private void OnDisable()
    {
        _sequence?.Kill();
        _sequence = null;

        // 비활성화 시에도 초기화
        if (_distortionImage != null && _distortionImage.material != null)
        {
            _distortionImage.material.SetFloat(DistortionStrengthId, 0f);
        }
        _dimOverlayCanvasgroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
    }
}