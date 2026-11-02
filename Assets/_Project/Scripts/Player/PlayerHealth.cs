using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using XR.Interaction.Toolkit.Samples;

/// <summary>
/// Здоровье игрока, активируемые импланты (Пропитал, Сандевистан), меню паузы и HUD.
/// Висит на XR Origin. Имя класса и методы back2Main/ToMain привязаны к кнопкам в сценах — не переименовывать.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Настройки здоровья")]
    public float maxHealth;
    public float currentHealth;

    [Header("Настройки баффов")]
    public InputActionProperty HealButton;
    [FormerlySerializedAs("SandewistanButton")] public InputActionProperty SandevistanButton;

    [Header("Настройки меню")]
    public InputActionProperty MenuButton;
    public GameObject MenuCanvas;
    public Slider healthSlider;
    public GameObject MainCheck; // галочка «все враги убиты»
    public GameObject SecCheck;  // галочка «предмет подобран»
    public TextMeshProUGUI hpText;

    [Header("Не трогать")]
    public GameObject controller; // объект с DynamicMoveProvider

    [Header("Katana")]
    public GameObject katana;

    [Header("Импланты на HUD (необязательно)")]
    [Tooltip("Иконка «Пропитал готов»: видна, если имплант куплен и ещё не использован на этаже")]
    public GameObject propitalReadyIcon;
    [Tooltip("Иконка «Сандевистан готов»")]
    public GameObject sandevistanReadyIcon;

    [Header("Отклик на урон")]
    [SerializeField] private float damageHapticAmplitude = 0.7f;
    [SerializeField] private float damageHapticDuration = 0.15f;

    private DynamicMoveProvider moveProvider;
    private float baseMoveSpeed;      // скорость с учётом «сильных ног»
    private float speedBoostEndTime;  // до какого момента действует ускорение после урона
    private bool isDead;
    private float defaultFixedDeltaTime;
    private DamageVignette damageVignette;
    private SandevistanEffect sandevistanEffect;
    private bool isPaused;
    private bool menuButtonWasPressed;
    private bool sandevistanRunning;

    private void Awake()
    {
        defaultFixedDeltaTime = Time.fixedDeltaTime;
        HealButton.action.Enable();
        SandevistanButton.action.Enable();
        MenuButton.action.Enable();

        moveProvider = controller.GetComponent<DynamicMoveProvider>();
        baseMoveSpeed = GameState.PlayerBasicSpeed;
        if (GameState.StrongLegs)
        {
            baseMoveSpeed *= GameState.StrongLegsMultiplier;
        }
        moveProvider.moveSpeed = baseMoveSpeed;

        maxHealth += GameState.PlayerHPBuff;
        if (katana != null) katana.SetActive(GameState.Katana);

        damageVignette = DamageVignette.Create(transform);
        sandevistanEffect = SandevistanEffect.Create(transform);
    }

    private void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();
    }

    private void Update()
    {
        HandleMenu();
        HandleMissionChecks();
        HandleImplantIcons();
        if (isPaused) return;

        HandleImplants();
        HandleSpeedBoost();
        GameState.RunTime += Time.unscaledDeltaTime;
    }

    // ---------- Урон и смерть ----------

    public void PlayerTakeDamage(float damage)
    {
        if (isDead) return;

        if (GameState.SpeedBuffAfterDamage)
        {
            ApplySpeedBoost();
        }

        currentHealth -= damage;
        UpdateHealthUI();
        Haptics.PulseBoth(damageHapticAmplitude, damageHapticDuration);
        if (damageVignette != null) damageVignette.Flash(damage / maxHealth);
        GameEvents.RaisePlayerDamaged(damage);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        GameState.DiedInCyberpunk = true;
        GameState.PlayerLost = true; // в меню покажется экран поражения
        GameState.ResetRun();
        SceneManager.LoadSceneAsync(0);
    }

    /// <summary>Выход в главное меню с потерей прогресса забега (кнопка в меню паузы).</summary>
    public void back2Main()
    {
        GameState.ResetRun();
        SceneManager.LoadSceneAsync(0);
    }

    /// <summary>Выход в главное меню с сохранением прогресса: в меню забег можно продолжить (SceneManagerMain.Continue).</summary>
    public void ToMain()
    {
        GameState.RunInProgress = true;
        GameState.ContinueScene = SceneManager.GetActiveScene().buildIndex;
        GameState.SaveData();
        SceneManager.LoadSceneAsync(0);
    }

    public float GetCurrentHealth() => currentHealth;
    public float GetHealthPercent() => currentHealth / maxHealth;

    // ---------- HUD ----------

    private void UpdateHealthUI()
    {
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth / maxHealth;
        }
        if (hpText != null)
        {
            hpText.text = Mathf.Max(0f, currentHealth).ToString("0");
        }
    }

    // ---------- Пауза ----------

    // Кнопка меню переключает паузу: время стоит, звук мира на паузе, импланты не тикают
    private void HandleMenu()
    {
        bool pressed = MenuButton.action.ReadValue<float>() >= 0.7f;
        if (pressed && !menuButtonWasPressed) SetPaused(!isPaused);
        menuButtonWasPressed = pressed;
    }

    /// <summary>Продолжить игру (для кнопки «Продолжить» в меню паузы).</summary>
    public void Resume() => SetPaused(false);

    private void SetPaused(bool paused)
    {
        if (isPaused == paused) return;
        isPaused = paused;
        if (MenuCanvas != null) MenuCanvas.SetActive(paused);
        AudioListener.pause = paused;
        ApplyTimeScale();
        GameEvents.RaisePauseChanged(paused);
    }

    // Итоговая скорость времени: пауза важнее Сандевистана
    private void ApplyTimeScale()
    {
        float gameScale = sandevistanRunning ? GameState.SandevistanTimeSlower : 1f;
        Time.timeScale = isPaused ? 0f : gameScale;
        Time.fixedDeltaTime = defaultFixedDeltaTime * gameScale; // физика замедляется вместе со временем
    }

    private void HandleMissionChecks()
    {
        if (MainCheck != null && GameState.LevelCleared && !MainCheck.activeSelf)
        {
            MainCheck.SetActive(true);
        }
        if (SecCheck != null && GameState.ItemPickedUp && !SecCheck.activeSelf)
        {
            SecCheck.SetActive(true);
        }
    }

    // ---------- Импланты ----------

    private void HandleImplants()
    {
        if (HealButton.action.ReadValue<float>() >= 0.7f
            && GameState.PropitalHeal && !GameState.PropitalHealActive
            && currentHealth < maxHealth)
        {
            StartCoroutine(PropitalRoutine());
        }

        if (SandevistanButton.action.ReadValue<float>() >= 0.7f
            && GameState.Sandevistan && !GameState.SandevistanActive)
        {
            StartCoroutine(SandevistanRoutine());
        }
    }

    private void HandleImplantIcons()
    {
        SetActiveIfChanged(propitalReadyIcon, GameState.PropitalHeal && !GameState.PropitalHealActive);
        SetActiveIfChanged(sandevistanReadyIcon, GameState.Sandevistan && !GameState.SandevistanActive);
    }

    private static void SetActiveIfChanged(GameObject go, bool active)
    {
        if (go != null && go.activeSelf != active) go.SetActive(active);
    }

    // Ускорение после урона: таймер вместо корутины, чтобы повторные попадания продлевали эффект, а не обрывали его.
    private void ApplySpeedBoost()
    {
        speedBoostEndTime = Time.time + GameState.SpeedTimeAfterDamage;
        moveProvider.moveSpeed = baseMoveSpeed * GameState.SpeedAfterDamageValue;
    }

    private void HandleSpeedBoost()
    {
        if (speedBoostEndTime > 0f && Time.time >= speedBoostEndTime)
        {
            speedBoostEndTime = 0f;
            moveProvider.moveSpeed = baseMoveSpeed;
        }
    }

    // «Пропитал»: восстанавливает по 1 HP в секунду в течение PropitalHealValue секунд. Один раз за этаж.
    private IEnumerator PropitalRoutine()
    {
        GameState.PropitalHealActive = true;
        float ticks = 0f;
        float tickTimer = 0f;
        while (ticks < GameState.PropitalHealValue)
        {
            if (tickTimer <= 0f)
            {
                if (currentHealth < maxHealth) currentHealth++;
                UpdateHealthUI();
                ticks += 1f;
                tickTimer = 1f;
            }
            yield return null;
            if (!isPaused) tickTimer -= Time.unscaledDeltaTime; // реальное время, но на паузе не лечит
        }
    }

    // «Сандевистан»: замедление времени на SandevistanTime секунд. Один раз за этаж.
    private IEnumerator SandevistanRoutine()
    {
        GameState.SandevistanActive = true;
        sandevistanRunning = true;
        ApplyTimeScale();
        if (sandevistanEffect != null) sandevistanEffect.SetActive(true);
        GameEvents.RaiseSandevistanChanged(true);

        // Реальное время, но на паузе действие импланта не тратится
        float remaining = GameState.SandevistanTime;
        while (remaining > 0f)
        {
            yield return null;
            if (!isPaused) remaining -= Time.unscaledDeltaTime;
        }

        sandevistanRunning = false;
        ApplyTimeScale();
        if (sandevistanEffect != null) sandevistanEffect.SetActive(false);
        GameEvents.RaiseSandevistanChanged(false);
    }

    private void OnDestroy()
    {
        // Если сцена выгрузилась на паузе или во время Сандевистана — не оставляем остановленное время и звук.
        AudioListener.pause = false;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = defaultFixedDeltaTime;
    }
}
