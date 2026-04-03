using UnityEngine;

[CreateAssetMenu(fileName = "SO_WeaponBase", menuName = "Scriptable Objects/SO_WeaponBase")]
public class SO_WeaponBase : ScriptableObject
{
    public string weaponName;

    [Header("Recoil")]
    [Tooltip("반동 세기")] public float recoilForce;
    
    [Header("Bullet")]
    public GameObject bulletPrefab;
    [Tooltip("발사 속도")] public float bulletSpeed = 10f;
    [Tooltip("한 번에 쏘는 총알 수")] public int pelletCount = 1;
    public float spreadAngle = 0f;
    public int maxAmmo = 3;

    [Header("Fire Rate")]
    public float fireRate = 0.5f;

    [Header("Haptic Feedback")]
    public float lowFrequency = 0.15f;
    public float highFrequency = 0.35f;
    public float duration = 0.08f;
}

