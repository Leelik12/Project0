using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Смерть: тело становится рэгдоллом и отлетает от последнего попадания (у не-гуманоидов — анимация смерти),
/// агент и коллайдеры попаданий отключаются, объект удаляется через несколько секунд.
/// </summary>
public class EnemyDeathState : EnemyBaseState
{
    private const float AnimationDestroyDelay = 3.7f;
    private const float RagdollDestroyDelay = 8f;

    public override void EnterState(EnemyStateManager manager)
    {
        manager.enabled = false; // останавливает Update автомата
        manager.SetSpeed(0);

        NavMeshAgent agent = manager.GetComponent<NavMeshAgent>();
        if (agent != null) agent.enabled = false;

        foreach (Collider collider in manager.GetComponentsInChildren<Collider>())
        {
            collider.enabled = false;
        }

        // Оружие больше не держат: контроллер хвата не должен двигать его к точкам удержания
        if (manager.controller != null) manager.controller.enabled = false;

        // Рэгдолл сам включает коллайдеры своих костей
        bool ragdoll = EnemyRagdoll.TryActivate(manager.animator, manager.GetComponent<EnemyHealth>());
        if (!ragdoll) manager.animator.SetTrigger("Die");

        manager.StartCoroutine(WaitAndDestroy(manager, ragdoll ? RagdollDestroyDelay : AnimationDestroyDelay));
    }

    public override void ExitState(EnemyStateManager manager) { }
    public override void UpdateState(EnemyStateManager manager) { }

    private static IEnumerator WaitAndDestroy(EnemyStateManager manager, float delay)
    {
        yield return new WaitForSeconds(delay);
        Object.Destroy(manager.gameObject);
    }
}
