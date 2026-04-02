using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 플레이어 체력 상태(피격, 빈사)에 따른 비네트 UI 연출 제어기
/// </summary>
public class MetaUIVignetteBinder : MonoBehaviour
{
    /// <summary>
    /// 빈사 상태 체력 구간별 심장박동 연출 설정
    /// </summary>
    [Serializable]
    private struct HeartbeatStage
    {
        [Min(1)] public int maxHpThreshold;                 // 발동 최대 체력
        public VignetteEffectSettings firstBeatSettings;    
        public VignetteEffectSettings secondBeatSettings;   
        [Min(0f)] public float delayBetweenBeats;           // 1차~2차 박동 간격
        [Min(0f)] public float delayBetweenCycles;          // 사이클 반복 대기 시간
    }

    [Header("References")]
    [SerializeField] private PlayerHealth _playerHealth;
    [SerializeField] private VignetteService _vignetteService;

    [Header("Default Hit")]
    [SerializeField] private VignetteEffectSettings _hitSettings; // 일반 피격 시 재생할 기본 연출

    [Header("Low HP Heartbeat")]
    [SerializeField] private HeartbeatStage[] _heartbeatStages;   // 체력 구간별 박동 세팅

    private Coroutine _heartbeatRoutine;
    private HeartbeatStage _activeStage;
    private int _activeThreshold = -1; // 현재 적용 중인 체력 임계값 (-1: 미적용)

    private void Start()
    {
        // 체력 이벤트 구독
        _playerHealth.OnHit += HandleHit;
        _playerHealth.OnHeal += HandleHeal;
        _playerHealth.OnDie += HandleDie;

        RefreshHeartbeatState();
    }

    private void OnDestroy()
    {
        // 메모리 릭 방지를 위한 구독 해제
        _playerHealth.OnHit -= HandleHit;
        _playerHealth.OnHeal -= HandleHeal;
        _playerHealth.OnDie -= HandleDie;

        StopHeartbeat();
    }

    private void HandleHit(int damage)
    {
        _vignetteService.Play(_hitSettings); // 피격 기본 연출 즉시 재생
        RefreshHeartbeatState();             // 체력 변화에 따른 박동 상태 갱신
    }

    private void HandleHeal(int amount)
    {
        RefreshHeartbeatState();
    }

    private void HandleDie()
    {
        StopHeartbeat(); // 사망 시 빈사 연출 중지
    }

    /// <summary>
    /// 현재 체력에 맞는 박동 연출 평가 및 전환
    /// </summary>
    private void RefreshHeartbeatState()
    {
        // 1. 조건에 맞는 단계가 없으면 중지
        if (!TryGetHeartbeatStage(out var nextStage))
        {
            StopHeartbeat();
            return;
        }

        // 2. 이미 동일한 임계값의 연출이 진행 중이면 무시
        if (_heartbeatRoutine != null && _activeThreshold == nextStage.maxHpThreshold)
            return;

        StartHeartbeat(nextStage);
    }

    /// <summary>
    /// 현재 체력 조건을 만족하는 '가장 타이트한(낮은)' 임계값 세팅 검색
    /// </summary>
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
                continue; // 현재 체력이 임계값 초과면 패스

            if (candidate.maxHpThreshold >= bestThreshold)
                continue; // 더 낮은(적합한) 임계값을 이미 찾았다면 패스

            stage = candidate;
            bestThreshold = candidate.maxHpThreshold;
            found = true;
        }

        return found;
    }

    private void StartHeartbeat(HeartbeatStage stage)
    {
        StopHeartbeat(); // 기존 연출 초기화

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

    /// <summary>
    /// 2박자(쿵-쾅) 심장박동 코루틴
    /// </summary>
    private IEnumerator HeartbeatRoutine()
    {
        while (IsActiveStageStillValid())
        {
            // First beat
            _vignetteService.Play(_activeStage.firstBeatSettings);
            yield return new WaitForSeconds(_activeStage.delayBetweenBeats);

            // 대기 시간 중 상태 변경(회복/사망) 체크
            if (!IsActiveStageStillValid())
                break;

            // Second beat
            _vignetteService.Play(_activeStage.secondBeatSettings);
            yield return new WaitForSeconds(_activeStage.delayBetweenCycles);
        }

        _heartbeatRoutine = null;
    }

    /// <summary>
    /// 연출 루프 유지 조건 (생존 여부 및 해당 체력 구간 유지)
    /// </summary>
    private bool IsActiveStageStillValid()
    {
        return _playerHealth != null &&
               _playerHealth.CurrentHp > 0 &&
               _playerHealth.CurrentHp <= _activeThreshold;
    }
}