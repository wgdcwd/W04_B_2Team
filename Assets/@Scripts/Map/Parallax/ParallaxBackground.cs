using System.Collections.Generic;
using UnityEngine;

public class ParallaxBackground : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private List<ParallaxLayer> layers = new();

    private Vector3 _lastTargetPosition;

    private void Awake()
    {
        if (target == null && Camera.main != null)
            target = Camera.main.transform;

        if (layers.Count == 0)
            layers.AddRange(GetComponentsInChildren<ParallaxLayer>());
    }

    private void Start()
    {
        if (target == null)
        {
            enabled = false;
            return;
        }

        _lastTargetPosition = target.position;
    }

    private void LateUpdate()
    {
        Vector3 delta = target.position - _lastTargetPosition;

        foreach (ParallaxLayer layer in layers)
            layer.Move(delta);

        _lastTargetPosition = target.position;
    }
}