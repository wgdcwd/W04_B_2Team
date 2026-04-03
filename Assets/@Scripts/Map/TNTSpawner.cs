using UnityEngine;

public class TNTSpawner : MonoBehaviour
{
    [SerializeField] private GameObject _tntPrefab;
    [SerializeField] private float _respawnDelay = 5f;

    private GameObject _currentTNT;
    private float _respawnTimer = -1f;

    private void Start()
    {
        SpawnTNT();
    }

    private void Update()
    {
        if (_respawnTimer < 0f) return;

        _respawnTimer -= Time.deltaTime;
        if (_respawnTimer <= 0f)
        {
            _respawnTimer = -1f;
            SpawnTNT();
        }
    }

    public void OnTNTExploded()
    {
        _currentTNT = null;
        _respawnTimer = _respawnDelay;
    }

    private void SpawnTNT()
    {
        _currentTNT = Instantiate(_tntPrefab, transform.position, Quaternion.identity);

        if (_currentTNT.TryGetComponent<Explosives>(out var explosives))
            explosives.SetSpawner(this);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }
}
