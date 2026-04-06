using UnityEngine;


public class Boss2BeanControl : MonoBehaviour
{
    [Header("Laser Collision")]
    [SerializeField] private GameObject _impactPrefab;
    [SerializeField] private float _rayDistance = 30f;
    [SerializeField] private LayerMask _groundLayerMask = ~0;

    [Header("Laser Visual")]
    [SerializeField] private Transform _positionOffset;
    [SerializeField] private bool _alignToNormal = true;
    [SerializeField] private bool _alignToLaserPerpendicular = true;
    [SerializeField] private float _impactRotationOffset = 90f;
    [SerializeField] private float _rayStartOffset = 0.05f;
    [SerializeField] private float _laserThickness = 0.5f;
    [SerializeField] private float _laserBaseLength = 20f;
    [SerializeField] private float _laserVisualOffset = 0.5f;

    private GameObject _impactInstance;
    private Collider2D _selfCollider;
    private Vector3 _baseLocalPosition;

    void Awake()
    {
        _selfCollider = GetComponent<Collider2D>();
        _baseLocalPosition = transform.localPosition;

        float initialThickness = Mathf.Abs(transform.localScale.x);
        float initialLength = Mathf.Abs(transform.localScale.y);

        if (initialThickness > Mathf.Epsilon)
            _laserThickness = initialThickness;

        if (initialLength > Mathf.Epsilon)
            _laserBaseLength = initialLength;

        Vector3 localDirection = _baseLocalPosition.sqrMagnitude > Mathf.Epsilon
            ? _baseLocalPosition.normalized
            : Vector3.down;
        _baseLocalPosition += localDirection * _laserVisualOffset;
    }

    void LateUpdate()
    {
        UpdateImpactMarker();
        DrawDebugRay();
    }

    void OnDisable()
    {
        ResetLaserVisual();
        HideImpactMarker();
    }

    void OnDestroy()
    {
        ResetLaserVisual();
        DestroyImpactMarker();
    }

    void UpdateImpactMarker()
    {
        RaycastHit2D hit = GetImpactHit();

        if (hit.collider == null)
        {
            ResetLaserVisual();
            HideImpactMarker();
            return;
        }

        UpdateLaserVisual(hit.distance + _rayStartOffset);

        if (_impactPrefab == null)
            return;

        if (_impactInstance == null)
            _impactInstance = Instantiate(_impactPrefab);

        _impactInstance.SetActive(true);
        _impactInstance.transform.position = hit.point + (Vector2)_positionOffset.position;
        ApplyImpactRotation(hit);
    }

    Vector2 GetRayOrigin()
    {
        float halfLength = GetCurrentLaserWorldLength() * 0.5f;
        Vector2 direction = GetRayDirection();
        return (Vector2)transform.position - direction * halfLength + direction * _rayStartOffset;
    }

    RaycastHit2D GetImpactHit()
    {
        Vector2 origin = GetRayOrigin();
        Vector2 direction = GetRayDirection();
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction, _rayDistance);
        Debug.DrawRay(origin, direction);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hitCollider = hits[i].collider;
            if (hitCollider == null)
                continue;

            if (hitCollider == _selfCollider)
                continue;

            if (!IsInLayerMask(hitCollider.gameObject.layer, _groundLayerMask))
                continue;

            return hits[i];
        }

        return default;
    }

    Vector2 GetRayDirection()
    {
        Vector3 localDirection = _baseLocalPosition.sqrMagnitude > Mathf.Epsilon
            ? _baseLocalPosition.normalized
            : Vector3.down;

        return transform.parent != null
            ? (Vector2)transform.parent.TransformDirection(localDirection).normalized
            : (Vector2)localDirection.normalized;
    }

    void UpdateLaserVisual(float hitDistance)
    {
        float localToWorldYScale = GetLocalToWorldYScale();
        if (_laserBaseLength <= Mathf.Epsilon || localToWorldYScale <= Mathf.Epsilon)
            return;

        float targetLocalLength = hitDistance / localToWorldYScale;
        float ratio = Mathf.Clamp01(targetLocalLength / _laserBaseLength);
        Vector3 localScale = transform.localScale;
        localScale.x = _laserThickness;
        localScale.y = targetLocalLength;
        transform.localScale = localScale;
        transform.localPosition = _baseLocalPosition * ratio;
    }

    void ResetLaserVisual()
    {
        Vector3 localScale = transform.localScale;
        localScale.x = _laserThickness;
        localScale.y = _laserBaseLength;
        transform.localScale = localScale;
        transform.localPosition = _baseLocalPosition;
    }

    float GetCurrentLaserWorldLength()
    {
        return Mathf.Abs(transform.localScale.y) * GetLocalToWorldYScale();
    }

    float GetLocalToWorldYScale()
    {
        Transform parent = transform.parent;
        if (parent == null)
            return 1f;

        return Mathf.Abs(parent.lossyScale.y);
    }

    bool IsInLayerMask(int layer, LayerMask layerMask)
    {
        return (layerMask.value & (1 << layer)) != 0;
    }

    void ApplyImpactRotation(RaycastHit2D hit)
    {
        if (_alignToLaserPerpendicular)
        {
            Vector2 perpendicular = Vector2.Perpendicular(GetRayDirection());
            if (perpendicular.sqrMagnitude > 0f)
                _impactInstance.transform.up = perpendicular.normalized;

            ApplyImpactRotationOffset();
            return;
        }

        if (_alignToNormal)
        {
            Vector2 up = hit.normal.sqrMagnitude > 0f ? hit.normal : Vector2.up;
            _impactInstance.transform.up = up;
        }

        ApplyImpactRotationOffset();
    }

    void ApplyImpactRotationOffset()
    {
        _impactInstance.transform.Rotate(0f, 0f, _impactRotationOffset, Space.Self);
    }

    void HideImpactMarker()
    {
        DestroyImpactMarker();
    }

    void DestroyImpactMarker()
    {
        if (_impactInstance == null)
            return;

        Destroy(_impactInstance);
        _impactInstance = null;
    }

    void OnDrawGizmos()
    {
        Vector2 origin = GetRayOrigin();
        Vector2 direction = GetRayDirection();
        RaycastHit2D hit = GetImpactHit();
        Vector2 endPoint = origin + direction * _rayDistance;

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(origin, 0.12f);
        Gizmos.color = hit.collider != null ? Color.cyan : Color.gray;
        Gizmos.DrawLine(origin, endPoint);

        if (hit.collider == null)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(hit.point, 0.15f);
        Gizmos.DrawLine(hit.point, hit.point + hit.normal * 0.5f);
    }

    void DrawDebugRay()
    {
        Vector2 origin = GetRayOrigin();
        Vector2 direction = GetRayDirection();
        RaycastHit2D hit = GetImpactHit();
        Vector2 endPoint = origin + direction * _rayDistance;

        Debug.DrawLine(origin, endPoint, hit.collider != null ? Color.cyan : Color.gray);

        if (hit.collider == null)
            return;

        Debug.DrawLine(hit.point, hit.point + hit.normal * 0.5f, Color.red);
    }
}

