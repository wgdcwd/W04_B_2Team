using UnityEngine;

public class BossLaserImpactMarker : MonoBehaviour
{
    [SerializeField] private GameObject _impactPrefab;
    [SerializeField] private float _rayDistance = 30f;
    [SerializeField] private LayerMask _groundLayerMask = ~0;
    [SerializeField] private Vector3 _positionOffset;
    [SerializeField] private bool _alignToNormal = true;
    [SerializeField] private float _rayStartOffset = 0.05f;

    private GameObject _impactInstance;
    private Collider2D _selfCollider;

    void Awake()
    {
        _selfCollider = GetComponent<Collider2D>();
    }

    void OnEnable()
    {
        UpdateImpactMarker();
    }

    void LateUpdate()
    {
        UpdateImpactMarker();
        DrawDebugRay();
    }

    void OnDisable()
    {
        HideImpactMarker();
    }

    void OnDestroy()
    {
        if (_impactInstance != null)
            Destroy(_impactInstance);
    }

    void UpdateImpactMarker()
    {
        RaycastHit2D hit = GetImpactHit();

        if (hit.collider == null)
        {
            HideImpactMarker();
            return;
        }

        if (_impactPrefab == null)
            return;

        if (_impactInstance == null)
            _impactInstance = Instantiate(_impactPrefab);

        _impactInstance.SetActive(true);
        _impactInstance.transform.position = hit.point + (Vector2)_positionOffset;

        if (_alignToNormal)
        {
            Vector2 up = hit.normal.sqrMagnitude > 0f ? hit.normal : Vector2.up;
            _impactInstance.transform.up = up;
        }
    }

    Vector2 GetRayOrigin()
    {
        float halfLength = Mathf.Abs(transform.lossyScale.y) * 0.5f;
        Vector2 direction = GetRayDirection();
        return (Vector2)transform.position - direction * halfLength + direction * _rayStartOffset;
    }

    RaycastHit2D GetImpactHit()
    {
        Vector2 origin = GetRayOrigin();
        Vector2 direction = GetRayDirection();
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction, _rayDistance);

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
        return transform.up.normalized;
    }

    bool IsInLayerMask(int layer, LayerMask layerMask)
    {
        return (layerMask.value & (1 << layer)) != 0;
    }

    void HideImpactMarker()
    {
        if (_impactInstance != null)
            _impactInstance.SetActive(false);
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
