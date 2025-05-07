using MikeNspired.XRIStarterKit;
using UnityEngine;

public class MeeleWeaponDamage : MonoBehaviour
{
    [SerializeField] private float damageAmount;
    [SerializeField] private float damageCooldown = 0.5f;

    private float lastDamageTime = 0f;

    private void OnTriggerEnter(Collider collision)
    {
        Debug.Log($"{this.name} столкнулся с: {collision.name}");
        if (Time.time - lastDamageTime < damageCooldown) return;

        EnemyHeaths enemyHealth = collision.GetComponentInParent<EnemyHeaths>();

        if (enemyHealth != null)
        {
            if (StaticHolder.StrongArms)
            {
                enemyHealth.TakeDamage(damageAmount * StaticHolder.StrongArmsKoef);
            }
            else
            {
                enemyHealth.TakeDamage(damageAmount);
            }
            lastDamageTime = Time.time;
        }
    }
}
