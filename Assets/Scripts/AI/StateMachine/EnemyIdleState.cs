using UnityEngine;
public class EnemyIdleState : EnemyBaseState
{
    private System.DateTime FirstDate;
    public override void EnterState(EnemyStateManager manager)
    {
        manager.animator.SetBool("isAgro", false);
        manager.animator.SetBool("isAttack", false);
        Debug.Log("Входим в Idle");
        FirstDate = System.DateTime.Now;
        manager.SetSpeed(0);
    }
    public override void ExitState(EnemyStateManager manager)
    {
        Debug.Log("Выходим из Idle");
    }
    public override void UpdateState(EnemyStateManager manager)
    {
        if (manager.DistanceToTarget() < manager.agroDistance)
        {
            manager.SwitchState(manager.AgroState);
            return;
        }
        if ((System.DateTime.Now - FirstDate).Seconds > 10)
        {
            manager.SwitchState(manager.PatrolState);
            return;
        }
    }
}