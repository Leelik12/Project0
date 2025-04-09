using UnityEngine;
using UnityEngine.AI;

public class GoToTarget : MonoBehaviour
{
    [SerializeField] NavMeshAgent agent;
    [SerializeField] Transform target;

    private void Update()
    {
        agent.destination = target.position;
    }
}
