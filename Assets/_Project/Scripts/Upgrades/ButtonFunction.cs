using UnityEngine;

/// <summary>
/// Обработчики кнопок карточек в магазине апгрейдов. Все значения прокачки настраиваются в инспекторе.
/// Имя класса и имена методов привязаны к кнопкам в сцене Lobby — не переименовывать.
/// Проценты — буквально 10, 20, 50; «Value» — абсолютное число.
/// </summary>
public class ButtonFunction : MonoBehaviour
{
    [Header("Скорость стрельбы")]
    public int FireRateUpdatePercent;
    [Header("Урон оружия")]
    public int DamageUpdatePercent;
    [Header("Размер магазина")]
    public float MagazineValueUpdateValue;
    [Header("Прокачка здоровья игрока")]
    public int PlayerHpBuffValue;
    [Header("Ускорение после получения урона")]
    public int PlayerSpeedBuffAfterDamagePercent;
    public float PlayerSpeedBuffAfterDamageTime;
    [Header("Кнопка отхила")]
    public int HealHPPoints;
    [Header("Сандевистан")]
    public int SandewistanTimeWorkable;
    public float SandewistanTimeSlower; // множитель Time.timeScale (стандарт 1)
    [Header("Урон в ближке бафф")]
    public int MeleeDamageBuffPercent;
    [Header("Быстрые ноги")]
    public int SpeedBuffAllTimePercent;
    public int PlayerHandDamageBuffPercent;
    public int PlayerSpeedBuffPercent;

    // ---------- Оружие ----------

    // FireRate — интервал между выстрелами, поэтому при прокачке уменьшается
    public void UpdateFireRate() => GameState.BuffGunFireRate *= 1f - FireRateUpdatePercent / 100f;
    public void UpdateDamage() => GameState.BuffGunDamage *= 1f + DamageUpdatePercent / 100f;
    public void UpdateMagazineValue() => GameState.BuffGunMaxAmmo *= MagazineValueUpdateValue;
    public void UpdateLCU() => GameState.LaserSightUnlocked = true; // лазерный прицел работает и на сложном режиме
    public void UpdateAddGrenade() => GameState.CurrentGrenade = true;

    public void UpdateChangeCurrentGunTo0() => ChangeGun(0);
    public void UpdateChangeCurrentGunTo1() => ChangeGun(1);
    public void UpdateChangeCurrentGunTo2() => ChangeGun(2);
    public void UpdateChangeCurrentGunTo3() => ChangeGun(3);
    public void UpdateChangeCurrentGunTo5() => ChangeGun(5);
    public void UpdateChangeCurrentGunTo6() => ChangeGun(6);
    public void UpdateChangeCurrentGunTo7() => ChangeGun(7);
    public void UpdateChangeCurrentGunTo8() => ChangeGun(8);

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

    public void UpdateAkimbo()
    {
        if (IsPistol(GameState.CurrentGun)) GameState.Akimbo = true;
        else GameState.AkimboWas = true;
    }

    // ---------- Киберимпланты ----------

    public void UpdateHPBuff() => GameState.PlayerHPBuff += PlayerHpBuffValue;

    public void UpdateSpeedUp()
    {
        GameState.SpeedBuffAfterDamage = true;
        GameState.SpeedTimeAfterDamage = PlayerSpeedBuffAfterDamageTime;
        GameState.SpeedAfterDamageValue *= 1f + PlayerSpeedBuffAfterDamagePercent / 100f;
    }

    public void UpdateHealer()
    {
        GameState.PropitalHeal = true;
        GameState.PropitalHealValue = HealHPPoints;
    }

    public void UpdateSandevistan()
    {
        GameState.Sandevistan = true;
        GameState.SandevistanTime = SandewistanTimeWorkable;
        GameState.SandevistanTimeSlower = SandewistanTimeSlower;
    }

    public void UpdateKatana() => GameState.Katana = true;

    public void UpdateStrongArm()
    {
        GameState.StrongArms = true;
        GameState.StrongArmsKoef += MeleeDamageBuffPercent / 100f;
    }

    public void UpdateStrongLeg()
    {
        GameState.StrongLegs = true;
        GameState.StrongLegsKoef += SpeedBuffAllTimePercent / 100f;
    }
}
