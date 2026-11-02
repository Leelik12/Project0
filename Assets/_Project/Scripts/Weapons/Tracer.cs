using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Трассер выстрела: короткая светящаяся полоса, которая летит от ствола к точке попадания.
/// Создаётся из кода и переиспользуется (без префаба); настройки — в GameFeedbackSettings.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class Tracer : MonoBehaviour
{
    private static readonly Stack<Tracer> Pool = new();
    private static Material sharedMaterial;
    private static bool materialFailed;

    private LineRenderer line;
    private Vector3 start, end, direction;
    private float distance, travelled, length, speed;

    /// <summary>Запускает трассер от from к to. fallbackMaterial — если шейдер трассеров недоступен (например, материал лазера).</summary>
    public static void Spawn(Vector3 from, Vector3 to, bool enemy, Material fallbackMaterial = null)
    {
        Material material = GetMaterial() ?? fallbackMaterial;
        if (material == null || (to - from).sqrMagnitude < 0.01f) return;

        Tracer tracer = null;
        while (Pool.Count > 0 && tracer == null) tracer = Pool.Pop();
        if (tracer == null) tracer = Create();

        GameFeedbackSettings settings = GameFeedbackSettings.Instance;
        tracer.line.sharedMaterial = material;
        tracer.line.widthMultiplier = settings.tracerWidth;
        Color color = enemy ? settings.enemyTracerColor : settings.playerTracerColor;
        tracer.line.startColor = new Color(color.r, color.g, color.b, 0f); // хвост прозрачный
        tracer.line.endColor = color;
        tracer.Launch(from, to, settings.tracerLength, settings.tracerSpeed);
    }

    private static Tracer Create()
    {
        var go = new GameObject("Tracer");
        LineRenderer line = go.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.numCapVertices = 2;
        return go.AddComponent<Tracer>();
    }

    // Аддитивный прозрачный материал на URP Particles/Unlit, настроенный из кода
    private static Material GetMaterial()
    {
        if (sharedMaterial != null || materialFailed) return sharedMaterial;

        Shader shader = GameFeedbackSettings.Instance.tracerShader;
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
        {
            materialFailed = true;
            return null;
        }

        sharedMaterial = new Material(shader) { name = "Tracer", renderQueue = 3000 };
        sharedMaterial.SetFloat("_Surface", 1f);
        sharedMaterial.SetFloat("_Blend", 2f); // additive
        sharedMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        sharedMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        sharedMaterial.SetFloat("_ZWrite", 0f);
        sharedMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        sharedMaterial.SetColor("_BaseColor", Color.white);
        return sharedMaterial;
    }

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
    }

    private void Launch(Vector3 from, Vector3 to, float streakLength, float streakSpeed)
    {
        start = from;
        end = to;
        distance = Vector3.Distance(from, to);
        direction = (to - from) / distance;
        length = streakLength;
        speed = streakSpeed;
        travelled = 0f;

        gameObject.SetActive(true);
        UpdateLine();
    }

    private void Update()
    {
        // Масштабированное время: в Сандевистане трассеры замедляются вместе со всем миром
        travelled += speed * Time.deltaTime;
        if (travelled - length >= distance)
        {
            gameObject.SetActive(false);
            Pool.Push(this);
            return;
        }
        UpdateLine();
    }

    private void UpdateLine()
    {
        float head = Mathf.Min(travelled, distance);
        float tail = Mathf.Clamp(travelled - length, 0f, distance);
        line.SetPosition(0, start + direction * tail);
        line.SetPosition(1, start + direction * head);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Init()
    {
        Pool.Clear();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single) Pool.Clear();
    }
}
