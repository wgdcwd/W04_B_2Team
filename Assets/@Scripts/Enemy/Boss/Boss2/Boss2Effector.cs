using System.Collections.Generic;
using UnityEngine;

public class Boss2Effector : MonoBehaviour
{   
    [Header("파티클 상태 레퍼런스")]
    //[SerializeField] List<ParticleSystem> _smokeParticles;
    [SerializeField] ParticleSystem _smokePS1;
    [SerializeField] ParticleSystem _smokePS2;
    [SerializeField] ParticleSystem _smokePS3;
    
    [Header("파티클 공격 레퍼런스")]
    [SerializeField] ParticleSystem _sparkPS;
    [SerializeField] private int _sparkBurstCount = 12;

    [Header("파티클 죽음 레퍼런스")]
    //[SerializeField] ParticleSystem _deathPS;

    [Header("체력당 상태 효과 퍼센트")]
    [SerializeField] private float _fstSmokePercent = .7f;
    [SerializeField] private float _sndSmokePercent = .4f;
    [SerializeField] private float _trdSmokePercent = .15f;
    

    [SerializeField] private Boss2Controller _boss2Controller;
    private float _lastHP;
    private float _maxHealth;
    private float _currentHealth;

    void Start()
    {
        StopSmokeParticles();
        //_deathPS.Stop();
        if (_boss2Controller == null)
        {
            Debug.LogWarning("[Boss2Effector] : Can't find _boss2Controller");
            return;
        }

        _maxHealth = _boss2Controller.MaxHp;
        _currentHealth = _boss2Controller.CurrentHp;

        _lastHP = _currentHealth;
    }

    void OnDestroy()
    {
        StopSmokeParticles();
    }

    public void PlaySmokeOnHP()
    {
        _currentHealth = _boss2Controller.CurrentHp;

        if (_lastHP != _currentHealth)
        {
            float _percent =  _currentHealth /  _maxHealth;

            if (_percent <= _trdSmokePercent)
            {
                if (!_smokePS3.isPlaying){ 
                    _smokePS3.Play();
                    Debug.Log("[Boss2Effector] : Smoke 3 Start");
                }
            }

            else if (_percent <= _sndSmokePercent)
            {
                if (!_smokePS2.isPlaying) {
                    _smokePS2.Play();
                    Debug.Log("[Boss2Effector] : Smoke 2 Start");
                }
            }

            else if (_percent <= _fstSmokePercent)
            {
                if (!_smokePS1.isPlaying) {
                    _smokePS1.Play();
                    Debug.Log("[Boss2Effector] : Smoke 1 Start");
                }
            }

            _lastHP = _currentHealth;
        }
    }

    public void PlayDeathEffect()
    {
        //_deathPS.Play();
    }

    public void PlaySparkOnRay()
    {
        if (_sparkPS == null)
            return;

        _sparkPS.Play();
        _sparkPS.Emit(_sparkBurstCount);
    }

    void StopSmokeParticles()
    {
        _smokePS1?.Stop();
        _smokePS2?.Stop();
        _smokePS3?.Stop();
    }
}
