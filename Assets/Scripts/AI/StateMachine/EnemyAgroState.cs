using UnityEngine;
public class EnemyAgroState : EnemyBaseState
{
    public override void EnterState(EnemyStateManager manager)
    {
        manager.animator.SetBool("isAgro", true);
        manager.animator.SetBool("isAttack", false);
        manager.animator.SetBool("isPatrol", false);
        manager.viewAngle = manager.basicAngle;
        Debug.Log("Входим в Agro");
        if (manager.controller != null)
        {
            manager.controller.SetHoldRun();
        }
        manager.SetSpeed(manager.runSpeed);
        manager.SetDistance(manager.player);
    }
    public override void ExitState(EnemyStateManager manager)
    {
        Debug.Log("Выходим из Agro");
    }
    public override void UpdateState(EnemyStateManager manager)
    {
       

        if (!manager.CanSeePlayer() && !manager.isAgroFromInfection && !manager.isTakeDamage)
        {
            if (manager.lastKnownPosition.HasValue)
            {
                manager.SwitchState(manager.SearchState);
            }
            else
            {
                manager.SwitchState(manager.IdleState);
            }
            return;
        }
        if (manager.CanSeePlayer())
        {
            manager.isTakeDamage = false;
            manager.isAgroFromInfection = false;
        }
        if (manager.DistanceToTarget() < manager.attackDistance)
        {
            manager.SwitchState(manager.AttackState);
            return;
        }
    }

}