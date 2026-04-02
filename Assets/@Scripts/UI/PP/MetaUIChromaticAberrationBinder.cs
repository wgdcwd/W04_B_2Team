using UnityEngine;

public class MetaUIChromaticAberrationBinder : MonoBehaviour
{
    [SerializeField] private PlayerHealth _playerHealth;
    [SerializeField] private ChromaticAberrationService _chromaticAberrationService;
    [SerializeField] float duration;



    private void Start()
    {
        _playerHealth.OnHit += HandleHit;
    }

    private void OnDestroy()
    {
        _playerHealth.OnHit -= HandleHit;
    }

    private void HandleHit(int damage)
    {
        _chromaticAberrationService.DoTweenPlay(0.3f);
    }
}
