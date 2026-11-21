using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Огнестрельное оружие игрока: hitscan-выстрел по зажатию грип+триггер той руки, в которой оружие,
/// магазины, отдача затвора, лазерный прицел, эффекты попадания.
/// </summary>
public class VRGun : MonoBehaviour
{
    /// <summary>Оружие, которое сейчас держит игрок (для HUD). null — ничего не держит.</summary>
    public static VRGun Held { get; private set; }

    private const float PressThreshold = 0.8f;
    private const float HeadshotMultiplier = 2f;
    private const float LegshotMultiplier = 0.5f;
    private const float LegshotSlowFactor = 0.9f;
    public const float InfiniteAmmoThreshold = 9999f; // у лазерного оружия запас патронов условно бесконечный

    [SerializeField] private XRGrabInteractable grabInteractable;

    [Header("Перезарядка")]
    [SerializeField] public float maxAmmo;
    public float currentAmmo;
    public bool IsLoaded => currentAmmo > 0;
    public bool IsCharged = true;                 // магазин вставлен
    public GameObject emptyMagazinePrefab;        // префаб пустого магазина
    public Transform ejectPoint;                  // откуда выпадает магазин
    [SerializeField] private GameObject internalMagazineModel; // встроенный визуальный магазин

    [Header("Подвижные части")]
    [SerializeField] private Transform movablePart;   // затвор
    [SerializeField] private float recoilDistance = 0.2f;
    [SerializeField] private float recoilDuration = 0.1f;

    [Header("Настройки стрельбы")]
    public float fireRate;   // интервал между выстрелами, сек
    public float damage;
    public float range = 100f;

    [Header("Настройки лазера")]
    public bool laserEnabled = true;
    public LineRenderer laserLine;

    [Header("Muzzle Flash")]
    public ParticleSystem muzzleFlash;
    public Light muzzleLight;
    public float lightDuration;

    [Header("XR")]
    public Transform firePoint;
    public InputActionProperty LeftTrigger;
    public InputActionProperty RightTrigger;
    public InputActionProperty LeftGrip;
    public InputActionProperty RightGrip;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip shotSound;

    [Header("Декали")]
    [SerializeField] private GameObject hitEffectPrefabDust;
    [SerializeField] private GameObject hitEffectPrefabSparks;
    [SerializeField] private float hitEffectLifetime = 2f;
    [SerializeField] private float effectOffset = 0.01f;

    [Header("Отдача в руку")]
    [Tooltip("Насколько градусов ствол подбрасывает вверх за выстрел")]
    [SerializeField] private float recoilAngle = 4f;
    [Tooltip("Насколько метров оружие отходит назад за выстрел")]
    [SerializeField] private float recoilKickBack = 0.015f;
    [Tooltip("Скорость возврата в исходное положение (больше — быстрее)")]
    [SerializeField] private float recoilReturnSpeed = 14f;
    [Tooltip("Импульс, которым пуля толкает физические объекты (трупы, ящики)")]
    [SerializeField] private float impactForce = 8f;

    [Header("Счётчик патронов на оружии")]
    [SerializeField] private bool showAmmoCounter = true;
    [Tooltip("Где висит счётчик. Пусто — над стволом, в 12 см за дулом")]
    [SerializeField] private Transform ammoCounterAnchor;

    private float nextFireTime;
    private WeaponRecoil recoil;
    private bool LaserAllowed => laserEnabled && laserLine != null && (!GameState.Difficulty || GameState.LaserSightUnlocked);

    private void Awake()
    {
        LeftGrip.action.Enable();
        RightGrip.action.Enable();
        LeftTrigger.action.Enable();
        RightTrigger.action.Enable();

        // Применяем купленные апгрейды
        fireRate *= GameState.BuffGunFireRate;
        damage *= GameState.BuffGunDamage;
        currentAmmo *= GameState.BuffGunMaxAmmo;
        maxAmmo *= GameState.BuffGunMaxAmmo;

        if (laserLine != null) laserLine.enabled = LaserAllowed;

        recoil = new WeaponRecoil(recoilAngle, recoilKickBack, recoilReturnSpeed);
        if (showAmmoCounter) GunAmmoCounter.Create(this, ammoCounterAnchor);
    }

    private void Update()
    {
        bool held = grabInteractable != null && grabInteractable.isSelected;
        if (held) Held = this;
        else if (Held == this) Held = null;

        if (currentAmmo <= 0 && IsCharged)
        {
            IsCharged = false;
            EjectMagazine(); // автоматически выбрасываем магазин, когда патроны кончились
        }

        if (held && IsLoaded && Time.time >= nextFireTime && IsFirePressedForHoldingHand())
        {
            nextFireTime = Time.time + fireRate;
            Shoot();
        }

        if (LaserAllowed) UpdateLaser();
        else if (laserLine != null) laserLine.enabled = false;

        recoil.Update(held ? grabInteractable : null, firePoint);
    }

    private void OnDisable()
    {
        if (Held == this) Held = null;
        recoil?.Reset();
    }

    // Стреляет только та рука, в которой лежит оружие (attachTransform содержит "L" или "R")
    private bool IsFirePressedForHoldingHand()
    {
        string attachName = grabInteractable.attachTransform != null ? grabInteractable.attachTransform.name : null;
        if (attachName == null) return false;

        bool leftHand = attachName.Contains("L") && IsPressed(LeftGrip) && IsPressed(LeftTrigger);
        bool rightHand = attachName.Contains("R") && IsPressed(RightGrip) && IsPressed(RightTrigger);
        return leftHand || rightHand;
    }

    private static bool IsPressed(InputActionProperty action) => action.action.ReadValue<float>() > PressThreshold;

    public void Shoot()
    {
        currentAmmo--;
        GameState.RegisterShot();

        PlayShotEffects();
        Haptics.Pulse(grabInteractable, 0.6f, 0.08f);

        // Сильные руки держат оружие крепче
        recoil.Kick(GameState.StrongArms ? 0.6f : 1f);

        Vector3 direction = firePoint.forward;
        float shotDamage = damage * PlayerStealth.ConsumeAmbush(); // выстрел из невидимости — критический

        bool hasHit = Physics.Raycast(firePoint.position, direction, out RaycastHit hit, range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        Vector3 end = hasHit ? hit.point : firePoint.position + direction * range;
        Tracer.Spawn(firePoint.position, end, enemy: false, TracerFallback);
        if (!hasHit) return;

        SpawnHitEffects(hit);
        PushBody(hit, direction);
        EnemyHealth target = ApplyDamage(hit, direction, shotDamage);

        if (GameState.ModExplosive > 0f) WeaponMods.Explode(hit.point, hit.normal, shotDamage * GameState.ModExplosive, target);
        if (target != null && GameState.ModPiercing) Pierce(hit, direction, shotDamage, target);
        else if (target == null && GameState.ModRicochet) Ricochet(hit, shotDamage);
    }

    private Material TracerFallback => laserLine != null ? laserLine.sharedMaterial : null;

    // Пробитие: пуля проходит первого врага и бьёт следующего за ним с меньшим уроном
    private void Pierce(RaycastHit first, Vector3 direction, float shotDamage, EnemyHealth firstTarget)
    {
        float remaining = range - first.distance;
        RaycastHit[] hits = Physics.RaycastAll(first.point, direction, remaining, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit next in hits)
        {
            if (next.collider.GetComponentInParent<EnemyHealth>() == firstTarget) continue; // ещё внутри первого врага

            Tracer.Spawn(first.point, next.point, enemy: false, TracerFallback);
            SpawnHitEffects(next);
            ApplyDamage(next, direction, shotDamage * WeaponMods.PierceDamageMultiplier, countsAsHit: false);
            return;
        }
    }

    // Рикошет: промах отскакивает от поверхности в ближайшего видимого врага
    private void Ricochet(RaycastHit wallHit, float shotDamage)
    {
        if (!WeaponMods.TryFindRicochetTarget(wallHit, out EnemyHealth target, out RaycastHit enemyHit)) return;

        Tracer.Spawn(wallHit.point, enemyHit.point, enemy: false, TracerFallback);
        SpawnHitEffects(enemyHit);
        ApplyDamage(enemyHit, (enemyHit.point - wallHit.point).normalized, shotDamage * WeaponMods.RicochetDamageMultiplier);
    }

    private void PlayShotEffects()
    {
        if (muzzleFlash != null) muzzleFlash.Play();
        if (muzzleLight != null) StartCoroutine(MuzzleLightFlash());
        if (audioSource != null && shotSound != null) audioSource.PlayOneShot(shotSound);
        StartCoroutine(MoveRecoil());
    }

    private void SpawnHitEffects(RaycastHit hit)
    {
        Vector3 pos = hit.point + hit.normal * effectOffset;
        Quaternion rot = Quaternion.LookRotation(hit.normal);

        ObjectPool.Spawn(hitEffectPrefabDust, pos, rot, hitEffectLifetime);
        ObjectPool.Spawn(hitEffectPrefabSparks, pos, rot, hitEffectLifetime);
    }

    private void PushBody(RaycastHit hit, Vector3 direction)
    {
        Rigidbody body = hit.rigidbody;
        if (body != null && !body.isKinematic) body.AddForceAtPosition(direction * impactForce, hit.point, ForceMode.Impulse);
    }

    /// <summary>
    /// Урон по врагу в точке попадания. Возвращает врага или null, если попали не во врага.
    /// countsAsHit = false для второго врага при пробитии: выстрел один, попадание в точность уже засчитано.
    /// </summary>
    private EnemyHealth ApplyDamage(RaycastHit hit, Vector3 direction, float baseDamage, bool countsAsHit = true)
    {
        Collider col = hit.collider;
        if (col.TryGetComponent(out ShieldPlate shield)) // щит щитовика: урон не проходит, щит копит повреждения
        {
            shield.Absorb(hit.point, baseDamage);
            return null;
        }
        if (!col.CompareTag("Head") && !col.CompareTag("Leg") && !col.CompareTag("Body")) return null;

        EnemyHealth target = col.GetComponentInParent<EnemyHealth>();
        if (target == null || target.IsDead) return null; // по трупу не считаем ни урон, ни точность

        bool headshot = col.CompareTag("Head");
        float finalDamage = baseDamage;
        if (headshot)
        {
            finalDamage *= HeadshotMultiplier;
        }
        else if (col.name.ToLower().Contains("leg"))
        {
            finalDamage *= LegshotMultiplier;

            // Попадание в ногу замедляет врага
            EnemyStateManager enemy = col.GetComponentInParent<EnemyStateManager>();
            if (enemy != null)
            {
                enemy.walkSpeed *= LegshotSlowFactor;
                enemy.runSpeed *= LegshotSlowFactor;
            }
        }

        if (countsAsHit) GameState.RegisterHit(finalDamage);
        else GameState.RegisterDamage(finalDamage);
        target.TakeDamage(finalDamage, hit.point, direction, headshot);

        if (GameState.ModIncendiary > 0f) EnemyBurn.Ignite(target, GameState.ModIncendiary);
        if (GameState.ModLeech > 0f && PlayerHealth.Current != null) PlayerHealth.Current.Heal(finalDamage * GameState.ModLeech);
        if (headshot) RefillOnHeadshot();
        return target;
    }

    // Пассивный имплант: хедшот возвращает патроны в магазин (у лазеров патроны и так бесконечные)
    private void RefillOnHeadshot()
    {
        if (GameState.HeadshotAmmo <= 0 || !IsCharged || maxAmmo >= InfiniteAmmoThreshold) return;
        currentAmmo = Mathf.Min(maxAmmo, currentAmmo + GameState.HeadshotAmmo);
    }

    /// <summary>Оружие выпало из убитого врага: в магазине только то, что осталось.</summary>
    public void SetupAsDrop(float ammo)
    {
        currentAmmo = Mathf.Min(maxAmmo, ammo);
        IsCharged = currentAmmo > 0;
        if (internalMagazineModel != null) internalMagazineModel.SetActive(IsCharged);
    }

    // ---------- Магазины ----------

    public bool CanInsertMagazine() => !IsCharged;

    public void InsertMagazine(VRMagazine magazine)
    {
        IsCharged = true;
        currentAmmo = maxAmmo;

        magazine.gameObject.SetActive(false);
        if (internalMagazineModel != null) internalMagazineModel.SetActive(true);
        Haptics.Pulse(grabInteractable, 0.3f, 0.05f);
    }

    public void EjectMagazine()
    {
        if (emptyMagazinePrefab != null && ejectPoint != null)
        {
            Instantiate(emptyMagazinePrefab, ejectPoint.position, ejectPoint.rotation);
        }
        if (internalMagazineModel != null) internalMagazineModel.SetActive(false);
        currentAmmo = 0;
    }

    // ---------- Эффекты ----------

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

    private void UpdateLaser()
    {
        Vector3 endPoint = Physics.Raycast(firePoint.position, firePoint.forward, out RaycastHit hit, range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            ? hit.point
            : firePoint.position + firePoint.forward * range;

        laserLine.enabled = true;
        laserLine.SetPosition(0, firePoint.position);
        laserLine.SetPosition(1, endPoint);
    }
}
