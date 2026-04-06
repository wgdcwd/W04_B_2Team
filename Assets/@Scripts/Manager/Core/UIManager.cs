using UnityEngine;

public class UIManager : MonoBehaviour, IInitializable
{
    public bool IsInitialized { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject _pausePanel;
    [SerializeField] private GameObject _gameOverPanel;

    [Header("Panel Scripts")]
    [SerializeField] private UI_Pause _uiPausePanel;
    [SerializeField] private UI_GameOver _uiGameOverPanel;

    private GameStateManager _gameStateManager;
    private PauseController _pauseController;
    private SceneFlowManager _sceneFlowManager;
    private PoolManager _poolManager;

    public void Initialize()
    {
        if (IsInitialized)
            return;

        if (!ManagerRegistry.TryGet(out _gameStateManager))
        {
            Debug.LogError($"{name}: GameStateManager not found.");
            return;
        }

        ManagerRegistry.TryGet(out _pauseController);
        ManagerRegistry.TryGet(out _sceneFlowManager);
        ManagerRegistry.TryGet(out _poolManager);

        _gameStateManager.OnStateChanged += HandleStateChanged;

        RebindUI();

        IsInitialized = true;
    }

    public void RebindUI()
    {
        UnbindPanelEvents();
        FindUIReferences();
        BindPanelEvents();
        HideAll();
    }

    private void FindUIReferences()
    {
        _uiPausePanel = FindAnyObjectByType<UI_Pause>(FindObjectsInactive.Include);
        _uiGameOverPanel = FindAnyObjectByType<UI_GameOver>(FindObjectsInactive.Include);

        _pausePanel = null;
        _gameOverPanel = null;

        if (_uiPausePanel != null)
            _pausePanel = _uiPausePanel.gameObject;

        if (_uiGameOverPanel != null)
            _gameOverPanel = _uiGameOverPanel.gameObject;
    }

    private void BindPanelEvents()
    {
        if (_uiPausePanel != null)
        {
            _uiPausePanel.OnRetryRequested += HandleRetryRequested;
            _uiPausePanel.OnMainMenuRequested += HandleMainMenuRequested;
        }

        if (_uiGameOverPanel != null)
        {
            _uiGameOverPanel.OnRetryRequested += HandleRetryRequested;
            _uiGameOverPanel.OnMainMenuRequested += HandleMainMenuRequested;
        }
    }

    private void UnbindPanelEvents()
    {
        if (_uiPausePanel != null)
        {
            _uiPausePanel.OnRetryRequested -= HandleRetryRequested;
            _uiPausePanel.OnMainMenuRequested -= HandleMainMenuRequested;

            _uiGameOverPanel.OnRetryRequested -= HandleRetryRequested;
            _uiGameOverPanel.OnMainMenuRequested -= HandleMainMenuRequested;
        }
    }

    private void HandleStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.Paused:
                ShowPause();
                Cursor.visible = true;
                break;

            case GameState.GameOver:
                ShowGameOver();
                Cursor.visible = true;
                break;

            default:
                HideAll();
                Cursor.visible = false;
                break;
        }
    }

    private void HandleRetryRequested()
    {
        _gameStateManager.ChangeState(GameState.Respawning);
        _pauseController?.ResumeGame();
        _poolManager?.ClearRuntimeObjects();
        _sceneFlowManager?.LoadStage();
    }

    private void HandleMainMenuRequested()
    {
        _poolManager?.ClearRuntimeObjects();
        _sceneFlowManager?.LoadMainMenu();
    }

    public void ShowPause()
    {
        if (_pausePanel != null)
        {
            _pausePanel.SetActive(true);
            _uiPausePanel.ApplyFirstSelection();
        }
            

        if (_gameOverPanel != null)
            _gameOverPanel.SetActive(false);
    }

    public void HidePause()
    {
        if (_pausePanel != null)
            _pausePanel.SetActive(false);
    }

    public void ShowGameOver()
    {
        if (_pausePanel != null)
            _pausePanel.SetActive(false);

        if (_gameOverPanel != null)
            _gameOverPanel.SetActive(true);
    }

    public void HideGameOver()
    {
        if (_gameOverPanel != null)
            _gameOverPanel.SetActive(false);
    }

    public void HideAll()
    {
        if (_pausePanel != null)
            _pausePanel.SetActive(false);

        if (_gameOverPanel != null)
            _gameOverPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_gameStateManager != null)
            _gameStateManager.OnStateChanged -= HandleStateChanged;

        UnbindPanelEvents();
    }
}