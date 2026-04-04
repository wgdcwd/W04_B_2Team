using System.Collections;
using UnityEngine;

public class Boss2Skill4 : MonoBehaviour, ISkill
{
    [Header("Warning")]
    public SpriteRenderer warningSpace;
    public float warningRiseHeight = 3f;    // 얼마나 위로 올라오는지
    public float warningRiseDuration = 2f;  // 올라오는 데 걸리는 시간
    public float warningBlinkDuration = 2f; // 멈춰서 깜빡이는 시간
    public float warningBlinkInterval = 0.2f;

    [Header("Lava")]
    public SpriteRenderer lavaSpace;
    public float lavaRiseHeight = 5f;
    public float lavaRiseDuration = 1f;
    public float lavaStayDuration = 3f;     // 올라와서 머무는 시간
    public float lavaDuration = 1f;         // 내려가는 시간

    [Header("데미지")]
    public int lavaDamage = 1;

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
        // 1. Warning 올라오기
        yield return StartCoroutine(WarningRoutine());

        // 2. Lava 올라오기
        yield return StartCoroutine(LavaRoutine());
    }

    IEnumerator WarningRoutine()
    {
        // 활성화 후 올라오기
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

        // 멈춰서 깜빡이기
        yield return StartCoroutine(BlinkRoutine(warningSpace, warningBlinkDuration, warningBlinkInterval));

        // 원래 위치로 복구 후 비활성화
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

        // 올라오기
        Vector3 riseTarget = _lavaOrigin + Vector3.up * lavaRiseHeight;
        float elapsed = 0f;

        while (elapsed < lavaRiseDuration)
        {
            elapsed += Time.deltaTime;
            lavaSpace.transform.position = Vector3.Lerp(_lavaOrigin, riseTarget, elapsed / lavaRiseDuration);
            yield return null;
        }
        lavaSpace.transform.position = riseTarget;

        // 머물기
        yield return new WaitForSeconds(lavaStayDuration);

        // 내려가기
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