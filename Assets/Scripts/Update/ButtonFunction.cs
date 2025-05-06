using UnityEngine;

public class ButtonFunction : MonoBehaviour
{ //если написано процент, то буквально 10 20 50, если вэлью, то число 1 2 3
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
    public float SandewistanTimeSlower;//стандартное время в юнити 1


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
    {//все работает
        Debug.Log("Скорострельность была " + StaticHolder.BuffGunFireRate);
        StaticHolder.BuffGunFireRate = StaticHolder.BuffGunFireRate * (1f - FireRateUpdatePercent / 100f);
        Debug.Log("Скорострельность увеличена до " + StaticHolder.BuffGunFireRate);
    }
    public void UpdateDamage()
    {//все работает
        Debug.Log("Урон был " + StaticHolder.BuffGunDamage);
        StaticHolder.BuffGunDamage = StaticHolder.BuffGunDamage * (1f + DamageUpdatePercent/100f);
        Debug.Log("Урон увеличен до " + StaticHolder.BuffGunDamage);
    }
    public void UpdateMagazineValue()
    {//все работает
        Debug.Log("Размер магазина был " + StaticHolder.BuffGunMaxAmmo);
        StaticHolder.BuffGunMaxAmmo = StaticHolder.BuffGunMaxAmmo * (MagazineValueUpdateValue);
        Debug.Log("Размер магазина увеличен до " + StaticHolder.BuffGunMaxAmmo);
    }
    public void UpdateLCU()
    {//все работает
        StaticHolder.Difficulty = false;
        Debug.Log("ЛЦУ добавлен");
    }
    public void UpdateChangeCurrentGunTo0()
    {
        Debug.Log("Оружие было " + StaticHolder.CurrentGun);
        StaticHolder.CurrentGun = 0;
        if (StaticHolder.Akimbo)
        {
            StaticHolder.AkimboWas = true;
        }
        StaticHolder.Akimbo = false;
        Debug.Log("Оружие стало " + StaticHolder.CurrentGun);
    }
    public void UpdateChangeCurrentGunTo1()
    {
        Debug.Log("Оружие было " + StaticHolder.CurrentGun);
        StaticHolder.CurrentGun = 1;
        if (StaticHolder.AkimboWas || StaticHolder.Akimbo)
        {
            StaticHolder.Akimbo = true;
        }
        StaticHolder.AkimboWas = false;
        Debug.Log("Оружие стало " + StaticHolder.CurrentGun);
    }
    public void UpdateChangeCurrentGunTo2()
    {
        Debug.Log("Оружие было " + StaticHolder.CurrentGun);
        StaticHolder.CurrentGun = 2;
        if (StaticHolder.Akimbo)
        {
            StaticHolder.AkimboWas = true;
        }
        StaticHolder.Akimbo = false;
        Debug.Log("Оружие стало " + StaticHolder.CurrentGun);
    }
    public void UpdateChangeCurrentGunTo3()
    {
        Debug.Log("Оружие было " + StaticHolder.CurrentGun);
        StaticHolder.CurrentGun = 3;
        if (StaticHolder.AkimboWas || StaticHolder.Akimbo)
        {
            StaticHolder.Akimbo = true;
        }
        StaticHolder.AkimboWas = false;
        Debug.Log("Оружие стало " + StaticHolder.CurrentGun);
    }
    public void UpdateChangeCurrentGunTo5()
    {
        Debug.Log("Оружие было " + StaticHolder.CurrentGun);
        StaticHolder.CurrentGun = 5;
        if (StaticHolder.Akimbo)
        {
            StaticHolder.AkimboWas = true;
        }
        StaticHolder.Akimbo = false;
        Debug.Log("Оружие стало " + StaticHolder.CurrentGun);
    }
    public void UpdateChangeCurrentGunTo6()
    {
        Debug.Log("Оружие было " + StaticHolder.CurrentGun);
        StaticHolder.CurrentGun = 6;
        if (StaticHolder.Akimbo)
        {
            StaticHolder.AkimboWas = true;
        }
        StaticHolder.Akimbo = false;
        Debug.Log("Оружие стало " + StaticHolder.CurrentGun);
    }
    public void UpdateChangeCurrentGunTo7()
    {
        Debug.Log("Оружие было " + StaticHolder.CurrentGun);
        StaticHolder.CurrentGun = 7;
        if (StaticHolder.Akimbo)
        {
            StaticHolder.AkimboWas = true;
        }
        StaticHolder.Akimbo = false;
        Debug.Log("Оружие стало " + StaticHolder.CurrentGun);
    }
    public void UpdateChangeCurrentGunTo8()
    {
        Debug.Log("Оружие было " + StaticHolder.CurrentGun);
        StaticHolder.CurrentGun = 8;
        if (StaticHolder.Akimbo)
        {
            StaticHolder.AkimboWas = true;
        }
        StaticHolder.Akimbo = false;
        Debug.Log("Оружие стало " + StaticHolder.CurrentGun);
    }
    public void UpdateAddGrenade()
    {
        Debug.Log("Гранаты в инвентаре" + StaticHolder.CurrentGrenade);
        StaticHolder.CurrentGrenade = true;
        Debug.Log("Гранаты в инвентаре" + StaticHolder.CurrentGrenade);
    }
    public void UpdateHPBuff()
    {//все работает
        Debug.Log("Текущее максимальное здоровье игрока - " + StaticHolder.PlayerHPBuff);
        StaticHolder.PlayerHPBuff += PlayerHpBuffValue;
        Debug.Log("Новое максимальное здоровье игрока - " + StaticHolder.PlayerHPBuff);
    }
    public void UpdateSpeedUp()// в скрипте здоровья отрабатывает
    {//должно работать
        Debug.Log("Текущая максимальная скорость игрока - " + StaticHolder.PlayerBasicSpeed);
        StaticHolder.SpeedBuffAfterDamage = true;
        StaticHolder.SpeedTimeAfterDamage = PlayerSpeedBuffAfterDamageTime;
        StaticHolder.SpeedAfterDamageValue = StaticHolder.SpeedAfterDamageValue * (1f + PlayerSpeedBuffAfterDamagePercent / 100f);
        Debug.Log("Новая максимальная скорость игрока - " + StaticHolder.PlayerBasicSpeed);
    }
    public void UpdateHealer()// в скрипте здоровья отрабатывает
    {//работает на левом контроллере! дальняя кнопка по идее
        Debug.Log("Пропитал есть - " + StaticHolder.PropitalHeal);
        StaticHolder.PropitalHeal = true;
        StaticHolder.PropitalHealValue = HealHPPoints;
        Debug.Log("Пропитал теперь - " + StaticHolder.PropitalHeal);
    }
    public void UpdateSandevistan()// в скрипте здоровья отрабатывает
    {//оно реально работает, я в шоке
        Debug.Log("Сандевистан есть - " + StaticHolder.Sandevistan);
        StaticHolder.Sandevistan = true;
        StaticHolder.SandevistanTime = SandewistanTimeWorkable;
        StaticHolder.SandevistanTimeSlower = SandewistanTimeSlower;
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
