using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Полоса HP босса в мировом пространстве: парит перед игроком в сторону вертолёта (в поле зрения, не ближе <see cref="distance"/>),
/// всегда повёрнута к игроку. Появляется, когда бой начался; при смене фазы вспыхивает и краснеет; после смерти гаснет.
/// Весь интерфейс строится кодом, в сцене нужен только этот компонент.
/// </summary>
public class BossHealthBar : MonoBehaviour
{
    private const string UiFont = "Fonts/TechnoDeath UI SDF";
    private const float CanvasWidth = 800f;
    private const float CanvasHeight = 130f;
    private const float PhaseFlashTime = 2.5f;
    private const float HideDelay = 1.5f;
    private const float FadeSpeed = 4f;

    private static readonly Color BackColor = new(0.02f, 0.05f, 0.08f, 0.75f);
    private static readonly Color Phase1Color = new(0.2f, 0.95f, 1f, 1f);
    private static readonly Color Phase2Color = new(1f, 0.25f, 0.2f, 1f);

    [SerializeField] private HelicopterController controller;
    [SerializeField] private EnemyHealth health;
    [SerializeField] private HelicopterBossPhases phases;
    [SerializeField] private Transform anchor;                       // точка вертолёта, в сторону которой смотрит полоса

    [Header("Вид")]
    [SerializeField] private string bossName = "Ударный вертолёт";
    [SerializeField] private float distance = 6f;                    // от камеры до полосы, м
    [SerializeField] private float width = 3.2f;                     // ширина полосы в мире, м
    [SerializeField] private float maxYawFromView = 30f;             // насколько в сторону от взгляда может уйти полоса
    [SerializeField] private Vector2 pitchRange = new(8f, 24f);      // угол над горизонтом, град
    [SerializeField] private float followSharpness = 6f;

    private Transform cam;
    private Canvas canvas;
    private CanvasGroup group;
    private RectTransform fill;
    private Image fillImage;
    private TextMeshProUGUI nameText;
    private TextMeshProUGUI phaseText;
    private float shownFraction = 1f;
    private float phaseFlashUntil;
    private float hideAt = -1f;
    private bool finished;
    private bool positioned;

    private void Awake()
    {
        BuildUi();
        group.alpha = 0f;
        canvas.enabled = false;
    }

    private void OnEnable()
    {
        if (phases != null) phases.PhaseChanged += OnPhaseChanged;
    }

    private void OnDisable()
    {
        if (phases != null) phases.PhaseChanged -= OnPhaseChanged;
    }

    private void OnPhaseChanged(int phase)
    {
        phaseFlashUntil = Time.time + PhaseFlashTime;
        phaseText.text = UiText.Widen("Фаза 2");
        fillImage.color = Phase2Color;
    }

    private void LateUpdate()
    {
        if (cam == null && controller != null) cam = controller.player;
        if (controller == null || cam == null || finished) return;

        bool fightOn = controller.IsActivated && health != null && !controller.IsDefeated && !health.IsDead;
        if (fightOn)
        {
            if (!canvas.enabled) canvas.enabled = true;
            group.alpha = Mathf.MoveTowards(group.alpha, 1f, FadeSpeed * Time.deltaTime);
            UpdateFill();
            UpdatePose();
            UpdatePhaseFlash();
            return;
        }

        if (!controller.IsActivated) return; // бой ещё не начался

        // Босс побеждён (или объект уже уничтожен): показываем пустую полосу и гасим
        if (hideAt < 0f)
        {
            hideAt = Time.time + HideDelay;
            SetFraction(0f);
        }
        if (anchor != null) UpdatePose();
        if (Time.time >= hideAt)
        {
            group.alpha = Mathf.MoveTowards(group.alpha, 0f, FadeSpeed * Time.deltaTime);
            if (group.alpha <= 0f)
            {
                canvas.enabled = false;
                finished = true;
            }
        }
    }

    private void UpdateFill()
    {
        float target = Mathf.Clamp01(health.GetCurrentHp() / Mathf.Max(1f, health.maxHealth));
        shownFraction = Mathf.MoveTowards(shownFraction, target, 1.5f * Time.deltaTime); // плавное убывание
        SetFraction(shownFraction);
    }

    private void SetFraction(float fraction)
    {
        shownFraction = fraction;
        fill.anchorMax = new Vector2(fraction, 1f);
    }

    // Мигание цвета и надписи фазы после перехода
    private void UpdatePhaseFlash()
    {
        if (Time.time > phaseFlashUntil)
        {
            bool second = phases != null && phases.Phase >= 2;
            phaseText.alpha = second ? 1f : 0f;
            if (second) fillImage.color = Phase2Color;
            return;
        }
        float blink = 0.5f + 0.5f * Mathf.Sin(Time.time * 18f);
        phaseText.alpha = 1f;
        fillImage.color = Color.Lerp(Phase2Color, Color.white, blink * 0.7f);
    }

    // Полоса в сторону вертолёта, но в пределах угла обзора: по горизонту и высоте ограничиваем
    private void UpdatePose()
    {
        Vector3 camPos = cam.position;
        Vector3 toAnchor = anchor != null ? anchor.position - camPos : cam.forward;
        float anchorYaw = Mathf.Atan2(toAnchor.x, toAnchor.z) * Mathf.Rad2Deg;
        float horizontal = new Vector2(toAnchor.x, toAnchor.z).magnitude;
        float anchorPitch = Mathf.Atan2(toAnchor.y, horizontal) * Mathf.Rad2Deg;

        Vector3 flatForward = cam.forward;
        flatForward.y = 0f;
        float viewYaw = flatForward.sqrMagnitude > 0.0001f ? Mathf.Atan2(flatForward.x, flatForward.z) * Mathf.Rad2Deg : anchorYaw;

        float yaw = viewYaw + Mathf.Clamp(Mathf.DeltaAngle(viewYaw, anchorYaw), -maxYawFromView, maxYawFromView);
        float pitch = Mathf.Clamp(anchorPitch, pitchRange.x, pitchRange.y);

        Vector3 direction = Quaternion.Euler(-pitch, yaw, 0f) * Vector3.forward;
        Vector3 targetPos = camPos + direction * distance;

        Transform t = transform;
        t.position = positioned ? Vector3.Lerp(t.position, targetPos, 1f - Mathf.Exp(-followSharpness * Time.deltaTime)) : targetPos;
        positioned = true;
        t.rotation = Quaternion.LookRotation(t.position - camPos, Vector3.up);
    }

    // ---------- Построение интерфейса ----------

    private void BuildUi()
    {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        group = gameObject.AddComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;

        var rect = (RectTransform)transform;
        rect.sizeDelta = new Vector2(CanvasWidth, CanvasHeight);
        rect.localScale = Vector3.one * (width / CanvasWidth);

        TMP_FontAsset font = Resources.Load<TMP_FontAsset>(UiFont);

        // Фон и рамка полосы
        RectTransform back = CreateImage("Back", rect, BackColor);
        back.anchorMin = new Vector2(0f, 0f);
        back.anchorMax = new Vector2(1f, 0.52f);
        back.offsetMin = back.offsetMax = Vector2.zero;

        // Заполнение: растягивается по ширине через anchorMax.x
        RectTransform inner = CreateImage("FillArea", back, Color.clear);
        inner.anchorMin = Vector2.zero;
        inner.anchorMax = Vector2.one;
        inner.offsetMin = new Vector2(8f, 8f);
        inner.offsetMax = new Vector2(-8f, -8f);

        fill = CreateImage("Fill", inner, Phase1Color);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.offsetMin = fill.offsetMax = Vector2.zero;
        fillImage = fill.GetComponent<Image>();

        // Отметка порога второй фазы
        float threshold = phases != null ? phases.Phase2Threshold : 0.5f;
        RectTransform mark = CreateImage("PhaseMark", inner, new Color(1f, 1f, 1f, 0.9f));
        mark.anchorMin = new Vector2(threshold, 0f);
        mark.anchorMax = new Vector2(threshold, 1f);
        mark.sizeDelta = new Vector2(4f, 0f);

        nameText = CreateText("Name", rect, font, 58f, TextAlignmentOptions.BottomLeft);
        nameText.rectTransform.anchorMin = new Vector2(0f, 0.52f);
        nameText.rectTransform.anchorMax = new Vector2(0.7f, 1f);
        nameText.rectTransform.offsetMin = new Vector2(6f, 0f);
        nameText.rectTransform.offsetMax = Vector2.zero;
        nameText.text = UiText.Widen(bossName);
        nameText.color = Phase1Color;

        phaseText = CreateText("Phase", rect, font, 52f, TextAlignmentOptions.BottomRight);
        phaseText.rectTransform.anchorMin = new Vector2(0.7f, 0.52f);
        phaseText.rectTransform.anchorMax = new Vector2(1f, 1f);
        phaseText.rectTransform.offsetMin = Vector2.zero;
        phaseText.rectTransform.offsetMax = new Vector2(-6f, 0f);
        phaseText.color = Phase2Color;
        phaseText.alpha = 0f;
    }

    private static RectTransform CreateImage(string name, RectTransform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rt;
    }

    private static TextMeshProUGUI CreateText(string name, RectTransform parent, TMP_FontAsset font, float size, TextAlignmentOptions alignment)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.fontSize = size;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        return text;
    }
}
