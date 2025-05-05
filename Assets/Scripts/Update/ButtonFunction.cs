using UnityEngine;

public class ButtonFunction : MonoBehaviour
{
    public int FireRateUpdatePercent;
    public int DamageUpdatePercent;
    public int MagazineValueUpdatePercent;
    //Сверху подкапотка прокачки, все изменения прокачки происходят через эти настройки
    //Не забудь поменять текст на карточках при изменении этих значений
    public void ShablonButton()
    {

    }
    public void UpdateFireRate() //Все хорошо, при прокачке значение должно падать. FireRate - время между выстрелами
    {
        Debug.Log("Скорострельность была " + StaticHolder.CurrentGunFireRate);
        StaticHolder.CurrentGunFireRate = StaticHolder.CurrentGunFireRate * (100 - FireRateUpdatePercent) / 100;
        Debug.Log("Скорострельность увеличена до " + StaticHolder.CurrentGunFireRate);
    }
    public void UpdateDamage()
    {
        Debug.Log("Урон был " + StaticHolder.CurrentGunDamage);
        StaticHolder.CurrentGunDamage = StaticHolder.CurrentGunDamage * (100 + DamageUpdatePercent) / 100;
        Debug.Log("Урон увеличен до " + StaticHolder.CurrentGunDamage);
    }
    public void UpdateMagazineValue()
    {
        Debug.Log("Размер магазина был " + StaticHolder.CurrentGunMaxAmmo);
        StaticHolder.CurrentGunMaxAmmo = StaticHolder.CurrentGunMaxAmmo * (100 + MagazineValueUpdatePercent)/100;
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
}
