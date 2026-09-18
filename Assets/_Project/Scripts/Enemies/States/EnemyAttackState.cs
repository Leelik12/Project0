using UnityEngine;

/// <summary>Стоит и атакует (сами удары/выстрелы — события анимации). Поворачивается к игроку.</summary>
public class EnemyAttackState : EnemyBaseState
{
    private const float AttackAcceleration = 1000f; // мгновенная остановка
    private const float DefaultAcceleration = 8f;
    private const float TurnSpeed = 30f;

    public override void EnterState(EnemyStateManager manager)
    {
        manager.SetSpeed(0);
        manager.animator.SetBool("isAttack", true);
        manager.agent.acceleration = AttackAcceleration;
        manager.controller?.SetHoldAttack();
    }

    public override void ExitState(EnemyStateManager manager)
    {
        manager.agent.acceleration = DefaultAcceleration;
    }

    public override void UpdateState(EnemyStateManager manager)
    {
        bool seesPlayer = manager.CanSeePlayer();
        bool inRange = manager.DistanceToPlayer() <= manager.attackDistance;

        if (!inRange && seesPlayer)
        {
            manager.SwitchState(manager.AgroState);
            return;
        }

        if (!seesPlayer)
        {
            // Игрока не видно: если знаем, где он был — ищем; если далеко и нет причин злиться — в Idle
            if (manager.lastKnownPosition.HasValue)
                manager.SwitchState(manager.SearchState);
            else if (!inRange && !manager.isAgroFromInfection && !manager.isTakeDamage)
                manager.SwitchState(manager.IdleState);
            else
                manager.SwitchState(manager.SearchState);
            return;
        }

        // Поворачиваемся к игроку
        Vector3 direction = manager.player.position - manager.transform.position;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            Quaternion look = Quaternion.LookRotation(direction);
            manager.transform.rotation = Quaternion.Slerp(manager.transform.rotation, look, Time.deltaTime * TurnSpeed);
        }
    }
}
