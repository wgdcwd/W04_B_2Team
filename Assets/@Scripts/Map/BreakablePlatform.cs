using System.Collections;
using UnityEngine;

public class BreakablePlatform : MonoBehaviour
{
    [SerializeField] float _breakDelay = 1f;
    [SerializeField] float _respawnDelay = 3f;

    SpriteRenderer _renderer;
    Collider2D _collider;
    Transform _playerTransform;
    Color _originalColor;
    bool _isBroken = false;

    void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _collider = GetComponent<Collider2D>();
        _originalColor = _renderer.color;
    }

    void Start()
    {
        _playerTransform = GameObject.FindWithTag("Player").transform;
    }

    void FixedUpdate()
    {
        if (_isBroken) return;

        bool playerBelow = _playerTransform.position.y < transform.position.y;
        _collider.enabled = !playerBelow;
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.GetComponent<Player>() == null) return;

        float normalY = col.contacts[0].normal.y;
        if (normalY < -0.5f)
            StartCoroutine(BreakRoutine());
    }

    IEnumerator BreakRoutine()
    {
        if (_isBroken) yield break;
        _isBroken = true;

        // 점점 검게
        float elapsed = 0f;
        while (elapsed < _breakDelay)
        {
            elapsed += Time.deltaTime;
            _renderer.color = Color.Lerp(_originalColor, Color.black, elapsed / _breakDelay);
            yield return null;
        }

        _renderer.enabled = false;
        _collider.enabled = false;
        _renderer.color = _originalColor; // 색상 미리 복구

        yield return new WaitForSeconds(_respawnDelay);

        _renderer.enabled = true;
        _isBroken = false;
    }
}