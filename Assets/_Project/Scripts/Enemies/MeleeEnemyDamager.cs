using UnityEngine;

/// <summary>Триггер на оружии/руке врага ближнего боя: наносит урон игроку с задержкой между ударами.</summary>
public class MeleeEnemyDamager : MonoBehaviour
{
    [SerializeField] private float damageAmount;
    [SerializeField] private float damageCooldown = 0.5f;

    private float lastDamageTime;

    private void OnTriggerEnter(Collider collision)
    {
        if (Time.time - lastDamageTime < damageCooldown) return;

        PlayerHealth player = collision.GetComponent<PlayerHealth>();
        if (player == null) return;

        player.PlayerTakeDamage(damageAmount);
        lastDamageTime = Time.time;
    }
}
