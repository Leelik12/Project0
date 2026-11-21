using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Активные киберимпланты с перезарядкой: Пропитал (отхил), Сандевистан (замедление времени),
/// оптический камуфляж (невидимость) и взлом «Короткое замыкание» (оглушить врага, на которого смотришь).
/// Перезарядка идёт в реальном времени и стоит на паузе. Готовность видна на индикаторах в меню,
/// а когда имплант перезарядился — короткая вибрация.
/// Кнопки: Пропитал и Сандевистан — из PlayerHealth (B и A на правом контроллере),
/// камуфляж — X на левом, взлом — нажатие правого стика.
/// </summary>
public class PlayerImplants : MonoBehaviour
{
    private const float PropitalCooldown = 45f;
    private const float PropitalDuration = 5f; // за сколько секунд восстанавливается всё лечение
    private const float SandevistanCooldown = 60f;
    private const float OpticalCamoCooldown = 40f;
    private const float ShortCircuitCooldown = 20f;
    private const float ShortCircuitRange = 25f;
    private const float ShortCircuitAngle = 12f; // насколько точно нужно смотреть на врага
    private const float EnemyChestHeight = 1.3f;
    private const float PressThreshold = 0.7f;

    private static readonly Color ReadyColor = new(0.25f, 1f, 0.45f);
    private static readonly Color CooldownColor = new(0.6f, 0.6f, 0.6f);

    private sealed class Implant
    {
        public string Name;
        public Func<bool> Owned;
        public float Cooldown;
        public InputAction Button;
        public Func<bool> Activate; // false — не сработал (нет цели, полное здоровье), перезарядка не тратится
        public GameObject Indicator;
        public TMP_Text Label;
        public float ReadyAt;
        public bool WasPressed;
        public bool WasReady = true;
    }

    private PlayerHealth health;
    private Implant[] implants;
    private InputAction camoButton;
    private InputAction shortCircuitButton;
    private ImplantScreenEffect sandevistanEffect;
    private ImplantScreenEffect camoEffect;
    private float clock; // реальное время без пауз

    public void Init(PlayerHealth owner)
    {
        health = owner;
        sandevistanEffect = ImplantScreenEffect.Create(transform, ImplantScreenEffect.Style.Sandevistan);
        camoEffect = ImplantScreenEffect.Create(transform, ImplantScreenEffect.Style.OpticalCamo);

        camoButton = new InputAction("Optical Camo", InputActionType.Button, "<XRController>{LeftHand}/{PrimaryButton}");
        shortCircuitButton = new InputAction("Short Circuit", InputActionType.Button, "<XRController>{RightHand}/{Primary2DAxisClick}");
        camoButton.Enable();
        shortCircuitButton.Enable();

        implants = new[]
        {
            Make("Пропитал", () => GameState.PropitalHeal, PropitalCooldown, owner.HealButton.action, StartPropital, owner.propitalReadyIcon),
            Make("Сандевистан", () => GameState.Sandevistan, SandevistanCooldown, owner.SandevistanButton.action, StartSandevistan, owner.sandevistanReadyIcon),
            Make("Камуфляж", () => GameState.OpticalCamo, OpticalCamoCooldown, camoButton, StartOpticalCamo, owner.opticalCamoReadyIcon),
            Make("Взлом", () => GameState.ShortCircuit, ShortCircuitCooldown, shortCircuitButton, ShortCircuit, owner.shortCircuitReadyIcon),
        };
    }

    /// <summary>Сколько имплантов с перезарядкой (для наручного HUD).</summary>
    public int ImplantCount => implants != null ? implants.Length : 0;

    /// <summary>
    /// Состояние импланта для HUD. Возвращает false, если имплант не куплен.
    /// remaining — секунд до готовности (0 — готов), cooldown — полная перезарядка.
    /// </summary>
    public bool TryGetStatus(int index, out string name, out float remaining, out float cooldown)
    {
        name = null;
        remaining = 0f;
        cooldown = 1f;
        if (implants == null || index < 0 || index >= implants.Length) return false;

        Implant implant = implants[index];
        name = implant.Name;
        cooldown = implant.Cooldown;
        remaining = Mathf.Max(0f, implant.ReadyAt - clock);
        return implant.Owned();
    }

    private static Implant Make(string name, Func<bool> owned, float cooldown, InputAction button, Func<bool> activate, GameObject indicator) => new()
    {
        Name = name,
        Owned = owned,
        Cooldown = cooldown,
        Button = button,
        Activate = activate,
        Indicator = indicator,
        Label = indicator != null ? indicator.GetComponent<TMP_Text>() : null,
    };

    private void Update()
    {
        if (implants == null) return;
        if (!health.IsPaused) clock += Time.unscaledDeltaTime;

        foreach (Implant implant in implants)
        {
            bool ready = clock >= implant.ReadyAt;
            if (ready && !implant.WasReady && implant.Owned()) Haptics.PulseBoth(0.25f, 0.08f); // перезарядился
            implant.WasReady = ready;

            bool pressed = implant.Button != null && implant.Button.ReadValue<float>() >= PressThreshold;
            if (pressed && !implant.WasPressed && ready && !health.IsPaused && implant.Owned() && implant.Activate())
            {
                implant.ReadyAt = clock + implant.Cooldown;
                implant.WasReady = false;
            }
            implant.WasPressed = pressed;

            UpdateIndicator(implant);
        }
    }

    private void UpdateIndicator(Implant implant)
    {
        if (implant.Indicator == null) return;

        bool owned = implant.Owned();
        bool ready = clock >= implant.ReadyAt;
        if (implant.Label == null)
        {
            SetActiveIfChanged(implant.Indicator, owned && ready);
            return;
        }

        SetActiveIfChanged(implant.Indicator, owned);
        if (!owned) return;

        string text = ready ? implant.Name : UiText.Widen($"{implant.Name} {Mathf.CeilToInt(implant.ReadyAt - clock)}");
        if (implant.Label.text != text) implant.Label.text = text;
        implant.Label.color = ready ? ReadyColor : CooldownColor;
    }

    private static void SetActiveIfChanged(GameObject go, bool active)
    {
        if (go.activeSelf != active) go.SetActive(active);
    }

    // Ждёт seconds секунд реального времени, не считая паузу
    private IEnumerator WaitUnpaused(float seconds, Func<bool> stopEarly = null)
    {
        float end = clock + seconds;
        while (clock < end && (stopEarly == null || !stopEarly())) yield return null;
    }

    // ---------- Пропитал ----------

    private bool StartPropital()
    {
        if (health.GetHealthPercent() >= 1f) return false;
        StartCoroutine(PropitalRoutine());
        return true;
    }

    private IEnumerator PropitalRoutine()
    {
        float perSecond = GameState.PropitalHealValue / PropitalDuration;
        float end = clock + PropitalDuration;
        while (clock < end)
        {
            yield return null;
            if (!health.IsPaused) health.Heal(perSecond * Time.unscaledDeltaTime);
        }
    }

    // ---------- Сандевистан ----------

    private bool StartSandevistan()
    {
        StartCoroutine(SandevistanRoutine());
        return true;
    }

    private IEnumerator SandevistanRoutine()
    {
        health.SetSandevistan(true);
        sandevistanEffect.SetActive(true);
        yield return WaitUnpaused(GameState.SandevistanTime);
        health.SetSandevistan(false);
        sandevistanEffect.SetActive(false);
    }

    // ---------- Оптический камуфляж ----------

    private bool StartOpticalCamo()
    {
        StartCoroutine(OpticalCamoRoutine());
        return true;
    }

    private IEnumerator OpticalCamoRoutine()
    {
        PlayerStealth.SetInvisible(true);
        camoEffect.SetActive(true);
        // Выстрел снимает камуфляж раньше (PlayerStealth.ConsumeAmbush)
        yield return WaitUnpaused(GameState.OpticalCamoTime, () => !PlayerStealth.IsInvisible);
        PlayerStealth.SetInvisible(false);
        camoEffect.SetActive(false);
    }

    // ---------- Короткое замыкание ----------

    private bool ShortCircuit()
    {
        Camera cam = Camera.main;
        if (cam == null) return false;

        EnemyStateManager target = FindLookedAtEnemy(cam.transform);
        if (target == null)
        {
            Haptics.PulseBoth(0.1f, 0.05f); // некого взламывать — перезарядку не тратим
            return false;
        }

        Vector3 chest = target.transform.position + Vector3.up * EnemyChestHeight;
        Vector3 from = cam.transform.position - cam.transform.up * 0.25f;
        Tracer.Spawn(from, chest, enemy: false);

        target.Stun(GameState.ShortCircuitStunTime);
        EnemyHealth targetHealth = target.GetComponent<EnemyHealth>();
        if (targetHealth != null)
        {
            GameState.RegisterDamage(GameState.ShortCircuitDamage);
            targetHealth.TakeDamage(GameState.ShortCircuitDamage, chest, (chest - from).normalized, false);
        }
        return true;
    }

    private static EnemyStateManager FindLookedAtEnemy(Transform head)
    {
        EnemyStateManager best = null;
        float bestAngle = ShortCircuitAngle;

        foreach (EnemyStateManager enemy in EnemyManager.AllEnemies)
        {
            if (enemy == null || !enemy.enabled) continue; // мёртвые выключают автомат

            Vector3 chest = enemy.transform.position + Vector3.up * EnemyChestHeight;
            Vector3 toEnemy = chest - head.position;
            float distance = toEnemy.magnitude;
            if (distance > ShortCircuitRange) continue;

            float angle = Vector3.Angle(head.forward, toEnemy);
            if (angle >= bestAngle) continue;

            // Взлом нужен прямой видимости: первое, во что упирается взгляд, должно быть этим врагом
            if (Physics.Raycast(head.position, toEnemy / distance, out RaycastHit hit, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                && hit.collider.GetComponentInParent<EnemyStateManager>() != enemy)
            {
                continue;
            }

            best = enemy;
            bestAngle = angle;
        }
        return best;
    }

    private void OnDestroy()
    {
        PlayerStealth.SetInvisible(false); // статическое состояние не должно пережить сцену
        camoButton?.Dispose();
        shortCircuitButton?.Dispose();
    }
}
