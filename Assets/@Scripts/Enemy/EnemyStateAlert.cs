using UnityEngine;

public enum EnemyState
{
    Idle,
    Alert,
    Dead
}

[DisallowMultipleComponent]
public class EnemyStateAlert : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyBase _enemy;
    [SerializeField] private SpaceUI_EnemyState _spaceUI;

    [Header("Alert")]
    [SerializeField] private float _alertDuration = 1.2f;

    public EnemyState CurrentState { get; private set; } = EnemyState.Idle;

    private void Awake()
    {
        if (_enemy == null)
            _enemy = GetComponent<EnemyBase>();

        if (_spaceUI == null)
            _spaceUI = GetComponentInChildren<SpaceUI_EnemyState>(true);
    }

    private void OnEnable()
    {
        SubscribeEvents();
        SetIdle();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();

        if (_spaceUI != null)
            _spaceUI.HideImmediate();
    }

    private void SubscribeEvents()
    {
        if (_enemy == null) return;

        _enemy.OnAlerted += HandleAlerted;
        _enemy.OnDeath += HandleDeath;
        _enemy.OnDeathFinished += HandleDeathFinished;
    }

    private void UnsubscribeEvents()
    {
        if (_enemy == null) return;

        _enemy.OnAlerted -= HandleAlerted;
        _enemy.OnDeath -= HandleDeath;
        _enemy.OnDeathFinished -= HandleDeathFinished;
    }

    private void HandleAlerted(EnemyBase enemy)
    {
        PlayAlert();
    }

    private void HandleDeath(EnemyBase enemy)
    {
        PlayDead();
    }

    private void HandleDeathFinished(EnemyBase enemy)
    {
        SetIdle();
    }

    public void PlayAlert()
    {
        if (_spaceUI == null) return;
        if (CurrentState == EnemyState.Dead) return;

        CurrentState = EnemyState.Alert;
        _spaceUI.ShowAlert(_alertDuration);
    }

    public void PlayDead()
    {
        if (_spaceUI == null) return;

        CurrentState = EnemyState.Dead;
        _spaceUI.ShowDead();
    }

    public void SetIdle()
    {
        CurrentState = EnemyState.Idle;

        if (_spaceUI != null)
            _spaceUI.HideImmediate();
    }
}
