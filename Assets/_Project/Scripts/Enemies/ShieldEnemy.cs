using UnityEngine;

/// <summary>
/// Щитовик (E-04): держит баллистический щит перед корпусом. Выстрелы и удары в щит урона врагу не наносят
/// (коллайдер щита без тегов Head/Body/Leg, см. ShieldPlate), зато щит звенит и копит повреждения.
/// Набрав absorbLimit, щит выбивается из рук — дальше враг уязвим. Обходить с фланга, бить в голову поверх щита,
/// по ногам или взрывчаткой (взрыв считается по частям тела врага, щит его не прикрывает).
/// </summary>
public class ShieldEnemy : MonoBehaviour
{
    [Header("Щит")]
    [Tooltip("Корень щита (модель + коллайдер), закреплён на кости корпуса")]
    [SerializeField] private Transform shield;
    [Tooltip("Сколько урона щит принимает, прежде чем его выбьет из рук")]
    [SerializeField] private float absorbLimit = 150f;
    [SerializeField] private AudioSource audioSource;
    [SerializeField, Range(0f, 1f)] private float pingVolume = 0.8f;

    [Header("Выбитый щит")]
    [SerializeField] private float knockForce = 3f;
    [SerializeField] private float lifetimeAfterBreak = 6f;

    private float absorbed;
    private bool broken;
    private EnemyHealth health;

    public bool IsBroken => broken;
    public float Absorbed => absorbed;

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();
        if (audioSource == null && shield != null)
        {
            audioSource = shield.gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            audioSource.maxDistance = 25f;
        }
    }

    private void OnEnable() => GameEvents.EnemyKilled += OnEnemyKilled;
    private void OnDisable() => GameEvents.EnemyKilled -= OnEnemyKilled;

    /// <summary>Попадание в щит: звон, накопление урона, при превышении лимита щит выбивается.</summary>
    public void Absorb(Vector3 point, float damage)
    {
        if (broken) return;

        absorbed += damage;
        Ping(pingVolume);
        if (absorbed >= absorbLimit) Break(true);
    }

    private void Ping(float volume)
    {
        if (audioSource == null) return;
        audioSource.pitch = Random.Range(0.9f, 1.15f);
        audioSource.PlayOneShot(ProceduralSounds.ShieldPing, volume);
    }

    // Враг убит — щит просто падает из рук
    private void OnEnemyKilled(EnemyHealth enemy)
    {
        if (enemy == health) Break(false);
    }

    private void Break(bool reactToIt)
    {
        if (broken || shield == null) return;
        broken = true;

        ShieldPlate plate = shield.GetComponentInChildren<ShieldPlate>();
        if (plate != null) Destroy(plate); // выбитый щит — обычный предмет: попадания по нему больше не «звенят»

        Vector3 forward = transform.forward;
        shield.SetParent(null, true);
        var body = shield.gameObject.AddComponent<Rigidbody>();
        body.mass = 6f;
        body.AddForce((forward + Vector3.up * 0.6f) * knockForce, ForceMode.Impulse);
        body.AddTorque(Random.onUnitSphere * 2f, ForceMode.Impulse);
        Destroy(shield.gameObject, lifetimeAfterBreak);

        if (reactToIt)
        {
            Ping(1f);
            // Щит выбили — враг злится и бросается в бой
            GetComponent<EnemyStateManager>()?.OnDamageTaken();
        }
    }
}
