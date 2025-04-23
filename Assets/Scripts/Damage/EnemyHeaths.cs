using UnityEngine;

public class EnemyHeaths : MonoBehaviour
{
    [SerializeField] public float maxHealth = 100f;
    public float currentHealth = 0f;

    private EnemyStateManager stateManager;

    private void Start()
    {
        currentHealth = maxHealth;
        stateManager = GetComponent<EnemyStateManager>();
    }

    public float GetCurrentHp() { return currentHealth; }
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;

        Debug.Log($"[EnemyHealth] Враг получил урон: {damage}, осталось здоровья: {currentHealth}");
 
        if (currentHealth <= 0)
        {
            Debug.Log("[EnemyHealth] Здоровье на нуле. Враг умирает.");
            stateManager.Die();
        } else
        {
            stateManager.SwitchState(stateManager.AgroState);
            stateManager.isTakeDamage = true;
        }
    }
}