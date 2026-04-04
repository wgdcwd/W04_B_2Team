using DG.Tweening;
using System;
using Unity.Cinemachine;
using UnityEngine;

public class NextScene : MonoBehaviour
{
    [SerializeField] private string _sceneName;

    [SerializeField] bool _isBossStage = false;

    [SerializeField] CinemachineCamera _vcam;

    private SceneFlowManager _sceneFlowManager;
    private CheckpointManager _checkpointManager;
    private GameStateManager _gameStateManager;

    private void Awake()
    {
        ManagerRegistry.TryGet(out _sceneFlowManager);
        ManagerRegistry.TryGet(out _checkpointManager);
        ManagerRegistry.TryGet(out _gameStateManager);
    }

    public void NextStage()
    {

        _vcam.Priority = 100;
        DOTween.To(
            () => _vcam.Lens.OrthographicSize,
            x => _vcam.Lens.OrthographicSize = x,
            0.1f,
            2f
        ).SetEase(Ease.OutCubic)
        .OnComplete(() =>  // 줌인 끝난 후 씬 전환
        {
            if (_sceneFlowManager == null)
                return;

            if (_gameStateManager == null || _gameStateManager.CurrentState != GameState.Respawning)
            {
                _checkpointManager?.ClearCheckpoint();
            }

            _sceneFlowManager.SetCurrentStage(_sceneName);
            _sceneFlowManager.LoadStage();
        });
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        Player player = collision.GetComponent<Player>();

        if (player != null)
        {
            NextStage();
        }
    }
}