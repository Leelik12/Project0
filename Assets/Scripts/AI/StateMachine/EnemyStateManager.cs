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
    public Transform playerHead;
    [SerializeField] private LayerMask obstructionMask;

    [Header("Скорость ходьбы")]
    public float walkSpeed = 2f;
    public float runSpeed = 3f;
   
    [Header("Обзор врага")]
    [SerializeField] public float viewAngle = 120f;
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
    [HideInInspector] public Vector3? lastKnownPosition = null;
    private int currentPatrolIndex = 0;
    private Transform target;
    [HideInInspector] public EnemyWeaponController controller;
    [HideInInspector] public float basicAngle;
    private VRgunForEnemy weapon;

    EnemyBaseState currentState;
    public EnemyIdleState IdleState = new EnemyIdleState();
    public EnemyAgroState AgroState = new EnemyAgroState();
    public EnemyAttackState AttackState = new EnemyAttackState();
    public EnemyPatrolState PatrolState = new EnemyPatrolState();
    public EnemySearchState SearchState = new EnemySearchState();
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
        basicAngle = viewAngle;
        if (playerHead == null)
        {
            playerHead = Camera.main.transform; // Или твоя XR-камера напрямую
        }
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player").transform;
        }
        target = player;
        if (isWeapon)
        {
            controller = GetComponent<EnemyWeaponController>();
            weapon = GetComponentInChildren<VRgunForEnemy>();
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
        // Найдём голову игрока (в ВР это Camera.main.transform или твоя камера в XR Rig)
        Transform playerHead = Camera.main.transform;
        Vector3 playerHeadPos = playerHead.position;

        Vector3 eyeOrigin = transform.position + Vector3.up * 1.7f; // глаза врага
        Vector3 directionToHead = (playerHeadPos - eyeOrigin).normalized;
        float distanceToHead = Vector3.Distance(eyeOrigin, playerHeadPos);

        // Проверка угла
        float angleToHead = Vector3.Angle(transform.forward, directionToHead);
        if (angleToHead > viewAngle / 2f || distanceToHead > viewDistance)
            return false;

        // Здесь пытаемся сначала использовать простой Raycast
        RaycastHit hit;
        if (Physics.Raycast(eyeOrigin, directionToHead, out hit, distanceToHead, obstructionMask, QueryTriggerInteraction.Ignore))
        {
            Debug.Log($"Raycast blocked by: {hit.collider.gameObject.name}");
            return false;
        }

        // Дополнительная проверка с помощью SphereCast
        if (Physics.SphereCast(eyeOrigin, 0.1f, directionToHead, out hit, distanceToHead, obstructionMask, QueryTriggerInteraction.Ignore))
        {
            Debug.Log($"SphereCast blocked by: {hit.collider.gameObject.name}");
            return false;
        }

        // Проверка с использованием CheckSphere, чтобы исключить мелкие объекты
        if (Physics.CheckSphere(eyeOrigin, 0.1f, obstructionMask))
        {
            return false;  // Если есть преграда — игрок не виден
        }

        // Видит игрока
        lastKnownPosition = playerHeadPos;
        return true;
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

            if (player != null && agent != null)
            {
                Vector3 headPosition = transform.position + Vector3.up * 1.7f * agent.transform.localScale.y;
                Vector3 smallForward = transform.forward * 0.2f;
                origin = headPosition + smallForward;

                Vector3 directionToPlayer = (player.position - origin).normalized;
                float distanceToPlayer = Vector3.Distance(origin, player.position);

                RaycastHit hit;

                // Raycast (с маской препятствий)
                if (Physics.Raycast(origin, directionToPlayer, out hit, distanceToPlayer, obstructionMask, QueryTriggerInteraction.Ignore))
                {
                    // Нарисовать линию до точки столкновения
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(origin, hit.point);

                    // Шарики
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawSphere(origin, 0.1f); // начало

                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(hit.point, 0.15f); // место столкновения
                }
                else
                {
                    // Если не врезался — нарисовать до игрока
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(origin, player.position);

                    Gizmos.color = Color.yellow;
                    Gizmos.DrawSphere(origin, 0.1f);

                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(player.position, 0.15f);
                }
            }

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

    public void OnDamageTaken()
    {
        isTakeDamage = true;
        if (currentState != AgroState && currentState != AttackState && currentState != deathState)
        {
            SwitchState(AgroState);
        }
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
