using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class UIButtonHover : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    ISelectHandler, IDeselectHandler
{
    [SerializeField] private Button _button;
    [SerializeField] private RectTransform _target;
    [SerializeField] private float _hoverScale = 1.05f;
    [SerializeField] private float _duration = 0.15f;

    private Vector3 _originScale;

    private void Awake()
    {
        if (_button == null) _button = GetComponent<Button>();
        if (_target == null) _target = GetComponent<RectTransform>();

        _originScale = _target.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData) => Hover(true);
    public void OnPointerExit(PointerEventData eventData) => Hover(false);
    public void OnSelect(BaseEventData eventData) => Hover(true);
    public void OnDeselect(BaseEventData eventData) => Hover(false);

    private void Hover(bool isHover)
    {
        if (_button != null && !_button.interactable) return;

        Vector3 scale = isHover ? _originScale * _hoverScale : _originScale;

        _target.DOKill();
        _target
            .DOScale(scale, _duration)
            .SetUpdate(true);
    }

    private void OnDisable()
    {
        _target.DOKill();
        _target.localScale = _originScale;
    }
}
