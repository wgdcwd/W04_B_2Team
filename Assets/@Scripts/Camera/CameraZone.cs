using Unity.Cinemachine;
using UnityEngine;

public class CameraZone : MonoBehaviour
{
    [SerializeField] private CinemachineCamera _zoneCam;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _zoneCam.Priority = 20;
        
        //other.gameObject.GetComponent<PlayerAimer>().SetVCam(_zoneCam);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _zoneCam.Priority = 0;
    }
}
