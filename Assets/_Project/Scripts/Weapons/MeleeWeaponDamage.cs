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

        EnemyHealth enemyHealth = collision.GetComponentInParent<EnemyHealth>();
        if (enemyHealth == null || enemyHealth.IsDead) return;

        float damage = GameState.StrongArms ? damageAmount * GameState.StrongArmsMultiplier : damageAmount;
        enemyHealth.TakeDamage(damage);
        GameState.RegisterDamage(damage);
        Haptics.Pulse(grabInteractable, 0.8f, 0.12f);
        lastDamageTime = Time.time;
    }
}
