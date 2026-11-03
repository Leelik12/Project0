using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Логика магазина без сцены: места с карточками собираются в коде, закрытые методы зовутся через reflection.
/// Раскладка как в Lobby: оружие, имплант, прокачка, любая карточка и место за найденный предмет.
/// </summary>
public class UpgradeShopTests
{
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

    private GameStateSandbox sandbox;
    private GameObject root;
    private UpgradeShop shop;
    private UpgradeCardView[] views;

    [SetUp]
    public void SetUp()
    {
        sandbox = new GameStateSandbox();
        GameState.ResetRun();

        root = new GameObject("ShopTest");
        shop = root.AddComponent<UpgradeShop>();
        views = new UpgradeCardView[5];
        var places = new UpgradeShop.Place[5];
        for (int i = 0; i < places.Length; i++)
        {
            var go = new GameObject("Card" + (i + 1));
            go.transform.SetParent(root.transform);
            views[i] = go.AddComponent<UpgradeCardView>();
            places[i] = new UpgradeShop.Place { view = views[i] };
        }
        places[0].category = UpgradeCategory.Weapon;
        places[1].category = UpgradeCategory.Implant;
        places[2].category = UpgradeCategory.WeaponUpgrade;
        places[3].anyCategory = true;
        places[4].anyCategory = true;
        places[4].requiresFoundItem = true;

        typeof(UpgradeShop).GetField("places", Private).SetValue(shop, places);
        typeof(UpgradeShop).GetField("catalog", Private).SetValue(shop, UpgradeCatalog.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(root);
        sandbox.Dispose();
    }

    private void Call(string method, params object[] args) => typeof(UpgradeShop).GetMethod(method, Private).Invoke(shop, args);

    [Test]
    public void Roll_GivesDistinctCardsOfTheRightCategory()
    {
        for (int attempt = 0; attempt < 50; attempt++)
        {
            Call("Roll", false);
            var seen = new HashSet<UpgradeCardData>();
            for (int i = 0; i < 4; i++)
            {
                Assert.IsFalse(views[i].IsEmpty, $"место {i + 1} пустое");
                Assert.IsTrue(seen.Add(views[i].Card), $"карточка {views[i].Card.name} выпала дважды");
                Assert.IsTrue(UpgradeEffects.IsAvailable(views[i].Card));
                Assert.Greater(views[i].Price, 0);
            }
            Assert.AreEqual(UpgradeCategory.Weapon, views[0].Card.category);
            Assert.AreEqual(UpgradeCategory.Implant, views[1].Card.category);
            Assert.AreEqual(UpgradeCategory.WeaponUpgrade, views[2].Card.category);
        }
    }

    [Test]
    public void Roll_ItemPlaceOpensOnlyAfterFoundItem()
    {
        GameState.ItemPickedUp = false;
        Call("Roll", false);
        Assert.IsTrue(views[4].IsEmpty);
        Assert.IsFalse(views[4].gameObject.activeSelf);

        GameState.ItemPickedUp = true;
        Call("Roll", false);
        Assert.IsFalse(views[4].IsEmpty);
        Assert.IsTrue(views[4].gameObject.activeSelf);
    }

    [Test]
    public void Buy_SpendsEddiesAndAppliesCard()
    {
        Call("Roll", false);
        UpgradeCardView view = views[1];
        GameState.Eddies = view.Price + 7;

        Call("Buy", view);

        Assert.AreEqual(7, GameState.Eddies);
        Assert.IsTrue(view.Sold);
        Call("Buy", view); // повторное нажатие на проданную карточку
        Assert.AreEqual(7, GameState.Eddies);
    }

    [Test]
    public void Buy_WithoutMoneyDoesNothing()
    {
        Call("Roll", false);
        UpgradeCardView view = views[0];
        GameState.Eddies = view.Price - 1;
        int gun = GameState.CurrentGun;

        Call("Buy", view);

        Assert.AreEqual(view.Price - 1, GameState.Eddies);
        Assert.IsFalse(view.Sold);
        Assert.AreEqual(gun, GameState.CurrentGun);
    }

    [Test]
    public void Reroll_KeepsSoldCardsAndGetsPricier()
    {
        UpgradeCatalog catalog = UpgradeCatalog.Instance;
        Call("Roll", false);
        GameState.Eddies = 10000;
        Call("Buy", views[2]);
        UpgradeCardData sold = views[2].Card;

        int before = GameState.Eddies;
        Call("Reroll");
        Assert.AreEqual(before - catalog.rerollPrice, GameState.Eddies);
        Assert.IsTrue(views[2].Sold);
        Assert.AreSame(sold, views[2].Card);
        for (int i = 0; i < 4; i++)
        {
            if (i != 2) Assert.AreNotSame(sold, views[i].Card, "проданная карточка выпала снова");
        }

        before = GameState.Eddies;
        Call("Reroll");
        Assert.AreEqual(before - catalog.rerollPrice - catalog.rerollPriceStep, GameState.Eddies);
    }

    [Test]
    public void Reroll_WithoutMoneyDoesNothing()
    {
        Call("Roll", false);
        UpgradeCardData first = views[0].Card;
        GameState.Eddies = UpgradeCatalog.Instance.rerollPrice - 1;
        Call("Reroll");
        Assert.AreSame(first, views[0].Card);
        Assert.AreEqual(UpgradeCatalog.Instance.rerollPrice - 1, GameState.Eddies);
    }
}
