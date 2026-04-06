using UnityEngine;

public class PauseController : MonoBehaviour, IInitializable
{
    public bool IsInitialized { get; private set; }
    public bool IsPauseBlocked { get; private set; }

    private InputManager _inputManager;
    private GameStateManager _gameStateManager;

    public void Initialize()
    {
        if (IsInitialized)
            return;

        ManagerRegistry.TryGet(out _inputManager);
        ManagerRegistry.TryGet(out _gameStateManager);

        if (_inputManager == null || _gameStateManager == null)
        {
            Debug.LogError($"{name}: PauseController Initialize failed.");
            return;
        }

        _inputManager.OnPause += HandlePauseInput;

        IsInitialized = true;
    }

    private void HandlePauseInput(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {

        GameState currentState = _gameStateManager.CurrentState;

        if (!ctx.started || currentState == GameState.GameOver || IsPauseBlocked)
            return;

        if (currentState == GameState.Playing)
        {
            PauseGame();
            return;
        }

        if (currentState == GameState.Paused)
        {
            ResumeGame();
        }
    }

    public void PauseGame()
    {
        Time.timeScale = 0f;
        _inputManager.DisablePlayerInput();
        _inputManager.EnableUIInput();

        _gameStateManager.ChangeState(GameState.Paused);

        Debug.Log("Game Paused");
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
        _inputManager.EnablePlayerInput();
        _inputManager.DisableUIInput();

        _gameStateManager.ChangeState(GameState.Playing);

        Debug.Log("Game Resumed");
    }

    public void SetPauseBlocked(bool blocked)
    {
        IsPauseBlocked = blocked;
    }

    private void OnDestroy()
    {
        if (_inputManager != null)
        {
            _inputManager.OnPause -= HandlePauseInput;
        }

        if (Time.timeScale == 0f)
        {
            Time.timeScale = 1f;
        }
    }
}
