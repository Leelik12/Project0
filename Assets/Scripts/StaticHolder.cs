using UnityEngine;
// Список оружий по коду 0 - Акм; 1 - Лазер. пистолет; 2 - Лазер. винтовка; 3 - Пистолет; 4 - Граната; 5 - Винтовка P40; 6 - дробовик; 7 - Полицейская дубинка; 8 - Бейсбольная бита
public static class StaticHolder
{
    //Для настроек и статистики достижений
    public static float GunVolume = 0.6f;
    public static float EnvVolume = 0.6f;
    public static bool Difficulty = false;
    public static int countShots;
    public static int countHits;
    public static float Damage;

    //Для контролирования состояния уровня
    public static bool levelCheksComplete;
    public static bool UpdateWasBought;
    public static int CurrentLevel =2;
    public static bool DieStation;

    //Все что нужно для апдейт сцены. Если че то надо добавить в этот скрипт писать ТОЛЬКО сверху
    public static int CurrentGun;
    public static bool CurrentGrenade; //есть они или нет
    public static float CurrentGunFireRate = 100f;// три сотки снизу для тестов, при билде стереть
    public static float CurrentGunDamage = 100f;
    public static int CurrentGunMaxAmmo = 100;

    public static int PlayerHP = 100;
    public static float PlayerSpeed = 100f;

    public static bool SpeedBuffAfterDamage;
    public static float SpeedAfterDamageValue;
    public static bool PropitalHeal;
    public static bool Sandevistan;
    public static bool Akimbo;
    public static bool Katana;
    public static bool StrongArms;
    public static float StrongArmsKoef;
    public static bool StrongLegs;
    public static float StrongLegsKoef;

}
