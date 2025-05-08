using UnityEngine;

public class EnemySearchState : EnemyBaseState
{
    public override void EnterState(EnemyStateManager manager)
    {
        Debug.Log("Входим в Search");
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
            manager.agent.SetDestination(manager.lastKnownPosition.Value);
        }
    }

    public override void ExitState(EnemyStateManager manager)
    {
        Debug.Log("Выходим из Search");
    }

    public override void UpdateState(EnemyStateManager manager)
    {
        if (manager.CanSeePlayer())
        {
            manager.SwitchState(manager.AgroState);
            return;
        }
        if (!manager.agent.pathPending && manager.agent.remainingDistance <= 0.25f)
        {
            manager.lastKnownPosition = null;
            manager.SwitchState(manager.IdleState);
            return;
        }
    }
}
