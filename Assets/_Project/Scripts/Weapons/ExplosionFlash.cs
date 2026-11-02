using UnityEngine;

/// <summary>Короткая оранжевая вспышка света на месте взрыва разрывной пули. Создаётся из кода, без префаба.</summary>
public class ExplosionFlash : MonoBehaviour
{
    private const float Duration = 0.18f;

    private Light flash;
    private float startIntensity;
    private float age;

    public static void Spawn(Vector3 position, float radius)
    {
        var go = new GameObject("ExplosionFlash");
        go.transform.position = position;
        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.55f, 0.2f);
        light.range = radius * 2f;
        light.intensity = 6f;
        light.shadows = LightShadows.None;

        var effect = go.AddComponent<ExplosionFlash>();
        effect.flash = light;
        effect.startIntensity = light.intensity;
    }

    private void Update()
    {
        age += Time.deltaTime;
        flash.intensity = Mathf.Lerp(startIntensity, 0f, age / Duration);
        if (age >= Duration) Destroy(gameObject);
    }
}
