using UnityEngine;

public class EnemyAttackState : EnemyBaseState
{
    public override void EnterState(EnemyStateManager manager)
    {
        manager.animator.SetBool("isAttack", true);
        manager.SetSpeed(0);
        Debug.Log("Входим в Attack");
    }
    public override void ExitState(EnemyStateManager manager)
    {
        Debug.Log("Выходим из Attack");
    }
    public override void UpdateState(EnemyStateManager manager)
    {
        Debug.Log("Атака");
    }
}