using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public class PathSegment
{
    public Transform[] waypoints;
    public float speed = 4f;
}

public class CollapseWave : MonoBehaviour
{
    [Header("경로")]
    public PathSegment[] segments;

    [Header("붕괴")]
    public Tilemap tilemap;          // 붕괴시킬 타일맵
    public float collapseRadius = 3f;
    public float startDelay = 1f;
    public float queryInterval = 0.5f; // 타일 검사 주기 (초당 2회)

    [Header("연출")]
    public ParticleSystem[] dustParticles;
    public int burstCount = 5;

    [Header("카메라 흔들림")]
    public CinemachineCamera virtualCamera;
    public float shakeDuration = 0.1f;
    public float shakeAmplitude = 0.5f;
    public float shakeFrequency = 1f;

    [Header("플레이어 추적")]
    public Transform player;
    public float minDistance = 3f;  // 이 거리 이하면 최저 속도
    public float maxDistance = 10f; // 이 거리 이상이면 최고 속도
    public float minSpeedMult = 0.3f; // 최저 속도 배율 (seg.speed * 0.3)
    public float maxSpeedMult = 2f;   // 최고 속도 배율 (seg.speed * 2.0)

    [Header("데미지")]
    public int collapseDamage = 1;
    PlayerHealth _playerHealth;

    // 화면 흔들림
    CinemachineBasicMultiChannelPerlin _perlin;
    Coroutine _shakeCoroutine;

    int _segIndex = 0;
    int _wpIndex = 0;
    bool _active = false;
    float _queryTimer = 0f;

    // 이미 지운 셀 추적 → 중복 처리 방지
    HashSet<Vector3Int> _removed = new HashSet<Vector3Int>();

    void Start()
    {

        if (player != null)
            _playerHealth = player.GetComponent<PlayerHealth>();

        if (virtualCamera != null)
            _perlin = virtualCamera.GetComponent<CinemachineBasicMultiChannelPerlin>();

        if (segments?.Length > 0 && segments[0].waypoints?.Length > 0)
            transform.position = segments[0].waypoints[0].position;

        Invoke(nameof(Activate), startDelay);
    }

    public void Activate() => _active = true;
    public bool IsFinished() => _segIndex >= segments.Length;

    void Update()
    {
        if (!_active || IsFinished()) return;

        MoveAlongPath();

        // 매 프레임 말고 일정 간격으로만 타일 검사
        _queryTimer += Time.deltaTime;
        if (_queryTimer >= queryInterval)
        {
            CollapseNearbyTiles();
            TryDamagePlayer();
            Shake();
            _queryTimer = 0f;
        }
    }

    void MoveAlongPath()
    {
        var seg = segments[_segIndex];
        var target = seg.waypoints[_wpIndex].position;

        // 플레이어와의 거리로 속도 배율 계산
        float speed = seg.speed;
        if (player != null)
        {
            float dist = Vector3.Distance(transform.position, player.position);

            // dist가 minDistance~maxDistance 사이에서 배율을 선형 보간
            float t = Mathf.InverseLerp(minDistance, maxDistance, dist);
            float mult = Mathf.Lerp(minSpeedMult, maxSpeedMult, t);
            speed *= mult;
        }

        transform.position = Vector3.MoveTowards(
            transform.position, target, speed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, target) < 0.05f)
        {
            _wpIndex++;
            if (_wpIndex >= seg.waypoints.Length)
            {
                _segIndex++;
                _wpIndex = 0;
            }
        }
    }

    void CollapseNearbyTiles()
    {
        Vector3Int center = tilemap.WorldToCell(transform.position);
        int radiusInCells = Mathf.CeilToInt(collapseRadius);

        for (int x = -radiusInCells; x <= radiusInCells; x++)
        {
            for (int y = -radiusInCells; y <= radiusInCells; y++)
            {
                Vector3Int cell = center + new Vector3Int(x, y, 0);

                if (_removed.Contains(cell)) continue;
                if (!tilemap.HasTile(cell)) continue;

                Vector3 worldPos = tilemap.GetCellCenterWorld(cell);
                if (Vector3.Distance(transform.position, worldPos) > collapseRadius) continue;

                RemoveTile(cell, worldPos);
            }
        }

        SpawnBurstParticles();

    }
    void TryDamagePlayer()
    {
        if (_playerHealth == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= collapseRadius)
            _playerHealth.TakeDamage(collapseDamage);
    }

    void RemoveTile(Vector3Int cell, Vector3 worldPos)
    {
        _removed.Add(cell);
        tilemap.SetTile(cell, null);
    }

    void SpawnBurstParticles()
    {
        if (dustParticles == null || dustParticles.Length == 0) return;

        for (int i = 0; i < burstCount; i++)
        {
            Vector2 randOffset = Random.insideUnitCircle * collapseRadius;
            Vector3 spawnPos = transform.position + new Vector3(randOffset.x, randOffset.y, 0f);

            var emitParams = new ParticleSystem.EmitParams();
            emitParams.position = spawnPos;
            emitParams.applyShapeToPosition = true;

            // 모든 파티클 시스템에 동일한 위치로 emit
            foreach (var ps in dustParticles)
                if (ps != null) ps.Emit(emitParams, 1);
        }
    }

    void Shake()
    {
        if (_perlin == null) return;

        if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);
        _shakeCoroutine = StartCoroutine(ShakeRoutine());
    }

    IEnumerator ShakeRoutine()
    {
        // 가까울수록 강하게
        float dist = player != null
            ? Vector3.Distance(transform.position, player.position)
            : maxDistance;
        float t = Mathf.InverseLerp(minDistance, maxDistance, dist);
        float amplitude = Mathf.Lerp(shakeAmplitude, shakeAmplitude * 0.2f, t);

        _perlin.FrequencyGain = shakeFrequency;

        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            _perlin.AmplitudeGain = Mathf.Lerp(amplitude, 0f, elapsed / shakeDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        _perlin.AmplitudeGain = 0f;
        _perlin.FrequencyGain = 0f;
    }


    void OnDrawGizmos()
    {
        if (segments == null) return;

        Color[] colors = { Color.yellow, Color.cyan, Color.green, Color.magenta };

        for (int s = 0; s < segments.Length; s++)
        {
            var wps = segments[s].waypoints;
            if (wps == null) continue;

            Gizmos.color = colors[s % colors.Length];
            foreach (var wp in wps)
                if (wp) Gizmos.DrawSphere(wp.position, 0.25f);

            for (int i = 0; i < wps.Length - 1; i++)
                if (wps[i] && wps[i + 1]) Gizmos.DrawLine(wps[i].position, wps[i + 1].position);
        }

        // 붕괴 반경
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.2f);
        Gizmos.DrawSphere(transform.position, collapseRadius);
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, collapseRadius);

        if (player == null) return;

        // minDistance 원 (초록 → 이 안에 들어오면 느려짐)
        Gizmos.color = new Color(0f, 1f, 0f, 0.1f);
        Gizmos.DrawSphere(transform.position, minDistance);
        Gizmos.color = new Color(0f, 1f, 0f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, minDistance);

        // maxDistance 원 (빨강 → 이 밖으로 나가면 최고속)
        Gizmos.color = new Color(1f, 0f, 0f, 0.05f);
        Gizmos.DrawSphere(transform.position, maxDistance);
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, maxDistance);

        // 웨이브 → 플레이어 거리 선
        float dist = Vector3.Distance(transform.position, player.position);
        float t = Mathf.InverseLerp(minDistance, maxDistance, dist);

        // 거리에 따라 선 색상 변화 (초록 → 빨강)
        Gizmos.color = Color.Lerp(Color.green, Color.red, t);
        Gizmos.DrawLine(transform.position, player.position);
    }
}