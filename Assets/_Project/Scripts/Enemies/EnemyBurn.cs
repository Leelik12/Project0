using UnityEngine;

/// <summary>
/// Поджог от зажигательных патронов: урон каждые полсекунды, пока горит, и мерцающий оранжевый свет на враге.
/// Повторное попадание продлевает горение, а не складывает урон.
/// </summary>
public class EnemyBurn : MonoBehaviour
{
    private const float BurnDuration = 3f;
    private const float TickInterval = 0.5f;
    private const float ChestHeight = 1.3f;

    private EnemyHealth health;
    private Light glow;
    private float damagePerSecond;
    private float endTime;
    private float nextTick;

    public static void Ignite(EnemyHealth enemy, float damagePerSecond)
    {
        if (enemy == null || enemy.IsDead || damagePerSecond <= 0f) return;

        if (!enemy.TryGetComponent(out EnemyBurn burn))
        {
            burn = enemy.gameObject.AddComponent<EnemyBurn>();
            burn.health = enemy;
            burn.nextTick = Time.time + TickInterval;
        }
        burn.damagePerSecond = Mathf.Max(burn.damagePerSecond, damagePerSecond);
        burn.endTime = Time.time + BurnDuration;
        burn.enabled = true;
        burn.SetGlow(true);
    }

    private void Update()
    {
        if (health == null || health.IsDead || Time.time >= endTime)
        {
            SetGlow(false);
            enabled = false;
            return;
        }

        if (glow != null) glow.intensity = 1.5f + Mathf.PerlinNoise(Time.time * 12f, 0f) * 1.5f;
        if (Time.time < nextTick) return;

        nextTick = Time.time + TickInterval;
        float dealt = damagePerSecond * TickInterval;
        Vector3 chest = transform.position + Vector3.up * ChestHeight;
        GameState.RegisterDamage(dealt);
        health.TakeDamage(dealt, chest, Vector3.zero, false);
    }

    private void SetGlow(bool on)
    {
        if (on && glow == null)
        {
            var go = new GameObject("BurnGlow");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.up * ChestHeight;
            glow = go.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = new Color(1f, 0.45f, 0.1f);
            glow.range = 2.5f;
            glow.shadows = LightShadows.None;
        }
        if (glow != null) glow.enabled = on;
    }
}
