using System;
using UnityEngine;

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

    public bool TryConsume()
    {
        if (!IsReady) return false;
        if (CurrentAmmo <= 0) return false;

        CurrentAmmo--;
        OnAmmoChanged?.Invoke(CurrentAmmo);
        _nextFireTime = Time.time + Data.fireRate;
        return true;
    }

    public void Reload()
    {
        if (CurrentAmmo == Data.maxAmmo) return;
        CurrentAmmo = Data.maxAmmo;
        OnAmmoChanged?.Invoke(CurrentAmmo);
    }
}
