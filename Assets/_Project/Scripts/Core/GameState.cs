using UnityEngine;

/// <summary>
/// Глобальное состояние игры: настройки, статистика достижений, прогресс забега и купленные импланты.
/// Живёт между сценами (static) и сохраняется в PlayerPrefs через SaveData / LoadData.
/// Коды оружия: 0 — АКМ, 1 — лазерный пистолет, 2 — лазерная винтовка, 3 — пистолет, 4 — граната,
/// 5 — винтовка P40, 6 — дробовик, 7 — полицейская дубинка, 8 — бейсбольная бита.
/// </summary>
public static class GameState
{
    // ---------- Настройки и статистика (сохраняются всегда) ----------
    public static float GunVolume = 0.6f;
    public static float EnvVolume = 0.6f;
    public static bool Difficulty = false;          // true — сложный режим (без лазерного прицела)
    public static bool LaserSightUnlocked = false;  // куплен апгрейд «ЛЦУ»: лазер доступен даже на сложном
    public static int countShots;
    public static int countHits;
    public static float Damage;
    public static bool Ciborg;                      // достижение «Стать боргом»
    public static bool DiedinCyberpunk;             // достижение «Таков киберпанк»

    // ---------- Флаги для главного меню (только в памяти, не сохраняются) ----------
    public static bool GameOver = false;   // победа: показать экран End
    public static bool PlayerLost = false; // поражение: показать экран Defeat

    // ---------- Состояние уровня ----------
    public static bool levelCheksComplete;     // все враги на этаже уничтожены
    public static bool ItemPickedUp = false;   // найден опциональный предмет (даёт 5-ю карточку в магазине)
    public static int CurrentLevel = 2;        // индекс текущего этажа в Build Settings
    public static bool UpdateLevelEnd = false; // апгрейд куплен, можно ехать на следующий этаж

    // ---------- Оружие и прокачка ----------
    public static int CurrentGun = 0;
    public static bool CurrentGrenade;
    public static bool BuffGrenade = false;
    public static float BuffGunFireRate = 1f;  // множитель интервала между выстрелами (<1 — быстрее)
    public static float BuffGunDamage = 1f;
    public static float BuffGunMaxAmmo = 1f;

    // ---------- Киберимпланты ----------
    public static int PlayerHPBuff = 0;
    public static float PlayerBasicSpeed = 3f;

    public static bool SpeedBuffAfterDamage = false;
    public static float SpeedAfterDamageValue = 1f;
    public static float SpeedTimeAfterDamage;

    public static bool PropitalHeal = false;       // куплен «Пропитал» (отхил по кнопке)
    public static bool PropitalHealActive = false; // уже использован на этом этаже
    public static float PropitalHealValue;

    public static bool Sandevistan = false;        // куплен «Сандевистан» (замедление времени)
    public static bool SandevistanActive = false;  // уже использован на этом этаже
    public static int SandevistanTime;
    public static float SandevistanTimeSlower;

    public static bool Akimbo = false;    // второй пистолет в руках
    public static bool AkimboWas = false; // акимбо куплен, но текущее оружие не пистолет
    public static bool Katana = false;
    public static bool StrongArms = false;
    public static float StrongArmsKoef = 1f;
    public static bool StrongLegs = false;
    public static float StrongLegsKoef = 1f;

    /// <summary>Сброс всего забега (оружие, прокачка, импланты). Вызывается при смерти, победе и выходе в меню.</summary>
    public static void ResetRun()
    {
        if (StrongArms && StrongLegs) Ciborg = true;

        CurrentGun = 0;
        CurrentGrenade = false;
        BuffGrenade = false;
        BuffGunFireRate = 1f;
        BuffGunDamage = 1f;
        BuffGunMaxAmmo = 1f;

        PlayerHPBuff = 0;
        PlayerBasicSpeed = 3f;
        SpeedBuffAfterDamage = false;
        SpeedAfterDamageValue = 1f;
        PropitalHeal = false;
        PropitalHealActive = false;
        Sandevistan = false;
        SandevistanActive = false;
        Akimbo = false;
        AkimboWas = false;
        Katana = false;
        StrongArms = false;
        StrongArmsKoef = 1f;
        StrongLegs = false;
        StrongLegsKoef = 1f;

        levelCheksComplete = false;
        ItemPickedUp = false;
        UpdateLevelEnd = false;

        SaveData();
    }

    /// <summary>Сброс одноразовых на этаж эффектов при переходе между сценами.</summary>
    public static void ResetPerLevelFlags()
    {
        levelCheksComplete = false;
        PropitalHealActive = false;
        SandevistanActive = false;
    }

    public static void SaveData()
    {
        PlayerPrefs.SetFloat("GunVolume", GunVolume);
        PlayerPrefs.SetFloat("EnvVolume", EnvVolume);
        SetBool("Difficulty", Difficulty);
        SetBool("LaserSightUnlocked", LaserSightUnlocked);
        PlayerPrefs.SetInt("countShots", countShots);
        PlayerPrefs.SetInt("countHits", countHits);
        PlayerPrefs.SetFloat("Damage", Damage);
        SetBool("Ciborg", Ciborg);
        SetBool("DiedinCyberpunk", DiedinCyberpunk);

        SetBool("levelCheksComplete", levelCheksComplete);
        SetBool("ItemPickedUp", ItemPickedUp);
        PlayerPrefs.SetInt("CurrentLevel", CurrentLevel);
        SetBool("UpdateLevelEnd", UpdateLevelEnd);

        PlayerPrefs.SetInt("CurrentGun", CurrentGun);
        SetBool("CurrentGrenade", CurrentGrenade);
        SetBool("BuffGrenade", BuffGrenade);
        PlayerPrefs.SetFloat("BuffGunFireRate", BuffGunFireRate);
        PlayerPrefs.SetFloat("BuffGunDamage", BuffGunDamage);
        PlayerPrefs.SetFloat("BuffGunMaxAmmo", BuffGunMaxAmmo);

        PlayerPrefs.SetInt("PlayerHPBuff", PlayerHPBuff);
        PlayerPrefs.SetFloat("PlayerBasicSpeed", PlayerBasicSpeed);

        SetBool("SpeedBuffAfterDamage", SpeedBuffAfterDamage);
        PlayerPrefs.SetFloat("SpeedAfterDamageValue", SpeedAfterDamageValue);
        PlayerPrefs.SetFloat("SpeedTimeAfterDamage", SpeedTimeAfterDamage);

        SetBool("PropitalHeal", PropitalHeal);
        PlayerPrefs.SetFloat("PropitalHealValue", PropitalHealValue);

        SetBool("Sandevistan", Sandevistan);
        PlayerPrefs.SetInt("SandevistanTime", SandevistanTime);
        PlayerPrefs.SetFloat("SandevistanTimeSlower", SandevistanTimeSlower);

        SetBool("Akimbo", Akimbo);
        SetBool("AkimboWas", AkimboWas);
        SetBool("Katana", Katana);
        SetBool("StrongArms", StrongArms);
        PlayerPrefs.SetFloat("StrongArmsKoef", StrongArmsKoef);
        SetBool("StrongLegs", StrongLegs);
        PlayerPrefs.SetFloat("StrongLegsKoef", StrongLegsKoef);

        PlayerPrefs.Save();
    }

    public static void LoadData()
    {
        GunVolume = PlayerPrefs.GetFloat("GunVolume", 0.6f);
        EnvVolume = PlayerPrefs.GetFloat("EnvVolume", 0.6f);
        Difficulty = GetBool("Difficulty");
        LaserSightUnlocked = GetBool("LaserSightUnlocked");
        countShots = PlayerPrefs.GetInt("countShots", 0);
        countHits = PlayerPrefs.GetInt("countHits", 0);
        Damage = PlayerPrefs.GetFloat("Damage", 0f);
        Ciborg = GetBool("Ciborg");
        DiedinCyberpunk = GetBool("DiedinCyberpunk");

        levelCheksComplete = GetBool("levelCheksComplete");
        ItemPickedUp = GetBool("ItemPickedUp");
        CurrentLevel = PlayerPrefs.GetInt("CurrentLevel", 2);
        UpdateLevelEnd = GetBool("UpdateLevelEnd");

        CurrentGun = PlayerPrefs.GetInt("CurrentGun", 0);
        CurrentGrenade = GetBool("CurrentGrenade");
        BuffGrenade = GetBool("BuffGrenade");
        BuffGunFireRate = PlayerPrefs.GetFloat("BuffGunFireRate", 1f);
        BuffGunDamage = PlayerPrefs.GetFloat("BuffGunDamage", 1f);
        BuffGunMaxAmmo = PlayerPrefs.GetFloat("BuffGunMaxAmmo", 1f);

        PlayerHPBuff = PlayerPrefs.GetInt("PlayerHPBuff", 0);
        PlayerBasicSpeed = PlayerPrefs.GetFloat("PlayerBasicSpeed", 3f);

        SpeedBuffAfterDamage = GetBool("SpeedBuffAfterDamage");
        SpeedAfterDamageValue = PlayerPrefs.GetFloat("SpeedAfterDamageValue", 1f);
        SpeedTimeAfterDamage = PlayerPrefs.GetFloat("SpeedTimeAfterDamage", 0f);

        PropitalHeal = GetBool("PropitalHeal");
        PropitalHealValue = PlayerPrefs.GetFloat("PropitalHealValue", 0f);

        Sandevistan = GetBool("Sandevistan");
        SandevistanTime = PlayerPrefs.GetInt("SandevistanTime", 0);
        SandevistanTimeSlower = PlayerPrefs.GetFloat("SandevistanTimeSlower", 0f);

        Akimbo = GetBool("Akimbo");
        AkimboWas = GetBool("AkimboWas");
        Katana = GetBool("Katana");
        StrongArms = GetBool("StrongArms");
        StrongArmsKoef = PlayerPrefs.GetFloat("StrongArmsKoef", 1f);
        StrongLegs = GetBool("StrongLegs");
        StrongLegsKoef = PlayerPrefs.GetFloat("StrongLegsKoef", 1f);
    }

    private static void SetBool(string key, bool value) => PlayerPrefs.SetInt(key, value ? 1 : 0);
    private static bool GetBool(string key) => PlayerPrefs.GetInt(key, 0) == 1;
}
