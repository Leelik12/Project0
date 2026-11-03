using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Разрывные патроны (WeaponMods.Explode): урон по площади один раз на врага, с затуханием и без урона через стены.
/// Объекты ставятся далеко от содержимого открытой сцены и удаляются после теста.
/// </summary>
public class ExplosionTests
{
    private static readonly Vector3 Origin = new(5000f, 0f, 5000f);
    private const float Damage = 50f;

    private GameStateSandbox sandbox;
    private readonly List<GameObject> spawned = new();

    [SetUp]
    public void SetUp()
    {
        sandbox = new GameStateSandbox();
        GameState.ResetRun();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (GameObject go in spawned) Object.DestroyImmediate(go);
        spawned.Clear();
        // Вспышка взрыва в редакторе сама не исчезает
        foreach (ExplosionFlash flash in Object.FindObjectsByType<ExplosionFlash>(FindObjectsSortMode.None)) Object.DestroyImmediate(flash.gameObject);
        sandbox.Dispose();
    }

    // Враг как настоящий: ноги, тело и голова — отдельные коллайдеры
    private EnemyHealth Enemy(Vector3 position)
    {
        var go = new GameObject("TestEnemy");
        spawned.Add(go);
        go.transform.position = position;
        foreach (float y in new[] { 0.4f, 1.1f, 1.6f })
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            part.transform.SetParent(go.transform, false);
            part.transform.localPosition = Vector3.up * y;
            part.transform.localScale = Vector3.one * 0.35f;
        }
        EnemyHealth health = go.AddComponent<EnemyHealth>();
        health.maxHealth = 100f;
        typeof(EnemyHealth).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(health, null);
        return health;
    }

    private void Wall(Vector3 center, Vector3 size)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        spawned.Add(wall);
        wall.transform.position = center;
        wall.transform.localScale = size;
    }

    private static void Explode(Vector3 point, Vector3 normal, EnemyHealth direct = null)
    {
        Physics.SyncTransforms();
        WeaponMods.Explode(point, normal, Damage, direct);
    }

    private static float Taken(EnemyHealth enemy) => enemy.maxHealth - enemy.currentHealth;

    [Test]
    public void HitsEachEnemyOnce()
    {
        EnemyHealth enemy = Enemy(Origin + Vector3.forward * 1f);
        Explode(Origin + Vector3.up * 1.1f, Vector3.forward);

        Assert.Greater(Taken(enemy), Damage * 0.4f - 0.01f, "взрыв не задел врага рядом");
        Assert.LessOrEqual(Taken(enemy), Damage, "взрыв попал по одному врагу несколько раз");
    }

    [Test]
    public void DamageFallsOffWithDistance()
    {
        EnemyHealth near = Enemy(Origin + Vector3.right * 0.6f);
        EnemyHealth far = Enemy(Origin + Vector3.left * 2.2f);
        Explode(Origin + Vector3.up * 1.1f, Vector3.up);

        Assert.Greater(Taken(far), 0f);
        Assert.Greater(Taken(near), Taken(far));
    }

    [Test]
    public void WallShieldsEnemyBehindIt()
    {
        // Пуля попала в стену; враг в метре за ней, в радиусе взрыва
        Wall(Origin + new Vector3(0f, 1f, 0.25f), new Vector3(4f, 3f, 0.5f));
        EnemyHealth hidden = Enemy(Origin + Vector3.forward * 1.2f);
        Explode(Origin + new Vector3(0f, 1.1f, 0f), Vector3.back);

        Assert.AreEqual(0f, Taken(hidden), "взрыв прошёл сквозь стену");
    }

    [Test]
    public void LowCoverStillLetsHeadBeHit()
    {
        // Невысокое укрытие закрывает ноги, но голова видна
        Wall(Origin + new Vector3(0f, 0.35f, 0.8f), new Vector3(3f, 0.7f, 0.2f));
        EnemyHealth enemy = Enemy(Origin + Vector3.forward * 1.3f);
        Explode(Origin + new Vector3(0f, 0.9f, 0f), Vector3.up);

        Assert.Greater(Taken(enemy), 0f);
    }

    [Test]
    public void CorpseIsNotCover()
    {
        EnemyHealth corpse = Enemy(Origin + Vector3.forward * 0.8f);
        corpse.TakeDamage(1000f);
        EnemyHealth behind = Enemy(Origin + Vector3.forward * 1.6f);
        Explode(Origin + Vector3.up * 1.1f, Vector3.forward);

        Assert.Greater(Taken(behind), 0f, "труп закрыл живого врага от взрыва");
    }

    [Test]
    public void DamageUsesNearestVisiblePart()
    {
        // Враг стоит вплотную к точке взрыва головой: урон почти полный, даже если ноги дальше
        EnemyHealth enemy = Enemy(Origin + Vector3.forward * 0.4f);
        Explode(Origin + Vector3.up * 1.6f, Vector3.back);

        Assert.Greater(Taken(enemy), Damage * 0.9f);
    }

    [Test]
    public void DirectTargetIsNotHitTwice()
    {
        EnemyHealth direct = Enemy(Origin + Vector3.forward * 0.5f);
        Explode(Origin + new Vector3(0f, 1.1f, 0.3f), Vector3.back, direct);
        Assert.AreEqual(0f, Taken(direct));
    }

    [Test]
    public void ExplosionDamageCountsWithoutAccuracy()
    {
        Enemy(Origin + Vector3.forward * 1f);
        Explode(Origin + Vector3.up * 1.1f, Vector3.forward);

        Assert.Greater(GameState.RunDamage, 0f);
        Assert.AreEqual(0, GameState.RunHits, "взрыв не должен влиять на точность");
    }
}
