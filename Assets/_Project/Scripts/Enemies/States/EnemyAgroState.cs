/// <summary>Бежит к игроку. Потерял из виду — в Search (если есть последняя позиция) или Idle. Подошёл — в Attack.</summary>
public class EnemyAgroState : EnemyBaseState
{
    public override void EnterState(EnemyStateManager manager)
    {
        manager.animator.SetBool("isAgro", true);
        manager.animator.SetBool("isAttack", false);
        manager.animator.SetBool("isPatrol", false);
        manager.viewAngle = manager.basicAngle;
        manager.controller?.SetHoldRun();
        manager.SetSpeed(manager.runSpeed);
        manager.SetDistance(manager.player);
    }

    public override void ExitState(EnemyStateManager manager) { }

    public override void UpdateState(EnemyStateManager manager)
    {
        if (manager.CanSeePlayer())
        {
            // Видим игрока — причины «слепой» агрессии больше не нужны
            manager.isTakeDamage = false;
            manager.isAgroFromInfection = false;
        }
        else if (!manager.isAgroFromInfection && !manager.isTakeDamage)
        {
            manager.SwitchState(manager.lastKnownPosition.HasValue ? manager.SearchState : manager.IdleState);
            return;
        }

        if (manager.DistanceToTarget() < manager.attackDistance)
        {
            manager.SwitchState(manager.AttackState);
        }
    }
}
