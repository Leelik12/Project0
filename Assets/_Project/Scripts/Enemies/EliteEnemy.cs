using UnityEngine;

/// <summary>
/// Элитный враг: больше здоровья, светится цветом своего модификатора и даёт больше эдди.
/// Модификаторы: быстрый (жёлтый), бронированный (синий, получает половину урона), регенерирующий (зелёный).
/// Назначается из LevelManager случайному врагу этажа.
/// </summary>
public class EliteEnemy : MonoBehaviour
{
    public enum Modifier
    {
        Fast,
        Armored,
        Regenerating,
    }

    private const float HealthMultiplier = 2.5f;
    private const float FastSpeedMultiplier = 1.4f;
    private const float FastAnimationMultiplier = 1.25f;
    private const float ArmoredDamageTaken = 0.5f;
    private const float RegenerationPerSecond = 0.03f; // доля максимального здоровья
    private const float ChestHeight = 1.3f;
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

    private EnemyHealth health;
    private Light glow;

    public Modifier Kind { get; private set; }

    public static EliteEnemy Make(EnemyHealth enemy)
    {
        var elite = enemy.gameObject.AddComponent<EliteEnemy>();
        elite.health = enemy;
        elite.Kind = (Modifier)Random.Range(0, 3);
        elite.Apply();
        return elite;
    }

    private void Apply()
    {
        health.maxHealth *= HealthMultiplier;
        health.currentHealth = health.maxHealth;

        EnemyStateManager state = GetComponent<EnemyStateManager>();
        switch (Kind)
        {
            case Modifier.Fast:
                if (state != null)
                {
                    state.walkSpeed *= FastSpeedMultiplier;
                    state.runSpeed *= FastSpeedMultiplier;
                    if (state.animator != null) state.animator.speed *= FastAnimationMultiplier;
                }
                break;
            case Modifier.Armored:
                health.damageTakenMultiplier *= ArmoredDamageTaken;
                break;
        }

        Glow(KindColor(Kind));
    }

    private static Color KindColor(Modifier kind) => kind switch
    {
        Modifier.Fast => new Color(1f, 0.85f, 0.2f),
        Modifier.Armored => new Color(0.3f, 0.6f, 1f),
        _ => new Color(0.3f, 1f, 0.4f),
    };

    // Свет у груди и подсветка материалов (у каждого элитного врага свои копии материалов)
    private void Glow(Color color)
    {
        var go = new GameObject("EliteGlow");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.up * ChestHeight;
        glow = go.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.color = color;
        glow.range = 3f;
        glow.intensity = 2f;
        glow.shadows = LightShadows.None;

        foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
        {
            if (renderer is not SkinnedMeshRenderer && renderer is not MeshRenderer) continue;
            foreach (Material material in renderer.materials)
            {
                if (!material.HasProperty(EmissionColor)) continue;
                material.EnableKeyword("_EMISSION");
                material.SetColor(EmissionColor, color * 0.6f);
            }
        }
    }

    private void Update()
    {
        if (health == null || health.IsDead)
        {
            if (glow != null) glow.enabled = false; // гаснет при смерти
            enabled = false;
            return;
        }

        if (glow != null) glow.intensity = 1.6f + Mathf.Sin(Time.time * 4f) * 0.5f;
        if (Kind == Modifier.Regenerating) health.Heal(health.maxHealth * RegenerationPerSecond * Time.deltaTime);
    }
}
