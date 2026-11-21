using UnityEngine;

/// <summary>
/// Самонаводящаяся ракета вертолёта: медленная, с ограниченной скоростью поворота, поэтому от неё можно увернуться
/// или спрятаться за укрытием. Её можно сбить (EnemyHealth на теле с тегом Body). Взрывается рядом с игроком
/// или при ударе о препятствие; урон по игроку идёт, только если между ним и взрывом нет стены.
/// Вид ракеты — дочерний объект модели (заглушка из примитивов); пищит и мигает светом тем чаще, чем ближе к игроку.
/// </summary>
[RequireComponent(typeof(EnemyHealth))]
public class HelicopterMissile : MonoBehaviour
{
    private const int HitBufferSize = 8;
    private static readonly RaycastHit[] HitBuffer = new RaycastHit[HitBufferSize];

    [Header("Полёт")]
    [SerializeField] private float speed = 8f;                  // м/с
    [SerializeField] private float startSpeedFraction = 0.4f;   // доля скорости при вылете, разгон за accelerationTime
    [SerializeField] private float accelerationTime = 1.2f;
    [SerializeField] private float turnRate = 55f;              // град/с — чем меньше, тем легче увернуться
    [SerializeField] private float lifetime = 14f;              // потом самоуничтожение без урона
    [SerializeField] private float armingTime = 0.4f;           // первое время ракета не взрывается о пилон/вертолёт

    [Header("Взрыв")]
    [SerializeField] private float damage = 25f;
    [SerializeField] private float explosionRadius = 2.5f;
    [SerializeField] private float proximityRadius = 1.1f;      // взрыв, если подлетела к игроку ближе
    [SerializeField] private float explosionLifetime = 0.5f;
    [SerializeField] private Material explosionMaterial;        // эмиссивный материал вспышки
    [SerializeField] private AudioClip explosionSound;

    [Header("Предупреждение")]
    [SerializeField] private AudioSource beepSource;
    [SerializeField] private Light warningLight;
    [SerializeField] private float beepSlowInterval = 0.7f;     // писк вдали
    [SerializeField] private float beepFastInterval = 0.12f;    // писк вблизи
    [SerializeField] private float beepFullDistance = 25f;      // дальше — писк самый редкий

    private EnemyHealth health;
    private Transform target;
    private Transform ignoreRoot;
    private Vector3 targetOffset = new(0f, -0.3f, 0f);          // целится чуть ниже камеры — в корпус
    private float age;
    private float nextBeepTime;
    private bool launched;
    private bool exploded;

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();
        if (beepSource != null && beepSource.clip == null) beepSource.clip = ProceduralSounds.MissileBeep;
    }

    /// <summary>Запускает ракету на цель. ignoreRoot — тело вертолёта, с ним ракета не сталкивается.</summary>
    public void Launch(Transform targetTransform, Transform ignoreRoot)
    {
        target = targetTransform;
        this.ignoreRoot = ignoreRoot;
        launched = true;
    }

    private void Update()
    {
        if (!launched || exploded) return;

        age += Time.deltaTime;
        if (health.IsDead || age >= lifetime || target == null)
        {
            Fizzle();
            return;
        }

        Vector3 toTarget = target.position + targetOffset - transform.position;
        float distance = toTarget.magnitude;

        // Поворот к цели с ограниченной скоростью
        if (distance > 0.01f)
        {
            Quaternion want = Quaternion.LookRotation(toTarget / distance);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, want, turnRate * Time.deltaTime);
        }

        float currentSpeed = speed * Mathf.Lerp(startSpeedFraction, 1f, Mathf.Clamp01(age / accelerationTime));
        float step = currentSpeed * Time.deltaTime;

        if (distance <= proximityRadius)
        {
            Explode(true);
            return;
        }

        if (age >= armingTime && HitsObstacle(step))
        {
            Explode(true);
            return;
        }

        transform.position += transform.forward * step;
        UpdateWarning(distance);
    }

    // Луч вперёд на длину шага: попал в стену или предмет — взрыв (укрытие защищает от ракеты)
    private bool HitsObstacle(float step)
    {
        int count = Physics.RaycastNonAlloc(transform.position, transform.forward, HitBuffer, step + 0.3f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            Transform hit = HitBuffer[i].collider.transform;
            if (hit.IsChildOf(transform)) continue;
            if (ignoreRoot != null && hit.IsChildOf(ignoreRoot)) continue;
            return true;
        }
        return false;
    }

    // Писк и мигание: чем ближе ракета к игроку, тем чаще
    private void UpdateWarning(float distance)
    {
        float closeness = 1f - Mathf.Clamp01(distance / beepFullDistance);
        float interval = Mathf.Lerp(beepSlowInterval, beepFastInterval, closeness);

        if (Time.time >= nextBeepTime)
        {
            nextBeepTime = Time.time + interval;
            if (beepSource != null) beepSource.Play();
        }

        if (warningLight != null)
        {
            float phase = Mathf.Repeat(Time.time / interval, 1f);
            warningLight.intensity = phase < 0.35f ? 4f : 1f;
        }
    }

    /// <summary>Взрыв без урона: ракету сбили или босс умер.</summary>
    public void Fizzle()
    {
        if (!exploded) Explode(false);
    }

    private void Explode(bool dealDamage)
    {
        exploded = true;

        if (dealDamage && target != null && PlayerHealth.Current != null)
        {
            Vector3 point = transform.position;
            Vector3 chest = target.position + targetOffset;
            bool inRadius = (chest - point).sqrMagnitude <= explosionRadius * explosionRadius;
            bool covered = inRadius && Physics.Linecast(point, chest, out RaycastHit block, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                && block.collider.transform.root != target.root && !block.collider.CompareTag("Player");
            if (inRadius && !covered) PlayerHealth.Current.PlayerTakeDamage(damage);
        }

        SpawnExplosionEffect();
        Destroy(gameObject);
    }

    // Вспышка: расширяющаяся эмиссивная сфера со светом, сама удаляется
    private void SpawnExplosionEffect()
    {
        if (explosionSound != null) AudioSource.PlayClipAtPoint(explosionSound, transform.position);

        var flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flash.name = "MissileExplosion";
        flash.transform.position = transform.position;
        flash.transform.localScale = Vector3.one * 0.5f;
        Destroy(flash.GetComponent<Collider>());
        if (explosionMaterial != null) flash.GetComponent<Renderer>().sharedMaterial = explosionMaterial;

        var light = flash.AddComponent<Light>();
        light.color = new Color(1f, 0.55f, 0.2f);
        light.range = explosionRadius * 2f;
        light.intensity = 6f;

        flash.AddComponent<ExplosionFlash>().Init(explosionLifetime, explosionRadius * 2f);
    }

    /// <summary>Расширяет и гасит вспышку взрыва, затем удаляет объект.</summary>
    private class ExplosionFlash : MonoBehaviour
    {
        private float duration;
        private float endSize;
        private float time;
        private Light flashLight;

        public void Init(float duration, float endSize)
        {
            this.duration = Mathf.Max(0.05f, duration);
            this.endSize = endSize;
            flashLight = GetComponent<Light>();
        }

        private void Update()
        {
            time += Time.deltaTime;
            float k = Mathf.Clamp01(time / duration);
            transform.localScale = Vector3.one * Mathf.Lerp(0.5f, endSize, k);
            if (flashLight != null) flashLight.intensity = 6f * (1f - k);
            if (k >= 1f) Destroy(gameObject);
        }
    }
}
