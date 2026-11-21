using TMPro;
using UnityEngine;

/// <summary>
/// Голографический счётчик на оружии: патроны в магазине и сколько врагов осталось на этаже.
/// Создаётся из VRGun, виден только пока оружие в руке. Краснеет, когда патронов меньше четверти.
/// </summary>
public class GunAmmoCounter : MonoBehaviour
{
    private const float InfiniteAmmo = 999f; // у лазерного оружия запас патронов условно бесконечный
    private const string UiFont = "Fonts/TechnoDeath UI SDF"; // шрифт интерфейса со стилизованными цифрами
    private static readonly Color NormalColor = new(0.55f, 1f, 1f, 0.95f);
    private static readonly Color LowColor = new(1f, 0.3f, 0.25f, 0.95f);

    private VRGun gun;
    private TextMeshPro text;
    private int shownAmmo = int.MinValue;
    private int shownEnemies = int.MinValue;

    /// <summary>Создаёт счётчик на оружии. anchor — точка крепления; null — над стволом за дулом.</summary>
    public static GunAmmoCounter Create(VRGun gun, Transform anchor)
    {
        var go = new GameObject("AmmoCounter");
        if (anchor != null)
        {
            go.transform.SetParent(anchor, false);
        }
        else
        {
            Transform fire = gun.firePoint;
            go.transform.SetParent(gun.transform, false);
            go.transform.SetPositionAndRotation(
                fire.position - fire.forward * 0.12f + fire.up * 0.045f,
                Quaternion.LookRotation(fire.forward, fire.up)); // читается со стороны стрелка
        }

        GunAmmoCounter counter = go.AddComponent<GunAmmoCounter>();
        counter.gun = gun;
        return counter;
    }

    private void Awake()
    {
        text = gameObject.AddComponent<TextMeshPro>();
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>(UiFont);
        if (font != null) text.font = font;
        text.fontSize = 0.22f;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.rectTransform.sizeDelta = new Vector2(0.12f, 0.05f);
        text.color = NormalColor;
        text.enabled = false;
    }

    private void LateUpdate()
    {
        bool visible = gun != null && VRGun.Held == gun;
        if (text.enabled != visible) text.enabled = visible;
        if (!visible) return;

        int ammo = gun.maxAmmo >= InfiniteAmmo ? -1 : Mathf.Max(0, (int)gun.currentAmmo);
        int enemies = LevelManager.EnemiesRemaining;
        if (ammo == shownAmmo && enemies == shownEnemies) return;

        shownAmmo = ammo;
        shownEnemies = enemies;

        string ammoText = ammo < 0 ? "--" : ammo.ToString();
        text.text = enemies > 0 ? $"{ammoText}\n<size=50%>врагов: {enemies}</size>" : ammoText;
        text.color = ammo >= 0 && ammo <= gun.maxAmmo * 0.25f ? LowColor : NormalColor;
    }
}
