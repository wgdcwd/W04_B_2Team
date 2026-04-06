using DG.Tweening;
using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.U2D;

public class NextScene : MonoBehaviour
{
    [SerializeField] private string _sceneName;
    [SerializeField] bool _isBossStage = false;
    [SerializeField] bool _isLastStage = false;
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
        _vcam = GetComponentInChildren<CinemachineCamera>();
        _vcam.Priority = 100;

        PixelPerfectCamera pixelPerfect = Camera.main.GetComponent<PixelPerfectCamera>();
        if (pixelPerfect != null) pixelPerfect.enabled = false;

        DOTween.To(
            () => _vcam.Lens.OrthographicSize,
            x => _vcam.Lens.OrthographicSize = x,
            0.1f,
            2f
        ).SetEase(Ease.OutCubic)
        .OnComplete(() =>
        {
            if (pixelPerfect != null) pixelPerfect.enabled = true;

            if (_sceneFlowManager == null)
                return;
            if (_gameStateManager == null || _gameStateManager.CurrentState != GameState.Respawning)
            {
                _checkpointManager?.ClearCheckpoint();
            }
            if (_isLastStage)
            {
                GameManager.Instance.GameClear();
            }
            _sceneFlowManager.SetCurrentStage(_sceneName);
            _sceneFlowManager.LoadStage();
        });
    }

    public void TitleNextStage()
    {
        LoadNextStageCore();
    }

    private void LoadNextStageCore()
    {
        if (_sceneFlowManager == null)
            return;

        if (_gameStateManager == null || _gameStateManager.CurrentState != GameState.Respawning)
        {
            _checkpointManager?.ClearCheckpoint();
        }

        if (_isLastStage)
        {
            GameManager.Instance.GameClear();
            return;
        }

        _sceneFlowManager.SetCurrentStage(_sceneName);
        _sceneFlowManager.LoadStage();
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