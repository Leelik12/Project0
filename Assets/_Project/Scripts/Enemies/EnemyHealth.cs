using UnityEngine;

/// <summary>Здоровье врага. EnemyStateManager необязателен (например, у вертолёта-босса его нет).</summary>
public class EnemyHealth : MonoBehaviour
{
    [SerializeField] public float maxHealth = 100f;
    public float currentHealth;

    private EnemyStateManager stateManager;
    private bool isDead;

    private void Start()
    {
        currentHealth = maxHealth;
        stateManager = GetComponent<EnemyStateManager>();
    }

    public float GetCurrentHp() => currentHealth;
    public bool IsDead => isDead;

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        stateManager?.OnDamageTaken();

        if (currentHealth <= 0)
        {
            isDead = true;
            stateManager?.Die();
        }
    }
}
