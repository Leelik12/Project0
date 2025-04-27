using UnityEngine;
public class EnemyIdleState : EnemyBaseState
{
    private float idleTimer = 0f;

    public override void EnterState(EnemyStateManager manager)
    {
        manager.animator.SetBool("isAgro", false);
        manager.animator.SetBool("isAttack", false);
        manager.animator.SetBool("isPatrol", false);
        if (manager.controller != null)
        {
            manager.controller.SetHoldIdle();
        }
        Debug.Log("Входим в Idle");
        idleTimer = 0f;
        manager.SetSpeed(0);
    }

    public override void ExitState(EnemyStateManager manager)
    {
        Debug.Log("Выходим из Idle");
    }

    public override void UpdateState(EnemyStateManager manager)
    {
        manager.CheckForNearbyAggro();

        if (manager.CanSeePlayer())
        {
            manager.SwitchState(manager.AgroState);
            return;
        }

        idleTimer += Time.deltaTime;

        if (idleTimer > manager.timeIdle && manager.patrolPoints.Length != 0)
        {
            manager.SwitchState(manager.PatrolState);
            return;
        }
    }
}