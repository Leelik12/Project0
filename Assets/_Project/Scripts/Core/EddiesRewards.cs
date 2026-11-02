using UnityEngine;

/// <summary>
/// Начисляет эдди за убийства: обычное, хедшот, элита. Награды за зачистку этажа и предмет начисляет LevelManager.
/// Подписывается на <see cref="GameEvents"/> один раз при старте игры, в сценах ничего ставить не нужно.
/// </summary>
public static class EddiesRewards
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        GameEvents.EnemyHit -= OnEnemyHit;
        GameEvents.EnemyHit += OnEnemyHit;
    }

    private static void OnEnemyHit(EnemyHitInfo hit)
    {
        if (!hit.Killed || hit.Enemy == null) return;

        UpgradeCatalog catalog = UpgradeCatalog.Instance;
        int reward = catalog.killReward;
        if (hit.Headshot) reward += catalog.headshotKillBonus;
        if (hit.Enemy.TryGetComponent(out EliteEnemy _)) reward += catalog.eliteKillBonus;
        GameState.AddEddies(reward);
    }
}
