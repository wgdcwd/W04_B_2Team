using Unity.Cinemachine;
using UnityEngine;

public class CameraTrigger : MonoBehaviour
{
    [SerializeField] private bool _changeCamera = false;
    [SerializeField] private CinemachineCamera _zoneCamera;
    

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            CameraManager.Instance.SetLiveCamera(_zoneCamera);
            if (!_changeCamera)
            {
                CameraManager.Instance.SetBoundary(GetComponent<Collider2D>());
            }
            else
            {
                CameraManager.Instance.SetLiveCamera(_zoneCamera);
            }
        }
    }
}
