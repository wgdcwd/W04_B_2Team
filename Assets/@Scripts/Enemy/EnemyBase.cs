using System;
using System.Collections;
using UnityEngine;

public abstract class EnemyBase : EntityBase
{

    // 플레이어
    private DeadeyeSkill _deadeyeSkill;

    // 이벤트
    public event Action<EnemyBase> OnDeathFinished;

    // =====================
    // 마킹
    // =====================
    [SerializeField] private CircleDrawer _markIndicator;
    private bool _isMarked = false;
    private GameStateManager _gameStateManager;

    private void Awake()
    {
        ManagerRegistry.TryGet(out _gameStateManager);
    }

    public void ShowMark(bool show)
    {
        _markIndicator = GetComponentInChildren<CircleDrawer>();
        if (_markIndicator == null) return;
        _isMarked = show;
        _markIndicator.gameObject.SetActive(show);
    }

    protected override void Initialize()
    {
        base.Initialize();

    }

    // =====================
    // TakeDamage
    // =====================
    public virtual void TakeDamage(int damage, bool isAddGauge = false)
    {
        GameObject.FindWithTag("Player").GetComponent<DeadeyeSkill>().AddGauge(1);
        base.TakeDamage(damage);
    }

    public override void TakeDamage(int damage)
    {
        base.TakeDamage(damage);
    }

    public abstract override void Die();

    protected IEnumerator DieRoutine()
    {
        yield return StartCoroutine(OnDieRoutine());
        OnDeathFinished?.Invoke(this);
    }

    protected virtual IEnumerator OnDieRoutine()
    {
        yield break;
        //OnDeathFinished?.Invoke(this);
    }

    protected bool CanAct()
    {
        return _gameStateManager != null
            && _gameStateManager.CurrentState == GameState.Playing;
        //return true; // 일단 모든 상태에서 행동 가능하도록 허용. 필요시 GameState 체크 로직 추가.
    }
}