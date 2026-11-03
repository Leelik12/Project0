using NUnit.Framework;

/// <summary>Состояние забега: эдди, сброс забега, сохранение и загрузка.</summary>
public class GameStateTests
{
    private GameStateSandbox sandbox;

    [SetUp] public void SetUp() => sandbox = new GameStateSandbox();
    [TearDown] public void TearDown() => sandbox.Dispose();

    [Test]
    public void AddEddies_NeverGoesBelowZero()
    {
        GameState.Eddies = 10;
        GameState.AddEddies(-50);
        Assert.AreEqual(0, GameState.Eddies);
    }

    [Test]
    public void TrySpendEddies_SpendsOnlyWhenEnough()
    {
        GameState.Eddies = 30;
        Assert.IsFalse(GameState.TrySpendEddies(31));
        Assert.AreEqual(30, GameState.Eddies);
        Assert.IsTrue(GameState.TrySpendEddies(30));
        Assert.AreEqual(0, GameState.Eddies);
    }

    [Test]
    public void ResetRun_ClearsEveryBoughtUpgrade()
    {
        GameState.Eddies = 120;
        GameState.CurrentGun = 5;
        GameState.LaserSightUnlocked = true;
        GameState.BuffGunDamage = 1.5f;
        GameState.SpeedBuffAfterDamage = true;
        GameState.SpeedTimeAfterDamage = 4f;
        GameState.PropitalHeal = true;
        GameState.PropitalHealValue = 80f;
        GameState.Sandevistan = true;
        GameState.SandevistanTime = 16f;
        GameState.SandevistanTimeSlower = 0.3f;
        GameState.OpticalCamo = true;
        GameState.OpticalCamoTime = 12f;
        GameState.ShortCircuit = true;
        GameState.ShortCircuitDamage = 60f;
        GameState.ShortCircuitStunTime = 6f;
        GameState.Armor = 0.4f;
        GameState.ModExplosive = 0.5f;
        GameState.ModPiercing = true;

        GameState.ResetRun();

        Assert.AreEqual(0, GameState.Eddies);
        Assert.AreEqual(0, GameState.CurrentGun);
        Assert.IsFalse(GameState.LaserSightUnlocked, "ЛЦУ — карточка магазина, в новом забеге её нет");
        Assert.AreEqual(1f, GameState.BuffGunDamage);
        Assert.IsFalse(GameState.SpeedBuffAfterDamage);
        Assert.AreEqual(0f, GameState.SpeedTimeAfterDamage);
        Assert.IsFalse(GameState.PropitalHeal);
        Assert.AreEqual(0f, GameState.PropitalHealValue, "сила Пропитала из прошлого забега");
        Assert.IsFalse(GameState.Sandevistan);
        Assert.AreEqual(0f, GameState.SandevistanTime, "длительность Сандевистана из прошлого забега");
        Assert.AreEqual(0f, GameState.SandevistanTimeSlower);
        Assert.IsFalse(GameState.OpticalCamo);
        Assert.AreEqual(0f, GameState.OpticalCamoTime);
        Assert.IsFalse(GameState.ShortCircuit);
        Assert.AreEqual(0f, GameState.ShortCircuitDamage);
        Assert.AreEqual(0f, GameState.ShortCircuitStunTime);
        Assert.AreEqual(0f, GameState.Armor);
        Assert.AreEqual(0f, GameState.ModExplosive);
        Assert.IsFalse(GameState.ModPiercing);
    }

    [Test]
    public void ResetRun_KeepsSettingsAndStoresRunSummary()
    {
        GameState.GunVolume = 0.25f;
        GameState.Difficulty = true;
        GameState.SmoothTurn = false;
        GameState.RunKills = 7;
        GameState.RunShots = 20;
        GameState.RunHits = 10;

        GameState.ResetRun();

        Assert.AreEqual(0.25f, GameState.GunVolume);
        Assert.IsTrue(GameState.Difficulty);
        Assert.IsFalse(GameState.SmoothTurn);
        Assert.AreEqual(0, GameState.RunKills);
        Assert.AreEqual(7, GameState.LastRun.Kills);
        Assert.AreEqual(0.5f, GameState.LastRun.Accuracy, 1e-4f);
    }

    [Test]
    public void SaveAndLoad_RoundTripRunProgress()
    {
        GameState.Eddies = 95;
        GameState.PlayerHealthLeft = 42f;
        GameState.SandevistanTime = 12f;
        GameState.OpticalCamo = true;
        GameState.ModLeech = 0.1f;
        GameState.SaveData();

        GameState.Eddies = 0;
        GameState.PlayerHealthLeft = -1f;
        GameState.SandevistanTime = 0f;
        GameState.OpticalCamo = false;
        GameState.ModLeech = 0f;
        GameState.LoadData();

        Assert.AreEqual(95, GameState.Eddies);
        Assert.AreEqual(42f, GameState.PlayerHealthLeft);
        Assert.AreEqual(12f, GameState.SandevistanTime);
        Assert.IsTrue(GameState.OpticalCamo);
        Assert.AreEqual(0.1f, GameState.ModLeech, 1e-5f);
    }

    [Test]
    public void SaveAndExit_RollsBackFloorRewards()
    {
        GameState.Eddies = 50;
        GameState.RunKills = 3;
        GameState.RunShots = 10;
        GameState.MarkFloorStart();

        // На этаже: убийства, зачистка, предмет
        GameState.AddEddies(90);
        GameState.RunKills += 4;
        GameState.RunShots += 12;

        GameState.RollbackToFloorStart();
        Assert.AreEqual(50, GameState.Eddies, "эдди этажа можно было бы получать снова через «Продолжить»");
        Assert.AreEqual(3, GameState.RunKills);
        Assert.AreEqual(10, GameState.RunShots);
    }

    [Test]
    public void SaveAndExit_InShopKeepsEddies()
    {
        GameState.Eddies = 50;
        GameState.MarkFloorStart();
        GameState.AddEddies(90);
        GameState.ClearFloorStart(); // этаж закончился, игрок в магазине

        GameState.RollbackToFloorStart();
        Assert.AreEqual(140, GameState.Eddies);
    }

    [Test]
    public void ResetRun_ForgetsFloorStart()
    {
        GameState.Eddies = 50;
        GameState.MarkFloorStart();
        GameState.ResetRun();
        GameState.AddEddies(20);

        GameState.RollbackToFloorStart();
        Assert.AreEqual(20, GameState.Eddies, "снимок прошлого забега не должен вернуть его эдди");
    }

    [Test]
    public void RunSummary_AccuracyWithoutShotsIsZero()
    {
        Assert.AreEqual(0f, new RunSummary(1, 0, 0, 0, 0f, 0f).Accuracy);
    }

    [TestCase(0, 1)]
    [TestCase(2, 1)]
    [TestCase(4, 3)]
    [TestCase(6, 5)]
    [TestCase(99, 5)]
    public void CurrentFloor_IsClampedToFloorCount(int level, int floor)
    {
        GameState.CurrentLevel = level;
        Assert.AreEqual(floor, GameState.CurrentFloor);
    }

    [TestCase("Эдди: 10", "Эдди:  10")]
    [TestCase("уже  широко", "уже  широко")]
    [TestCase("", "")]
    public void UiTextWiden_DoublesSpacesOnce(string input, string expected)
    {
        Assert.AreEqual(expected, UiText.Widen(input));
        Assert.AreEqual(expected, UiText.Widen(UiText.Widen(input)), "повторный вызов не должен расширять ещё раз");
    }
}
