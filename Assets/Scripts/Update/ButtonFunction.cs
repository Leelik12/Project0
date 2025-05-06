using UnityEngine;

public class ButtonFunction : MonoBehaviour
{ //если написано процент, то буквально 10 20 50, если вэлью, то число 1 2 3
    public int FireRateUpdatePercent;
    public int DamageUpdatePercent;
    public int MagazineValueUpdateValue;
    public int PlayerHpBuffValue;
    public int PlayerSpeedBuffAfterDamagePercent;

    public int MeleeDamageBuffPercent;
    public int SpeedBuffAllTimePercent;

    public int PlayerHandDamageBuffPercent;
    public int PlayerSpeedBuffPercent;
    //Сверху подкапотка прокачки, все изменения прокачки происходят через эти настройки
    //Не забудь поменять текст на карточках при изменении этих значений
    public void ShablonButton()
    {
        Debug.Log("");

        Debug.Log("");
    }
    public void UpdateFireRate() //Все хорошо, при прокачке значение должно падать. FireRate - время между выстрелами
    {
        Debug.Log("Скорострельность была " + StaticHolder.CurrentGunFireRate);
        StaticHolder.CurrentGunFireRate = StaticHolder.CurrentGunFireRate * (1 - FireRateUpdatePercent / 100);
        Debug.Log("Скорострельность увеличена до " + StaticHolder.CurrentGunFireRate);
    }
    public void UpdateDamage()
    {
        Debug.Log("Урон был " + StaticHolder.CurrentGunDamage);
        StaticHolder.CurrentGunDamage = StaticHolder.CurrentGunDamage * (1 + DamageUpdatePercent / 100);
        Debug.Log("Урон увеличен до " + StaticHolder.CurrentGunDamage);
    }
    public void UpdateMagazineValue()
    {
        Debug.Log("Размер магазина был " + StaticHolder.CurrentGunMaxAmmo);
        StaticHolder.CurrentGunMaxAmmo = StaticHolder.CurrentGunMaxAmmo * (MagazineValueUpdateValue);
        Debug.Log("Размер магазина увеличен до " + StaticHolder.CurrentGunMaxAmmo);
    }
    public void UpdateLCU()
    {
        StaticHolder.Difficulty = false;
        Debug.Log("ЛЦУ добавлен");
    }
    public void UpdateChangeCurrentGunTo0()
    {
        Debug.Log("Оружие было " + StaticHolder.CurrentGun);
        StaticHolder.CurrentGun = 0;
        Debug.Log("Оружие стало " + StaticHolder.CurrentGun);
    }
    public void UpdateChangeCurrentGunTo1()
    {
        Debug.Log("Оружие было " + StaticHolder.CurrentGun);
        StaticHolder.CurrentGun = 1;
        Debug.Log("Оружие стало " + StaticHolder.CurrentGun);
    }
    public void UpdateChangeCurrentGunTo2()
    {
        Debug.Log("Оружие было " + StaticHolder.CurrentGun);
        StaticHolder.CurrentGun = 2;
        Debug.Log("Оружие стало " + StaticHolder.CurrentGun);
    }
    public void UpdateChangeCurrentGunTo3()
    {
        Debug.Log("Оружие было " + StaticHolder.CurrentGun);
        StaticHolder.CurrentGun = 3;
        Debug.Log("Оружие стало " + StaticHolder.CurrentGun);
    }
    public void UpdateChangeCurrentGunTo5()
    {
        Debug.Log("Оружие было " + StaticHolder.CurrentGun);
        StaticHolder.CurrentGun = 5;
        Debug.Log("Оружие стало " + StaticHolder.CurrentGun);
    }
    public void UpdateChangeCurrentGunTo6()
    {
        Debug.Log("Оружие было " + StaticHolder.CurrentGun);
        StaticHolder.CurrentGun = 6;
        Debug.Log("Оружие стало " + StaticHolder.CurrentGun);
    }
    public void UpdateChangeCurrentGunTo7()
    {
        Debug.Log("Оружие было " + StaticHolder.CurrentGun);
        StaticHolder.CurrentGun = 7;
        Debug.Log("Оружие стало " + StaticHolder.CurrentGun);
    }
    public void UpdateChangeCurrentGunTo8()
    {
        Debug.Log("Оружие было " + StaticHolder.CurrentGun);
        StaticHolder.CurrentGun = 8;
        Debug.Log("Оружие стало " + StaticHolder.CurrentGun);
    }
    public void UpdateAddGrenade()
    {
        Debug.Log("Гранаты в инвентаре" + StaticHolder.CurrentGrenade);
        StaticHolder.CurrentGrenade = true;
        Debug.Log("Гранаты в инвентаре" + StaticHolder.CurrentGrenade);
    }
    public void UpdateHPBuff()
    {
        Debug.Log("Текущее максимальное здоровье игрока - " + StaticHolder.PlayerHP);
        StaticHolder.PlayerHP += PlayerHpBuffValue;
        Debug.Log("Новое максимальное здоровье игрока - " + StaticHolder.PlayerHP);
    }
    public void UpdateSpeedUp()
    {
        Debug.Log("Текущая максимальная скорость игрока - " + StaticHolder.PlayerSpeed);
        StaticHolder.SpeedBuffAfterDamage = true;
        StaticHolder.SpeedAfterDamageValue = StaticHolder.SpeedAfterDamageValue * (1 + PlayerSpeedBuffAfterDamagePercent / 100);
        Debug.Log("Новая максимальная скорость игрока - " + StaticHolder.PlayerSpeed);

    }
    public void UpdateHealer()
    {
        Debug.Log("Пропитал есть - " + StaticHolder.PropitalHeal);
        StaticHolder.PropitalHeal = true;
        Debug.Log("Пропитал теперь - " + StaticHolder.PropitalHeal);
    }
    public void UpdateSandevistan()
    {
        Debug.Log("Сандевистан есть - " + StaticHolder.Sandevistan);
        StaticHolder.Sandevistan = true;
        Debug.Log("Сандевистан теперь - " + StaticHolder.Sandevistan);
    }
    public void UpdateAkimbo()
    {
        Debug.Log("Акимбо есть - " + StaticHolder.Akimbo);
        StaticHolder.Akimbo = true;
        Debug.Log("Акимбо теперь - " + StaticHolder.Akimbo);
    }
    public void UpdateKatana()
    {
        Debug.Log("Катана есть - " + StaticHolder.Katana);
        StaticHolder.Katana = true;
        Debug.Log("Катана теперь - " + StaticHolder.Katana);
    }
    public void UpdateStrongArm()
    {
        Debug.Log("Сильные руки есть - " + StaticHolder.StrongArms);
        StaticHolder.StrongArms = true;
        StaticHolder.StrongArmsKoef += 1 + MeleeDamageBuffPercent/100;
        Debug.Log("Сильные руки теперь - " + StaticHolder.StrongArms);
        Debug.Log("Коэф урона ближки теперь - " + StaticHolder.StrongArmsKoef);
    }
    public void UpdateStrongLeg()
    {
        Debug.Log("Сильные ноги есть - " + StaticHolder.StrongLegs);
        StaticHolder.StrongLegs = true;
        StaticHolder.StrongLegsKoef += 1 + SpeedBuffAllTimePercent / 100;
        Debug.Log("Сильные ноги теперь - " + StaticHolder.StrongLegs);
        Debug.Log("Коэф скорости все время теперь - " + StaticHolder.StrongArmsKoef);
    }
    public void UpdateMeleeBerserk()
    {
        Debug.Log("");

        Debug.Log("");
    }
}
