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
    public static bool GameOver = false;

    //Для контролирования состояния уровня
    public static bool levelCheksComplete;
    public static bool ItemPickedUp = false;
    public static bool UpdateWasBought;
    public static int CurrentLevel =2;
    public static bool DieStation;

    //Все что нужно для апдейт сцены
    public static int CurrentGun = 0;
    public static bool CurrentGrenade; //есть они или нет
    public static float CurrentGunFireRate;
    public static float CurrentGunDamage;
    public static int CurrentGunMaxAmmo;

    public static bool UpdateLevelEnd;

    public static bool BuffGrenade = false; //есть они или нет
    public static float BuffGunFireRate = 1f;
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

    public static void SaveData()
    {
        PlayerPrefs.SetFloat("GunVolume", GunVolume);
        PlayerPrefs.SetFloat("EnvVolume", EnvVolume);
        PlayerPrefs.SetInt("Difficulty", Difficulty ? 1 : 0);
        PlayerPrefs.SetInt("countShots", countShots);
        PlayerPrefs.SetInt("countHits", countHits);
        PlayerPrefs.SetFloat("Damage", Damage);
        PlayerPrefs.SetInt("Ciborg", Ciborg ? 1 : 0);
        PlayerPrefs.SetInt("DiedinCyberpunk", DiedinCyberpunk ? 1 : 0);
        PlayerPrefs.SetInt("GameOver", GameOver ? 1 : 0);

        PlayerPrefs.SetInt("levelCheksComplete", levelCheksComplete ? 1 : 0);
        PlayerPrefs.SetInt("ItemPickedUp", ItemPickedUp ? 1 : 0);
        PlayerPrefs.SetInt("UpdateWasBought", UpdateWasBought ? 1 : 0);
        PlayerPrefs.SetInt("CurrentLevel", CurrentLevel);
        PlayerPrefs.SetInt("DieStation", DieStation ? 1 : 0);

        PlayerPrefs.SetInt("CurrentGun", CurrentGun);
        PlayerPrefs.SetInt("CurrentGrenade", CurrentGrenade ? 1 : 0);
        PlayerPrefs.SetFloat("CurrentGunFireRate", CurrentGunFireRate);
        PlayerPrefs.SetFloat("CurrentGunDamage", CurrentGunDamage);
        PlayerPrefs.SetInt("CurrentGunMaxAmmo", CurrentGunMaxAmmo);

        PlayerPrefs.SetInt("UpdateLevelEnd", UpdateLevelEnd ? 1 : 0);

        PlayerPrefs.SetInt("BuffGrenade", BuffGrenade ? 1 : 0);
        PlayerPrefs.SetFloat("BuffGunFireRate", BuffGunFireRate);
        PlayerPrefs.SetFloat("BuffGunDamage", BuffGunDamage);
        PlayerPrefs.SetFloat("BuffGunMaxAmmo", BuffGunMaxAmmo);

        PlayerPrefs.SetInt("PlayerHPBuff", PlayerHPBuff);
        PlayerPrefs.SetFloat("PlayerBasicSpeed", PlayerBasicSpeed);

        PlayerPrefs.SetInt("SpeedBuffAfterDamage", SpeedBuffAfterDamage ? 1 : 0);
        PlayerPrefs.SetFloat("SpeedAfterDamageValue", SpeedAfterDamageValue);
        PlayerPrefs.SetFloat("SpeedTimeAfterDamage", SpeedTimeAfterDamage);

        PlayerPrefs.SetInt("PropitalHeal", PropitalHeal ? 1 : 0);
        PlayerPrefs.SetInt("PropitalHealActive", PropitalHealActive ? 1 : 0);
        PlayerPrefs.SetFloat("PropitalHealValue", PropitalHealValue);

        PlayerPrefs.SetInt("Sandevistan", Sandevistan ? 1 : 0);
        PlayerPrefs.SetInt("SandevistanActive", SandevistanActive ? 1 : 0);
        PlayerPrefs.SetInt("SandevistanTime", SandevistanTime);
        PlayerPrefs.SetFloat("SandevistanTimeSlower", SandevistanTimeSlower);

        PlayerPrefs.SetInt("Akimbo", Akimbo ? 1 : 0);
        PlayerPrefs.SetInt("AkimboWas", AkimboWas ? 1 : 0);

        PlayerPrefs.SetInt("Katana", Katana ? 1 : 0);
        PlayerPrefs.SetInt("StrongArms", StrongArms ? 1 : 0);
        PlayerPrefs.SetFloat("StrongArmsKoef", StrongArmsKoef);
        PlayerPrefs.SetInt("StrongLegs", StrongLegs ? 1 : 0);
        PlayerPrefs.SetFloat("StrongLegsKoef", StrongLegsKoef);

        PlayerPrefs.Save();
    }

    public static void LoadData()
    {
        GunVolume = PlayerPrefs.GetFloat("GunVolume", 0.6f);
        EnvVolume = PlayerPrefs.GetFloat("EnvVolume", 0.6f);
        Difficulty = PlayerPrefs.GetInt("Difficulty", 0) == 1;
        countShots = PlayerPrefs.GetInt("countShots", 0);
        countHits = PlayerPrefs.GetInt("countHits", 0);
        Damage = PlayerPrefs.GetFloat("Damage", 0f);
        Ciborg = PlayerPrefs.GetInt("Ciborg", 0) == 1;
        DiedinCyberpunk = PlayerPrefs.GetInt("DiedinCyberpunk", 0) == 1;
        GameOver = PlayerPrefs.GetInt("GameOver", 0) == 1;

        levelCheksComplete = PlayerPrefs.GetInt("levelCheksComplete", 0) == 1;
        ItemPickedUp = PlayerPrefs.GetInt("ItemPickedUp", 0) == 1;
        UpdateWasBought = PlayerPrefs.GetInt("UpdateWasBought", 0) == 1;
        CurrentLevel = PlayerPrefs.GetInt("CurrentLevel", 2);
        DieStation = PlayerPrefs.GetInt("DieStation", 0) == 1;

        CurrentGun = PlayerPrefs.GetInt("CurrentGun", 0);
        CurrentGrenade = PlayerPrefs.GetInt("CurrentGrenade", 0) == 1;
        CurrentGunFireRate = PlayerPrefs.GetFloat("CurrentGunFireRate", 0f);
        CurrentGunDamage = PlayerPrefs.GetFloat("CurrentGunDamage", 0f);
        CurrentGunMaxAmmo = PlayerPrefs.GetInt("CurrentGunMaxAmmo", 0);

        UpdateLevelEnd = PlayerPrefs.GetInt("UpdateLevelEnd", 0) == 1;

        BuffGrenade = PlayerPrefs.GetInt("BuffGrenade", 0) == 1;
        BuffGunFireRate = PlayerPrefs.GetFloat("BuffGunFireRate", 1f);
        BuffGunDamage = PlayerPrefs.GetFloat("BuffGunDamage", 1f);
        BuffGunMaxAmmo = PlayerPrefs.GetFloat("BuffGunMaxAmmo", 1f);

        PlayerHPBuff = PlayerPrefs.GetInt("PlayerHPBuff", 0);
        PlayerBasicSpeed = PlayerPrefs.GetFloat("PlayerBasicSpeed", 3f);

        SpeedBuffAfterDamage = PlayerPrefs.GetInt("SpeedBuffAfterDamage", 0) == 1;
        SpeedAfterDamageValue = PlayerPrefs.GetFloat("SpeedAfterDamageValue", 1f);
        SpeedTimeAfterDamage = PlayerPrefs.GetFloat("SpeedTimeAfterDamage", 0f);

        PropitalHeal = PlayerPrefs.GetInt("PropitalHeal", 0) == 1;
        PropitalHealActive = PlayerPrefs.GetInt("PropitalHealActive", 0) == 1;
        PropitalHealValue = PlayerPrefs.GetFloat("PropitalHealValue", 0f);

        Sandevistan = PlayerPrefs.GetInt("Sandevistan", 0) == 1;
        SandevistanActive = PlayerPrefs.GetInt("SandevistanActive", 0) == 1;
        SandevistanTime = PlayerPrefs.GetInt("SandevistanTime", 0);
        SandevistanTimeSlower = PlayerPrefs.GetFloat("SandevistanTimeSlower", 0f);

        Akimbo = PlayerPrefs.GetInt("Akimbo", 0) == 1;
        AkimboWas = PlayerPrefs.GetInt("AkimboWas", 0) == 1;

        Katana = PlayerPrefs.GetInt("Katana", 0) == 1;
        StrongArms = PlayerPrefs.GetInt("StrongArms", 0) == 1;
        StrongArmsKoef = PlayerPrefs.GetFloat("StrongArmsKoef", 1f);
        StrongLegs = PlayerPrefs.GetInt("StrongLegs", 0) == 1;
        StrongLegsKoef = PlayerPrefs.GetFloat("StrongLegsKoef", 1f);
    }
}

