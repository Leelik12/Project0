using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Карточка на стене магазина: показывает данные UpgradeCardData, цену и редкость (цвет рамки и названия).
/// Кнопка сообщает о покупке в UpgradeShop, сама ничего не применяет.
/// </summary>
public class UpgradeCardView : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Button buyButton;
    [SerializeField] private TMP_Text buyButtonText;
    [Tooltip("Рамка, которая красится в цвет редкости")]
    [SerializeField] private Outline frame;

    private static readonly Color SoldColor = new(0.4f, 0.4f, 0.4f);
    private static readonly Color TooExpensiveColor = new(1f, 0.35f, 0.3f);
    private static readonly Color AffordableColor = Color.white;

    public UpgradeCardData Card { get; private set; }
    public UpgradeRarity Rarity { get; private set; }
    public int Price { get; private set; }
    public bool Sold { get; private set; }
    public bool IsEmpty => Card == null;

    /// <summary>Нажата кнопка покупки.</summary>
    public event Action<UpgradeCardView> BuyClicked;

    private void Awake()
    {
        if (buyButton != null) buyButton.onClick.AddListener(() => BuyClicked?.Invoke(this));
    }

    public void Show(UpgradeCardData card, UpgradeRarity rarity, int price)
    {
        Card = card;
        Rarity = rarity;
        Price = price;
        Sold = false;
        gameObject.SetActive(card != null);
        if (card == null) return;

        UpgradeCatalog.RarityTier tier = UpgradeCatalog.Instance.GetTier(rarity);
        if (titleText != null)
        {
            titleText.enableVertexGradient = false;
            titleText.text = UiText.Widen(card.title);
            titleText.color = rarity == UpgradeRarity.Common ? Color.white : tier.color;
        }
        if (descriptionText != null)
        {
            string rarityLine = card.scalesWithRarity && rarity != UpgradeRarity.Common ? $"<b>{tier.name}</b>\n" : "";
            descriptionText.text = UiText.Widen(rarityLine + card.FormatDescription(tier.power));
        }
        if (iconImage != null)
        {
            iconImage.sprite = card.icon;
            iconImage.enabled = card.icon != null;
        }
        if (frame != null)
        {
            frame.enabled = rarity != UpgradeRarity.Common;
            frame.effectColor = tier.color;
        }
        if (buyButton != null) buyButton.interactable = true;
    }

    public void MarkSold()
    {
        Sold = true;
        if (buyButton != null) buyButton.interactable = false;
        if (buyButtonText != null)
        {
            buyButtonText.enableVertexGradient = false;
            buyButtonText.text = "Куплено";
            buyButtonText.color = SoldColor;
        }
    }

    /// <summary>Обновить надпись на кнопке: цена и хватает ли денег.</summary>
    public void RefreshPrice(int eddies)
    {
        if (Sold || buyButtonText == null) return;
        buyButtonText.enableVertexGradient = false;
        buyButtonText.text = Price > 0 ? UiText.Widen($"{Price} эдди") : "Взять";
        buyButtonText.color = eddies >= Price ? AffordableColor : TooExpensiveColor;
    }
}
