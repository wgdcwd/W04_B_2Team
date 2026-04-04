using UnityEngine;

public class MetaUIDryFireBinder : MonoBehaviour
{
    [SerializeField] private PlayerAttack _playerAttack;
    [SerializeField] private LensDistortionService _lensDistortionService;
    [SerializeField] private FilmGrainService _filmGrainService;
    [SerializeField] private ChromaticAberrationService _chromaticAberrationService;
    [SerializeField] private LensDistortionEffectSettings _lensDistortionSettings;
    [SerializeField] private FilmGrainEffectSettings _filmGrainSettings;
    [SerializeField] private ChromaticAberrationEffectSettings _chromaticAberrationSettings;

    public void Bind(PlayerAttack playerAttack)
    {
        if (_playerAttack == playerAttack)
            return;

        Unbind();
        _playerAttack = playerAttack;

        if (_playerAttack == null)
            return;

        _playerAttack.OnDryFire += HandleFire;
    }

    private void OnDestroy()
    {
        Unbind();
    }

    private void Unbind()
    {
        if (_playerAttack == null)
            return;

        _playerAttack.OnDryFire -= HandleFire;
        _playerAttack = null;
    }

    private void HandleFire(DryFireContext context)
    {
        _lensDistortionService?.Play(_lensDistortionSettings);
        _filmGrainService?.Play(_filmGrainSettings);
        _chromaticAberrationService?.Play(_chromaticAberrationSettings);
    }
}
