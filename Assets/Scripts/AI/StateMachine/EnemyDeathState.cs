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

        var agent = manager.GetComponent<NavMeshAgent>();
        if (agent != null) agent.enabled = false;

        var colliders = manager.GetComponentsInChildren<Collider>();
        foreach (var collider in colliders)
        {
            collider.enabled = false;
        }

        manager.StartCoroutine(WaitAndDie(manager, 3f));
    }

    private IEnumerator WaitAndDie(EnemyStateManager manager, float delay)
    {

        yield return new WaitForSeconds(delay);

        var particles = manager.GetComponentInChildren<ParticleSystem>();
        if (particles != null)
        {
            particles.Play();
        }

        yield return new WaitForSeconds(particles.main.duration + 0.7f);
        Object.Destroy(manager.gameObject);
    }
    public override void ExitState(EnemyStateManager manager)
    {
    }
    public override void UpdateState(EnemyStateManager manager)
    {
    }
}
