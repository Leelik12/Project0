using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Камикадзе (E-07): бежит к игроку (погоню ведёт обычный EnemyStateManager), пищит чаще по мере приближения,
/// а вблизи поджигает запал и подрывается. Убитый раньше взрывается на месте и задевает своих.
/// Убийство выстрелом в голову — «обезвреженный» подрыв: радиус и урон уменьшены (headshotScale).
/// Урон игроку и врагам идёт только по прямой видимости от центра взрыва, к краю слабеет.
/// </summary>
public class KamikazeEnemy : MonoBehaviour
{
    private const float ChestHeight = 1.1f;
    private const float MinFalloff = 0.4f;
    private const int BufferSize = 512; // в плотных уровнях в радиус попадает много статики

    private static readonly Collider[] OverlapBuffer = new Collider[BufferSize];
    private static readonly RaycastHit[] LineBuffer = new RaycastHit[64];
    private static readonly List<EnemyHealth> Processed = new();

    [Header("Подрыв")]
    [Tooltip("Горизонтальная дистанция до головы игрока, с которой поджигается запал")]
    [SerializeField] private float detonateDistance = 2f;
    [SerializeField] private float fuseTime = 0.6f;
    [SerializeField] private float radius = 3.5f;
    [SerializeField] private float playerDamage = 60f;
    [SerializeField] private float enemyDamage = 60f;
    [Tooltip("Множитель радиуса и урона, если камикадзе убили выстрелом в голову")]
    [SerializeField, Range(0f, 1f)] private float headshotScale = 0.5f;

    [Header("Писк")]
    [SerializeField] private float beepSlowInterval = 0.8f;
    [SerializeField] private float beepFastInterval = 0.1f;
    [SerializeField] private float beepFullDistance = 15f; // дальше — писк самый редкий
    [SerializeField] private AudioSource beepSource;

    [Header("Бомба")]
    [Tooltip("Модель заряда: прячется после взрыва")]
    [SerializeField] private GameObject bombVisual;

    private EnemyStateManager state;
    private EnemyHealth health;
    private float fuseEndTime = -1f;
    private float nextBeepTime;
    private bool exploded;

    public bool IsFuseLit => fuseEndTime >= 0f;
    public bool HasExploded => exploded;
    public float Radius => radius;

    private void Awake()
    {
        state = GetComponent<EnemyStateManager>();
        health = GetComponent<EnemyHealth>();
        if (beepSource == null)
        {
            beepSource = gameObject.AddComponent<AudioSource>();
            beepSource.playOnAwake = false;
            beepSource.spatialBlend = 1f;
            beepSource.maxDistance = 30f;
        }
    }

    private void OnEnable() => GameEvents.EnemyHit += OnEnemyHit;
    private void OnDisable() => GameEvents.EnemyHit -= OnEnemyHit;

    private void OnEnemyHit(EnemyHitInfo hit)
    {
        // Убили, пока не успел подорваться — взрыв на месте смерти
        if (hit.Enemy == health && hit.Killed && !exploded) Explode(hit.Headshot ? headshotScale : 1f);
    }

    private void Update()
    {
        if (exploded || health.IsDead || state == null || state.player == null || state.IsStunned) return;

        // Пищит и подрывается только тот, кто уже охотится на игрока
        bool hunting = state.CurrentState == state.AgroState || state.CurrentState == state.AttackState;
        if (!hunting && !IsFuseLit) return; // запал уже горит — взрыв неизбежен, даже если игрока потеряли из виду

        float distance = FlatDistanceToPlayer();

        if (!IsFuseLit && distance <= detonateDistance) LightFuse();
        if (IsFuseLit && Time.time >= fuseEndTime)
        {
            Detonate();
            return;
        }

        UpdateBeep(distance);
    }

    private float FlatDistanceToPlayer()
    {
        Transform head = state.playerHead != null ? state.playerHead : state.player;
        Vector3 offset = head.position - transform.position;
        offset.y = 0f;
        return offset.magnitude;
    }

    private void LightFuse()
    {
        fuseEndTime = Time.time + fuseTime;
        nextBeepTime = 0f; // писк сразу
        if (state.agent != null && state.agent.isActiveAndEnabled && state.agent.isOnNavMesh) state.agent.isStopped = true; // замер перед взрывом
    }

    private void UpdateBeep(float distance)
    {
        if (Time.time < nextBeepTime) return;

        float closeness = IsFuseLit ? 1f : 1f - Mathf.Clamp01(distance / beepFullDistance);
        nextBeepTime = Time.time + Mathf.Lerp(beepSlowInterval, beepFastInterval, closeness);
        beepSource.pitch = Mathf.Lerp(0.9f, 1.7f, closeness);
        beepSource.PlayOneShot(ProceduralSounds.MissileBeep, 0.9f);
    }

    /// <summary>Подрыв по запалу: взрыв, затем враг считается погибшим.</summary>
    private void Detonate()
    {
        Explode(1f);
        health.TakeDamage(health.maxHealth * 100f, transform.position + Vector3.up * ChestHeight, Vector3.up, false);
    }

    /// <summary>Взрыв в центре груди. scale уменьшает радиус и урон (обезвреженный подрыв).</summary>
    public void Explode(float scale = 1f)
    {
        if (exploded) return;
        exploded = true;

        Vector3 center = transform.position + Vector3.up * ChestHeight;
        float blastRadius = radius * scale;
        if (bombVisual != null) bombVisual.SetActive(false);

        ExplosionFlash.Spawn(center, blastRadius);
        AudioSource.PlayClipAtPoint(ProceduralSounds.Explosion, center, 1f);

        DamagePlayer(center, blastRadius, playerDamage * scale);
        DamageEnemies(center, blastRadius, enemyDamage * scale);
    }

    private void DamagePlayer(Vector3 center, float blastRadius, float damage)
    {
        PlayerHealth player = PlayerHealth.Current;
        if (player == null || state == null || state.player == null) return;

        Transform head = state.playerHead != null ? state.playerHead : state.player;
        float distance = Vector3.Distance(center, head.position);
        if (distance > blastRadius || !HasClearPath(center, head.position, player.transform)) return;

        player.PlayerTakeDamage(damage * Falloff(distance, blastRadius));
    }

    private void DamageEnemies(Vector3 center, float blastRadius, float damage)
    {
        int count = Physics.OverlapSphereNonAlloc(center, blastRadius, OverlapBuffer, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        Processed.Clear();
        for (int i = 0; i < count; i++)
        {
            EnemyHealth enemy = OverlapBuffer[i].GetComponentInParent<EnemyHealth>();
            if (enemy == null || enemy == health || enemy.IsDead || Processed.Contains(enemy)) continue;

            // Берём первую часть тела врага, которую видно из центра взрыва
            if (!HasClearPath(center, OverlapBuffer[i].bounds.center, enemy.transform)) continue;

            Processed.Add(enemy);
            float distance = Vector3.Distance(center, OverlapBuffer[i].ClosestPoint(center));
            Vector3 direction = (enemy.transform.position - center).normalized;
            enemy.TakeDamage(damage * Falloff(distance, blastRadius), center, direction, false);
        }
        Processed.Clear();
    }

    private static float Falloff(float distance, float blastRadius) =>
        Mathf.Lerp(1f, MinFalloff, Mathf.Clamp01(distance / Mathf.Max(0.01f, blastRadius)));

    // Первое препятствие на пути (кроме самого камикадзе и трупов) — цель или её часть
    private bool HasClearPath(Vector3 from, Vector3 to, Transform target)
    {
        Vector3 toTarget = to - from;
        float length = toTarget.magnitude;
        if (length < 0.001f) return true;

        int count = Physics.RaycastNonAlloc(from, toTarget / length, LineBuffer, length, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        System.Array.Sort(LineBuffer, 0, count, DistanceComparer.Instance);
        for (int i = 0; i < count; i++)
        {
            Transform hitTransform = LineBuffer[i].collider.transform;
            if (hitTransform.IsChildOf(transform)) continue;
            if (hitTransform.IsChildOf(target)) return true;

            EnemyHealth owner = hitTransform.GetComponentInParent<EnemyHealth>();
            if (owner != null && owner.IsDead) continue;
            return false;
        }
        return true;
    }

    private sealed class DistanceComparer : IComparer<RaycastHit>
    {
        public static readonly DistanceComparer Instance = new();
        public int Compare(RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance);
    }
}
