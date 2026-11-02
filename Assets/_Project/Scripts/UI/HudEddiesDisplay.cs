using TMPro;
using UnityEngine;

/// <summary>Показывает на HUD, сколько у игрока эдди. Повесить на объект с TextMeshProUGUI.</summary>
public class HudEddiesDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI eddiesText;
    [SerializeField] private string format = "Эдди: {0}";

    private int shown = int.MinValue;

    private void Awake()
    {
        if (eddiesText == null) eddiesText = GetComponent<TextMeshProUGUI>();
    }

    private void Update()
    {
        if (eddiesText == null || shown == GameState.Eddies) return;
        shown = GameState.Eddies;
        eddiesText.text = UiText.Widen(string.Format(format, shown));
    }
}
