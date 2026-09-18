using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>Смерть: анимация, отключение агента и коллайдеров, удаление объекта через несколько секунд.</summary>
public class EnemyDeathState : EnemyBaseState
{
    private const float DestroyDelay = 3.7f;

    public override void EnterState(EnemyStateManager manager)
    {
        manager.enabled = false; // останавливает Update автомата
        manager.animator.SetTrigger("Die");
        manager.SetSpeed(0);

        NavMeshAgent agent = manager.GetComponent<NavMeshAgent>();
        if (agent != null) agent.enabled = false;

        foreach (Collider collider in manager.GetComponentsInChildren<Collider>())
        {
            collider.enabled = false;
        }

        manager.StartCoroutine(WaitAndDestroy(manager));
    }

    public override void ExitState(EnemyStateManager manager) { }
    public override void UpdateState(EnemyStateManager manager) { }

    private static IEnumerator WaitAndDestroy(EnemyStateManager manager)
    {
        yield return new WaitForSeconds(DestroyDelay);
        Object.Destroy(manager.gameObject);
    }
}
