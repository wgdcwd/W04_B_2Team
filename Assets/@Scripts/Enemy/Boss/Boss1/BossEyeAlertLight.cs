using UnityEngine;
using UnityEngine.Rendering.Universal;

public class BossEyeAlertLight : MonoBehaviour
{
    [SerializeField] private Light2D _targetLight;
    [SerializeField] private bool _disableWhenOff = true;

    void Awake()
    {
        if (_targetLight == null)
            _targetLight = GetComponentInChildren<Light2D>(true);

        SetEnabled(false);
    }

    public void SetEnabled(bool isEnabled)
    {
        if (_targetLight == null)
            return;

        if (_disableWhenOff)
            _targetLight.enabled = isEnabled;
    }

    public void SetColor(Color color)
    {
        if (_targetLight == null)
            return;

        _targetLight.color = color;
    }

    public void SetState(Color color, bool isEnabled)
    {
        SetColor(color);
        SetEnabled(isEnabled);
    }
}
