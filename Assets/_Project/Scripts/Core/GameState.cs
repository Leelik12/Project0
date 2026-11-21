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
    public static bool SmoothTurn = true;           // плавный поворот стиком; false — пошаговый (snap)
    public static bool ComfortVignette = false;      // сужение обзора при движении (против укачивания)
    public static int ShotsFired;                   // за всё время, для достижений
    public static int ShotsHit;
    public static float TotalDamage;
    public static bool CyborgAchieved;              // достижение «Стать боргом»
    public static bool DiedInCyberpunk;             // достижение «Таков киберпанк»

    // ---------- Статистика текущего забега (для итогового экрана) ----------
    public static int RunKills;
    public static int RunShots;
    public static int RunHits;
    public static float RunDamage;
    public static float RunTime;                    // секунды реального времени на этажах
    public static bool RunInProgress;               // забег сохранён через «выход с сохранением» — можно продолжить
    public static int ContinueScene = 2;            // сцена, из которой вышли с сохранением

    // ---------- Ресурсы забега ----------
    public static int Eddies;                       // валюта: даётся за убийства, тратится в магазине
    public static float PlayerHealthLeft = -1f;     // здоровье переносится между этажами; -1 — полное

    /// <summary>Снимок забега, сделанный в ResetRun: его показывает экран победы/поражения в меню.</summary>
    public static RunSummary LastRun;

    // ---------- Флаги для главного меню (только в памяти, не сохраняются) ----------
    public static bool GameOver = false;   // победа: показать экран End
    public static bool PlayerLost = false; // поражение: показать экран Defeat

    // ---------- Состояние уровня ----------
    public static bool LevelCleared;           // все враги на этаже уничтожены
    public static bool ItemPickedUp = false;   // найден опциональный предмет (даёт 5-ю карточку в магазине)
    public static int CurrentLevel = 2;        // индекс текущего этажа в Build Settings
    public static bool UpdateLevelEnd = false; // апгрейд куплен, можно ехать на следующий этаж

    // ---------- Оружие и прокачка ----------
    public static int CurrentGun = 0;
    public static bool CurrentGrenade;
    public static bool BuffGrenade = false;
    public static bool GrenadeEmp;      // куплены гранаты W-07: ЭМИ, дымовая, липкая
    public static bool GrenadeSmoke;
    public static bool GrenadeSticky;
    public static float BuffGunFireRate = 1f;  // множитель интервала между выстрелами (<1 — быстрее)
    public static float BuffGunDamage = 1f;
    public static float BuffGunMaxAmmo = 1f;

    // ---------- Киберимпланты ----------
    public static int PlayerHPBuff = 0;
    public static float PlayerBasicSpeed = 3f;

    public static bool SpeedBuffAfterDamage = false;
    public static float SpeedAfterDamageValue = 1f;
    public static float SpeedTimeAfterDamage;

    // Активные импланты работают по кулдауну (см. PlayerImplants)
    public static bool PropitalHeal = false;       // куплен «Пропитал» (отхил по кнопке)
    public static float PropitalHealValue;

    public static bool Sandevistan = false;        // куплен «Сандевистан» (замедление времени)
    public static float SandevistanTime;
    public static float SandevistanTimeSlower;

    public static bool OpticalCamo = false;        // оптический камуфляж: невидимость
    public static float OpticalCamoTime;

    public static bool ShortCircuit = false;       // взлом «Короткое замыкание»: оглушение и урон врагу
    public static float ShortCircuitDamage;
    public static float ShortCircuitStunTime;

    // Пассивные импланты
    public static float KillHeal;                  // HP за убийство
    public static float Regeneration;              // HP в секунду вне боя
    public static int HeadshotAmmo;                // патронов в магазин за хедшот
    public static float Armor;                     // доля поглощаемого урона (0..MaxArmor)
    public const float MaxArmor = 0.6f;

    // Модификаторы оружия (действуют на любое огнестрельное оружие игрока)
    public static float ModExplosive;              // доля урона выстрела, которая взрывом бьёт по площади
    public static bool ModRicochet;                // промах рикошетит от стены в ближайшего врага
    public static bool ModPiercing;                // пуля проходит врага насквозь
    public static float ModIncendiary;             // урон поджога в секунду
    public static float ModLeech;                  // доля урона, которая лечит игрока

    public static bool Akimbo = false;    // второй пистолет в руках
    public static bool AkimboWas = false; // акимбо куплен, но текущее оружие не пистолет
    public static bool Katana = false;
    public static bool StrongArms = false;
    public static float StrongArmsMultiplier = 1f;
    public static bool StrongLegs = false;
    public static float StrongLegsMultiplier = 1f;

    public const int UpgradeShopScene = 1;
    public const int FirstFloorScene = 2;
    public const int FloorCount = 5;

    /// <summary>Номер этажа (1..FloorCount) по индексу сцены в CurrentLevel.</summary>
    public static int CurrentFloor => Mathf.Clamp(CurrentLevel - FirstFloorScene + 1, 1, FloorCount);

    // ---------- Статистика ----------

    public static void RegisterShot()
    {
        ShotsFired++;
        RunShots++;
    }

    /// <summary>Попадание пулей: идёт в точность и урон.</summary>
    public static void RegisterHit(float damage)
    {
        ShotsHit++;
        RunHits++;
        RegisterDamage(damage);
    }

    /// <summary>Урон без выстрела (граната, ближний бой): не влияет на точность.</summary>
    public static void RegisterDamage(float damage)
    {
        TotalDamage += damage;
        RunDamage += damage;
    }

    public static void RegisterKill() => RunKills++;

    public static void AddEddies(int amount) => Eddies = Mathf.Max(0, Eddies + amount);

    /// <summary>Списать эдди, если хватает.</summary>
    public static bool TrySpendEddies(int amount)
    {
        if (Eddies < amount) return false;
        Eddies -= amount;
        return true;
    }

    /// <summary>Сброс всего забега (оружие, прокачка, импланты). Вызывается при смерти, победе и выходе в меню.</summary>
    public static void ResetRun()
    {
        if (StrongArms && StrongLegs) CyborgAchieved = true;

        LastRun = new RunSummary(CurrentFloor, RunKills, RunShots, RunHits, RunDamage, RunTime);
        RunKills = 0;
        RunShots = 0;
        RunHits = 0;
        RunDamage = 0f;
        RunTime = 0f;
        RunInProgress = false;
        Eddies = 0;
        PlayerHealthLeft = -1f;

        CurrentGun = 0;
        CurrentGrenade = false;
        BuffGrenade = false;
        GrenadeEmp = false;
        GrenadeSmoke = false;
        GrenadeSticky = false;
        BuffGunFireRate = 1f;
        BuffGunDamage = 1f;
        BuffGunMaxAmmo = 1f;
        LaserSightUnlocked = false;

        PlayerHPBuff = 0;
        PlayerBasicSpeed = 3f;
        SpeedBuffAfterDamage = false;
        SpeedAfterDamageValue = 1f;
        SpeedTimeAfterDamage = 0f;
        PropitalHeal = false;
        PropitalHealValue = 0f;
        Sandevistan = false;
        SandevistanTime = 0f;
        SandevistanTimeSlower = 0f;
        OpticalCamo = false;
        OpticalCamoTime = 0f;
        ShortCircuit = false;
        ShortCircuitDamage = 0f;
        ShortCircuitStunTime = 0f;
        KillHeal = 0f;
        Regeneration = 0f;
        HeadshotAmmo = 0;
        Armor = 0f;
        ModExplosive = 0f;
        ModRicochet = false;
        ModPiercing = false;
        ModIncendiary = 0f;
        ModLeech = 0f;
        Akimbo = false;
        AkimboWas = false;
        Katana = false;
        StrongArms = false;
        StrongArmsMultiplier = 1f;
        StrongLegs = false;
        StrongLegsMultiplier = 1f;

        LevelCleared = false;
        ItemPickedUp = false;
        UpdateLevelEnd = false;
        hasFloorStart = false;

        SaveData();
    }

    // ---------- Снимок на входе на этаж ----------
    // «Выход с сохранением» с этажа начинает его заново со всеми врагами, поэтому награды и статистика
    // этого этажа откатываются к моменту входа: иначе одних и тех же врагов можно было бы «фармить» бесконечно.
    private static bool hasFloorStart;
    private static int floorStartEddies;
    private static int floorStartKills;
    private static int floorStartShots;
    private static int floorStartHits;
    private static float floorStartDamage;

    /// <summary>Запомнить состояние на входе на этаж (вызывает LevelManager).</summary>
    public static void MarkFloorStart()
    {
        hasFloorStart = true;
        floorStartEddies = Eddies;
        floorStartKills = RunKills;
        floorStartShots = RunShots;
        floorStartHits = RunHits;
        floorStartDamage = RunDamage;
    }

    /// <summary>Этаж закончился (ушли со сцены): откатывать больше нечего.</summary>
    public static void ClearFloorStart() => hasFloorStart = false;

    /// <summary>Вернуть эдди и статистику забега к моменту входа на этаж. Без снимка (магазин) ничего не делает.</summary>
    public static void RollbackToFloorStart()
    {
        if (!hasFloorStart) return;
        Eddies = floorStartEddies;
        RunKills = floorStartKills;
        RunShots = floorStartShots;
        RunHits = floorStartHits;
        RunDamage = floorStartDamage;
    }

    /// <summary>Сброс флагов этажа при переходе между сценами.</summary>
    public static void ResetPerLevelFlags()
    {
        LevelCleared = false;
    }

    public static void SaveData()
    {
        PlayerPrefs.SetFloat("GunVolume", GunVolume);
        SetBool("SmoothTurn", SmoothTurn);
        SetBool("ComfortVignette", ComfortVignette);
        PlayerPrefs.SetFloat("EnvVolume", EnvVolume);
        SetBool("Difficulty", Difficulty);
        SetBool("LaserSightUnlocked", LaserSightUnlocked);
        PlayerPrefs.SetInt("countShots", ShotsFired);
        PlayerPrefs.SetInt("countHits", ShotsHit);
        PlayerPrefs.SetFloat("Damage", TotalDamage);
        SetBool("Ciborg", CyborgAchieved);
        SetBool("DiedinCyberpunk", DiedInCyberpunk);

        SetBool("levelCheksComplete", LevelCleared);
        SetBool("ItemPickedUp", ItemPickedUp);
        PlayerPrefs.SetInt("CurrentLevel", CurrentLevel);
        SetBool("UpdateLevelEnd", UpdateLevelEnd);

        PlayerPrefs.SetInt("RunKills", RunKills);
        PlayerPrefs.SetInt("RunShots", RunShots);
        PlayerPrefs.SetInt("RunHits", RunHits);
        PlayerPrefs.SetFloat("RunDamage", RunDamage);
        PlayerPrefs.SetFloat("RunTime", RunTime);
        SetBool("RunInProgress", RunInProgress);
        PlayerPrefs.SetInt("ContinueScene", ContinueScene);
        PlayerPrefs.SetInt("Eddies", Eddies);
        PlayerPrefs.SetFloat("PlayerHealthLeft", PlayerHealthLeft);

        PlayerPrefs.SetInt("CurrentGun", CurrentGun);
        SetBool("CurrentGrenade", CurrentGrenade);
        SetBool("BuffGrenade", BuffGrenade);
        SetBool("GrenadeEmp", GrenadeEmp);
        SetBool("GrenadeSmoke", GrenadeSmoke);
        SetBool("GrenadeSticky", GrenadeSticky);
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
        PlayerPrefs.SetFloat("SandevistanDuration", SandevistanTime);
        PlayerPrefs.SetFloat("SandevistanTimeSlower", SandevistanTimeSlower);

        SetBool("OpticalCamo", OpticalCamo);
        PlayerPrefs.SetFloat("OpticalCamoTime", OpticalCamoTime);
        SetBool("ShortCircuit", ShortCircuit);
        PlayerPrefs.SetFloat("ShortCircuitDamage", ShortCircuitDamage);
        PlayerPrefs.SetFloat("ShortCircuitStunTime", ShortCircuitStunTime);

        PlayerPrefs.SetFloat("KillHeal", KillHeal);
        PlayerPrefs.SetFloat("Regeneration", Regeneration);
        PlayerPrefs.SetInt("HeadshotAmmo", HeadshotAmmo);
        PlayerPrefs.SetFloat("Armor", Armor);

        PlayerPrefs.SetFloat("ModExplosive", ModExplosive);
        SetBool("ModRicochet", ModRicochet);
        SetBool("ModPiercing", ModPiercing);
        PlayerPrefs.SetFloat("ModIncendiary", ModIncendiary);
        PlayerPrefs.SetFloat("ModLeech", ModLeech);

        SetBool("Akimbo", Akimbo);
        SetBool("AkimboWas", AkimboWas);
        SetBool("Katana", Katana);
        SetBool("StrongArms", StrongArms);
        PlayerPrefs.SetFloat("StrongArmsKoef", StrongArmsMultiplier);
        SetBool("StrongLegs", StrongLegs);
        PlayerPrefs.SetFloat("StrongLegsKoef", StrongLegsMultiplier);

        PlayerPrefs.Save();
    }

    public static void LoadData()
    {
        GunVolume = PlayerPrefs.GetFloat("GunVolume", 0.6f);
        SmoothTurn = PlayerPrefs.GetInt("SmoothTurn", 1) == 1;
        ComfortVignette = GetBool("ComfortVignette");
        EnvVolume = PlayerPrefs.GetFloat("EnvVolume", 0.6f);
        Difficulty = GetBool("Difficulty");
        LaserSightUnlocked = GetBool("LaserSightUnlocked");
        ShotsFired = PlayerPrefs.GetInt("countShots", 0);
        ShotsHit = PlayerPrefs.GetInt("countHits", 0);
        TotalDamage = PlayerPrefs.GetFloat("Damage", 0f);
        CyborgAchieved = GetBool("Ciborg");
        DiedInCyberpunk = GetBool("DiedinCyberpunk");

        LevelCleared = GetBool("levelCheksComplete");
        ItemPickedUp = GetBool("ItemPickedUp");
        CurrentLevel = PlayerPrefs.GetInt("CurrentLevel", 2);
        UpdateLevelEnd = GetBool("UpdateLevelEnd");

        RunKills = PlayerPrefs.GetInt("RunKills", 0);
        RunShots = PlayerPrefs.GetInt("RunShots", 0);
        RunHits = PlayerPrefs.GetInt("RunHits", 0);
        RunDamage = PlayerPrefs.GetFloat("RunDamage", 0f);
        RunTime = PlayerPrefs.GetFloat("RunTime", 0f);
        RunInProgress = GetBool("RunInProgress");
        ContinueScene = PlayerPrefs.GetInt("ContinueScene", FirstFloorScene);
        Eddies = PlayerPrefs.GetInt("Eddies", 0);
        PlayerHealthLeft = PlayerPrefs.GetFloat("PlayerHealthLeft", -1f);

        CurrentGun = PlayerPrefs.GetInt("CurrentGun", 0);
        CurrentGrenade = GetBool("CurrentGrenade");
        BuffGrenade = GetBool("BuffGrenade");
        GrenadeEmp = GetBool("GrenadeEmp");
        GrenadeSmoke = GetBool("GrenadeSmoke");
        GrenadeSticky = GetBool("GrenadeSticky");
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
        SandevistanTime = PlayerPrefs.GetFloat("SandevistanDuration", 0f);
        SandevistanTimeSlower = PlayerPrefs.GetFloat("SandevistanTimeSlower", 0f);

        OpticalCamo = GetBool("OpticalCamo");
        OpticalCamoTime = PlayerPrefs.GetFloat("OpticalCamoTime", 0f);
        ShortCircuit = GetBool("ShortCircuit");
        ShortCircuitDamage = PlayerPrefs.GetFloat("ShortCircuitDamage", 0f);
        ShortCircuitStunTime = PlayerPrefs.GetFloat("ShortCircuitStunTime", 0f);

        KillHeal = PlayerPrefs.GetFloat("KillHeal", 0f);
        Regeneration = PlayerPrefs.GetFloat("Regeneration", 0f);
        HeadshotAmmo = PlayerPrefs.GetInt("HeadshotAmmo", 0);
        Armor = PlayerPrefs.GetFloat("Armor", 0f);

        ModExplosive = PlayerPrefs.GetFloat("ModExplosive", 0f);
        ModRicochet = GetBool("ModRicochet");
        ModPiercing = GetBool("ModPiercing");
        ModIncendiary = PlayerPrefs.GetFloat("ModIncendiary", 0f);
        ModLeech = PlayerPrefs.GetFloat("ModLeech", 0f);

        Akimbo = GetBool("Akimbo");
        AkimboWas = GetBool("AkimboWas");
        Katana = GetBool("Katana");
        StrongArms = GetBool("StrongArms");
        StrongArmsMultiplier = PlayerPrefs.GetFloat("StrongArmsKoef", 1f);
        StrongLegs = GetBool("StrongLegs");
        StrongLegsMultiplier = PlayerPrefs.GetFloat("StrongLegsKoef", 1f);
    }

    private static void SetBool(string key, bool value) => PlayerPrefs.SetInt(key, value ? 1 : 0);
    private static bool GetBool(string key) => PlayerPrefs.GetInt(key, 0) == 1;
}

/// <summary>Итог одного забега для экрана победы/поражения.</summary>
public readonly struct RunSummary
{
    public readonly int Floor;
    public readonly int Kills;
    public readonly int Shots;
    public readonly int Hits;
    public readonly float Damage;
    public readonly float Time;

    public RunSummary(int floor, int kills, int shots, int hits, float damage, float time)
    {
        Floor = floor;
        Kills = kills;
        Shots = shots;
        Hits = hits;
        Damage = damage;
        Time = time;
    }

    public float Accuracy => Shots > 0 ? (float)Hits / Shots : 0f;

    public override string ToString()
    {
        int minutes = (int)(Time / 60f);
        int seconds = (int)(Time % 60f);
        return $"Этаж: {Floor}/{GameState.FloorCount}\n" +
               $"Убито врагов: {Kills}\n" +
               $"Точность: {Accuracy:P0} ({Hits}/{Shots})\n" +
               $"Нанесено урона: {Damage:0}\n" +
               $"Время: {minutes:00}:{seconds:00}";
    }
}
