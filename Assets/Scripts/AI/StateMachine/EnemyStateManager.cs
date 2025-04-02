using UnityEngine;
using UnityEngine.AI;

public class EnemyStateManager : MonoBehaviour
{
    [SerializeField] NavMeshAgent agent;
    [SerializeField] public Transform player;
    [SerializeField] public float walkSpeed;
    [SerializeField] public float agroDistance;
    [SerializeField] public float attackDistance;
    [SerializeField] Transform[] patrolPoints;
    private int currentPatrolIndex = 0;
    Transform target;

    EnemyBaseState currentState;
    public EnemyIdleState IdleState = new EnemyIdleState();
    public EnemyAgroState AgroState = new EnemyAgroState();
    public EnemyAttackState AttackState = new EnemyAttackState();
    public EnemyPatrolState PatrolState = new EnemyPatrolState();

    public void SwitchState(EnemyBaseState newState)
    {
        if (currentState != null)
            currentState.ExitState(this);
        currentState = newState;
        currentState.EnterState(this);
    }

    private void Start()
    {
        target = player;
        SwitchState(IdleState);
    }

    private void Update()
    {
        SetDistance(target);
        agent.destination = target.position;
        currentState.UpdateState(this);
    }

    public void SetSpeed(float speed)
    {
        agent.speed = speed;
    }
    public void SetDistance(Transform distance)
    {
        target = distance;
    }

    public float DistanceToTarget()
    {
        return (transform.position - target.transform.position).magnitude;
    }
    public float DistanceToPlayer()
    {
        return (transform.position - player.transform.position).magnitude;
    }
    public Transform GetNextPatrolPoint()
    {
        if (patrolPoints.Length == 0) return transform;
        Transform point = patrolPoints[currentPatrolIndex];
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        return point;
    }
}
