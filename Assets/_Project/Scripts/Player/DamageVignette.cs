using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Красная виньетка по краям обзора при получении урона. Создаётся из кода (PlayerHealth), в сцене настраивать не нужно:
/// холст Screen Space – Camera перед камерой игрока, текстура радиального градиента генерируется при старте.
/// </summary>
public class DamageVignette : MonoBehaviour
{
    private const int TextureSize = 128;
    private const float MinAlpha = 0.35f;
    private const float MaxAlpha = 0.85f;
    private const float FadeTime = 0.6f;

    private RawImage image;
    private Canvas canvas;
    private float alpha;

    /// <summary>Создаёт виньетку дочерним объектом owner. Камеру ищет сама (Camera.main).</summary>
    public static DamageVignette Create(Transform owner)
    {
        var go = new GameObject("DamageVignette");
        go.transform.SetParent(owner, false);
        return go.AddComponent<DamageVignette>();
    }

    private void Awake()
    {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.sortingOrder = 1000;

        var imageGo = new GameObject("Image", typeof(RectTransform));
        imageGo.transform.SetParent(transform, false);
        var rect = (RectTransform)imageGo.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        image = imageGo.AddComponent<RawImage>();
        image.texture = CreateVignetteTexture();
        image.raycastTarget = false;
        image.enabled = false;
    }

    /// <summary>Вспышка виньетки. strength — доля потерянного здоровья (0..1), чем больше, тем ярче.</summary>
    public void Flash(float strength)
    {
        if (!AttachToCamera()) return;
        alpha = Mathf.Max(alpha, Mathf.Clamp(MinAlpha + strength * 2f, MinAlpha, MaxAlpha));
        Apply();
    }

    private void Update()
    {
        if (alpha <= 0f) return;

        // Реальное время — затухание не тянется во время Сандевистана
        alpha = Mathf.Max(0f, alpha - Time.unscaledDeltaTime * MaxAlpha / FadeTime);
        Apply();
    }

    private void Apply()
    {
        image.enabled = alpha > 0f;
        image.color = new Color(1f, 1f, 1f, alpha);
    }

    private bool AttachToCamera()
    {
        if (canvas.worldCamera != null) return true;

        Camera cam = Camera.main;
        if (cam == null) return false;

        canvas.worldCamera = cam;
        canvas.planeDistance = cam.nearClipPlane + 0.05f;
        return true;
    }

    private static Texture2D CreateVignetteTexture()
    {
        var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            name = "DamageVignette"
        };

        var pixels = new Color32[TextureSize * TextureSize];
        float half = (TextureSize - 1) * 0.5f;
        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                float dx = (x - half) / half;
                float dy = (y - half) / half;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1.1f, distance));
                pixels[y * TextureSize + x] = new Color(0.75f, 0f, 0f, a);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }
}
