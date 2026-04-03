using System;
using Unity.Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour, IInitializable
{

    [Header("Cameras")]
    //[SerializeField] private CinemachineCamera _cineCam;


    public bool IsInitialized { get; private set; }

    public void Initialize()
    {
        if (IsInitialized) return;

        IsInitialized = true;
    }
}
