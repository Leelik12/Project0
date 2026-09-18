/// <summary>Ходит по точкам патруля. При виде игрока запоминает текущую точку и переходит в Agro.</summary>
public class EnemyPatrolState : EnemyBaseState
{
    private const float ArriveDistance = 0.25f;

    public override void EnterState(EnemyStateManager manager)
    {
        manager.animator.SetBool("isPatrol", true);
        manager.SetDistance(manager.GetNextPatrolPoint());
        manager.SetSpeed(manager.walkSpeed);
        manager.viewAngle = manager.basicAngle;
        manager.controller?.SetHoldWalk();
    }

    public override void ExitState(EnemyStateManager manager) { }

    public override void UpdateState(EnemyStateManager manager)
    {
        manager.CheckForNearbyAggro();

        if (manager.CanSeePlayer())
        {
            manager.GetLastPatrolPoint();
            manager.SwitchState(manager.AgroState);
            return;
        }

        if (manager.agent.pathPending || manager.agent.remainingDistance > ArriveDistance) return;

        if (manager.stopAfterPatrol)
        {
            manager.animator.SetBool("isPatrol", false);
            manager.SwitchState(manager.IdleState);
        }
        else
        {
            manager.SetDistance(manager.GetNextPatrolPoint());
        }
    }
}
