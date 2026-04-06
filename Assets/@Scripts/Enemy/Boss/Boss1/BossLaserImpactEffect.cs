using UnityEngine;

public class BossLaserImpactEffect : MonoBehaviour
{
    private ParticleSystem[] _particleSystems;

    void Awake()
    {
        _particleSystems = GetComponentsInChildren<ParticleSystem>(true);
    }

    void OnEnable()
    {
        PlayParticles();
    }

    void PlayParticles()
    {
        for (int i = 0; i < _particleSystems.Length; i++)
        {
            _particleSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _particleSystems[i].Play(true);
        }
    }

}
