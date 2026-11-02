using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Модификаторы оружия из магазина: разрывные патроны, рикошет, пробитие. Поджог — EnemyBurn,
/// вампиризм — в VRGun. Включаются полями GameState.Mod* и работают с любым огнестрельным оружием игрока.
/// </summary>
public static class WeaponMods
{
    public const float ExplosionRadius = 2.5f;
    public const float PierceDamageMultiplier = 0.7f;
    public const float RicochetDamageMultiplier = 0.75f;
    public const float RicochetRange = 15f;
    private const float EnemyChestHeight = 1.3f;

    private static readonly Collider[] OverlapBuffer = new Collider[64];
    private static readonly HashSet<EnemyHealth> Damaged = new();

    /// <summary>Взрыв в точке попадания: урон всем врагам в радиусе, кроме того, в кого уже попала пуля.</summary>
    public static void Explode(Vector3 point, float explosionDamage, EnemyHealth directTarget)
    {
        if (explosionDamage <= 0f) return;

        ExplosionFlash.Spawn(point, ExplosionRadius);

        int count = Physics.OverlapSphereNonAlloc(point, ExplosionRadius, OverlapBuffer, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        Damaged.Clear();
        for (int i = 0; i < count; i++)
        {
            EnemyHealth enemy = OverlapBuffer[i].GetComponentInParent<EnemyHealth>();
            if (enemy == null || enemy == directTarget || enemy.IsDead || !Damaged.Add(enemy)) continue;

            // Урон слабеет к краю взрыва
            float distance = Vector3.Distance(point, OverlapBuffer[i].ClosestPoint(point));
            float falloff = Mathf.Lerp(1f, 0.4f, distance / ExplosionRadius);
            float dealt = explosionDamage * falloff;

            GameState.RegisterDamage(dealt);
            enemy.TakeDamage(dealt, point, (enemy.transform.position - point).normalized, false);
        }
    }

    /// <summary>Ближайший враг, которого видно из точки рикошета, и точка попадания в него.</summary>
    public static bool TryFindRicochetTarget(RaycastHit wallHit, out EnemyHealth target, out RaycastHit enemyHit)
    {
        target = null;
        enemyHit = default;
        float bestDistance = RicochetRange;
        Vector3 origin = wallHit.point + wallHit.normal * 0.05f;

        foreach (EnemyStateManager enemy in EnemyManager.AllEnemies)
        {
            if (enemy == null || !enemy.enabled) continue;

            Vector3 chest = enemy.transform.position + Vector3.up * EnemyChestHeight;
            Vector3 toEnemy = chest - origin;
            float distance = toEnemy.magnitude;
            if (distance >= bestDistance) continue;
            if (Vector3.Dot(toEnemy, wallHit.normal) <= 0f) continue; // пуля не уходит в стену

            if (!Physics.Raycast(origin, toEnemy / distance, out RaycastHit hit, distance + 0.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
            EnemyHealth health = hit.collider.GetComponentInParent<EnemyHealth>();
            if (health == null || health.IsDead || hit.collider.GetComponentInParent<EnemyStateManager>() != enemy) continue;

            target = health;
            enemyHit = hit;
            bestDistance = distance;
        }
        return target != null;
    }
}
