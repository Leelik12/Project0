using UnityEngine;
using UnityEngine.AI;
using System.Collections;
public class DeathState : EnemyBaseState
{
    public override void EnterState(EnemyStateManager manager)
    {
        manager.enabled = false;
        manager.animator.SetTrigger("Die");
        manager.SetSpeed(0);

        // Отключаем NavMeshAgent
        var agent = manager.GetComponent<NavMeshAgent>();
        if (agent != null) agent.enabled = false;

        // Отключаем все коллайдеры у объекта и его детей
        var colliders = manager.GetComponentsInChildren<Collider>();
        foreach (var collider in colliders)
        {
            collider.enabled = false;
        }

        // Запускаем корутину через менеджер, так как он может быть отключен
        manager.StartCoroutine(WaitAndDie(manager, manager.animator.GetCurrentAnimatorStateInfo(0).length + 0.1f));

        Debug.Log("Enemy is dead!");
    }

    private IEnumerator WaitAndDie(EnemyStateManager manager, float delay)
    {
        yield return new WaitForSeconds(delay);
        GameObject.Destroy(manager.gameObject);
    }
    public override void ExitState(EnemyStateManager manager)
    {
    }
    public override void UpdateState(EnemyStateManager manager)
    {
    }
}
