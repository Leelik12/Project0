using System.Collections;
using UnityEngine;

/// <summary>
/// Огнестрел врага: hitscan-выстрел с разбросом по вызову Shoot() (событие анимации через EnemyStateManager).
/// Лазер показывает направление последнего выстрела.
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
    [SerializeField] private float hitEffectLifetime = 100f;
    [SerializeField] private float effectOffset = 0.01f;

    private Vector3 lastShotDirection = Vector3.forward;

    public void Shoot()
    {
        if (muzzleFlash != null) muzzleFlash.Play();
        if (muzzleLight != null) StartCoroutine(MuzzleLightFlash());
        if (audioSource != null && shotSound != null) audioSource.PlayOneShot(shotSound);
        StartCoroutine(MoveRecoil());

        Vector3 direction = GetSpreadDirection(firePoint.forward, spreadAngle);
        lastShotDirection = direction;

        if (!Physics.Raycast(firePoint.position, direction, out RaycastHit hit, range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return;

        Vector3 pos = hit.point + hit.normal * effectOffset;
        Quaternion rot = Quaternion.LookRotation(hit.normal);
        if (hitEffectPrefabDust != null) Destroy(Instantiate(hitEffectPrefabDust, pos, rot), hitEffectLifetime);
        if (hitEffectPrefabSparks != null) Destroy(Instantiate(hitEffectPrefabSparks, pos, rot), hitEffectLifetime);

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

        Vector3 endPoint = Physics.Raycast(firePoint.position, lastShotDirection, out RaycastHit hit, range)
            ? hit.point
            : firePoint.position + lastShotDirection * range;

        laserLine.enabled = true;
        laserLine.SetPosition(0, firePoint.position);
        laserLine.SetPosition(1, endPoint);
    }
}
