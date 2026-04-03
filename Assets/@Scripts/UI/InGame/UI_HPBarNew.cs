using System.Collections;
using UnityEngine;

public class UI_HPBarNew : UI_Base
{
    [Header("References")]
    [SerializeField] private PlayerHealth _playerHealth;
    [SerializeField] private UI_BarSlot[] _slots;

    private SlotBar _slotBar;

    protected override void BindEvents()
    {
    }

    private IEnumerator Start()
    {
        if (!ValidateReferences())
            yield break;

        yield return null;

        _slotBar = new SlotBar(_slots);
        _slotBar.Initialize(_playerHealth.CurrentHp, _playerHealth.MaxHp);

        _playerHealth.OnHit += HandleHit;
        _playerHealth.OnHeal += HandleHeal;
    }

    private void OnDestroy()
    {
        if (_playerHealth == null)
            return;

        _playerHealth.OnHit -= HandleHit;
        _playerHealth.OnHeal -= HandleHeal;
    }

    private bool ValidateReferences()
    {
        if (_playerHealth == null)
            return false;

        if (_slots == null || _slots.Length == 0)
            return false;

        return true;
    }

    private void HandleHit(int damage)
    {
        if (_slotBar == null)
            return;

        _slotBar.SetValue(_playerHealth.CurrentHp, _playerHealth.InvincibleDuration);
    }

    private void HandleHeal(int amount)
    {
        if (_slotBar == null)
            return;

        _slotBar.SetValue(_playerHealth.CurrentHp, 0f);
    }
}

public class SlotBar
{
    private readonly UI_BarSlot[] _slots;

    private int _currentValue;
    private int _maxValue;

    public SlotBar(UI_BarSlot[] slots)
    {
        _slots = slots;
    }

    public void Initialize(int currentValue, int maxValue)
    {
        _maxValue = Mathf.Max(0, maxValue);
        _currentValue = Mathf.Clamp(currentValue, 0, _maxValue);

        RefreshImmediate();
    }

    public void SetValue(int newValue, float consumeEchoDuration)
    {
        int nextValue = Mathf.Clamp(newValue, 0, _maxValue);

        if (nextValue < _currentValue)
            PlayDecrease(nextValue, consumeEchoDuration);
        else if (nextValue > _currentValue)
            PlayIncrease(nextValue);

        _currentValue = nextValue;
    }

    private void RefreshImmediate()
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] == null)
                continue;

            bool isActiveSlot = i < _maxValue;
            bool isFilled = i < _currentValue;

            _slots[i].gameObject.SetActive(isActiveSlot);

            if (!isActiveSlot)
                continue;

            _slots[i].SetFilledImmediate(isFilled);
        }
    }

    private void PlayDecrease(int nextValue, float consumeEchoDuration)
    {
        for (int i = _currentValue - 1; i >= nextValue; i--)
        {
            if (!IsValidIndex(i))
                continue;

            _slots[i].PlayConsumeEcho(consumeEchoDuration);
        }
    }

    private void PlayIncrease(int nextValue)
    {
        for (int i = _currentValue; i < nextValue; i++)
        {
            if (!IsValidIndex(i))
                continue;

            _slots[i].PlayRecover();
        }
    }

    private bool IsValidIndex(int index)
    {
        return _slots != null && index >= 0 && index < _slots.Length;
    }
}
