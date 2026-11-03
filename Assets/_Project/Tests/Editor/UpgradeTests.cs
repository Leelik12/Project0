using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>Каталог карточек, цены, редкость и действие каждой карточки на GameState.</summary>
public class UpgradeTests
{
    private GameStateSandbox sandbox;
    private readonly List<UnityEngine.Object> created = new();

    [SetUp]
    public void SetUp()
    {
        sandbox = new GameStateSandbox();
        GameState.ResetRun();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (UnityEngine.Object obj in created) UnityEngine.Object.DestroyImmediate(obj);
        created.Clear();
        sandbox.Dispose();
    }

    private static UpgradeCatalog Catalog => UpgradeCatalog.Instance;

    private UpgradeCardData Card(UpgradeEffect effect, float value, float secondary = 0f, int gunCode = 0)
    {
        var card = ScriptableObject.CreateInstance<UpgradeCardData>();
        card.title = effect.ToString();
        card.description = "{0}";
        card.effect = effect;
        card.value = value;
        card.secondaryValue = secondary;
        card.gunCode = gunCode;
        created.Add(card);
        return card;
    }

    // ---------- Каталог ----------

    [Test]
    public void Catalog_IsLoadedFromResources()
    {
        Assert.IsNotNull(Resources.Load<UpgradeCatalog>("UpgradeCatalog"), "нет Resources/UpgradeCatalog.asset");
        Assert.IsNotEmpty(Catalog.cards);
    }

    [Test]
    public void Catalog_CardsAreFilledIn()
    {
        var titles = new HashSet<string>();
        foreach (UpgradeCardData card in Catalog.cards)
        {
            Assert.IsNotNull(card, "пустой слот в списке карточек");
            Assert.IsFalse(string.IsNullOrWhiteSpace(card.title), $"{card.name}: нет названия");
            Assert.IsTrue(titles.Add(card.title), $"{card.name}: повтор названия «{card.title}»");
            Assert.IsFalse(string.IsNullOrWhiteSpace(card.description), $"{card.name}: нет описания");
            Assert.IsNotNull(card.icon, $"{card.name}: нет иконки");
            Assert.Greater(card.price, 0, $"{card.name}: бесплатная карточка");
        }
    }

    [Test]
    public void Catalog_EffectValuesMakeSense()
    {
        foreach (UpgradeCardData card in Catalog.cards)
        {
            switch (card.effect)
            {
                case UpgradeEffect.ChangeGun:
                    Assert.That(card.gunCode, Is.InRange(0, 8), $"{card.name}: неизвестный код оружия");
                    break;
                case UpgradeEffect.Sandevistan:
                    Assert.That(card.secondaryValue, Is.GreaterThan(0f).And.LessThan(1f), $"{card.name}: замедление времени должно быть в (0; 1)");
                    Assert.Greater(card.value, 0f, $"{card.name}: длительность");
                    break;
                case UpgradeEffect.ShortCircuit:
                    Assert.Greater(card.secondaryValue, 0f, $"{card.name}: длительность оглушения");
                    break;
                case UpgradeEffect.SpeedAfterDamage:
                    Assert.Greater(card.secondaryValue, 0f, $"{card.name}: длительность ускорения");
                    break;
                case UpgradeEffect.FireRate:
                case UpgradeEffect.Damage:
                case UpgradeEffect.MagazineSize:
                case UpgradeEffect.HealthBoost:
                case UpgradeEffect.Propital:
                case UpgradeEffect.OpticalCamo:
                case UpgradeEffect.KillHeal:
                case UpgradeEffect.Regeneration:
                case UpgradeEffect.HeadshotAmmo:
                case UpgradeEffect.Armor:
                case UpgradeEffect.ModExplosive:
                case UpgradeEffect.ModIncendiary:
                case UpgradeEffect.ModLeech:
                case UpgradeEffect.StrongArms:
                case UpgradeEffect.StrongLegs:
                    Assert.Greater(card.value, 0f, $"{card.name}: эффект без силы");
                    break;
            }
        }
    }

    [Test]
    public void Catalog_EveryShopCategoryHasCards()
    {
        foreach (UpgradeCategory category in Enum.GetValues(typeof(UpgradeCategory)))
        {
            Assert.IsTrue(Catalog.cards.Exists(c => c != null && c.category == category), $"нет карточек категории {category}");
        }
    }

    [Test]
    public void Prices_AreMultiplesOfFiveAndGrowWithRarity()
    {
        foreach (UpgradeCardData card in Catalog.cards)
        {
            int previous = -1;
            foreach (UpgradeRarity rarity in Enum.GetValues(typeof(UpgradeRarity)))
            {
                int price = Catalog.GetPrice(card, rarity);
                Assert.AreEqual(0, price % 5, $"{card.name} {rarity}: {price}");
                Assert.GreaterOrEqual(price, previous, $"{card.name}: редкая дешевле обычной");
                previous = price;
            }
        }
    }

    [Test]
    public void RollRarity_FollowsWeights()
    {
        UnityEngine.Random.State state = UnityEngine.Random.state;
        UnityEngine.Random.InitState(12345);
        var counts = new int[3];
        const int rolls = 20000;
        for (int i = 0; i < rolls; i++) counts[(int)Catalog.RollRarity()]++;
        UnityEngine.Random.state = state;

        float total = 0f;
        foreach (UpgradeCatalog.RarityTier tier in Catalog.rarities) total += tier.weight;
        for (int i = 0; i < 3; i++)
        {
            Assert.AreEqual(Catalog.rarities[i].weight / total, counts[i] / (float)rolls, 0.02f, Catalog.rarities[i].name);
        }
    }

    [Test]
    public void FormatDescription_SubstitutesScaledValue()
    {
        UpgradeCardData card = Card(UpgradeEffect.Damage, 25f);
        card.description = "Урон +{0}%";
        Assert.AreEqual("Урон +25%", card.FormatDescription(1f));
        Assert.AreEqual("Урон +37.5%", card.FormatDescription(1.5f).Replace(',', '.'));
        Assert.AreEqual("Урон +50%", card.FormatDescription(2f));
    }

    // ---------- Эффекты ----------

    [Test]
    public void EveryCatalogCard_ChangesGameState()
    {
        foreach (UpgradeCardData card in Catalog.cards)
        {
            GameState.ResetRun();
            if (card.effect == UpgradeEffect.ChangeGun && card.gunCode == 0) GameState.CurrentGun = 1;
            string before = Describe();
            UpgradeEffects.Apply(card, 1f);
            Assert.AreNotEqual(before, Describe(), $"{card.name} ({card.effect}) ничего не изменила");
        }
    }

    [Test]
    public void UniqueCards_DisappearFromShopAfterPurchase()
    {
        foreach (UpgradeCardData card in Catalog.cards)
        {
            GameState.ResetRun();
            if (card.effect == UpgradeEffect.ChangeGun && card.gunCode == 0) GameState.CurrentGun = 1;
            Assert.IsTrue(UpgradeEffects.IsAvailable(card), $"{card.name} недоступна в новом забеге");

            UpgradeEffects.Apply(card, 1f);
            bool unique = card.effect is UpgradeEffect.ChangeGun or UpgradeEffect.AddGrenade or UpgradeEffect.LaserSight
                or UpgradeEffect.Akimbo or UpgradeEffect.Propital or UpgradeEffect.Sandevistan or UpgradeEffect.Katana
                or UpgradeEffect.OpticalCamo or UpgradeEffect.ShortCircuit or UpgradeEffect.ModRicochet or UpgradeEffect.ModPiercing;
            if (unique) Assert.IsFalse(UpgradeEffects.IsAvailable(card), $"{card.name} можно купить второй раз");
        }
    }

    [Test]
    public void Armor_IsCapped()
    {
        UpgradeCardData armor = Card(UpgradeEffect.Armor, 25f);
        for (int i = 0; i < 5; i++) UpgradeEffects.Apply(armor, 2f);
        Assert.AreEqual(GameState.MaxArmor, GameState.Armor, 1e-5f);
        Assert.IsFalse(UpgradeEffects.IsAvailable(armor));
    }

    [Test]
    public void FireRate_NeverDropsBelowFloor()
    {
        UpgradeEffects.Apply(Card(UpgradeEffect.FireRate, 200f), 1f);
        Assert.AreEqual(0.2f, GameState.BuffGunFireRate, 1e-5f);
    }

    [Test]
    public void ActiveImplants_ScaleWithRarity()
    {
        UpgradeEffects.Apply(Card(UpgradeEffect.Propital, 40f), 2f);
        UpgradeEffects.Apply(Card(UpgradeEffect.ShortCircuit, 30f, 2f), 1.5f);
        Assert.AreEqual(80f, GameState.PropitalHealValue);
        Assert.AreEqual(45f, GameState.ShortCircuitDamage);
        Assert.AreEqual(3f, GameState.ShortCircuitStunTime);
    }

    [Test]
    public void HealthBoost_WithoutPlayer_AddsToCarriedHealth()
    {
        GameState.PlayerHealthLeft = 60f;
        UpgradeEffects.Apply(Card(UpgradeEffect.HealthBoost, 25f), 1f);
        Assert.AreEqual(25, GameState.PlayerHPBuff);
        Assert.AreEqual(85f, GameState.PlayerHealthLeft);

        GameState.PlayerHealthLeft = -1f; // полное здоровье так и остаётся полным
        UpgradeEffects.Apply(Card(UpgradeEffect.HealthBoost, 25f), 1f);
        Assert.AreEqual(-1f, GameState.PlayerHealthLeft);
    }

    [Test]
    public void Akimbo_FollowsPistols()
    {
        UpgradeCardData akimbo = Card(UpgradeEffect.Akimbo, 0f);
        GameState.CurrentGun = 0; // АКМ
        UpgradeEffects.Apply(akimbo, 1f);
        Assert.IsFalse(GameState.Akimbo);
        Assert.IsTrue(GameState.AkimboWas, "акимбо откладывается до пистолета");

        UpgradeEffects.Apply(Card(UpgradeEffect.ChangeGun, 0f, gunCode: 3), 1f);
        Assert.IsTrue(GameState.Akimbo);
        Assert.IsFalse(GameState.AkimboWas);

        UpgradeEffects.Apply(Card(UpgradeEffect.ChangeGun, 0f, gunCode: 6), 1f);
        Assert.IsFalse(GameState.Akimbo);
        Assert.IsTrue(GameState.AkimboWas);
    }

    // Все поля GameState одной строкой — чтобы заметить любое изменение
    private static string Describe()
    {
        var sb = new System.Text.StringBuilder();
        foreach (var field in typeof(GameState).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
        {
            if (field.IsLiteral || field.Name == nameof(GameState.LastRun)) continue;
            sb.Append(field.Name).Append('=').Append(field.GetValue(null)).Append(';');
        }
        return sb.ToString();
    }
}
