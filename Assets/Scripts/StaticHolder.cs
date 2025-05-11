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
    public static bool Ciborg;
    public static bool DiedinCyberpunk;

    //Для контролирования состояния уровня
    public static bool levelCheksComplete;
    public static bool ItemPickedUp = false;
    public static bool UpdateWasBought;
    public static int CurrentLevel =2;
    public static bool DieStation;

    //Все что нужно для апдейт сцены. Если че то надо добавить в этот скрипт писать ТОЛЬКО сверху
    public static int CurrentGun = 0;
    public static bool CurrentGrenade; //есть они или нет
    public static float CurrentGunFireRate;// три сотки снизу для тестов, при билде стереть
    public static float CurrentGunDamage;
    public static int CurrentGunMaxAmmo;

    public static bool UpdateLevelEnd;



    public static bool BuffGrenade = false; //есть они или нет
    public static float BuffGunFireRate = 1f;// три сотки снизу для тестов, при билде стереть
    public static float BuffGunDamage = 1f;
    public static float BuffGunMaxAmmo = 1f;

    public static int PlayerHPBuff = 0;
    public static float PlayerBasicSpeed = 3f;

    public static bool SpeedBuffAfterDamage = false;
    public static float SpeedAfterDamageValue = 1f;
    public static float SpeedTimeAfterDamage;
    public static bool PropitalHeal = false;
    public static bool PropitalHealActive = false;
    public static float PropitalHealValue;
    public static bool Sandevistan = false;
    public static bool SandevistanActive = false;
    public static int SandevistanTime;
    public static float SandevistanTimeSlower;
    public static bool Akimbo = false;
    public static bool AkimboWas = false;
    public static bool Katana = false;
    public static bool StrongArms = false;
    public static float StrongArmsKoef = 1f;
    public static bool StrongLegs = false;
    public static float StrongLegsKoef = 1f;

}
