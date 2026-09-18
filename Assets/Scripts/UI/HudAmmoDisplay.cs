using TMPro;
using UnityEngine;

// Показывает на HUD патроны оружия, которое игрок держит в руке.
// Повесить на объект с TextMeshProUGUI внутри HUD-канваса.
public class HudAmmoDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private string emptyHandsText = "--";

    private void Awake()
    {
        if (ammoText == null) ammoText = GetComponent<TextMeshProUGUI>();
    }

    private void Update()
    {
        if (ammoText == null) return;

        VRGun gun = VRGun.Held;
        string text = gun == null
            ? emptyHandsText
            : $"{Mathf.Max(0, (int)gun.currentAmmo)} / {(int)gun.maxAmmo}";

        if (ammoText.text != text) ammoText.text = text;
    }
}
