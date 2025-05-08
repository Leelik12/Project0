using UnityEngine;

public class EnemyAttackState : EnemyBaseState
{
    public override void EnterState(EnemyStateManager manager)
    {
        manager.SetSpeed(0);
        manager.animator.SetBool("isAttack", true);
        manager.agent.acceleration = 1000;
        if (manager.controller != null)
        {
            manager.controller.SetHoldAttack();
        }
        Debug.Log("Входим в Attack");
    }
    public override void ExitState(EnemyStateManager manager)
    {
        Debug.Log("Выходим из Attack");
        manager.agent.acceleration = 8;
    }
    public override void UpdateState(EnemyStateManager manager)
    {
        Debug.Log("Атака");

        if (manager.DistanceToPlayer() > manager.attackDistance && !manager.CanSeePlayer() && manager.lastKnownPosition.HasValue)
        {
            manager.SwitchState(manager.SearchState);
        }

        if (manager.DistanceToPlayer() > manager.attackDistance)
        {
            manager.SwitchState(manager.AgroState);
            return;
        }

        if (!manager.CanSeePlayer() && !manager.isAgroFromInfection )
        {
            manager.SwitchState(manager.IdleState);
            return;
        }

        Vector3 direction = (manager.player.position - manager.transform.position).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            manager.transform.rotation = Quaternion.Slerp(manager.transform.rotation, lookRotation, Time.deltaTime * 30f);
        }

    }
}