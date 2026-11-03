using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

/// <summary>Здоровье врагов, элита, награды в эдди и камуфляж.</summary>
public class CombatTests
{
    private GameStateSandbox sandbox;
    private readonly List<GameObject> spawned = new();

    [SetUp]
    public void SetUp()
    {
        sandbox = new GameStateSandbox();
        GameState.ResetRun();
        // В игре EddiesRewards подписывается сам при запуске сцены
        typeof(EddiesRewards).GetMethod("Init", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject go in spawned) Object.DestroyImmediate(go);
        spawned.Clear();
        sandbox.Dispose();
    }

    private EnemyHealth Enemy(float hp = 100f, bool start = true)
    {
        var go = new GameObject("TestEnemy");
        spawned.Add(go);
        EnemyHealth health = go.AddComponent<EnemyHealth>();
        health.maxHealth = hp;
        health.currentHealth = hp;
        // Start — как в игре: ищет оружие и EnemyStateManager (у вертолёта его нет)
        if (start) typeof(EnemyHealth).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(health, null);
        return health;
    }

    [Test]
    public void Kill_WithoutStateManager_DoesNotThrow()
    {
        EnemyHealth enemy = Enemy();
        Assert.DoesNotThrow(() => enemy.TakeDamage(150f));
        Assert.IsTrue(enemy.IsDead);
        Assert.AreEqual(1, GameState.RunKills);
    }

    [Test]
    public void DeadEnemy_IgnoresDamageAndHealing()
    {
        EnemyHealth enemy = Enemy();
        enemy.TakeDamage(150f);
        float hp = enemy.currentHealth;
        enemy.TakeDamage(10f);
        enemy.Heal(50f);
        Assert.AreEqual(hp, enemy.currentHealth);
        Assert.AreEqual(1, GameState.RunKills, "убийство засчитано один раз");
    }

    [Test]
    public void Heal_IsCappedAtMax()
    {
        EnemyHealth enemy = Enemy();
        enemy.TakeDamage(30f);
        enemy.Heal(100f);
        Assert.AreEqual(100f, enemy.currentHealth);
    }

    [Test]
    public void KillReward_CountsHeadshotAndElite()
    {
        UpgradeCatalog catalog = UpgradeCatalog.Instance;

        Enemy().TakeDamage(500f);
        Assert.AreEqual(catalog.killReward, GameState.Eddies);

        GameState.Eddies = 0;
        Enemy().TakeDamage(500f, Vector3.zero, Vector3.forward, headshot: true);
        Assert.AreEqual(catalog.killReward + catalog.headshotKillBonus, GameState.Eddies);

        GameState.Eddies = 0;
        EnemyHealth elite = Enemy();
        EliteEnemy.Make(elite);
        elite.TakeDamage(10000f);
        Assert.AreEqual(catalog.killReward + catalog.eliteKillBonus, GameState.Eddies);
    }

    [Test]
    public void NonLethalHit_GivesNoEddies()
    {
        Enemy().TakeDamage(10f);
        Assert.AreEqual(0, GameState.Eddies);
    }

    [Test]
    public void Elite_HasMoreHealthAndArmorHalvesDamage()
    {
        for (int seed = 0; seed < 30; seed++)
        {
            Random.InitState(seed);
            EnemyHealth enemy = Enemy();
            EliteEnemy elite = EliteEnemy.Make(enemy);
            Assert.AreEqual(250f, enemy.maxHealth);
            Assert.AreEqual(250f, enemy.currentHealth);

            enemy.TakeDamage(20f);
            float expected = elite.Kind == EliteEnemy.Modifier.Armored ? 240f : 230f;
            Assert.AreEqual(expected, enemy.currentHealth, 1e-3f, elite.Kind.ToString());
            Assert.IsNotNull(enemy.GetComponentInChildren<Light>(), "элита светится");
        }
    }

    [Test]
    public void Elite_RollsEveryModifier()
    {
        var kinds = new HashSet<EliteEnemy.Modifier>();
        for (int seed = 0; seed < 60; seed++)
        {
            Random.InitState(seed);
            kinds.Add(EliteEnemy.Make(Enemy()).Kind);
        }
        Assert.AreEqual(3, kinds.Count);
    }

    [Test]
    public void Ambush_TriplesOnlyTheFirstShot()
    {
        PlayerStealth.SetInvisible(true);
        Assert.AreEqual(PlayerStealth.AmbushDamageMultiplier, PlayerStealth.ConsumeAmbush());
        Assert.IsFalse(PlayerStealth.IsInvisible, "выстрел снимает камуфляж");
        Assert.AreEqual(1f, PlayerStealth.ConsumeAmbush());
    }

    [Test]
    public void Stealth_ReportsChangesOnce()
    {
        int calls = 0;
        void OnChanged(bool _) => calls++;
        PlayerStealth.Changed += OnChanged;
        try
        {
            PlayerStealth.SetInvisible(true);
            PlayerStealth.SetInvisible(true);
            PlayerStealth.SetInvisible(false);
        }
        finally
        {
            PlayerStealth.Changed -= OnChanged;
        }
        Assert.AreEqual(2, calls);
    }
}
