using System.Collections.Generic;
using UnityEngine;

public class ParallaxBackground : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private List<ParallaxLayer> layers = new();

    private bool _isInitialized;
    private Transform _initializedTarget;

    private void Awake()
    {
        if (layers.Count == 0)
            layers.AddRange(GetComponentsInChildren<ParallaxLayer>(true));
    }

    private void Start()
    {
        TryInitialize();
    }

    private void LateUpdate()
    {
        if (!TryInitialize())
            return;

        Vector3 targetPosition = target.position;

        foreach (ParallaxLayer layer in layers)
        {
            if (layer == null)
                continue;

            layer.Move(targetPosition);
        }
    }

    private bool TryInitialize()
    {
        if (!TryResolveTarget())
            return false;

        if (_isInitialized && _initializedTarget == target)
            return true;

        _initializedTarget = target;

        foreach (ParallaxLayer layer in layers)
        {
            if (layer == null)
                continue;

            layer.Initialize(target.position);
        }

        _isInitialized = true;
        return true;
    }

    private bool TryResolveTarget()
    {
        if (target != null)
            return true;

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            return false;

        target = mainCamera.transform;
        return true;
    }
}
