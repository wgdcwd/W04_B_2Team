using System.Collections;
using UnityEngine;

public class Boss2Skill4 : MonoBehaviour, ISkill
{
    [Header("Warning")]
    public SpriteRenderer warningSpace;
    public float warningRiseHeight = 3f;
    public float warningRiseDuration = 2f;
    public float warningBlinkDuration = 2f;
    public float warningBlinkInterval = 0.2f;

    [Header("Lava")]
    public SpriteRenderer lavaSpace;
    public float lavaRiseHeight = 5f;
    public float lavaRiseDuration = 1f;
    public float lavaStayDuration = 3f;
    public float lavaDuration = 1f;

    [Header("데미지")]
    public int lavaDamage = 1;

    [Header("복귀 위치")]
    public Transform returnPoint;       // 복귀할 빈 오브젝트
    public float returnDuration = 1f;   // 복귀하는 데 걸리는 시간

    Vector3 _warningOrigin;
    Vector3 _lavaOrigin;

    void Awake()
    {
        _warningOrigin = warningSpace.transform.position;
        _lavaOrigin = lavaSpace.transform.position;
        warningSpace.gameObject.SetActive(false);
        lavaSpace.gameObject.SetActive(false);
    }

    public IEnumerator SkillRoutine()
    {
        // 0. 지정 위치로 복귀
        yield return StartCoroutine(ReturnRoutine());

        // 1. Warning 올라오기
        yield return StartCoroutine(WarningRoutine());

        // 2. Lava 올라오기
        yield return StartCoroutine(LavaRoutine());
    }

    IEnumerator ReturnRoutine()
    {
        Vector3 startPos = transform.position;
        Vector3 targetPos = returnPoint.position;
        float elapsed = 0f;

        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, targetPos, elapsed / returnDuration);
            yield return null;
        }

        transform.position = targetPos;
    }

    IEnumerator WarningRoutine()
    {
        warningSpace.transform.position = _warningOrigin;
        warningSpace.gameObject.SetActive(true);
        Vector3 riseTarget = _warningOrigin + Vector3.up * warningRiseHeight;
        float elapsed = 0f;

        while (elapsed < warningRiseDuration)
        {
            elapsed += Time.deltaTime;
            warningSpace.transform.position = Vector3.Lerp(_warningOrigin, riseTarget, elapsed / warningRiseDuration);
            yield return null;
        }

        warningSpace.transform.position = riseTarget;
        yield return StartCoroutine(BlinkRoutine(warningSpace, warningBlinkDuration, warningBlinkInterval));
        warningSpace.transform.position = _warningOrigin;
        warningSpace.gameObject.SetActive(false);
    }

    IEnumerator BlinkRoutine(SpriteRenderer sr, float duration, float interval)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            sr.enabled = !sr.enabled;
            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }
        sr.enabled = true;
    }

    IEnumerator LavaRoutine()
    {
        lavaSpace.transform.position = _lavaOrigin;
        lavaSpace.gameObject.SetActive(true);

        Vector3 riseTarget = _lavaOrigin + Vector3.up * lavaRiseHeight;
        float elapsed = 0f;

        while (elapsed < lavaRiseDuration)
        {
            elapsed += Time.deltaTime;
            lavaSpace.transform.position = Vector3.Lerp(_lavaOrigin, riseTarget, elapsed / lavaRiseDuration);
            yield return null;
        }

        lavaSpace.transform.position = riseTarget;
        yield return new WaitForSeconds(lavaStayDuration);

        elapsed = 0f;
        while (elapsed < lavaDuration)
        {
            elapsed += Time.deltaTime;
            lavaSpace.transform.position = Vector3.Lerp(riseTarget, _lavaOrigin, elapsed / lavaDuration);
            yield return null;
        }

        lavaSpace.transform.position = _lavaOrigin;
        lavaSpace.gameObject.SetActive(false);
    }
}