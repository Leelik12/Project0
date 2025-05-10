using System.Collections;
using UnityEngine;

public class HelicopterWeaponSystem : MonoBehaviour
{
    public Transform player;

    public Transform leftGun;
    public Transform rightGun;

    public GameObject muzzleFlashPrefab;
    public GameObject impactEffectPrefab;
    public GameObject tracerPrefab;

    public AudioClip fireSound;

    public float tracerSpeed = 100f;
    public float tracerMaxDistance = 100f;

    public float damage = 10f;

    public float minFireDelay = 3f;       // Мин. пауза между очередями
    public float maxFireDelay = 10f;      // Макс. пауза между очередями

    public int minShotsPerBurst = 5;      // Мин. выстрелов в очереди
    public int maxShotsPerBurst = 10;     // Макс. выстрелов в очереди
    public float timeBetweenShots = 0.1f; // Интервал между выстрелами в очереди

    public float aimRandomness = 2f;      // Разброс (в градусах)

    public LineRenderer leftLineRenderer;
    public LineRenderer rightLineRenderer;

    private AudioSource audioSource;
    public bool isActive = false;

    private void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = fireSound;
        audioSource.playOnAwake = false;

        // Настроим LineRenderer
        SetupLineRenderer(leftLineRenderer);
        SetupLineRenderer(rightLineRenderer);

        StartCoroutine(FireControlRoutine());
    }
    public void ActivateWeapons()
    {
        if (!isActive)
        {
            isActive = true;
            StartCoroutine(FireControlRoutine());
        }
    }

    void SetupLineRenderer(LineRenderer lr)
    {
        lr.positionCount = 2;
        lr.startWidth = 0.05f;
        lr.endWidth = 0.05f;
        lr.material = new Material(Shader.Find("Unlit/Color"));
        lr.material.color = Color.red;
    }

    IEnumerator FireControlRoutine()
    {
        while (isActive)
        {
            float waitTime = Random.Range(minFireDelay, maxFireDelay);
            yield return new WaitForSeconds(waitTime);

            int shotsInBurst = Random.Range(minShotsPerBurst, maxShotsPerBurst + 1);
            StartCoroutine(FireBurst(shotsInBurst));
        }
    }

    IEnumerator FireBurst(int shots)
    {
        for (int i = 0; i < shots; i++)
        {
            FireFromGun(leftGun);
            FireFromGun(rightGun);
            yield return new WaitForSeconds(timeBetweenShots);
        }

        // Очистка линий после очереди
        ClearLineRenderer(leftLineRenderer);
        ClearLineRenderer(rightLineRenderer);
    }

    void FireFromGun(Transform gunMuzzle)
    {
        // Целимся с разбросом
        Vector3 dirToPlayer = (player.position + Vector3.up * 1.5f) - gunMuzzle.position; // цель на уровень груди
        dirToPlayer = ApplySpread(dirToPlayer, aimRandomness);
        dirToPlayer.Normalize();

        // Отрисовка линии
        DrawLine(gunMuzzle, dirToPlayer, gunMuzzle == leftGun ? leftLineRenderer : rightLineRenderer);

        // Вспышка выстрела
        Instantiate(muzzleFlashPrefab, gunMuzzle.position, gunMuzzle.rotation, gunMuzzle);

        // Звук
        audioSource.PlayOneShot(fireSound);

        // Трассер
        GameObject tracer = Instantiate(tracerPrefab, gunMuzzle.position, Quaternion.LookRotation(dirToPlayer));
        Rigidbody tracerRb = tracer.GetComponent<Rigidbody>();
        if (tracerRb != null)
        {
            tracerRb.velocity = dirToPlayer * tracerSpeed;
        }

        // Проверка попадания (Raycast)
        RaycastHit hit;
        if (Physics.Raycast(gunMuzzle.position, dirToPlayer, out hit, tracerMaxDistance))
        {
            Instantiate(impactEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));

            if (hit.collider.CompareTag("Player"))
            {
                // hit.collider.GetComponent<PlayerHealth>()?.TakeDamage(damage);
            }
        }

        Destroy(tracer, 1f);
    }

    Vector3 ApplySpread(Vector3 direction, float angle)
    {
        // Добавляем случайный разброс
        float spreadAngle = Random.Range(-angle, angle);
        Quaternion spreadRotation = Quaternion.AngleAxis(spreadAngle, Random.insideUnitSphere);
        return spreadRotation * direction;
    }

    void DrawLine(Transform origin, Vector3 direction, LineRenderer lr)
    {
        lr.enabled = true;
        lr.SetPosition(0, origin.position);
        lr.SetPosition(1, origin.position + direction.normalized * tracerMaxDistance);
    }

    void ClearLineRenderer(LineRenderer lr)
    {
        lr.enabled = false;
    }
}
