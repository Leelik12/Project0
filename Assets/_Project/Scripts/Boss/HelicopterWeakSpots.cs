using UnityEngine;

/// <summary>
/// Слабые места и броня вертолёта. Урон по-прежнему идёт в общий EnemyHealth босса, а множитель выбирается по точке попадания:
/// рядом со слабым местом (двигатель, хвостовой винт) — x2, по остальному корпусу — x0.5 (броня).
/// Слабые места светятся пульсирующим эмиссивным светом и искрят, особенно при попадании.
/// </summary>
public class HelicopterWeakSpots : MonoBehaviour
{
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    [System.Serializable]
    public class WeakSpot
    {
        public Transform point;                 // центр слабого места
        public Renderer glow;                   // светящаяся сфера-индикатор
        public Light glowLight;
        public ParticleSystem sparks;
        [HideInInspector] public float lastHitTime = -10f;
    }

    [SerializeField] private EnemyHealth health;
    [SerializeField] private WeakSpot[] spots;

    [Header("Урон")]
    [SerializeField] private float weakSpotMultiplier = 2f;
    [SerializeField] private float armorMultiplier = 0.5f;
    [SerializeField] private float weakSpotRadius = 1.3f;        // м, радиус попадания вокруг точки

    [Header("Свечение")]
    [SerializeField] private Color glowColor = new(1f, 0.35f, 0.05f);
    [SerializeField] private float pulseSpeed = 4f;
    [SerializeField] private float minIntensity = 1.5f;
    [SerializeField] private float maxIntensity = 5f;
    [SerializeField] private int hitSparkCount = 10;

    private MaterialPropertyBlock block;

    private void Awake()
    {
        block = new MaterialPropertyBlock();
        if (health != null) health.hitDamageModifier = GetDamageMultiplier;
    }

    private void OnDestroy()
    {
        if (health != null && health.hitDamageModifier == GetDamageMultiplier) health.hitDamageModifier = null;
    }

    private float GetDamageMultiplier(Vector3 hitPoint)
    {
        float sqrRadius = weakSpotRadius * weakSpotRadius;
        for (int i = 0; i < spots.Length; i++)
        {
            WeakSpot spot = spots[i];
            if (spot.point == null || (spot.point.position - hitPoint).sqrMagnitude > sqrRadius) continue;

            spot.lastHitTime = Time.time;
            if (spot.sparks != null) spot.sparks.Emit(hitSparkCount);
            return weakSpotMultiplier;
        }
        return armorMultiplier;
    }

    private void Update()
    {
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed);
        for (int i = 0; i < spots.Length; i++)
        {
            WeakSpot spot = spots[i];
            float flash = Mathf.Clamp01(1f - (Time.time - spot.lastHitTime) / 0.25f); // вспышка при попадании
            float intensity = Mathf.Lerp(minIntensity, maxIntensity, Mathf.Max(pulse, flash));

            if (spot.glow != null)
            {
                spot.glow.GetPropertyBlock(block);
                block.SetColor(EmissionColorId, glowColor * intensity);
                spot.glow.SetPropertyBlock(block);
            }
            if (spot.glowLight != null) spot.glowLight.intensity = intensity * 0.6f;
        }
    }
}
