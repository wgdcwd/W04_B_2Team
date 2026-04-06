using System.Collections;
using UnityEngine;

public class ChargeBeam : MonoBehaviour
{
    [Header("차징 설정")]
    public int lineCount = 8;
    public float lineLength = 3f;
    public float chargeRadius = 1.5f;
    public float chargeDuration = 1.5f;
    public float minCircleScale = 0.1f;

    [Header("레이저 설정")]
    public float warningDuration = 0.5f;
    public float expandTime = 0.15f;
    public float shrinkTime = 0.15f;

    [Header("레퍼런스")]
    public GameObject chargingCircle;
    public GameObject warningLaser;
    public GameObject fireLaser;

    public bool IsFiring { get; private set; } = false;

    private LineRenderer[] _lines;
    private Vector3 _circleOriginalScale;
    private Vector3 _fireLaserOriginalScale;
    private Vector3 _warningLaserOriginalScale;
    private Vector3 _warningLaserOriginalLocalPosition;
    private Vector3 _fireLaserOriginalLocalPosition;
    private Coroutine _beamCoroutine;

    void Awake()
    {
        _circleOriginalScale = chargingCircle.transform.localScale;
        _warningLaserOriginalScale = warningLaser.transform.localScale;
        _fireLaserOriginalScale = fireLaser.transform.localScale;
        _warningLaserOriginalLocalPosition = warningLaser.transform.localPosition;
        _fireLaserOriginalLocalPosition = fireLaser.transform.localPosition;

        chargingCircle.SetActive(false);
        warningLaser.SetActive(false);
        fireLaser.SetActive(false);

        RestoreLaserTransforms();

        SetupLines();
    }

    void SetupLines()
    {
        _lines = new LineRenderer[lineCount];

        for (int i = 0; i < lineCount; i++)
        {
            GameObject obj = new GameObject($"ChargeLine_{i}");
            obj.transform.SetParent(transform);
            obj.SetActive(false);

            LineRenderer lr = obj.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = new Color(1f, 0.8f, 0f, 1f);
            lr.endColor = new Color(1f, 0.3f, 0f, 0.3f);
            lr.numCapVertices = 4;

            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(new Keyframe(0f, 1f, 0f, 0f));
            curve.AddKey(new Keyframe(1f, 1f, 0f, 0f));
            lr.widthCurve = curve;
            lr.widthMultiplier = 0.05f;

            _lines[i] = lr;
        }
    }

    // =====================
    // 외부에서 켜기/끄기
    // =====================

    /// <summary>차징부터 발사까지 진행 후 발사 상태 유지</summary>
    public void StartBeam()
    {
        if (_beamCoroutine != null) StopCoroutine(_beamCoroutine);
        _beamCoroutine = StartCoroutine(StartBeamRoutine());
    }

    /// <summary>발사 상태에서 레이저 끄기</summary>
    public void StopBeam()
    {
        if (_beamCoroutine != null) StopCoroutine(_beamCoroutine);
        _beamCoroutine = StartCoroutine(StopBeamRoutine());
    }

    /// <summary>차징 ~ 발사 완료까지 기다리는 코루틴 (외부에서 yield 가능)</summary>
    public IEnumerator WaitUntilFiring()
    {
        yield return StartCoroutine(StartBeamRoutine());
    }

    // =====================
    // 내부 루틴
    // =====================
    IEnumerator StartBeamRoutine()
    {
        IsFiring = false;
        yield return StartCoroutine(ChargePhase());
        yield return StartCoroutine(WarningPhase());
        yield return StartCoroutine(ExpandPhase());
        IsFiring = true;
        // 여기서 멈춤 — StopBeam() 호출까지 유지
    }

    IEnumerator StopBeamRoutine()
    {
        IsFiring = false;
        yield return StartCoroutine(ShrinkPhase());
        CleanUp();
    }

    // =====================
    // 1단계 : 차징
    // =====================
    IEnumerator ChargePhase()
    {
        chargingCircle.SetActive(true);
        chargingCircle.transform.localScale = _circleOriginalScale;

        for (int i = 0; i < lineCount; i++)
        {
            _lines[i].gameObject.SetActive(true);
            UpdateLinePosition(i, 1f);
        }

        float elapsed = 0f;
        while (elapsed < chargeDuration)
        {
            float t = elapsed / chargeDuration;

            float scale = Mathf.Lerp(1f, minCircleScale, t);
            chargingCircle.transform.localScale = _circleOriginalScale * scale;

            for (int i = 0; i < lineCount; i++)
                UpdateLinePosition(i, 1f - t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        chargingCircle.SetActive(false);
        foreach (var line in _lines)
            line.gameObject.SetActive(false);
    }

    void UpdateLinePosition(int index, float lengthRatio)
    {
        float angle = (360f / lineCount) * index * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        Vector2 center = chargingCircle.transform.position;

        Vector2 start = center + dir * (chargeRadius + lineLength * lengthRatio);
        Vector2 end = center;

        _lines[index].SetPosition(0, start);
        _lines[index].SetPosition(1, end);
    }

    // =====================
    // 2단계 : 예고
    // =====================
    IEnumerator WarningPhase()
    {
        warningLaser.transform.localScale = _warningLaserOriginalScale;
        warningLaser.transform.localPosition = GetAnchoredLocalPosition(_warningLaserOriginalScale, _warningLaserOriginalLocalPosition);
        warningLaser.SetActive(true);
        yield return new WaitForSeconds(warningDuration);
        warningLaser.SetActive(false);
    }

    // =====================
    // 3단계 : 레이저 펼치기
    // =====================
    IEnumerator ExpandPhase()
    {
        fireLaser.transform.localScale = new Vector3(0f, _fireLaserOriginalScale.y, _fireLaserOriginalScale.z);
        fireLaser.transform.localPosition = GetAnchoredLocalPosition(fireLaser.transform.localScale, _fireLaserOriginalLocalPosition);
        fireLaser.SetActive(true);

        float elapsed = 0f;
        while (elapsed < expandTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / expandTime);
            fireLaser.transform.localScale = new Vector3(
                Mathf.Lerp(0f, _fireLaserOriginalScale.x, t),
                _fireLaserOriginalScale.y,
                _fireLaserOriginalScale.z
            );
            fireLaser.transform.localPosition = GetAnchoredLocalPosition(fireLaser.transform.localScale, _fireLaserOriginalLocalPosition);
            yield return null;
        }
        fireLaser.transform.localScale = _fireLaserOriginalScale;
        fireLaser.transform.localPosition = GetAnchoredLocalPosition(_fireLaserOriginalScale, _fireLaserOriginalLocalPosition);
    }

    // =====================
    // 레이저 줄이기
    // =====================
    IEnumerator ShrinkPhase()
    {
        Vector3 from = fireLaser.transform.localScale;
        float elapsed = 0f;

        while (elapsed < shrinkTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / shrinkTime);
            fireLaser.transform.localScale = new Vector3(
                Mathf.Lerp(from.x, 0f, t),
                _fireLaserOriginalScale.y,
                _fireLaserOriginalScale.z
            );
            fireLaser.transform.localPosition = GetAnchoredLocalPosition(fireLaser.transform.localScale, _fireLaserOriginalLocalPosition);
            yield return null;
        }

        fireLaser.SetActive(false);
        fireLaser.transform.localScale = _fireLaserOriginalScale;
        fireLaser.transform.localPosition = GetAnchoredLocalPosition(_fireLaserOriginalScale, _fireLaserOriginalLocalPosition);
    }

    void CleanUp()
    {
        chargingCircle.SetActive(false);
        warningLaser.SetActive(false);
        fireLaser.SetActive(false);
        RestoreLaserTransforms();
        chargingCircle.transform.localScale = _circleOriginalScale;
    }

    void RestoreLaserTransforms()
    {
        warningLaser.transform.localScale = _warningLaserOriginalScale;
        fireLaser.transform.localScale = _fireLaserOriginalScale;
        warningLaser.transform.localPosition = GetAnchoredLocalPosition(_warningLaserOriginalScale, _warningLaserOriginalLocalPosition);
        fireLaser.transform.localPosition = GetAnchoredLocalPosition(_fireLaserOriginalScale, _fireLaserOriginalLocalPosition);
    }

    Vector3 GetAnchoredLocalPosition(Vector3 currentScale, Vector3 originalLocalPosition)
    {
        Vector3 direction = originalLocalPosition.sqrMagnitude > Mathf.Epsilon
            ? originalLocalPosition.normalized
            : Vector3.down;

        float halfLength = Mathf.Abs(currentScale.y) * 0.5f;
        return direction * halfLength;
    }
}
