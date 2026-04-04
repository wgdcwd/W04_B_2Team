using System;
using System.Collections;
using UnityEngine;

public class MetaUIVignetteBinder : MonoBehaviour
{
    [Serializable]
    private struct HeartbeatStage
    {
        [Min(1)] public int maxHpThreshold;
        public VignetteEffectSettings firstBeatSettings;
        public VignetteEffectSettings secondBeatSettings;
        [Min(0f)] public float delayBetweenBeats;
        [Min(0f)] public float delayBetweenCycles;
    }

    [Header("References")]
    [SerializeField] private PlayerHealth _playerHealth;
    [SerializeField] private VignetteService _vignetteService;

    [Header("Default Hit")]
    [SerializeField] private VignetteEffectSettings _hitSettings;

    [Header("Low HP Heartbeat")]
    [SerializeField] private HeartbeatStage[] _heartbeatStages;

    private Coroutine _heartbeatRoutine;
    private HeartbeatStage _activeStage;
    private int _activeThreshold = -1;

    public void Bind(PlayerHealth playerHealth)
    {
        if (_playerHealth == playerHealth)
            return;

        Unbind();
        _playerHealth = playerHealth;

        if (_playerHealth == null)
            return;

        _playerHealth.OnHit += HandleHit;
        _playerHealth.OnHeal += HandleHeal;
        _playerHealth.OnDie += HandleDie;

        RefreshHeartbeatState();
    }

    private void OnDestroy()
    {
        Unbind();
        StopHeartbeat();
    }

    private void Unbind()
    {
        if (_playerHealth == null)
            return;

        _playerHealth.OnHit -= HandleHit;
        _playerHealth.OnHeal -= HandleHeal;
        _playerHealth.OnDie -= HandleDie;
        _playerHealth = null;
    }

    private void HandleHit(int damage)
    {
        _vignetteService.Play(_hitSettings);
        RefreshHeartbeatState();
    }

    private void HandleHeal(int amount)
    {
        StopHeartbeat();
        _vignetteService.RestoreDefault();
    }

    private void HandleDie()
    {
        StopHeartbeat();
    }

    private void RefreshHeartbeatState()
    {
        if (!TryGetHeartbeatStage(out var nextStage))
        {
            StopHeartbeat();
            return;
        }

        if (_heartbeatRoutine != null && _activeThreshold == nextStage.maxHpThreshold)
            return;

        StartHeartbeat(nextStage);
    }

    private bool TryGetHeartbeatStage(out HeartbeatStage stage)
    {
        stage = default;

        if (_playerHealth == null || _heartbeatStages == null || _heartbeatStages.Length == 0)
            return false;

        int currentHp = _playerHealth.CurrentHp;
        if (currentHp <= 0)
            return false;

        bool found = false;
        int bestThreshold = int.MaxValue;

        foreach (var candidate in _heartbeatStages)
        {
            if (candidate.maxHpThreshold < 1)
                continue;

            if (currentHp > candidate.maxHpThreshold)
                continue;

            if (candidate.maxHpThreshold >= bestThreshold)
                continue;

            stage = candidate;
            bestThreshold = candidate.maxHpThreshold;
            found = true;
        }

        return found;
    }

    private void StartHeartbeat(HeartbeatStage stage)
    {
        StopHeartbeat();

        _activeStage = stage;
        _activeThreshold = stage.maxHpThreshold;
        _heartbeatRoutine = StartCoroutine(HeartbeatRoutine());
    }

    private void StopHeartbeat()
    {
        if (_heartbeatRoutine != null)
        {
            StopCoroutine(_heartbeatRoutine);
            _heartbeatRoutine = null;
        }

        _activeThreshold = -1;
    }

    private IEnumerator HeartbeatRoutine()
    {
        while (IsActiveStageStillValid())
        {
            _vignetteService.Play(_activeStage.firstBeatSettings);
            yield return new WaitForSeconds(_activeStage.delayBetweenBeats);

            if (!IsActiveStageStillValid())
                break;

            _vignetteService.Play(_activeStage.secondBeatSettings);
            yield return new WaitForSeconds(_activeStage.delayBetweenCycles);
        }

        _heartbeatRoutine = null;
    }

    private bool IsActiveStageStillValid()
    {
        return _playerHealth != null &&
               _playerHealth.CurrentHp > 0 &&
               _playerHealth.CurrentHp <= _activeThreshold;
    }
}
