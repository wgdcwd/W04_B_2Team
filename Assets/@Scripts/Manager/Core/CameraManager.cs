using System;
using Unity.Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    [SerializeField] private int livePriority = 10;
    [SerializeField] private int idlePriority = 20;

    private CinemachineCamera curCam;

    private void Awake()
    {
        Instance = this;
    }

    public void SetLiveCamera(CinemachineCamera cam)
    {
        if (curCam != null)
            curCam.Priority = idlePriority;
        curCam = cam;
        curCam.Priority = livePriority;
    }
}
