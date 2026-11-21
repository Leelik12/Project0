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

    [Header("Прицеливание снайпера")]
    [Tooltip("Сколько секунд лазер следит за игроком перед выстрелом. 0 — обычное предупреждение без слежения")]
    [SerializeField] private float trackTime = 0f;
    [Tooltip("Сколько секунд лазер замирает перед выстрелом после слежения")]
    [SerializeField] private float lockTime = 0.3f;
    [Tooltip("Пауза между выстрелами («перезарядка»)")]
    [SerializeField] private float shotCooldown = 0f;
    [Tooltip("Сколько секунд можно не видеть игрока, прежде чем прицеливание сорвётся")]
    [SerializeField] private float lostSightGrace = 0.3f;
    [SerializeField] private Color lockColor = new(1f, 1f, 0.85f, 1f);
    [SerializeField] private float lockWidthMultiplier = 4f;

    [Header("Трофей")]
    [Tooltip("Версия этого оружия для игрока: выпадает из убитого врага. Пусто — ничего не выпадает")]
    [SerializeField] private GameObject pickupPrefab;
    [Tooltip("Какая доля магазина остаётся в выпавшем оружии (случайно в пределах)")]
    [SerializeField] private Vector2 dropAmmoFraction = new(0.3f, 0.8f);
    [Tooltip("Сколько выстрелов остаётся в выпавшем лазерном оружии")]
    [SerializeField] private int laserDropShots = 20;

    private bool dropped;
    private Vector3 lastShotLocalDirection = Vector3.forward; // в координатах firePoint: лазер поворачивается вместе со стволом
    private bool isTelegraphing;
    private EnemyHealth owner;
    private EnemyStateManager ownerState;
    private Color laserStartColor, laserEndColor;
    private float laserWidth = 1f;
    private float nextShotTime;

    private void Awake()
    {
        owner = GetComponentInParent<EnemyHealth>();
        ownerState = GetComponentInParent<EnemyStateManager>();
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
        if (Time.time < nextShotTime) return;

        if (trackTime > 0f)
        {
            StartCoroutine(TrackAndFire());
            return;
        }

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
        if (ownerState != null && ownerState.IsStunned) yield break; // оглушили, пока целился
        Fire(localDirection);
    }

    // Снайпер: лазер следит за игроком, потом замирает и только после этого выстрел. Потеря цели срывает выстрел.
    private IEnumerator TrackAndFire()
    {
        isTelegraphing = true;
        SetLaserTelegraph(true);

        bool cancelled = false;
        float lostFor = 0f;
        for (float t = 0f; t < trackTime; t += Time.deltaTime)
        {
            if (owner != null && owner.IsDead) { cancelled = true; break; }
            lostFor = ownerState != null && ownerState.CanSeePlayer() ? 0f : lostFor + Time.deltaTime;
            if (lostFor > lostSightGrace) { cancelled = true; break; }
            AimAtPlayer();
            yield return null;
        }

        if (!cancelled)
        {
            // Фиксация: направление запоминаем в мире, чтобы поворот корпуса не сдвигал лазер
            Vector3 lockedDirection = firePoint.TransformDirection(lastShotLocalDirection);
            SetLaserLock(true);
            if (audioSource != null) audioSource.PlayOneShot(ProceduralSounds.MissileBeep, 1f);
            for (float t = 0f; t < lockTime; t += Time.deltaTime)
            {
                lastShotLocalDirection = firePoint.InverseTransformDirection(lockedDirection);
                yield return null;
            }
            lastShotLocalDirection = firePoint.InverseTransformDirection(lockedDirection);
        }

        SetLaserTelegraph(false);
        isTelegraphing = false;
        if (cancelled || (owner != null && owner.IsDead) || (ownerState != null && ownerState.IsStunned))
        {
            nextShotTime = Time.time + 1f; // сорванное прицеливание: короткая пауза
            yield break;
        }

        nextShotTime = Time.time + shotCooldown;
        Fire(lastShotLocalDirection);
    }

    private void AimAtPlayer()
    {
        if (ownerState == null || ownerState.playerHead == null) return;
        Vector3 aimPoint = ownerState.playerHead.position + Vector3.down * 0.3f; // в корпус
        lastShotLocalDirection = firePoint.InverseTransformDirection((aimPoint - firePoint.position).normalized);
    }

    private void SetLaserLock(bool on)
    {
        if (laserLine == null) return;
        laserLine.startColor = on ? lockColor : telegraphColor;
        laserLine.endColor = on ? lockColor : telegraphColor;
        laserLine.widthMultiplier = laserWidth * (on ? lockWidthMultiplier : telegraphWidthMultiplier);
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

    /// <summary>
    /// Враг убит: на месте его оружия появляется версия для игрока с остатком патронов (магазинов к нему нет,
    /// так что пользоваться можно до конца этажа), а оружие в руке врага прячется.
    /// </summary>
    public void DropForPlayer()
    {
        if (dropped || pickupPrefab == null) return;
        dropped = true;
        StopAllCoroutines();

        GameObject drop = Instantiate(pickupPrefab, transform.position, transform.rotation);
        if (drop.TryGetComponent(out VRGun gun))
        {
            float ammo = gun.maxAmmo >= VRGun.InfiniteAmmoThreshold
                ? laserDropShots
                : Mathf.Max(1f, Mathf.Round(gun.maxAmmo * Random.Range(dropAmmoFraction.x, dropAmmoFraction.y)));
            gun.SetupAsDrop(ammo);
        }
        if (drop.TryGetComponent(out Rigidbody body) && !body.isKinematic)
        {
            body.AddForce((Random.insideUnitSphere + Vector3.up) * 1.5f, ForceMode.Impulse);
        }

        foreach (Renderer part in GetComponentsInChildren<Renderer>()) part.enabled = false;
        if (muzzleLight != null) muzzleLight.enabled = false;
        enabled = false; // лазер больше не обновляем
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
