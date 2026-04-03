using Unity.Cinemachine;
using UnityEngine;

public class CameraZone : MonoBehaviour
{
    [SerializeField] private CinemachineCamera _zoneCam;
    [SerializeField] private CinemachineCamera _defaultCam;

    private PlayerAimer _aimer;

    private void Awake()
    {
        _aimer = FindFirstObjectByType<PlayerAimer>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _zoneCam.Priority = 20;
        _aimer?.SetComposer(_zoneCam);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _zoneCam.Priority = 0;
        _aimer?.SetComposer(_defaultCam);
    }
}