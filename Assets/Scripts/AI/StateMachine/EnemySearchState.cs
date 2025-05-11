using UnityEngine;

public class EnemySearchState : EnemyBaseState
{
    public override void EnterState(EnemyStateManager manager)
    {
        manager.animator.SetBool("isAgro", true);
        manager.animator.SetBool("isAttack", false);
        manager.animator.SetBool("isPatrol", false);

        if (manager.controller != null)
        {
            manager.controller.SetHoldRun();
        }

        manager.SetSpeed(manager.runSpeed);

        if (manager.lastKnownPosition.HasValue)
        {
            Vector3 dest = manager.lastKnownPosition.Value;
            manager.agent.SetDestination(dest);
        }
    }

    public override void ExitState(EnemyStateManager manager)
    {
    }

    public override void UpdateState(EnemyStateManager manager)
    {
        if (manager.DistanceToPlayer() <= 1f)
        {
            manager.viewAngle = 360f;
        } else
        {
            manager.viewAngle = manager.basicAngle;
        }
        if (manager.CanSeePlayer())
        {
            manager.SwitchState(manager.AgroState);
            return;
        }

        if (!manager.agent.pathPending && manager.agent.remainingDistance <= manager.agent.stoppingDistance)
        {
            manager.lastKnownPosition = null;
            manager.SwitchState(manager.PatrolState);
            return;
        }
    }

}
