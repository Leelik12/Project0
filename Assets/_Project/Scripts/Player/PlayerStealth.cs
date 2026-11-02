/// <summary>
/// Оптический камуфляж: пока игрок невидим, враги его не видят (EnemyStateManager.CanSeePlayer),
/// а первый выстрел из невидимости — критический и снимает камуфляж.
/// </summary>
public static class PlayerStealth
{
    public const float AmbushDamageMultiplier = 3f;

    public static bool IsInvisible { get; private set; }

    public static event System.Action<bool> Changed;

    public static void SetInvisible(bool invisible)
    {
        if (IsInvisible == invisible) return;
        IsInvisible = invisible;

        if (invisible)
        {
            // Враги, которые гнались вслепую (после урона или от соседей), тоже теряют игрока и идут искать
            foreach (EnemyStateManager enemy in EnemyManager.AllEnemies)
            {
                enemy.isTakeDamage = false;
                enemy.isAgroFromInfection = false;
            }
        }
        Changed?.Invoke(invisible);
    }

    /// <summary>Выстрел из невидимости: вернёт множитель урона и выключит камуфляж.</summary>
    public static float ConsumeAmbush()
    {
        if (!IsInvisible) return 1f;
        SetInvisible(false);
        return AmbushDamageMultiplier;
    }
}
