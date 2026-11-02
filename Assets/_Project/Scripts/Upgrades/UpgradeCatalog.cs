using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Все карточки магазина, таблица редкостей, цены услуг и награды в эдди.
/// Ассет лежит в Resources/UpgradeCatalog и подхватывается из кода.
/// </summary>
[CreateAssetMenu(menuName = "Project0/Upgrade Catalog", fileName = "UpgradeCatalog")]
public class UpgradeCatalog : ScriptableObject
{
    private const string ResourcePath = "UpgradeCatalog";
    private static UpgradeCatalog instance;

    [Serializable]
    public struct RarityTier
    {
        public string name;
        [Tooltip("Относительный шанс выпадения")]
        public float weight;
        [Tooltip("Множитель силы эффекта")]
        public float power;
        [Tooltip("Множитель цены")]
        public float priceMultiplier;
        public Color color;
    }

    public List<UpgradeCardData> cards = new();

    [Header("Редкость (по порядку: обычная, редкая, легендарная)")]
    public RarityTier[] rarities =
    {
        new() { name = "Обычная", weight = 72f, power = 1f, priceMultiplier = 1f, color = new Color(0.8f, 0.85f, 0.9f) },
        new() { name = "Редкая", weight = 22f, power = 1.5f, priceMultiplier = 1.5f, color = new Color(0.3f, 0.65f, 1f) },
        new() { name = "Легендарная", weight = 6f, power = 2f, priceMultiplier = 2.2f, color = new Color(1f, 0.6f, 0.1f) },
    };

    [Header("Услуги магазина")]
    public int rerollPrice = 20;
    [Tooltip("На сколько дорожает каждый следующий переброс в этом магазине")]
    public int rerollPriceStep = 10;
    public int healPrice = 30;
    public float healAmount = 50f;

    [Header("Награды в эдди")]
    public int killReward = 10;
    public int headshotKillBonus = 5;
    public int eliteKillBonus = 40;
    public int floorClearReward = 30;
    public int itemPickupReward = 40;

    public static UpgradeCatalog Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<UpgradeCatalog>(ResourcePath);
                if (instance == null) instance = CreateInstance<UpgradeCatalog>();
            }
            return instance;
        }
    }

    public RarityTier GetTier(UpgradeRarity rarity) => rarities[Mathf.Clamp((int)rarity, 0, rarities.Length - 1)];

    public UpgradeRarity RollRarity()
    {
        float total = 0f;
        foreach (RarityTier tier in rarities) total += tier.weight;

        float roll = UnityEngine.Random.value * total;
        for (int i = 0; i < rarities.Length; i++)
        {
            roll -= rarities[i].weight;
            if (roll <= 0f) return (UpgradeRarity)i;
        }
        return UpgradeRarity.Common;
    }

    public int GetPrice(UpgradeCardData card, UpgradeRarity rarity) =>
        Mathf.RoundToInt(card.price * GetTier(rarity).priceMultiplier / 5f) * 5;
}
