using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>Урон от оружия ближнего боя игрока (дубинка, бита, катана) с учётом импланта «Сильные руки».</summary>
public class MeleeWeaponDamage : MonoBehaviour
{
    [SerializeField] private float damageAmount;
    [SerializeField] private float damageCooldown = 0.5f;

    private float lastDamageTime;
    private XRGrabInteractable grabInteractable; // для вибрации в руке, которая держит оружие

    private void Awake()
    {
        grabInteractable = GetComponentInParent<XRGrabInteractable>();
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (Time.time - lastDamageTime < damageCooldown) return;

        if (collision.TryGetComponent(out ShieldPlate shield)) // щит щитовика: удар урона не наносит
        {
            shield.Absorb(collision.ClosestPoint(transform.position), damageAmount);
            Haptics.Pulse(grabInteractable, 0.8f, 0.12f);
            lastDamageTime = Time.time;
            return;
        }

        EnemyHealth enemyHealth = collision.GetComponentInParent<EnemyHealth>();
        if (enemyHealth == null || enemyHealth.IsDead) return;

        float damage = GameState.StrongArms ? damageAmount * GameState.StrongArmsMultiplier : damageAmount;
        Vector3 point = collision.ClosestPoint(transform.position);
        Vector3 direction = (point - transform.position).normalized;
        enemyHealth.TakeDamage(damage, point, direction, collision.CompareTag("Head"));
        GameState.RegisterDamage(damage);
        Haptics.Pulse(grabInteractable, 0.8f, 0.12f);
        lastDamageTime = Time.time;
    }
}
