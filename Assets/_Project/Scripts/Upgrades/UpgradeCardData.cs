using UnityEngine;

/// <summary>Где карточка может выпасть в магазине.</summary>
public enum UpgradeCategory
{
    Weapon,         // новое оружие и граната
    Implant,        // киберимпланты: активные и пассивные
    WeaponUpgrade,  // прокачка и модификаторы оружия
}

/// <summary>Что делает карточка при покупке (см. UpgradeEffects).</summary>
public enum UpgradeEffect
{
    ChangeGun,
    AddGrenade,
    FireRate,
    Damage,
    MagazineSize,
    LaserSight,
    Akimbo,
    HealthBoost,
    SpeedAfterDamage,
    Propital,
    Sandevistan,
    Katana,
    StrongArms,
    StrongLegs,
    OpticalCamo,
    ShortCircuit,
    KillHeal,
    Regeneration,
    HeadshotAmmo,
    Armor,
    ModExplosive,
    ModRicochet,
    ModPiercing,
    ModIncendiary,
    ModLeech,
    // Новые значения только в конец: в ассетах карточек эффект хранится числом
    AddGrenadeEmp,
    AddGrenadeSmoke,
    AddGrenadeSticky,
}

public enum UpgradeRarity
{
    Common,
    Rare,
    Legendary,
}

/// <summary>
/// Карточка магазина апгрейдов: текст, картинка, цена и эффект. Сила эффекта (value) умножается на множитель
/// редкости, если карточка его поддерживает. В описании {0} заменяется на итоговое значение.
/// </summary>
[CreateAssetMenu(menuName = "Project0/Upgrade Card", fileName = "Card")]
public class UpgradeCardData : ScriptableObject
{
    public string title;
    [TextArea(2, 5)] public string description;
    public Sprite icon;

    [Header("Магазин")]
    public UpgradeCategory category;
    [Min(0)] public int price = 50;
    [Tooltip("Редкость усиливает эффект и цену. Выключено — карточка всегда обычная")]
    public bool scalesWithRarity = true;

    [Header("Эффект")]
    public UpgradeEffect effect;
    [Tooltip("Основное значение: проценты, HP, секунды — зависит от эффекта")]
    public float value;
    [Tooltip("Дополнительное значение (например, длительность оглушения или замедление времени)")]
    public float secondaryValue;
    [Tooltip("Код оружия для ChangeGun (см. GameState)")]
    public int gunCode;

    /// <summary>Описание с подставленным значением с учётом редкости.</summary>
    public string FormatDescription(float power)
    {
        float shown = value * power;
        string number = Mathf.Approximately(shown, Mathf.Round(shown)) ? shown.ToString("0") : shown.ToString("0.#");
        return description.Contains("{0}") ? description.Replace("{0}", number) : description;
    }
}
