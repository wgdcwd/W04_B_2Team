using System.Collections;
using UnityEngine;

public class Boss2Skill3 : MonoBehaviour, ISkill
{
    [Header("이동 설정")]
    public Transform pointA;
    public Transform pointB;
    public float dashSpeed = 20f;  // 빠르게 지나가는 속도
    public int repeatCount = 2;     // 왕복 횟수

    [Header("대미지 설정")]
    public int contactDamage = 20;    // 플레이어 충돌 시 대미지

    [Header("연출")]
    public float preDashDelay = 0.15f;
    public float preDashFlashDuration = 0.08f;
    public Color preDashFlashColor = new Color(1f, 0.45f, 0.45f, 1f);

    private bool _isDashing = false;
    private SpriteRenderer[] _renderers;
    private Color[] _originalColors;

    void Awake()
    {
        _renderers = GetComponentsInChildren<SpriteRenderer>(true);
        _originalColors = new Color[_renderers.Length];

        for (int i = 0; i < _renderers.Length; i++)
            _originalColors[i] = _renderers[i].color;
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (!_isDashing) return;
        if (col.CompareTag("Player"))
            col.GetComponent<IDamageable>()?.TakeDamage(contactDamage);
    }

    public IEnumerator SkillRoutine()
    {
        _isDashing = true;

        // A → B → A → B ... repeatCount만큼 왕복
        Transform current = pointA;
        int totalMoves = repeatCount * 2; // 왕복 1회 = 2번 이동

        for (int i = 0; i < totalMoves; i++)
        {
            yield return StartCoroutine(PreDashRoutine());
            yield return StartCoroutine(DashToPosition(current.position));
            current = current == pointA ? pointB : pointA;
        }

        _isDashing = false;
        RestoreRendererColors();
    }

    IEnumerator PreDashRoutine()
    {
        SetRendererColors(preDashFlashColor);
        yield return new WaitForSeconds(preDashFlashDuration);
        RestoreRendererColors();

        if (preDashDelay > preDashFlashDuration)
            yield return new WaitForSeconds(preDashDelay - preDashFlashDuration);
    }

    IEnumerator DashToPosition(Vector3 target)
    {
        while (Vector3.Distance(transform.position, target) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, dashSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = target;
    }

    void SetRendererColors(Color color)
    {
        for (int i = 0; i < _renderers.Length; i++)
            _renderers[i].color = color;
    }

    void RestoreRendererColors()
    {
        for (int i = 0; i < _renderers.Length; i++)
            _renderers[i].color = _originalColors[i];
    }
}
