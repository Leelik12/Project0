using System.Collections;
using UnityEngine;

/// <summary>
/// Огнестрел врага: hitscan-выстрел с разбросом по вызову Shoot() (событие анимации через EnemyStateManager).
/// Лазер показывает направление последнего выстрела. Перед выстрелом лазер вспыхивает и смотрит туда,
/// куда полетит пуля, — у игрока есть доля секунды, чтобы уйти с линии огня.
/// </summary>
public class EnemyGun : MonoBehaviour
{
    [Header("Подвижные части")]
    [SerializeField] private Transform movablePart;
    [SerializeField] private float recoilDistance = 0.2f;
    [SerializeField] private float recoilDuration = 0.1f;

    [Header("Настройки стрельбы")]
    public float damage;
    public float range = 100f;

    [Header("Разброс выстрелов")]
    [SerializeField] private float spreadAngle = 5f; // градусы

    [Header("Настройки лазера")]
    public bool laserEnabled = true;
    public LineRenderer laserLine;

    [Header("Muzzle Flash")]
    public ParticleSystem muzzleFlash;
    public Light muzzleLight;
    public float lightDuration;

    [Header("XR")]
    public Transform firePoint;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip shotSound;

    [Header("Декали")]
    [SerializeField] private GameObject hitEffectPrefabDust;
    [SerializeField] private GameObject hitEffectPrefabSparks;
    [SerializeField] private float hitEffectLifetime = 2f;
    [SerializeField] private float effectOffset = 0.01f;

    [Header("Предупреждение перед выстрелом")]
    [Tooltip("Сколько секунд лазер горит перед выстрелом. 0 — стрелять сразу")]
    [SerializeField] private float telegraphTime = 0.35f;
    [SerializeField] private Color telegraphColor = new(1f, 0.15f, 0.1f, 1f);
    [SerializeField] private float telegraphWidthMultiplier = 2.5f;

    private Vector3 lastShotLocalDirection = Vector3.forward; // в координатах firePoint: лазер поворачивается вместе со стволом
    private bool isTelegraphing;
    private EnemyHealth owner;
    private Color laserStartColor, laserEndColor;
    private float laserWidth = 1f;

    private void Awake()
    {
        owner = GetComponentInParent<EnemyHealth>();
        if (laserLine != null)
        {
            laserStartColor = laserLine.startColor;
            laserEndColor = laserLine.endColor;
            laserWidth = laserLine.widthMultiplier;
        }
    }

    /// <summary>Событие анимации: выстрел (с предупреждением, если telegraphTime > 0).</summary>
    public void Shoot()
    {
        if (isTelegraphing) return;

        // Направление с разбросом выбираем заранее — предупреждение показывает настоящую траекторию
        Vector3 localDirection = firePoint.InverseTransformDirection(GetSpreadDirection(firePoint.forward, spreadAngle));
        if (telegraphTime > 0f) StartCoroutine(TelegraphAndFire(localDirection));
        else Fire(localDirection);
    }

    private IEnumerator TelegraphAndFire(Vector3 localDirection)
    {
        isTelegraphing = true;
        lastShotLocalDirection = localDirection;
        SetLaserTelegraph(true);

        yield return new WaitForSeconds(telegraphTime);

        SetLaserTelegraph(false);
        isTelegraphing = false;
        if (owner != null && owner.IsDead) yield break; // убили, пока целился
        Fire(localDirection);
    }

    private void SetLaserTelegraph(bool on)
    {
        if (laserLine != null)
        {
            laserLine.startColor = on ? telegraphColor : laserStartColor;
            laserLine.endColor = on ? telegraphColor : laserEndColor;
            laserLine.widthMultiplier = on ? laserWidth * telegraphWidthMultiplier : laserWidth;
        }
        if (muzzleLight != null && !laserEnabled) muzzleLight.enabled = on; // без лазера предупреждает вспышка света
    }

    private void Fire(Vector3 localDirection)
    {
        if (muzzleFlash != null) muzzleFlash.Play();
        if (muzzleLight != null) StartCoroutine(MuzzleLightFlash());
        if (audioSource != null && shotSound != null) audioSource.PlayOneShot(shotSound);
        StartCoroutine(MoveRecoil());

        Vector3 direction = firePoint.TransformDirection(localDirection);
        lastShotLocalDirection = localDirection;

        bool hasHit = Physics.Raycast(firePoint.position, direction, out RaycastHit hit, range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        Tracer.Spawn(firePoint.position, hasHit ? hit.point : firePoint.position + direction * range, enemy: true,
            laserLine != null ? laserLine.sharedMaterial : null);
        if (!hasHit) return;

        Vector3 pos = hit.point + hit.normal * effectOffset;
        Quaternion rot = Quaternion.LookRotation(hit.normal);
        ObjectPool.Spawn(hitEffectPrefabDust, pos, rot, hitEffectLifetime);
        ObjectPool.Spawn(hitEffectPrefabSparks, pos, rot, hitEffectLifetime);

        if (hit.collider.CompareTag("Player"))
        {
            PlayerHealth player = hit.collider.GetComponentInParent<PlayerHealth>();
            if (player != null) player.PlayerTakeDamage(damage);
        }
    }

    private static Vector3 GetSpreadDirection(Vector3 forward, float angle)
    {
        float spreadX = Random.Range(-angle, angle);
        float spreadY = Random.Range(-angle, angle);
        return Quaternion.Euler(spreadY, spreadX, 0) * forward;
    }

    private IEnumerator MuzzleLightFlash()
    {
        muzzleLight.enabled = true;
        yield return new WaitForSeconds(lightDuration);
        muzzleLight.enabled = false;
    }

    private IEnumerator MoveRecoil()
    {
        if (movablePart == null) yield break;

        Vector3 originalLocalPos = movablePart.localPosition;
        Vector3 recoiledLocalPos = originalLocalPos + new Vector3(0f, 0f, -recoilDistance);
        float halfDur = recoilDuration * 0.5f;

        for (float t = 0f; t < halfDur; t += Time.deltaTime)
        {
            movablePart.localPosition = Vector3.Lerp(originalLocalPos, recoiledLocalPos, t / halfDur);
            yield return null;
        }
        for (float t = 0f; t < halfDur; t += Time.deltaTime)
        {
            movablePart.localPosition = Vector3.Lerp(recoiledLocalPos, originalLocalPos, t / halfDur);
            yield return null;
        }
        movablePart.localPosition = originalLocalPos;
    }

    private void Update()
    {
        if (!laserEnabled || laserLine == null || firePoint == null) return;

        Vector3 lastShotDirection = firePoint.TransformDirection(lastShotLocalDirection);
        Vector3 endPoint = Physics.Raycast(firePoint.position, lastShotDirection, out RaycastHit hit, range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            ? hit.point
            : firePoint.position + lastShotDirection * range;

        laserLine.enabled = true;
        laserLine.SetPosition(0, firePoint.position);
        laserLine.SetPosition(1, endPoint);
    }
}
