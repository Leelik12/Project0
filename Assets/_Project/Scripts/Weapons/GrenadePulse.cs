using UnityEngine;

/// <summary>Расширяющаяся светящаяся сфера — визуал волны ЭМИ-гранаты. Создаётся из кода, сама удаляется.</summary>
public class GrenadePulse : MonoBehaviour
{
    private float radius;
    private float duration;
    private float age;

    public static void Spawn(Vector3 position, float radius, Material material, float duration = 0.4f)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "GrenadePulse";
        Destroy(go.GetComponent<Collider>());
        go.transform.position = position;
        go.transform.localScale = Vector3.zero;
        if (material != null) go.GetComponent<Renderer>().sharedMaterial = material;

        var pulse = go.AddComponent<GrenadePulse>();
        pulse.radius = radius;
        pulse.duration = duration;
    }

    private void Update()
    {
        age += Time.deltaTime;
        float t = Mathf.Clamp01(age / duration);
        transform.localScale = Vector3.one * (radius * 2f * (1f - (1f - t) * (1f - t))); // быстрый старт, плавное замедление
        if (t >= 1f) Destroy(gameObject);
    }
}
