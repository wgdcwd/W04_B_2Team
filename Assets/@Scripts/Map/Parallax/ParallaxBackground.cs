using System.Collections.Generic;
using UnityEngine;

public class ParallaxBackground : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private List<ParallaxLayer> layers = new();

    private Vector3 _lastTargetPosition;
    private float _startTargetY;

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
        _startTargetY = target.position.y;

        foreach (ParallaxLayer layer in layers)
            layer.Initialize(_startTargetY);
    }

    private void LateUpdate()
    {
        float deltaX = target.position.x - _lastTargetPosition.x;
        float targetY = target.position.y;

        foreach (ParallaxLayer layer in layers)
            layer.Move(deltaX, targetY);

        _lastTargetPosition = target.position;
    }
}
