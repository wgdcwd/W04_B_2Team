using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_SkillBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Player _player;
    [SerializeField] private UI_BarSlot[] _slots;

    [Header("Gauge")]
    [SerializeField] private float _maxGauge = 100f;

    private DeadeyeSkill _skill;
    private UI_BarSlot[] _validSlots;
    private SlotBar _slotBar;

    private IEnumerator Start()
    {
        if (!ValidateReferences())
            yield break;

        yield return null;

        _skill = _player.deadeyeSkill;
        if (_skill == null)
            yield break;

        _validSlots = GetValidSlots();
        if (_validSlots.Length == 0)
            yield break;

        _slotBar = new SlotBar(_validSlots);
        _slotBar.Initialize(GetFilledSlotCount(_skill.CurrentGauge), _validSlots.Length);

        _skill.OnGaugeChanged += HandleGaugeChanged;
    }

    private void OnDestroy()
    {
        if (_skill == null)
            return;

        _skill.OnGaugeChanged -= HandleGaugeChanged;
    }

    private bool ValidateReferences()
    {
        if (_player == null)
            return false;

        if (_slots == null || _slots.Length == 0)
            return false;

        if (_maxGauge <= 0f)
            return false;

        return true;
    }

    private void HandleGaugeChanged(float currentGauge)
    {
        if (_slotBar == null)
            return;

        _slotBar.SetValue(GetFilledSlotCount(currentGauge));
    }

    private int GetFilledSlotCount(float currentGauge)
    {
        float normalizedGauge = Mathf.Clamp01(currentGauge / _maxGauge);
        int filledSlotCount = Mathf.FloorToInt(normalizedGauge * _validSlots.Length);

        if (currentGauge >= _maxGauge)
            return _validSlots.Length;

        return Mathf.Clamp(filledSlotCount, 0, _validSlots.Length);
    }

    private UI_BarSlot[] GetValidSlots()
    {
        List<UI_BarSlot> validSlots = new List<UI_BarSlot>();

        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] == null)
                continue;

            validSlots.Add(_slots[i]);
        }

        return validSlots.ToArray();
    }
}
