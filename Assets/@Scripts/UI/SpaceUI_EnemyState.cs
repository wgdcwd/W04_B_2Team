using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class SpaceUI_EnemyState : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private GameObject _alertSprite;
    [SerializeField] private GameObject _deadSprite;

    [Header("Animation")]
    [SerializeField] private float _startScale = 0.6f;
    [SerializeField] private float _overshootScale = 1.15f;
    [SerializeField] private float _growTime = 0.08f;
    [SerializeField] private float _settleTime = 0.08f;

    private Coroutine _playRoutine;
    private Vector3 _alertBaseScale = Vector3.one;
    private Vector3 _deadBaseScale = Vector3.one;

    private void Awake()
    {
        _alertBaseScale = GetBaseScale(_alertSprite);
        _deadBaseScale = GetBaseScale(_deadSprite);

        HideImmediate();
    }

    public void ShowAlert(float duration)
    {
        StopCurrentRoutine();
        _playRoutine = StartCoroutine(PlayAlertRoutine(duration));
    }

    public void ShowDead()
    {
        StopCurrentRoutine();
        _playRoutine = StartCoroutine(PlayDeadRoutine());
    }

    public void HideImmediate()
    {
        StopCurrentRoutine();
        HideObject(_alertSprite, _alertBaseScale);
        HideObject(_deadSprite, _deadBaseScale);
    }

    private IEnumerator PlayAlertRoutine(float duration)
    {
        HideObject(_deadSprite, _deadBaseScale);

        yield return PlayPopAnimation(_alertSprite, _alertBaseScale);

        if (duration > 0f)
            yield return new WaitForSeconds(duration);

        HideObject(_alertSprite, _alertBaseScale);
        _playRoutine = null;
    }

    private IEnumerator PlayDeadRoutine()
    {
        HideObject(_alertSprite, _alertBaseScale);
        yield return PlayPopAnimation(_deadSprite, _deadBaseScale);
        _playRoutine = null;
    }

    private IEnumerator PlayPopAnimation(GameObject target, Vector3 baseScale)
    {
        if (target == null)
            yield break;

        target.SetActive(true);

        Transform targetTransform = target.transform;
        Vector3 startScale = baseScale * _startScale;
        Vector3 overshoot = baseScale * _overshootScale;

        targetTransform.localScale = startScale;

        yield return ScaleOverTime(targetTransform, startScale, overshoot, _growTime);
        yield return ScaleOverTime(targetTransform, overshoot, baseScale, _settleTime);
    }

    private IEnumerator ScaleOverTime(Transform target, Vector3 from, Vector3 to, float duration)
    {
        if (target == null)
            yield break;

        if (duration <= 0f)
        {
            target.localScale = to;
            yield break;
        }

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            target.localScale = Vector3.Lerp(from, to, t);
            yield return null;
        }

        target.localScale = to;
    }

    private void HideObject(GameObject target, Vector3 baseScale)
    {
        if (target == null) return;

        target.transform.localScale = baseScale;
        target.SetActive(false);
    }

    private Vector3 GetBaseScale(GameObject target)
    {
        if (target == null)
            return Vector3.one;

        return target.transform.localScale;
    }

    private void StopCurrentRoutine()
    {
        if (_playRoutine == null) return;

        StopCoroutine(_playRoutine);
        _playRoutine = null;
    }
}
