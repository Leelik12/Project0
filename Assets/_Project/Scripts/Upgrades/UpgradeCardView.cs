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

    // Надпись на кнопке остаётся градиентной, как на остальных кнопках интерфейса; меняются только цвета градиента
    private static readonly VertexGradient SoldGradient = new(new Color(0.55f, 0.55f, 0.6f), new Color(0.55f, 0.55f, 0.6f), new Color(0.3f, 0.3f, 0.35f), new Color(0.3f, 0.3f, 0.35f));
    private static readonly VertexGradient TooExpensiveGradient = new(new Color(1f, 0.4f, 0.35f), new Color(1f, 0.4f, 0.35f), new Color(0.6f, 0.05f, 0.15f), new Color(0.6f, 0.05f, 0.15f));
    private static readonly Vector4 ButtonTextMargin = new(14f, 6f, 14f, 6f); // надпись не заходит на неоновую рамку кнопки
    private const string CurrencySuffix = " €"; // знак эдди из шрифта интерфейса

    private VertexGradient affordableGradient;
    private bool gradientCaptured;

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
        if (buyButtonText != null)
        {
            CaptureSceneGradient();
            buyButtonText.margin = ButtonTextMargin;
            buyButtonText.alignment = TextAlignmentOptions.CenterGeoAligned; // по центру рисунка: у цифр и кистевых букв разная высота
        }
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
            SetButtonText("Куплено", SoldGradient);
        }
    }

    /// <summary>Обновить надпись на кнопке: цена и хватает ли денег.</summary>
    public void RefreshPrice(int eddies)
    {
        if (Sold || buyButtonText == null) return;
        string label = Price > 0 ? Price + CurrencySuffix : "Взять";
        SetButtonText(label, eddies >= Price ? affordableGradient : TooExpensiveGradient);
    }

    // Градиент из сцены — общий стиль кнопок. Берётся до первой перезаписи: RefreshPrice может прийти раньше Awake,
    // пока карточка ещё не активна
    private void CaptureSceneGradient()
    {
        if (gradientCaptured || buyButtonText == null) return;
        affordableGradient = buyButtonText.colorGradient;
        gradientCaptured = true;
    }

    private void SetButtonText(string label, VertexGradient gradient)
    {
        CaptureSceneGradient();
        buyButtonText.text = label;
        buyButtonText.color = Color.white; // цвет умножается на градиент
        buyButtonText.enableVertexGradient = true;
        buyButtonText.colorGradient = gradient;
    }
}
