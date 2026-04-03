using System;
using UnityEngine;
using UnityEngine.UI;

public class UI_WeaponAmmo : MonoBehaviour
{
    [SerializeField] private Player _player;
    [SerializeField] private bool _isShotgun = true;
    [SerializeField] private RectTransform _bar;

    [Header("Color")]
    [SerializeField] private Color _filledColor = new Color(1f, 0.8f, 0f);
    [SerializeField] private Color _emptyColor = new Color(0.3f, 0.3f, 0.3f);

    private Image[] _slots;
    private WeaponInstance _weaponInstance;

    private void Start()
    {
        _weaponInstance = _isShotgun
            ? _player.playerAttack.Shotgun
            : _player.playerAttack.Current;

        _slots = CreateSlots(_weaponInstance.Data.maxAmmo);
        _weaponInstance.OnAmmoChanged += OnAmmoChanged;
        UpdateSlots(_weaponInstance.CurrentAmmo);
    }

    private void OnDestroy()
    {
        if (_weaponInstance != null)
            _weaponInstance.OnAmmoChanged -= OnAmmoChanged;
    }

    private void OnAmmoChanged(int currentAmmo)
    {
        UpdateSlots(currentAmmo);
    }

    private Image[] CreateSlots(int count)
    {
        foreach (Transform child in _bar)
            Destroy(child.gameObject);

        Image[] slots = new Image[count];

        for (int i = 0; i < count; i++)
        {
            GameObject go = new GameObject($"Slot_{i}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_bar, false);

            slots[i] = go.GetComponent<Image>();
            slots[i].color = _filledColor;
        }

        return slots;
    }

    private void UpdateSlots(int currentAmmo)
    {
        for (int i = 0; i < _slots.Length; i++)
            _slots[i].color = i < currentAmmo ? _filledColor : _emptyColor;
    }
}
