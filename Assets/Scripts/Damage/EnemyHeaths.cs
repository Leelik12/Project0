using UnityEngine;

public class EnemyHeaths : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    private EnemyStateManager stateManager;

    private void Start()
    {
        currentHealth = maxHealth;
        stateManager = GetComponent<EnemyStateManager>();
    }
    public void TakeDamage(float damage)
    {
        Debug.Log($"[EnemyHealth] Враг получил урон: {damage}, осталось здоровья: {currentHealth}");

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Debug.Log("[EnemyHealth] Здоровье на нуле. Враг умирает.");
            stateManager.Die();
        }
    }
}