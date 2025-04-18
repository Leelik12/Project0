using System.Xml;
using UnityEngine;
using UnityEngine.AI;

public class EnemyStateManager : MonoBehaviour
{
    [SerializeField] public Animator animator;
    [SerializeField] public NavMeshAgent agent;
    [SerializeField] public Transform player;
    [SerializeField] public float walkSpeed;
    [SerializeField] public float runSpeed;
    [SerializeField] public float attackDistance;
    [SerializeField] public Transform[] patrolPoints;
    [SerializeField] Collider[] damagerCollaider;
    [SerializeField] private float viewAngle = 150f;
    [SerializeField] private float viewDistance = 20f;
    [SerializeField] private LayerMask obstructionMask;
    private int currentPatrolIndex = 0;
    Transform target;

    EnemyBaseState currentState;
    public EnemyIdleState IdleState = new EnemyIdleState();
    public EnemyAgroState AgroState = new EnemyAgroState();
    public EnemyAttackState AttackState = new EnemyAttackState();
    public EnemyPatrolState PatrolState = new EnemyPatrolState();
    public DeathState deathState = new DeathState();
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
    public void GetLastPatrolPoint()
    {
        if (currentPatrolIndex != 0)
        {
            currentPatrolIndex = (currentPatrolIndex - 1) % patrolPoints.Length;
        } else
        {
            currentPatrolIndex = patrolPoints.Length - 1;
        }
            Transform point = patrolPoints[currentPatrolIndex];
    }
    public void Die()
    {
        SwitchState(deathState);
    }

    public bool CanSeePlayer()
    {
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);

        if (angleToPlayer < viewAngle / 2f && DistanceToPlayer() < viewDistance)
        {
            if (!Physics.Raycast(transform.position + Vector3.up * 1.7f, directionToPlayer, out RaycastHit hit, viewDistance, obstructionMask))
            {
                return true;
            }

            if (hit.transform == player)
            {
                return true;
            }
        }

        return false;
    }

    private void OnDrawGizmos()
    {
        // Настройки
        int segments = 60; // Чем больше — тем плавнее сектор
        float angleStep = viewAngle / segments;
        Vector3 origin = transform.position + Vector3.up * 1.7f;

        Gizmos.color = new Color(1f, 1f, 0f, 0.25f); // Жёлтый полупрозрачный

        Vector3 prevPoint = origin + Quaternion.Euler(0, -viewAngle / 2f, 0) * transform.forward * viewDistance;

        for (int i = 1; i <= segments; i++)
        {
            float angle = -viewAngle / 2f + angleStep * i;
            Vector3 nextPoint = origin + Quaternion.Euler(0, angle, 0) * transform.forward * viewDistance;
            Gizmos.DrawLine(origin, nextPoint);
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }

        // Границы FOV
        Vector3 leftBoundary = Quaternion.Euler(0, -viewAngle / 2f, 0) * transform.forward;
        Vector3 rightBoundary = Quaternion.Euler(0, viewAngle / 2f, 0) * transform.forward;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(origin, origin + leftBoundary * viewDistance);
        Gizmos.DrawLine(origin, origin + rightBoundary * viewDistance);
    }
    void CheckConditions()
    {
        if (currentState == AttackState)
        {
            if (DistanceToTarget() >= attackDistance)
            {
                SwitchState(AgroState);
                return;
            }
        }
    }

    void OnOffDamager(int isOff)
    {
        if (isOff == 0)
        {
            for (int i = 0; i < damagerCollaider.Length; i++)
                damagerCollaider[i].enabled = false;
        }
        else
        {
            for (int i = 0; i < damagerCollaider.Length; i++)
                damagerCollaider[i].enabled = true;
        }
    }

}
