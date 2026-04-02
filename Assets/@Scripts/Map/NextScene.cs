using System;
using UnityEngine;

public class NextScene : MonoBehaviour
{
    [SerializeField] private string _sceneName;

    [SerializeField] bool _isBossStage = false;

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
        if (_sceneFlowManager == null)
            return;

        if (_gameStateManager == null || _gameStateManager.CurrentState != GameState.Respawning)
        {
            _checkpointManager?.ClearCheckpoint();
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