using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Конечный автомат врага: Idle → Patrol → Agro → Attack, Search (потерял игрока), Death.
/// Зрение — конус FOV + луч до головы игрока. «Заражение» агрессией от соседей в радиусе.
/// Методы CheckConditions / Shoot / PlayFootstep вызываются событиями анимаций — не переименовывать.
/// </summary>
public class EnemyStateManager : MonoBehaviour
{
    private const float EyeHeight = 1.7f;

    [Header("Main")]
    public Animator animator;
    public NavMeshAgent agent;
    public Transform player;       // XR Origin (тег Player)
    public Transform playerHead;   // камера игрока; если пусто — Camera.main
    [SerializeField] private LayerMask obstructionMask;

    [Header("Скорость")]
    public float walkSpeed = 2f;
    public float runSpeed = 3f;

    [Header("Зрение")]
    [SerializeField] public float viewAngle = 120f;
    [SerializeField] private float viewDistance = 20f;
    [SerializeField] private float radiusInfection = 20f; // радиус «заражения» агрессией

    [Header("Отладка")]
    [SerializeField] private bool drawOverview = true;
    [SerializeField] private bool drawRadiusInfection = false;

    [Header("Атака")]
    public float attackDistance = 1.6f;
    public bool isWeapon = false;                 // есть ли огнестрел
    [SerializeField] private bool infection = true; // реагирует ли на агрессию соседей

    [Header("Патрулирование")]
    public float timeIdle = 10f;
    public bool stopAfterPatrol = false;
    public Transform[] patrolPoints;

    [Header("Звук шагов")]
    public AudioClip dirtWalkClip;
    public AudioClip metalWalkClip;
    public AudioSource footstepAudioSource;
    public LayerMask groundLayerMask;

    [HideInInspector] public bool isAgroFromInfection;
    [HideInInspector] public bool isTakeDamage;
    [HideInInspector] public Vector3? lastKnownPosition;
    [HideInInspector] public EnemyWeaponController controller;
    [HideInInspector] public float basicAngle;

    private int currentPatrolIndex;
    private Transform target;
    private EnemyGun weapon;

    public EnemyBaseState CurrentState { get; private set; }
    public readonly EnemyIdleState IdleState = new();
    public readonly EnemyAgroState AgroState = new();
    public readonly EnemyAttackState AttackState = new();
    public readonly EnemyPatrolState PatrolState = new();
    public readonly EnemySearchState SearchState = new();
    public readonly EnemyDeathState DeathState = new();

    // ---------- Жизненный цикл ----------

    private void Start()
    {
        basicAngle = viewAngle;

        if (playerHead == null && Camera.main != null) playerHead = Camera.main.transform;
        if (player == null)
        {
            GameObject playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo != null) player = playerGo.transform;
        }
        if (player == null)
        {
            Debug.LogError($"[{name}] Не найден объект с тегом Player — враг отключён.");
            enabled = false;
            return;
        }
        target = player;

        if (isWeapon)
        {
            controller = GetComponent<EnemyWeaponController>();
            weapon = GetComponentInChildren<EnemyGun>();
        }

        EnemyManager.Register(this);
        SwitchState(IdleState);
    }

    private void Update()
    {
        if (agent.isActiveAndEnabled && agent.isOnNavMesh && target != null)
        {
            agent.destination = target.position;
        }
        CurrentState?.UpdateState(this);
    }

    private void OnDestroy()
    {
        EnemyManager.Unregister(this);
    }

    public void SwitchState(EnemyBaseState newState)
    {
        CurrentState?.ExitState(this);
        CurrentState = newState;
        CurrentState.EnterState(this);
    }

    // ---------- Движение и цели ----------

    public void SetSpeed(float speed) => agent.speed = speed;
    public void SetDistance(Transform newTarget) => target = newTarget;
    public float DistanceToTarget() => target != null ? Vector3.Distance(transform.position, target.position) : float.MaxValue;
    public float DistanceToPlayer() => Vector3.Distance(transform.position, player.position);

    public Transform GetNextPatrolPoint()
    {
        if (patrolPoints.Length == 0) return transform;
        Transform point = patrolPoints[currentPatrolIndex];
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        return point;
    }

    /// <summary>Откатывает индекс патруля на шаг назад, чтобы после погони вернуться к прерванной точке.</summary>
    public void GetLastPatrolPoint()
    {
        if (patrolPoints.Length == 0) return;
        currentPatrolIndex = (currentPatrolIndex - 1 + patrolPoints.Length) % patrolPoints.Length;
    }

    // ---------- Здоровье ----------

    public void Die() => SwitchState(DeathState);

    public void OnDamageTaken()
    {
        isTakeDamage = true;
        if (CurrentState != AgroState && CurrentState != AttackState && CurrentState != DeathState)
        {
            SwitchState(AgroState);
        }
    }

    // ---------- Зрение ----------

    public bool CanSeePlayer()
    {
        if (playerHead == null) return false;

        Vector3 eyeOrigin = transform.position + Vector3.up * EyeHeight;
        Vector3 headPos = playerHead.position;
        Vector3 dir = (headPos - eyeOrigin).normalized;
        float dist = Vector3.Distance(eyeOrigin, headPos);

        if (dist > viewDistance) return false;
        if (Vector3.Angle(transform.forward, dir) > viewAngle / 2f) return false;

        if (Physics.Raycast(eyeOrigin, dir, dist, obstructionMask, QueryTriggerInteraction.Ignore)) return false;
        if (Physics.SphereCast(eyeOrigin, 0.1f, dir, out _, dist, obstructionMask, QueryTriggerInteraction.Ignore)) return false;
        if (Physics.CheckSphere(eyeOrigin, 0.1f, obstructionMask)) return false; // глаза внутри стены

        lastKnownPosition = headPos;
        return true;
    }

    /// <summary>Если сосед в радиусе уже в бою и между нами нет стены — тоже переходим в агрессию.</summary>
    public void CheckForNearbyAggro()
    {
        if (!infection) return;
        if (CurrentState == AgroState || CurrentState == AttackState) return;

        Vector3 origin = transform.position + Vector3.up * EyeHeight * transform.localScale.y;

        foreach (EnemyStateManager other in EnemyManager.AllEnemies)
        {
            if (other == this) continue;
            if (other.CurrentState != other.AgroState && other.CurrentState != other.AttackState) continue;

            float dist = Vector3.Distance(transform.position, other.transform.position);
            if (dist >= radiusInfection) continue;

            Vector3 dir = (other.transform.position - transform.position).normalized;
            if (Physics.Raycast(origin, dir, dist, obstructionMask)) continue;

            isAgroFromInfection = true;
            SwitchState(AgroState);
            return;
        }
    }

    // ---------- События анимации ----------

    // Animation Event в клипе атаки: если игрок отошёл — выходим из атаки
    private void CheckConditions()
    {
        if (CurrentState == AttackState && DistanceToTarget() >= attackDistance)
        {
            SwitchState(AgroState);
        }
    }

    // Animation Event в клипе стрельбы
    private void Shoot()
    {
        if (weapon != null) weapon.Shoot();
    }

    // Animation Event в клипах ходьбы/бега
    public void PlayFootstep()
    {
        if (footstepAudioSource == null) return;

        Vector3 origin = transform.position + Vector3.up * 0.3f;
        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 1.5f, groundLayerMask)) return;

        AudioClip clip = null;
        if (hit.collider.CompareTag("Metal"))
        {
            footstepAudioSource.maxDistance = 3f;
            footstepAudioSource.volume = 0.6f;
            clip = metalWalkClip;
        }
        else if (hit.collider.CompareTag("Dirt"))
        {
            footstepAudioSource.maxDistance = 3f;
            footstepAudioSource.volume = 0.05f;
            clip = dirtWalkClip;
        }

        if (clip != null) footstepAudioSource.PlayOneShot(clip);
    }

    // ---------- Отладка ----------

    private void OnDrawGizmos()
    {
        if (drawOverview) DrawViewCone();
        if (drawRadiusInfection) DrawCircle(transform.position, radiusInfection, new Color(1f, 0f, 1f));
    }

    private void DrawViewCone()
    {
        float scaleY = agent != null ? agent.transform.localScale.y : 1f;
        Vector3 origin = transform.position + Vector3.up * EyeHeight * scaleY;

        const int segments = 60;
        float step = viewAngle / segments;
        Gizmos.color = new Color(1f, 1f, 0f, 0.25f);
        Vector3 prev = origin + Quaternion.Euler(0, -viewAngle / 2f, 0) * transform.forward * viewDistance;
        for (int i = 1; i <= segments; i++)
        {
            Vector3 next = origin + Quaternion.Euler(0, -viewAngle / 2f + step * i, 0) * transform.forward * viewDistance;
            Gizmos.DrawLine(origin, next);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }

        Gizmos.color = Color.red;
        Gizmos.DrawLine(origin, origin + Quaternion.Euler(0, -viewAngle / 2f, 0) * transform.forward * viewDistance);
        Gizmos.DrawLine(origin, origin + Quaternion.Euler(0, viewAngle / 2f, 0) * transform.forward * viewDistance);

        if (player == null) return;

        Vector3 rayOrigin = origin + transform.forward * 0.2f;
        Vector3 toPlayer = (player.position - rayOrigin).normalized;
        float dist = Vector3.Distance(rayOrigin, player.position);
        bool blocked = Physics.Raycast(rayOrigin, toPlayer, out RaycastHit hit, dist, obstructionMask, QueryTriggerInteraction.Ignore);
        Vector3 end = blocked ? hit.point : player.position;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(rayOrigin, end);
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(rayOrigin, 0.1f);
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(end, 0.15f);
    }

    private static void DrawCircle(Vector3 center, float radius, Color color)
    {
        Gizmos.color = color;
        const int segments = 72;
        Vector3 prev = center + Vector3.right * radius;
        for (int i = 1; i <= segments; i++)
        {
            float a = 2f * Mathf.PI * i / segments;
            Vector3 next = center + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * radius;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}
