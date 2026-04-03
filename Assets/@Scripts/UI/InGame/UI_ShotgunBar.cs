using System.Collections;
using UnityEngine;

public class UI_ShotgunBar : MonoBehaviour
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

        BindShotgun();
    }

    private void OnDestroy()
    {
        UnbindShotgun();
    }

    private bool ValidateReferences()
    {
        if (_playerAttack == null)
            return false;

        if (_slots == null || _slots.Length == 0)
            return false;

        return true;
    }

    private void BindShotgun()
    {
        _weaponInstance = _playerAttack.Shotgun;

        if (_weaponInstance == null || _weaponInstance.Data == null)
            return;

        _slotBar = new SlotBar(_slots);
        _slotBar.Initialize(_weaponInstance.CurrentAmmo, _weaponInstance.Data.maxAmmo);

        _weaponInstance.OnAmmoChanged += HandleAmmoChanged;
    }

    private void UnbindShotgun()
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