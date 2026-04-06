using UnityEngine;

public class SkillZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (!other.TryGetComponent<Player>(out var player))
            return;

        if (player.deadeyeSkill == null)
            return;

        player.deadeyeSkill.AddGauge(999f);
    }
}
