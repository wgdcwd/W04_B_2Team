using UnityEngine;

public class MetaUIPlayerBinder : MonoBehaviour
{
    [SerializeField] private MetaUIChromaticAberrationBinder _chromaticAberrationBinder;
    [SerializeField] private MetaUIDryFireBinder _dryFireBinder;
    [SerializeField] private MetaUIVignetteBinder _vignetteBinder;

    private void OnEnable()
    {
        if (GameManager.Instance == null)
            return;

        GameManager.Instance.OnPlayerBound += HandlePlayerBound;

        if (GameManager.Instance.CurrentPlayer != null)
            HandlePlayerBound(GameManager.Instance.CurrentPlayer);
    }

    private void OnDisable()
    {
        if (GameManager.Instance == null)
            return;

        GameManager.Instance.OnPlayerBound -= HandlePlayerBound;
    }

    private void HandlePlayerBound(Player player)
    {
        if (player == null)
            return;

        _chromaticAberrationBinder?.Bind(player.playerHealth);
        _vignetteBinder?.Bind(player.playerHealth);
        _dryFireBinder?.Bind(player.playerAttack);
    }
}
