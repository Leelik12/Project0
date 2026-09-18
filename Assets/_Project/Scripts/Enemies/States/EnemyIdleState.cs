using UnityEngine;

/// <summary>Стоит на месте. Через timeIdle секунд уходит патрулировать (если есть точки), при виде игрока — в Agro.</summary>
public class EnemyIdleState : EnemyBaseState
{
    private float idleTimer;

    public override void EnterState(EnemyStateManager manager)
    {
        manager.animator.SetBool("isAgro", false);
        manager.animator.SetBool("isAttack", false);
        manager.animator.SetBool("isPatrol", false);
        manager.controller?.SetHoldIdle();
        manager.SetSpeed(0);
        idleTimer = 0f;
    }

    public override void ExitState(EnemyStateManager manager) { }

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
        }
    }
}
