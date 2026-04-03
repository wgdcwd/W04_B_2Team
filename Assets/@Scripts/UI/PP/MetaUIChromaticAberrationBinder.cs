using UnityEngine;

public class MetaUIChromaticAberrationBinder : MonoBehaviour
{
    [SerializeField] private PlayerHealth _playerHealth;
    [SerializeField] private ChromaticAberrationService _chromaticAberrationService;
    [SerializeField] private ChromaticAberrationEffectSettings _settings;

    public void Bind(PlayerHealth playerHealth)
    {
        if (_playerHealth == playerHealth)
            return;

        Unbind();
        _playerHealth = playerHealth;

        if (_playerHealth == null)
            return;

        _playerHealth.OnHit += HandleHit;
    }

    private void OnDestroy()
    {
        Unbind();
    }

    private void Unbind()
    {
        if (_playerHealth == null)
            return;

        _playerHealth.OnHit -= HandleHit;
        _playerHealth = null;
    }

    private void HandleHit(int damage)
    {
        _chromaticAberrationService.Play(_settings);
    }
}
