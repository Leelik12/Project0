using MikeNspired.XRIStarterKit;
using UnityEngine;

public class MeeleWeaponDamage : MonoBehaviour
{
    [SerializeField] private float damageAmount = 20f;
    [SerializeField] private float damageCooldown = 0.5f;

    private float lastDamageTime = 0f;

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"{this.name} столкнулся с: {collision.collider.name}");
        if (Time.time - lastDamageTime < damageCooldown) return;

        EnemyHeaths enemyHealth = collision.collider.GetComponentInParent<EnemyHeaths>();

        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(damageAmount);
            lastDamageTime = Time.time;
        }
    }
}
