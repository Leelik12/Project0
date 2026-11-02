using UnityEngine;

/// <summary>
/// Триггер на оружии/руке врага ближнего боя: наносит урон игроку с задержкой между ударами.
/// Бьёт только во время атаки — простое касание (враг идёт рядом, игрок толкнул биту) не ранит.
/// </summary>
public class MeleeEnemyDamager : MonoBehaviour
{
    [SerializeField] private float damageAmount;
    [SerializeField] private float damageCooldown = 0.5f;

    private float lastDamageTime;
    private EnemyStateManager owner;

    private void OnTriggerEnter(Collider collision)
    {
        if (Time.time - lastDamageTime < damageCooldown) return;

        // Оружие — вложенный префаб в руке врага; без владельца (оружие валяется отдельно) бьёт как раньше
        if (owner == null) owner = GetComponentInParent<EnemyStateManager>();
        if (owner != null && owner.CurrentState != owner.AttackState) return;

        PlayerHealth player = collision.GetComponent<PlayerHealth>();
        if (player == null) return;

        player.PlayerTakeDamage(damageAmount);
        lastDamageTime = Time.time;
    }
}
