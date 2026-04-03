using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAimer : MonoBehaviour
{
    [SerializeField] private Camera _cam;
    [SerializeField] private Transform _gunPivot; // 주무기(권총) 피봇

    [Header("Aim Assist")]
    [SerializeField] private float _aimAssistRadiusMouse = 1.5f;
    [SerializeField] private float _aimAssistRadiusGamepad = 4f;
    [SerializeField] private float _aimAssistAngleGamepad = 30f;
    [SerializeField] private float _aimAssistStrengthMouse = 0.15f; // 당기는 강도 (낮을수록 자연스러움)
    [SerializeField] private float _aimAssistStrengthGamepad = 0.3f;
    [SerializeField] private LayerMask _enemyLayer;

    public Vector2 AimDirection { get; private set; } = Vector2.right;

    public bool IsUsingGamepad { get; private set; }

    private void Awake()
    {
        if (_cam == null)
            _cam = Camera.main;
    }

    public void HandleLook(InputAction.CallbackContext ctx)
    {
        if (ctx.canceled) return;
        Vector2 input = ctx.ReadValue<Vector2>();
        if (input.sqrMagnitude < 0.01f) return;

        IsUsingGamepad = true;
        AimDirection = input.normalized;

        Vector2 detectCenter = (Vector2)transform.position;
        AimDirection = GetAimAssistDirection(AimDirection, detectCenter, _aimAssistRadiusGamepad, _aimAssistAngleGamepad);
        ApplyRotation();

    }

    public void HandleLookMouse(InputAction.CallbackContext ctx)
    {
        if (ctx.canceled) return;
        IsUsingGamepad = false;

        Vector2 input = ctx.ReadValue<Vector2>();
        Vector2 mouseWorld = _cam.ScreenToWorldPoint(input);
        Vector2 dir = mouseWorld - (Vector2)transform.position;
        if (dir.sqrMagnitude > 0.001f)
            AimDirection = dir.normalized;

        Vector2 detectCenter = mouseWorld;
        AimDirection = GetAimAssistDirection(AimDirection, detectCenter, _aimAssistRadiusMouse, 360f);
        ApplyRotation();
    }

    private void ApplyRotation()
    {
        float angle = Mathf.Atan2(AimDirection.y, AimDirection.x) * Mathf.Rad2Deg;
        _gunPivot.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    Vector2 GetAimAssistDirection(Vector2 aimDir, Vector2 detectCenter, float radius, float maxAngle)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(detectCenter, radius, _enemyLayer);
        if (hits.Length == 0) return aimDir;

        Collider2D closest = null;
        float closestAngle = float.MaxValue;
        foreach (Collider2D hit in hits)
        {
            Vector2 toEnemy = ((Vector2)hit.transform.position - (Vector2)transform.position).normalized;
            float angle = Vector2.Angle(aimDir, toEnemy);

            if (angle > maxAngle) continue; // 각도 범위 밖이면 무시

            if (angle < closestAngle)
            {
                closestAngle = angle;
                closest = hit;
            }
        }

        if (closest == null) return aimDir;

        Vector2 toClosest = ((Vector2)closest.transform.position - (Vector2)transform.position).normalized;
        float strength = IsUsingGamepad ? _aimAssistStrengthGamepad : _aimAssistStrengthMouse;
        return Vector2.Lerp(aimDir, toClosest, strength).normalized;
    }
}