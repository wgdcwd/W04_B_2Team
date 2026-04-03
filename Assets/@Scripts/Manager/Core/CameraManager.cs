using System;
using Unity.Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    [SerializeField] private int livePriority = 10;
    [SerializeField] private int idlePriority = 20;

    private CinemachineCamera curCam;

    public bool IsInitialized { get; private set; }

    public void Initialize()
    {
        if (IsInitialized) return;

        IsInitialized = true;
    }

    public void SetLiveCamera(CinemachineCamera cam)
    {
        if (curCam != null)
            curCam.Priority = idlePriority;
        curCam = cam;
        curCam.Priority = livePriority;
    }

    public void SetBoundary(Collider2D boundary)
    {
        curCam.GetComponent<CinemachineConfiner2D>().BoundingShape2D = boundary;
    }
}
