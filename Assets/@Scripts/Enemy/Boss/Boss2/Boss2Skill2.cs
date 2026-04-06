using System.Collections;
using UnityEngine;

public class Boss2Skill2 : MonoBehaviour, ISkill
{
    [Header("이동 설정")]
    public Transform pointA;
    public Transform pointB;
    public float moveSpeed = 4f;

    [Header("발사 설정")]
    public Transform[] turrets;
    public GameObject bulletPrefab;
    public int bulletDamage = 10;
    public float bulletSpeed = 8f;
    public float fireInterval = 0.5f;
    public float skillDuration = 5f;

    [Header("경고")]
    public GameObject warningSpace;

    private PoolManager _pool;
    private bool _isMoving = false; // 이동 제어 플래그

    void Awake()
    {
        ManagerRegistry.TryGet(out _pool);
    }

    public IEnumerator SkillRoutine()
    {
        if (warningSpace != null)
            warningSpace.SetActive(true);

        yield return new WaitForSeconds(1f); // 경고 1초 대기

        _isMoving = true;
        Coroutine moveCoroutine = StartCoroutine(MoveRoutine());
        yield return StartCoroutine(FireRoutine());
        _isMoving = false;
        yield return null;

        if (warningSpace != null)
            warningSpace.SetActive(false);
    }

    // =====================
    // 이동 루틴 (A↔B 왕복)
    // =====================
    IEnumerator MoveRoutine()
    {
        Transform current = pointA;
        while (_isMoving) // while(true) 대신 플래그로 제어
        {
            yield return StartCoroutine(MoveToPosition(current.position));
            if (!_isMoving) break;
            current = current == pointA ? pointB : pointA;
        }
    }

    IEnumerator MoveToPosition(Vector3 target)
    {
        while (_isMoving && Vector3.Distance(transform.position, target) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
            yield return null;
        }
        if (_isMoving)
            transform.position = target;
    }

    // =====================
    // 발사 루틴
    // =====================
    IEnumerator FireRoutine()
    {
        float elapsed = 0f;
        while (elapsed < skillDuration)
        {
            FireAllTurrets();
            yield return new WaitForSeconds(fireInterval);
            elapsed += fireInterval;
        }
    }

    void FireAllTurrets()
    {
        foreach (var turret in turrets)
        {
            if (turret == null) continue;
            FireFromTurret(turret);
        }
    }

    void FireFromTurret(Transform turret)
    {
        Quaternion rotation = turret.rotation * Quaternion.Euler(0f, 0f, -90f);
        GameObject bullet = _pool != null
            ? _pool.Get(bulletPrefab, turret.position, rotation)
            : Instantiate(bulletPrefab, turret.position, rotation);
        bullet.GetComponent<EnemyProjectile>()?.Initialize(bulletSpeed, bulletDamage);
    }
}