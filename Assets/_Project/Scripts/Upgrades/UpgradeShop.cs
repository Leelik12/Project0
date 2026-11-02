using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Магазин апгрейдов между этажами. На каждом месте — случайная карточка своей категории и редкости.
/// Карточки стоят эдди, купить можно сколько хватит денег; ассортимент можно перебросить, здоровье — подлечить.
/// Лифт на следующий этаж открыт сразу: копить эдди тоже выбор.
/// </summary>
public class UpgradeShop : MonoBehaviour
{
    [Serializable]
    public class Place
    {
        public UpgradeCardView view;
        [Tooltip("Карточка любой категории")]
        public bool anyCategory;
        public UpgradeCategory category;
        [Tooltip("Место открывается, только если на прошлом этаже найден предмет")]
        public bool requiresFoundItem;
    }

    [SerializeField] private Place[] places;

    [Header("Услуги (необязательно)")]
    [SerializeField] private TMP_Text eddiesText;
    [SerializeField] private Button rerollButton;
    [SerializeField] private TMP_Text rerollText;
    [SerializeField] private Button healButton;
    [SerializeField] private TMP_Text healText;

    private UpgradeCatalog catalog;
    private PlayerHealth player;
    private int rerollCount;

    private int RerollPrice => catalog.rerollPrice + catalog.rerollPriceStep * rerollCount;

    private void Start()
    {
        catalog = UpgradeCatalog.Instance;
        player = FindAnyObjectByType<PlayerHealth>();
        GameState.UpdateLevelEnd = true;

        foreach (Place place in places)
        {
            if (place.view != null) place.view.BuyClicked += Buy;
        }
        if (rerollButton != null) rerollButton.onClick.AddListener(Reroll);
        if (healButton != null) healButton.onClick.AddListener(Heal);

        Roll(keepSold: false);
        Refresh();
    }

    private void Update()
    {
        // Здоровье и эдди меняются и вне магазина (Пропитал, регенерация) — кнопка лечения должна это видеть
        RefreshHeal();
    }

    // ---------- Ассортимент ----------

    private void Roll(bool keepSold)
    {
        var used = new HashSet<UpgradeCardData>();
        if (keepSold)
        {
            foreach (Place place in places)
            {
                if (place.view != null && place.view.Sold) used.Add(place.view.Card);
            }
        }

        foreach (Place place in places)
        {
            if (place.view == null || (keepSold && place.view.Sold)) continue;

            if (place.requiresFoundItem && !GameState.ItemPickedUp)
            {
                place.view.Show(null, UpgradeRarity.Common, 0);
                continue;
            }

            UpgradeCardData card = PickCard(place, used);
            if (card != null) used.Add(card);

            UpgradeRarity rarity = card != null && card.scalesWithRarity ? catalog.RollRarity() : UpgradeRarity.Common;
            place.view.Show(card, rarity, card != null ? catalog.GetPrice(card, rarity) : 0);
        }
    }

    private UpgradeCardData PickCard(Place place, HashSet<UpgradeCardData> used)
    {
        var candidates = new List<UpgradeCardData>();
        foreach (UpgradeCardData card in catalog.cards)
        {
            if (card == null || used.Contains(card)) continue;
            if (!place.anyCategory && card.category != place.category) continue;
            if (!UpgradeEffects.IsAvailable(card)) continue;
            candidates.Add(card);
        }
        return candidates.Count > 0 ? candidates[UnityEngine.Random.Range(0, candidates.Count)] : null;
    }

    // ---------- Покупки ----------

    private void Buy(UpgradeCardView view)
    {
        if (view.Sold || view.IsEmpty) return;
        if (!GameState.TrySpendEddies(view.Price))
        {
            Haptics.PulseBoth(0.2f, 0.1f); // не хватает денег
            return;
        }

        float power = view.Card.scalesWithRarity ? catalog.GetTier(view.Rarity).power : 1f;
        UpgradeEffects.Apply(view.Card, power);
        view.MarkSold();
        Refresh();
    }

    private void Reroll()
    {
        if (!GameState.TrySpendEddies(RerollPrice)) return;
        rerollCount++;
        Roll(keepSold: true);
        Refresh();
    }

    private void Heal()
    {
        if (player == null || player.GetHealthPercent() >= 1f) return;
        if (!GameState.TrySpendEddies(catalog.healPrice)) return;
        player.Heal(catalog.healAmount);
        Refresh();
    }

    // ---------- Интерфейс ----------

    private void Refresh()
    {
        foreach (Place place in places)
        {
            if (place.view != null) place.view.RefreshPrice(GameState.Eddies);
        }
        if (eddiesText != null) eddiesText.text = UiText.Widen($"Эдди: {GameState.Eddies}");
        if (rerollText != null) rerollText.text = UiText.Widen($"Перебросить: {RerollPrice}");
        if (rerollButton != null) rerollButton.interactable = GameState.Eddies >= RerollPrice;
        RefreshHeal();
    }

    private void RefreshHeal()
    {
        bool hurt = player != null && player.GetHealthPercent() < 1f;
        if (healButton != null) healButton.interactable = hurt && GameState.Eddies >= catalog.healPrice;
        if (healText != null) healText.text = UiText.Widen(hurt ? $"Лечение +{catalog.healAmount:0} HP: {catalog.healPrice}" : "Здоровье полное");
    }
}
