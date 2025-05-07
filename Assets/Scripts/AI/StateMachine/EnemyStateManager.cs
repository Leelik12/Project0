using MikeNspired.XRIStarterKit;
using System.Runtime.CompilerServices;
using System.Xml;
using UnityEngine;
using UnityEngine.AI;

public class EnemyStateManager : MonoBehaviour
{
    [Header("Main")]
    public Animator animator;
    public NavMeshAgent agent;
    public Transform player;
    [SerializeField] private LayerMask obstructionMask;

    [Header("Скорость ходьбы")]
    public float walkSpeed = 2f;
    public float runSpeed = 3f;
   
    [Header("Обзор врага")]
    [SerializeField] private float viewAngle = 120f;
    [SerializeField] private float viewDistance = 20f;
    [SerializeField] private float radiusInfection = 20f;

    [Header("Отрисовка")]
    [SerializeField] private bool drawOverview = true;
    [SerializeField] private bool drawRadiusInfection = false;

    [Header("Атака")]
    public float attackDistance = 1.6f;
    public bool isWeapon = false;
    [SerializeField] private bool infection = true;

    [Header("Патрулирование")]
    public float timeIdle = 10f;
    public bool stopAfterPatrol = false;
    public Transform[] patrolPoints;

    [HideInInspector] public bool isAgroFromInfection = false;
    [HideInInspector] public bool isTakeDamage = false;
    private int currentPatrolIndex = 0;
    private Transform target;
    [HideInInspector] public EnemyWeaponController controller;
    private VRGun weapon;

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
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player").transform;
        }
        target = player;
        if (isWeapon)
        {
            controller = GetComponent<EnemyWeaponController>();
            weapon = GetComponentInChildren<VRGun>();
        }
            SwitchState(IdleState);
        EnemyManager.Register(this);
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
            if (!Physics.Raycast(transform.position + Vector3.up * 1.7f * agent.transform.localScale.y, directionToPlayer, out RaycastHit hit, viewDistance, obstructionMask))
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
        if (drawOverview)
        {
            // Настройки
            int segments = 60; // Чем больше — тем плавнее сектор
            float angleStep = viewAngle / segments;
            Vector3 origin = transform.position + Vector3.up * 1.7f * agent.transform.localScale.y;

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
        if (drawRadiusInfection)
        {
            Gizmos.color = new Color(1f, 0f, 1f); // Пурпурный
            Vector3 center = transform.position;

            int segments = 360;
            float radius = radiusInfection;
            float angleStep = 360f / segments;

            Vector3 prevPoint = center + new Vector3(Mathf.Cos(0), 0, Mathf.Sin(0)) * radius;

            for (int i = 1; i <= segments; i++)
            {
                float angle = angleStep * i * Mathf.Deg2Rad;
                Vector3 nextPoint = center + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
                Gizmos.DrawLine(prevPoint, nextPoint);
                prevPoint = nextPoint;
            }
        }
    }

    public void CheckForNearbyAggro()
    {
        if (!infection) return;

        foreach (var other in EnemyManager.AllEnemies)
        {
            if (other == this) continue;

            float dist = Vector3.Distance(transform.position, other.transform.position);
            if (dist < radiusInfection)
            {
                Vector3 directionToOther = (other.transform.position - transform.position).normalized;
                Vector3 origin = transform.position + Vector3.up * 1.7f * agent.transform.localScale.y;

                if (!Physics.Raycast(origin, directionToOther, out RaycastHit hit, dist, obstructionMask))
                {
                    if (other.currentState == other.AgroState || other.currentState == other.AttackState)
                    {
                        if (currentState != AgroState && currentState != AttackState)
                        {
                            Debug.Log($"{name} заразился агро от {other.name} (видимость подтверждена)");
                            SwitchState(AgroState);
                            isAgroFromInfection = true;
                            break;
                        }
                    }
                }
            }
        }
    }
    private void OnDestroy()
    {
        EnemyManager.Unregister(this);
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

    void Shoot()
    {
        if (weapon == null)
        {
            Debug.LogWarning($"{name}: Нет оружия для стрельбы!");
            return;
        }
        weapon.Shoot();
    }
}
