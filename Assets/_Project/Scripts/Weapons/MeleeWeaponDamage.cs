using UnityEngine;

/// <summary>Урон от оружия ближнего боя игрока (дубинка, бита, катана) с учётом импланта «Сильные руки».</summary>
public class MeleeWeaponDamage : MonoBehaviour
{
    [SerializeField] private float damageAmount;
    [SerializeField] private float damageCooldown = 0.5f;

    private float lastDamageTime;

    private void OnTriggerEnter(Collider collision)
    {
        if (Time.time - lastDamageTime < damageCooldown) return;

        EnemyHealth enemyHealth = collision.GetComponentInParent<EnemyHealth>();
        if (enemyHealth == null) return;

        float damage = GameState.StrongArms ? damageAmount * GameState.StrongArmsKoef : damageAmount;
        enemyHealth.TakeDamage(damage);
        lastDamageTime = Time.time;
    }
}
