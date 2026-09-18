using UnityEngine;

public class EnemyHeaths : MonoBehaviour
{
    [SerializeField] public float maxHealth = 100f;
    public float currentHealth = 0f;

    private EnemyStateManager stateManager; // может отсутствовать (например, у вертолёта-босса)
    private bool isDead = false;

    private void Start()
    {
        currentHealth = maxHealth;
        stateManager = GetComponent<EnemyStateManager>();
    }

    public float GetCurrentHp() { return currentHealth; }
    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        if (stateManager != null)
        {
            stateManager.OnDamageTaken();
        }

        if (currentHealth <= 0)
        {
            isDead = true;
            if (stateManager != null)
            {
                stateManager.Die();
            }
        }
    }
}
