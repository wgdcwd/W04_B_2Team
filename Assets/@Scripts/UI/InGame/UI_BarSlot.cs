using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class UI_BarSlot : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform _animatedRoot;
    [SerializeField] private Image _fillImage;
    [SerializeField] private Image _echoImage;

    [Header("Damage")]
    [SerializeField] private float _blinkInterval = 0.1f;
    [SerializeField] [Range(0f, 1f)] private float _minEchoAlpha = 0.25f;

    [Header("Heal")]
    [SerializeField] private float _healScale = 1.15f;
    [SerializeField] private float _healDuration = 0.2f;

    [Header("Fill Hit")]
    [SerializeField] private bool _useFillHitFlash = false;
    [SerializeField] private Color _fillHitColor = Color.white;
    [SerializeField] private float _fillHitDuration = 0.12f;

    [Header("Ready")]
    [SerializeField] [Range(0f, 1f)] private float _readyMinAlpha = 0.7f;
    [SerializeField] private float _readyPulseDuration = 0.6f;

    private Sequence _effectSequence;
    private Sequence _readySequence;
    private RectTransform _targetRect;
    private Color _baseFillColor;

    private void Awake()
    {
        CacheAnimatedRoot();
        CacheBaseFillColor();
    }

    private void OnDestroy()
    {
        KillAllTweens();
    }

    public void SetFilledImmediate(bool isFilled)
    {
        ResetVisual();

        if (_fillImage != null)
            _fillImage.enabled = isFilled;

        if (_echoImage != null)
            _echoImage.enabled = false;
    }

    public void PlayConsumeEcho(float duration)
    {
        PlayDamageEcho(duration);
    }

    public void PlayDamageEcho(float duration)
    {
        ResetVisual();

        if (_fillImage != null)
            _fillImage.enabled = false;

        if (_echoImage == null)
            return;

        _echoImage.enabled = true;
        SetAlpha(_echoImage, 1f);

        if (duration <= 0f)
        {
            _echoImage.enabled = false;
            return;
        }

        _effectSequence = DOTween.Sequence();

        float elapsed = 0f;
        bool dim = true;

        while (elapsed < duration)
        {
            float step = Mathf.Min(_blinkInterval, duration - elapsed);
            float targetAlpha = dim ? _minEchoAlpha : 1f;

            _effectSequence.Append(_echoImage.DOFade(targetAlpha, step).SetEase(Ease.Linear));

            dim = !dim;
            elapsed += step;
        }

        _effectSequence.OnComplete(() =>
        {
            _echoImage.enabled = false;
            SetAlpha(_echoImage, 1f);
            _effectSequence = null;
        });
    }

    public void PlayRecover()
    {
        PlayHeal();
    }

    public void PlayHeal()
    {
        ResetVisual();

        if (_fillImage != null)
            _fillImage.enabled = true;

        if (_echoImage != null)
            _echoImage.enabled = false;

        bool canPlayScale = _targetRect != null && _healDuration > 0f;
        bool canPlayFlash = _useFillHitFlash && _fillImage != null && _fillHitDuration > 0f;

        if (!canPlayScale && !canPlayFlash)
            return;

        _effectSequence = DOTween.Sequence();

        if (canPlayScale)
        {
            float halfDuration = _healDuration * 0.5f;
            _effectSequence.Append(_targetRect.DOScale(_healScale, halfDuration).SetEase(Ease.OutQuad));
            _effectSequence.Append(_targetRect.DOScale(1f, halfDuration).SetEase(Ease.InQuad));
        }

        if (canPlayFlash)
            _effectSequence.Join(CreateFillHitFlashTween());

        _effectSequence.OnComplete(() => _effectSequence = null);
    }

    public void PlayReadyLoop()
    {
        if (_fillImage == null || !_fillImage.enabled)
            return;

        if (_readyPulseDuration <= 0f)
            return;

        if (_readySequence != null)
            return;

        KillReadyTween();
        SetAlpha(_fillImage, 1f);

        float halfDuration = _readyPulseDuration * 0.5f;

        _readySequence = DOTween.Sequence();
        _readySequence.Append(_fillImage.DOFade(_readyMinAlpha, halfDuration).SetEase(Ease.InOutSine));
        _readySequence.Append(_fillImage.DOFade(1f, halfDuration).SetEase(Ease.InOutSine));
        _readySequence.SetLoops(-1);
    }

    public void StopReadyLoop()
    {
        if (_readySequence == null)
            return;

        KillReadyTween();
        SetAlpha(_fillImage, 1f);
    }

    private void CacheAnimatedRoot()
    {
        _targetRect = _animatedRoot != null ? _animatedRoot : GetComponent<RectTransform>();
    }

    private void CacheBaseFillColor()
    {
        if (_fillImage == null)
            return;

        _baseFillColor = _fillImage.color;
    }

    private void ResetVisual()
    {
        KillEffectTween();

        if (_targetRect != null)
            _targetRect.localScale = Vector3.one;

        ResetFillColor();
        SetAlpha(_fillImage, 1f);
        SetAlpha(_echoImage, 1f);
    }

    private void KillAllTweens()
    {
        KillEffectTween();
        KillReadyTween();
    }

    private void KillEffectTween()
    {
        if (_effectSequence != null)
        {
            _effectSequence.Kill();
            _effectSequence = null;
        }

        if (_targetRect != null)
            _targetRect.DOKill();

        if (_echoImage != null)
            _echoImage.DOKill();
    }

    private void KillReadyTween()
    {
        if (_readySequence != null)
        {
            _readySequence.Kill();
            _readySequence = null;
        }

        if (_fillImage != null)
            _fillImage.DOKill();
    }

    private void SetAlpha(Image image, float alpha)
    {
        if (image == null)
            return;

        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }

    private Tween CreateFillHitFlashTween()
    {
        if (_fillImage == null || _fillHitDuration <= 0f)
            return DOVirtual.DelayedCall(0f, () => { });

        float halfDuration = _fillHitDuration * 0.5f;

        Sequence flashSequence = DOTween.Sequence();
        flashSequence.Append(DOTween.To(() => 0f, value => SetFillColor(Color.Lerp(_baseFillColor, _fillHitColor, value)), 1f, halfDuration).SetEase(Ease.OutQuad));
        flashSequence.Append(DOTween.To(() => 1f, value => SetFillColor(Color.Lerp(_baseFillColor, _fillHitColor, value)), 0f, halfDuration).SetEase(Ease.InQuad));
        flashSequence.OnComplete(ResetFillColor);

        return flashSequence;
    }

    private void ResetFillColor()
    {
        if (_fillImage == null)
            return;

        Color color = _baseFillColor;
        color.a = _fillImage.color.a;
        _fillImage.color = color;
    }

    private void SetFillColor(Color color)
    {
        if (_fillImage == null)
            return;

        color.a = _fillImage.color.a;
        _fillImage.color = color;
    }
}
