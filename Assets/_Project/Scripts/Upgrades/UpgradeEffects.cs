using UnityEngine;

/// <summary>
/// Применение купленных карточек к GameState. power — множитель редкости (1 у обычной карточки).
/// Проценты в value — буквально 25, 50; HP и секунды — абсолютные числа.
/// </summary>
public static class UpgradeEffects
{
    public static void Apply(UpgradeCardData card, float power)
    {
        float v = card.value * power;
        switch (card.effect)
        {
            // ---------- Оружие ----------
            case UpgradeEffect.ChangeGun: ChangeGun(card.gunCode); break;
            case UpgradeEffect.AddGrenade: GameState.CurrentGrenade = true; break;
            // FireRate — интервал между выстрелами, поэтому при прокачке уменьшается
            case UpgradeEffect.FireRate: GameState.BuffGunFireRate *= Mathf.Max(0.2f, 1f - v / 100f); break;
            case UpgradeEffect.Damage: GameState.BuffGunDamage *= 1f + v / 100f; break;
            case UpgradeEffect.MagazineSize: GameState.BuffGunMaxAmmo *= 1f + v / 100f; break;
            case UpgradeEffect.LaserSight: GameState.LaserSightUnlocked = true; break; // лазер работает и на сложном режиме
            case UpgradeEffect.Akimbo:
                if (IsPistol(GameState.CurrentGun)) GameState.Akimbo = true;
                else GameState.AkimboWas = true;
                break;

            // ---------- Импланты ----------
            case UpgradeEffect.HealthBoost:
                GameState.PlayerHPBuff += Mathf.RoundToInt(v);
                // Новая прослойка приходит целой. Если игрок уже в сцене, поднимаем и его живой максимум,
                // иначе следующее лечение в магазине обрезало бы здоровье по старому максимуму
                if (PlayerHealth.Current != null) PlayerHealth.Current.AddMaxHealth(Mathf.RoundToInt(v));
                else if (GameState.PlayerHealthLeft > 0f) GameState.PlayerHealthLeft += Mathf.RoundToInt(v);
                break;
            case UpgradeEffect.SpeedAfterDamage:
                GameState.SpeedBuffAfterDamage = true;
                GameState.SpeedTimeAfterDamage = Mathf.Max(GameState.SpeedTimeAfterDamage, card.secondaryValue);
                GameState.SpeedAfterDamageValue *= 1f + v / 100f;
                break;
            case UpgradeEffect.Propital:
                GameState.PropitalHeal = true;
                GameState.PropitalHealValue = Mathf.Max(GameState.PropitalHealValue, v);
                break;
            case UpgradeEffect.Sandevistan:
                GameState.Sandevistan = true;
                GameState.SandevistanTime = Mathf.Max(GameState.SandevistanTime, v);
                GameState.SandevistanTimeSlower = card.secondaryValue;
                break;
            case UpgradeEffect.Katana: GameState.Katana = true; break;
            case UpgradeEffect.StrongArms:
                GameState.StrongArms = true;
                GameState.StrongArmsMultiplier += v / 100f;
                break;
            case UpgradeEffect.StrongLegs:
                GameState.StrongLegs = true;
                GameState.StrongLegsMultiplier += v / 100f;
                break;
            case UpgradeEffect.OpticalCamo:
                GameState.OpticalCamo = true;
                GameState.OpticalCamoTime = Mathf.Max(GameState.OpticalCamoTime, v);
                break;
            case UpgradeEffect.ShortCircuit:
                GameState.ShortCircuit = true;
                GameState.ShortCircuitDamage = Mathf.Max(GameState.ShortCircuitDamage, v);
                GameState.ShortCircuitStunTime = Mathf.Max(GameState.ShortCircuitStunTime, card.secondaryValue * power);
                break;
            case UpgradeEffect.KillHeal: GameState.KillHeal += v; break;
            case UpgradeEffect.Regeneration: GameState.Regeneration += v; break;
            case UpgradeEffect.HeadshotAmmo: GameState.HeadshotAmmo += Mathf.RoundToInt(v); break;
            case UpgradeEffect.Armor: GameState.Armor = Mathf.Min(GameState.MaxArmor, GameState.Armor + v / 100f); break;

            // ---------- Модификаторы оружия ----------
            case UpgradeEffect.ModExplosive: GameState.ModExplosive += v / 100f; break;
            case UpgradeEffect.ModRicochet: GameState.ModRicochet = true; break;
            case UpgradeEffect.ModPiercing: GameState.ModPiercing = true; break;
            case UpgradeEffect.ModIncendiary: GameState.ModIncendiary += v; break;
            case UpgradeEffect.ModLeech: GameState.ModLeech += v / 100f; break;
        }
    }

    /// <summary>Есть ли смысл предлагать карточку: уже купленное уникальное не показываем.</summary>
    public static bool IsAvailable(UpgradeCardData card)
    {
        switch (card.effect)
        {
            case UpgradeEffect.ChangeGun: return GameState.CurrentGun != card.gunCode;
            case UpgradeEffect.AddGrenade: return !GameState.CurrentGrenade;
            case UpgradeEffect.LaserSight: return !GameState.LaserSightUnlocked;
            case UpgradeEffect.Akimbo: return !GameState.Akimbo && !GameState.AkimboWas;
            case UpgradeEffect.Propital: return !GameState.PropitalHeal;
            case UpgradeEffect.Sandevistan: return !GameState.Sandevistan;
            case UpgradeEffect.Katana: return !GameState.Katana;
            case UpgradeEffect.OpticalCamo: return !GameState.OpticalCamo;
            case UpgradeEffect.ShortCircuit: return !GameState.ShortCircuit;
            case UpgradeEffect.Armor: return GameState.Armor < GameState.MaxArmor;
            case UpgradeEffect.ModRicochet: return !GameState.ModRicochet;
            case UpgradeEffect.ModPiercing: return !GameState.ModPiercing;
            default: return true;
        }
    }

    // Акимбо работает только с пистолетами (коды 1 и 3). При смене на другое оружие оно «откладывается»
    // в AkimboWas и возвращается, когда игрок снова берёт пистолет.
    private static bool IsPistol(int gunCode) => gunCode == 1 || gunCode == 3;

    private static void ChangeGun(int gunCode)
    {
        GameState.CurrentGun = gunCode;

        if (IsPistol(gunCode))
        {
            if (GameState.AkimboWas || GameState.Akimbo) GameState.Akimbo = true;
            GameState.AkimboWas = false;
        }
        else
        {
            if (GameState.Akimbo) GameState.AkimboWas = true;
            GameState.Akimbo = false;
        }
    }
}
