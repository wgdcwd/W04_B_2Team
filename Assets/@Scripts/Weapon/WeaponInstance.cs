using System;
using UnityEngine;

public enum WeaponConsumeResult
{
    Success,
    Cooldown,
    NoAmmo
}


public class WeaponInstance
{
    public SO_WeaponBase Data { get; private set; }
    public int CurrentAmmo { get; private set; }
    private float _nextFireTime = 0f;
    public bool IsReady => Time.time >= _nextFireTime;
    public bool NeedsReload => CurrentAmmo < Data.maxAmmo;

    public event Action<int> OnAmmoChanged;

    public WeaponInstance(SO_WeaponBase data)
    {
        Data = data;
        CurrentAmmo = data.maxAmmo;
    }

    // 탄창이 없을 때에만 UX 표기 위한 enum으로 판정 변경
    public WeaponConsumeResult TryConsumeDetailed()
    {
        if (!IsReady)
            return WeaponConsumeResult.Cooldown;

        if (CurrentAmmo <= 0)
            return WeaponConsumeResult.NoAmmo;

        CurrentAmmo--;
        OnAmmoChanged?.Invoke(CurrentAmmo);
        _nextFireTime = Time.time + Data.fireRate;

        return WeaponConsumeResult.Success;
    }

    public void Reload()
    {
        if (CurrentAmmo == Data.maxAmmo) return;
        CurrentAmmo = Data.maxAmmo;
        OnAmmoChanged?.Invoke(CurrentAmmo);
    }
}
