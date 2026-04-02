using UnityEngine;

public class MetaUIVignetteBinder : MonoBehaviour
{
    [SerializeField] private PlayerHealth _playerHealth;
    [SerializeField] private VignetteService _vignetteService;
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
        _vignetteService.DoTweenPlay();
    }
}
