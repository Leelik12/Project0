using UnityEngine;

/// <summary>
/// Здоровье врага. EnemyStateManager необязателен (например, у вертолёта-босса его нет).
/// У гуманоидов при старте добавляются реакция на попадания и рэгдолл при смерти.
/// </summary>
public class EnemyHealth : MonoBehaviour
{
    [SerializeField] public float maxHealth = 100f;
    public float currentHealth;
    [HideInInspector] public float damageTakenMultiplier = 1f; // броня элиты

    private EnemyStateManager stateManager;
    private EnemyHitReaction hitReaction;
    private EnemyGun weapon;
    private bool isDead;

    /// <summary>Последнее попадание — куда и в каком направлении (для рэгдолла и реакции).</summary>
    public Vector3 LastHitPoint { get; private set; }
    public Vector3 LastHitDirection { get; private set; }

    private void Start()
    {
        currentHealth = maxHealth;
        weapon = GetComponentInChildren<EnemyGun>();
        stateManager = GetComponent<EnemyStateManager>();

        Animator animator = stateManager != null ? stateManager.animator : null;
        if (animator != null && animator.isHuman)
        {
            hitReaction = gameObject.AddComponent<EnemyHitReaction>();
            hitReaction.Init(animator);
        }
    }

    public float GetCurrentHp() => currentHealth;
    public bool IsDead => isDead;

    public void Heal(float amount)
    {
        if (!isDead) currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }

    /// <summary>Урон без точки попадания (например, от скриптов).</summary>
    public void TakeDamage(float damage) => TakeDamage(damage, transform.position + Vector3.up, Vector3.zero, false);

    /// <summary>Урон с точкой и направлением удара: от них зависят реакция врага и то, куда отлетит тело.</summary>
    public void TakeDamage(float damage, Vector3 point, Vector3 direction, bool headshot)
    {
        if (isDead) return;

        LastHitPoint = point;
        LastHitDirection = direction;
        damage *= damageTakenMultiplier;
        currentHealth -= damage;
        bool killed = currentHealth <= 0;

        if (!killed)
        {
            stateManager?.OnDamageTaken();
            if (hitReaction != null) hitReaction.Flinch(direction, headshot);
        }

        GameEvents.RaiseEnemyHit(new EnemyHitInfo(this, damage, point, direction, headshot, killed));

        if (killed)
        {
            isDead = true;
            GameState.RegisterKill();
            GameEvents.RaiseEnemyKilled(this);
            if (weapon != null) weapon.DropForPlayer(); // до рэгдолла: оружие ещё в руке
            stateManager?.Die();
        }
    }
}
