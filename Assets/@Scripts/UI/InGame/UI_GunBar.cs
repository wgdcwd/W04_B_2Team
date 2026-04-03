using System.Collections;
using UnityEngine;

public class UI_GunBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerAttack _playerAttack;
    [SerializeField] private UI_BarSlot[] _slots;

    [Header("Animation")]
    [SerializeField] private float _consumeEchoDuration = 0.15f;

    private WeaponInstance _weaponInstance;
    private SlotBar _slotBar;

    private IEnumerator Start()
    {
        if (!ValidateReferences())
            yield break;

        yield return null;

        BindGun();
    }

    private void OnDestroy()
    {
        UnbindGun();
    }

    private bool ValidateReferences()
    {
        if (_playerAttack == null)
            return false;

        if (_slots == null || _slots.Length == 0)
            return false;

        return true;
    }

    private void BindGun()
    {
        _weaponInstance = _playerAttack.Current;

        if (_weaponInstance == null || _weaponInstance.Data == null)
            return;

        _slotBar = new SlotBar(_slots);
        _slotBar.Initialize(_weaponInstance.CurrentAmmo, _weaponInstance.Data.maxAmmo);

        _weaponInstance.OnAmmoChanged += HandleAmmoChanged;
    }

    private void UnbindGun()
    {
        if (_weaponInstance == null)
            return;

        _weaponInstance.OnAmmoChanged -= HandleAmmoChanged;
        _weaponInstance = null;
    }

    private void HandleAmmoChanged(int currentAmmo)
    {
        if (_slotBar == null)
            return;

        _slotBar.SetValue(currentAmmo, _consumeEchoDuration);
    }
}
