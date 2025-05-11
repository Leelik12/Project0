using UnityEngine;

public class MeleeEnemyDamager : MonoBehaviour
{
    [SerializeField] private float damageAmount;
    [SerializeField] private float damageCooldown = 0.5f;

    private float lastDamageTime = 0f;

    private void OnTriggerEnter(Collider collision)
    {
        //Debug.Log($"{this.name} столкнулся с: {collision.name}");
        if (Time.time - lastDamageTime < damageCooldown) return;

        PlayerHealth player = collision.GetComponent<PlayerHealth>();
        if (player != null)
        {
                player.PlayerTakeDamage(damageAmount);
            lastDamageTime = Time.time;
        }
    }
}

