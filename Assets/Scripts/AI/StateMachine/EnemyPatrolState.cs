using UnityEngine;

public class EnemyPatrolState : EnemyBaseState
{
    public override void EnterState(EnemyStateManager manager)
    {
        manager.animator.SetBool("isPatrol", true);
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

        if (manager.CanSeePlayer())
        {
            manager.GetLastPatrolPoint();
            manager.SwitchState(manager.AgroState);
            return;
        }

        if (!manager.agent.pathPending && manager.agent.remainingDistance <= 0.25f)
        {
            if (manager.stopAfterPatrol)
            {
                manager.SwitchState(manager.IdleState);
            }
            else
            {
                manager.SetDistance(manager.GetNextPatrolPoint());
            }
            return;
        }


    }
}