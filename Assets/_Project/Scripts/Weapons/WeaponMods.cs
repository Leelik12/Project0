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

    private const float ExplosionOriginOffset = 0.05f; // центр взрыва чуть перед поверхностью, чтобы луч не начинался в стене

    // С запасом: пол, стены и части тел (у каждого врага несколько коллайдеров) не должны вытеснять врагов
    private static readonly Collider[] OverlapBuffer = new Collider[256];
    private static readonly RaycastHit[] LineBuffer = new RaycastHit[32];
    private static readonly Comparer<RaycastHit> ByDistance = Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance));
    private static readonly Dictionary<EnemyHealth, float> Nearest = new(); // враг -> расстояние до ближайшей видимой части

    /// <summary>
    /// Взрыв в точке попадания: урон всем видимым из неё врагам в радиусе, кроме того, в кого уже попала пуля.
    /// normal — нормаль поверхности попадания; враг за стеной не задевается.
    /// </summary>
    public static void Explode(Vector3 point, Vector3 normal, float explosionDamage, EnemyHealth directTarget)
    {
        if (explosionDamage <= 0f) return;

        ExplosionFlash.Spawn(point, ExplosionRadius);

        Vector3 origin = point + normal * ExplosionOriginOffset;
        int count = Physics.OverlapSphereNonAlloc(point, ExplosionRadius, OverlapBuffer, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        // Из-за укрытия видна, например, голова, но не ноги: каждая часть тела проверяется отдельно,
        // а урон считается по ближайшей видимой
        Nearest.Clear();
        for (int i = 0; i < count; i++)
        {
            Collider part = OverlapBuffer[i];
            EnemyHealth enemy = part.GetComponentInParent<EnemyHealth>();
            if (enemy == null || enemy == directTarget || enemy.IsDead) continue;
            if (!InLineOfSight(origin, part, enemy)) continue;

            float distance = Vector3.Distance(point, part.ClosestPoint(point));
            if (!Nearest.TryGetValue(enemy, out float best) || distance < best) Nearest[enemy] = distance;
        }

        foreach (KeyValuePair<EnemyHealth, float> pair in Nearest)
        {
            // Урон слабеет к краю взрыва
            float falloff = Mathf.Lerp(1f, 0.4f, pair.Value / ExplosionRadius);
            float dealt = explosionDamage * falloff;

            GameState.RegisterDamage(dealt);
            pair.Key.TakeDamage(dealt, point, (pair.Key.transform.position - point).normalized, false);
        }
        Nearest.Clear();
    }

    // Первое препятствие на пути от центра взрыва к части тела — сам этот враг.
    // Трупы, сам игрок и оружие у него в руках укрытием не считаются.
    private static bool InLineOfSight(Vector3 origin, Collider part, EnemyHealth enemy)
    {
        Vector3 toPart = part.bounds.center - origin;
        float length = toPart.magnitude;
        if (length < 0.001f) return true;

        int count = Physics.RaycastNonAlloc(origin, toPart / length, LineBuffer, length, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        System.Array.Sort(LineBuffer, 0, count, ByDistance);
        for (int i = 0; i < count; i++)
        {
            Collider blocker = LineBuffer[i].collider;
            EnemyHealth owner = blocker.GetComponentInParent<EnemyHealth>();
            if (owner == enemy) return true;
            if (owner != null && owner.IsDead) continue;
            if (blocker.GetComponentInParent<PlayerHealth>() != null) continue;
            var held = blocker.GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            if (held != null && held.isSelected) continue;
            return false;
        }
        return true;
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
