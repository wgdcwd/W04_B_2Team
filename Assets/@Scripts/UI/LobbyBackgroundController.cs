using UnityEngine;

public class LobbyBackgroundController : MonoBehaviour
{
    [SerializeField] private GameObject _target;
    [SerializeField] private float _moveSpeed = 3f;

    private void Update()
    {
        if (_target == null) return;

        Vector3 pos = _target.transform.position;
        pos.x += _moveSpeed * Time.deltaTime;
        _target.transform.position = pos;
    }
}
