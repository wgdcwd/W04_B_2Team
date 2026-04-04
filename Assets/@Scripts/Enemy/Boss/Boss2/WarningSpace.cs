using System.Collections;
using UnityEngine;

public class WarningSpace : MonoBehaviour
{
    [SerializeField] float _blinkInterval = 0.3f;

    SpriteRenderer _renderer;

    void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
        StartCoroutine(BlinkRoutine());
    }

    void OnDisable()
    {
        StopAllCoroutines();
        _renderer.enabled = true; // 비활성화 시 원상복구
    }

    IEnumerator BlinkRoutine()
    {
        while (true)
        {
            _renderer.enabled = !_renderer.enabled;
            yield return new WaitForSeconds(_blinkInterval);
        }
    }
}