using System;
using UnityEngine;

/// <summary>
/// Игровые события. Источники (оружие, здоровье, импланты) только сообщают о факте, а звуки, эффекты и HUD
/// подписываются сами — без опроса в Update и без прямых ссылок друг на друга.
/// Подписчики со сцены обязаны отписываться в OnDestroy: события статические и живут между сценами.
/// </summary>
public static class GameEvents
{
    /// <summary>Враг получил урон (в том числе смертельный).</summary>
    public static event Action<EnemyHitInfo> EnemyHit;

    /// <summary>Враг умер.</summary>
    public static event Action<EnemyHealth> EnemyKilled;

    /// <summary>Игрок получил урон: величина урона.</summary>
    public static event Action<float> PlayerDamaged;

    /// <summary>Сандевистан включился (true) или выключился (false).</summary>
    public static event Action<bool> SandevistanChanged;

    /// <summary>Игра встала на паузу (true) или продолжилась (false).</summary>
    public static event Action<bool> PauseChanged;

    public static void RaiseEnemyHit(in EnemyHitInfo hit) => EnemyHit?.Invoke(hit);
    public static void RaiseEnemyKilled(EnemyHealth enemy) => EnemyKilled?.Invoke(enemy);
    public static void RaisePlayerDamaged(float damage) => PlayerDamaged?.Invoke(damage);
    public static void RaiseSandevistanChanged(bool active) => SandevistanChanged?.Invoke(active);
    public static void RaisePauseChanged(bool paused) => PauseChanged?.Invoke(paused);
}

/// <summary>Данные попадания по врагу.</summary>
public readonly struct EnemyHitInfo
{
    public readonly EnemyHealth Enemy;
    public readonly float Damage;
    public readonly Vector3 Point;
    public readonly Vector3 Direction; // куда летел удар (нормализован или ноль)
    public readonly bool Headshot;
    public readonly bool Killed;

    public EnemyHitInfo(EnemyHealth enemy, float damage, Vector3 point, Vector3 direction, bool headshot, bool killed)
    {
        Enemy = enemy;
        Damage = damage;
        Point = point;
        Direction = direction;
        Headshot = headshot;
        Killed = killed;
    }
}
