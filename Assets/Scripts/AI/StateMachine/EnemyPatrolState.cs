using UnityEngine;

public class EnemyPatrolState : EnemyBaseState
{
    public override void EnterState(EnemyStateManager manager)
    {
        Debug.Log("Входим в Patrol");
        manager.SetDistance(manager.GetNextPatrolPoint());
        manager.SetSpeed(manager.walkSpeed);
    }
    public override void ExitState(EnemyStateManager manager)
    {
        Debug.Log("Выходим из Patrol");
    }
    public override void UpdateState(EnemyStateManager manager)
    {

        if (manager.DistanceToPlayer() < manager.agroDistance)
        {
            manager.SwitchState(manager.AgroState);
            return;
        }

        if (manager.DistanceToTarget() < 1)
        {
            manager.SetDistance(manager.GetNextPatrolPoint());
            return;
        }

        
    }
}
