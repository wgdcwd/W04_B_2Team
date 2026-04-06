using UnityEngine;

public class BoseDamageZone : MonoBehaviour
{
    [Header("Damage Gameplay")]
    public int damage = 1;
    private Collider2D _damageCollider;

    void Awake()
    {
        _damageCollider = GetComponent<Collider2D>();
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            other.GetComponent<IDamageable>()?.TakeDamage(damage);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
            collision.gameObject.GetComponent<IDamageable>()?.TakeDamage(damage);

    }

    public void SetDamageEnabled(bool isEnabled)
    {
        if (_damageCollider != null)
            _damageCollider.enabled = isEnabled;
    }
}
