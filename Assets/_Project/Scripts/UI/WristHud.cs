using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Наручный HUD (U-01): «часы» на левом запястье. Показывает здоровье, броню, патроны оружия в руке, эдди,
/// сколько врагов осталось на этаже и готовность имплантов. Голограмма проявляется, когда игрок разворачивает
/// запястье к лицу (угол между нормалью экрана и направлением на камеру) и гаснет, когда рука опущена.
/// Префаб лежит в Resources/WristHUD и создаётся из PlayerHealth.Awake — риг XRI в сценах не меняется.
/// Меню паузы (кнопки, слайдер здоровья) остаётся на отдельном холсте и этим HUD не затрагивается.
/// </summary>
public class WristHud : MonoBehaviour
{
    private const string PrefabPath = "WristHUD";
    private const float InfiniteAmmo = 999f; // у лазерного оружия запас патронов условно бесконечный
    private const int MaxImplants = 4;

    private static readonly Color ReadyColor = new(0.25f, 1f, 0.45f);
    private static readonly Color CooldownColor = new(0.6f, 0.65f, 0.7f);
    private static readonly Color ReadyFill = new(0.25f, 1f, 0.45f, 0.3f);
    private static readonly Color CooldownFill = new(0.6f, 0.65f, 0.7f, 0.45f);
    private static readonly Color HpColor = new(0.35f, 1f, 0.9f);
    private static readonly Color HpLowColor = new(1f, 0.3f, 0.25f);

    [System.Serializable]
    private struct ImplantChip
    {
        public GameObject root;
        public Image fill;       // полоска перезарядки
        public TMP_Text label;
    }

    [Header("Голограмма")]
    [Tooltip("Корень холста: его -forward — нормаль экрана, смотрящая на игрока")]
    [SerializeField] private Transform holo;
    [SerializeField] private Canvas canvas;
    [SerializeField] private CanvasGroup group;

    [Header("Поля")]
    [SerializeField] private Image hpFill;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text armorText;
    [SerializeField] private TMP_Text ammoText;
    [SerializeField] private TMP_Text eddiesText;
    [SerializeField] private TMP_Text enemiesText;
    [SerializeField] private ImplantChip[] chips = new ImplantChip[MaxImplants];

    [Header("Появление")]
    [Tooltip("Косинус угла, с которого экран начинает проявляться (0.55 — около 57 градусов от оси взгляда)")]
    [SerializeField, Range(0f, 1f)] private float fadeStartDot = 0.55f;
    [Tooltip("Косинус угла, при котором экран виден полностью")]
    [SerializeField, Range(0f, 1f)] private float fadeFullDot = 0.85f;
    [Tooltip("Дальше этого расстояния от камеры экран не показывается")]
    [SerializeField] private float maxDistance = 0.75f;
    [SerializeField] private float fadeSpeed = 8f;

    private PlayerHealth health;
    private PlayerImplants implants;
    private Camera viewCamera;
    private float alpha;

    // Что сейчас показано — строки пересобираем только при изменении значений
    private int shownHp = int.MinValue, shownMaxHp = int.MinValue, shownArmor = int.MinValue;
    private int shownAmmo = int.MinValue, shownMaxAmmo = int.MinValue, shownEddies = int.MinValue, shownEnemies = int.MinValue;
    private readonly int[] shownImplantSeconds = new int[MaxImplants];
    private readonly bool[] shownImplantOwned = new bool[MaxImplants];

    /// <summary>Создаёт HUD на левом контроллере игрока. Возвращает null, если префаба или руки нет.</summary>
    public static WristHud Attach(PlayerHealth owner)
    {
        GameObject prefab = Resources.Load<GameObject>(PrefabPath);
        Transform hand = FindLeftController(owner.transform);
        if (prefab == null || hand == null) return null;

        // Положение и поворот на запястье заданы в самом префабе (локально относительно контроллера)
        WristHud hud = Instantiate(prefab, hand, false).GetComponent<WristHud>();
        hud.health = owner;
        hud.implants = owner.GetComponent<PlayerImplants>();
        hud.viewCamera = owner.GetComponentInChildren<Camera>();
        return hud;
    }

    private static Transform FindLeftController(Transform rig)
    {
        Transform hand = rig.Find("Camera Offset/Left Controller");
        if (hand != null) return hand;
        foreach (Transform t in rig.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == "Left Controller") return t;
        }
        return null;
    }

    private void Awake()
    {
        for (int i = 0; i < MaxImplants; i++)
        {
            shownImplantSeconds[i] = int.MinValue;
            shownImplantOwned[i] = true;
        }
        SetVisible(0f);
    }

    private void LateUpdate()
    {
        if (viewCamera == null)
        {
            viewCamera = Camera.main;
            if (viewCamera == null) return;
        }

        float target = ComputeVisibility();
        alpha = Mathf.MoveTowards(alpha, target, fadeSpeed * Time.unscaledDeltaTime);
        SetVisible(alpha);
        if (alpha <= 0f) return;

        RefreshVitals();
        RefreshAmmoAndEddies();
        RefreshImplants();
    }

    // 0..1: насколько экран повёрнут к глазам. Нормаль экрана — это -forward холста.
    private float ComputeVisibility()
    {
        Vector3 toCamera = viewCamera.transform.position - holo.position;
        float distance = toCamera.magnitude;
        if (distance < 0.0001f || distance > maxDistance) return 0f;

        float dot = Vector3.Dot(-holo.forward, toCamera / distance);
        return Mathf.Clamp01(Mathf.InverseLerp(fadeStartDot, fadeFullDot, dot));
    }

    private void SetVisible(float value)
    {
        bool show = value > 0.001f;
        if (canvas.enabled != show) canvas.enabled = show; // невидимый холст не рисуется и не пересобирается
        group.alpha = value;
    }

    // ---------- Обновление полей ----------

    private void RefreshVitals()
    {
        if (health == null) return;

        int hp = Mathf.CeilToInt(Mathf.Max(0f, health.currentHealth));
        int maxHp = Mathf.RoundToInt(health.maxHealth);
        if (hp != shownHp || maxHp != shownMaxHp)
        {
            shownHp = hp;
            shownMaxHp = maxHp;
            hpText.text = UiText.Widen($"HP {hp} / {maxHp}");
            float percent = health.GetHealthPercent();
            hpFill.fillAmount = Mathf.Clamp01(percent);
            hpFill.color = percent < 0.3f ? HpLowColor : HpColor;
        }

        int armor = Mathf.RoundToInt(GameState.Armor * 100f);
        if (armor != shownArmor)
        {
            shownArmor = armor;
            armorText.text = armor > 0 ? UiText.Widen($"Броня {armor}%") : string.Empty;
        }
    }

    private void RefreshAmmoAndEddies()
    {
        VRGun gun = VRGun.Held;
        int ammo = gun == null ? -2 : (gun.maxAmmo >= InfiniteAmmo ? -1 : Mathf.Max(0, (int)gun.currentAmmo));
        int maxAmmo = gun == null ? 0 : (int)gun.maxAmmo;
        if (ammo != shownAmmo || maxAmmo != shownMaxAmmo)
        {
            shownAmmo = ammo;
            shownMaxAmmo = maxAmmo;
            ammoText.text = ammo < 0 ? "--" : UiText.Widen($"{ammo} / {maxAmmo}");
            ammoText.color = ammo >= 0 && ammo <= maxAmmo * 0.25f ? HpLowColor : Color.white;
        }

        if (GameState.Eddies != shownEddies)
        {
            shownEddies = GameState.Eddies;
            eddiesText.text = UiText.Widen($"Эдди: {shownEddies}");
        }

        int enemies = LevelManager.EnemiesRemaining;
        if (enemies != shownEnemies)
        {
            shownEnemies = enemies;
            enemiesText.text = enemies > 0 ? UiText.Widen($"Врагов: {enemies}") : string.Empty;
        }
    }

    private void RefreshImplants()
    {
        if (implants == null) return;

        int count = Mathf.Min(implants.ImplantCount, MaxImplants);
        for (int i = 0; i < MaxImplants; i++)
        {
            ImplantChip chip = chips[i];
            string name = null;
            float remaining = 0f, cooldown = 1f;
            bool owned = i < count && implants.TryGetStatus(i, out name, out remaining, out cooldown);
            if (owned != shownImplantOwned[i])
            {
                shownImplantOwned[i] = owned;
                chip.root.SetActive(owned);
                shownImplantSeconds[i] = int.MinValue;
            }
            if (!owned) continue;

            int seconds = Mathf.CeilToInt(remaining);
            chip.fill.fillAmount = cooldown > 0f ? 1f - Mathf.Clamp01(remaining / cooldown) : 1f;
            if (seconds == shownImplantSeconds[i]) continue;

            shownImplantSeconds[i] = seconds;
            bool ready = seconds <= 0;
            chip.label.text = ready ? UiText.Widen(name) : UiText.Widen($"{name} {seconds}");
            chip.label.color = ready ? ReadyColor : CooldownColor;
            chip.fill.color = ready ? ReadyFill : CooldownFill;
        }
    }
}
