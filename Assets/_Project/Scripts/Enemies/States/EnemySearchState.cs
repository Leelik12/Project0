/// <summary>Бежит к последней известной позиции игрока. Добежал и не нашёл — возвращается к патрулю.</summary>
public class EnemySearchState : EnemyBaseState
{
    private const float CloseRangeFullView = 1f; // вплотную к игроку — видим на 360°

    public override void EnterState(EnemyStateManager manager)
    {
        manager.animator.SetBool("isAgro", true);
        manager.animator.SetBool("isAttack", false);
        manager.animator.SetBool("isPatrol", false);
        manager.controller?.SetHoldRun();
        manager.SetSpeed(manager.runSpeed);

        // Цель — точка, а не игрок: снимаем слежение за transform, чтобы Update не перезаписывал destination
        manager.SetDistance(null);
        if (manager.lastKnownPosition.HasValue && manager.agent.isOnNavMesh)
        {
            manager.agent.SetDestination(manager.lastKnownPosition.Value);
        }
    }

    public override void ExitState(EnemyStateManager manager) { }

    public override void UpdateState(EnemyStateManager manager)
    {
        manager.viewAngle = manager.DistanceToPlayer() <= CloseRangeFullView ? 360f : manager.basicAngle;

        if (manager.CanSeePlayer())
        {
            manager.SwitchState(manager.AgroState);
            return;
        }

        if (!manager.agent.pathPending && manager.agent.remainingDistance <= manager.agent.stoppingDistance)
        {
            manager.lastKnownPosition = null;
            manager.SwitchState(manager.PatrolState);
        }
    }
}
