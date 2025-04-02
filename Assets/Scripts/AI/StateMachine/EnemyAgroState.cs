using UnityEngine;
public class EnemyAgroState : EnemyBaseState
{
    public override void EnterState(EnemyStateManager manager)
    {
        Debug.Log("Входим в Agro");
        manager.SetSpeed(manager.walkSpeed);
        manager.SetDistance(manager.player);
    }
    public override void ExitState(EnemyStateManager manager)
    {
        Debug.Log("Выходим из Agro");
    }
    public override void UpdateState(EnemyStateManager manager)
    {
        if (manager.DistanceToTarget() >= manager.agroDistance)
        {
            manager.SwitchState(manager.IdleState);
            return;
        }
        if (manager.DistanceToTarget() < manager.attackDistance)
        {
            manager.SwitchState(manager.AttackState);
            return;
        }
    }
}