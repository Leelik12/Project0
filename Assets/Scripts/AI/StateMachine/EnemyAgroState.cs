using UnityEngine;
public class EnemyAgroState : EnemyBaseState
{
    public override void EnterState(EnemyStateManager manager)
    {
        manager.animator.SetBool("isAgro", true);
        manager.animator.SetBool("isAttack", false);
        manager.animator.SetBool("isPatrol", false);
        Debug.Log("Входим в Agro");
        manager.SetSpeed(manager.runSpeed);
        manager.SetDistance(manager.player);
        if (manager.controller != null)
        {
            Debug.Log($"{this} переложил {manager.controller.weapon}");
            manager.controller.SetHoldRun();
        }
    }
    public override void ExitState(EnemyStateManager manager)
    {
        Debug.Log("Выходим из Agro");
    }
    public override void UpdateState(EnemyStateManager manager)
    {
        if (!manager.CanSeePlayer() && !manager.isAgroFromInfection && !manager.isTakeDamage)
        {
            manager.SwitchState(manager.IdleState);
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