using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using XR.Interaction.Toolkit.Samples;

/// <summary>
/// Здоровье игрока, пассивные импланты, меню паузы и HUD. Активные импланты — в PlayerImplants (создаётся отсюда).
/// Здоровье переносится между этажами через GameState.PlayerHealthLeft.
/// Висит на XR Origin. Имя класса и методы back2Main/ToMain привязаны к кнопкам в сценах — не переименовывать.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    private const float RegenerationDelay = 5f; // сколько секунд без урона до начала регенерации
    private const float MinSandevistanTimeScale = 0.05f;

    /// <summary>Игрок на текущей сцене (null в меню без рига).</summary>
    public static PlayerHealth Current { get; private set; }

    [Header("Настройки здоровья")]
    public float maxHealth;
    public float currentHealth;

    [Header("Кнопки активных имплантов")]
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
    [Tooltip("Индикатор «Пропитала»: если на объекте есть текст, в нём показывается перезарядка; иначе объект виден, пока имплант готов")]
    public GameObject propitalReadyIcon;
    public GameObject sandevistanReadyIcon;
    public GameObject opticalCamoReadyIcon;
    public GameObject shortCircuitReadyIcon;

    [Header("Отклик на урон")]
    [SerializeField] private float damageHapticAmplitude = 0.7f;
    [SerializeField] private float damageHapticDuration = 0.15f;

    private DynamicMoveProvider moveProvider;
    private float baseMoveSpeed;      // скорость с учётом «сильных ног»
    private float speedBoostEndTime;  // до какого момента действует ускорение после урона
    private bool isDead;
    private float defaultFixedDeltaTime;
    private DamageVignette damageVignette;
    private bool menuButtonWasPressed;
    private bool sandevistanRunning;
    private float lastDamageTime = -100f;

    public bool IsPaused { get; private set; }

    private void Awake()
    {
        Current = this;
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
        gameObject.AddComponent<PlayerImplants>().Init(this);
        WristHud.Attach(this); // наручный HUD на левой руке (префаб из Resources)
        GameEvents.EnemyHit += OnEnemyHit;
    }

    private void Start()
    {
        // Здоровье переносится с прошлого этажа; в начале забега — полное
        currentHealth = GameState.PlayerHealthLeft > 0f ? Mathf.Min(GameState.PlayerHealthLeft, maxHealth) : maxHealth;
        UpdateHealthUI();
        if (MenuCanvas != null) MenuCanvas.SetActive(false); // в сценах меню сохранено включённым, а показывается только на паузе
    }

    private void Update()
    {
        HandleMenu();
        HandleMissionChecks();
        if (IsPaused) return;

        HandleSpeedBoost();
        HandleRegeneration();
        GameState.RunTime += Time.unscaledDeltaTime;
    }

    // ---------- Урон, лечение и смерть ----------

    public void PlayerTakeDamage(float damage)
    {
        if (isDead) return;

        if (GameState.SpeedBuffAfterDamage)
        {
            ApplySpeedBoost();
        }

        damage *= 1f - GameState.Armor; // подкожная броня
        lastDamageTime = Time.time;
        SetHealth(currentHealth - damage);
        Haptics.PulseBoth(damageHapticAmplitude, damageHapticDuration);
        if (damageVignette != null) damageVignette.Flash(damage / maxHealth);
        GameEvents.RaisePlayerDamaged(damage);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>Восстановить здоровье (импланты, вампиризм, лечение в магазине).</summary>
    public void Heal(float amount)
    {
        if (isDead || amount <= 0f) return;
        SetHealth(Mathf.Min(maxHealth, currentHealth + amount));
    }

    /// <summary>Имплант «+HP», купленный прямо сейчас (в магазине): новая прослойка приходит целой.</summary>
    public void AddMaxHealth(float amount)
    {
        if (isDead || amount <= 0f) return;
        maxHealth += amount;
        SetHealth(currentHealth + amount);
    }

    private void SetHealth(float value)
    {
        currentHealth = value;
        GameState.PlayerHealthLeft = Mathf.Max(1f, currentHealth);
        UpdateHealthUI();
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
        GameState.RollbackToFloorStart(); // этаж начнётся заново — награды за него тоже
        GameState.SaveData();
        SceneManager.LoadSceneAsync(0);
    }

    public float GetCurrentHealth() => currentHealth;
    public float GetHealthPercent() => maxHealth > 0f ? currentHealth / maxHealth : 0f;

    // ---------- Пассивные импланты ----------

    // Вампиризм за убийство; модификатор оружия «Вампиризм» лечит в VRGun
    private void OnEnemyHit(EnemyHitInfo hit)
    {
        if (hit.Killed && GameState.KillHeal > 0f) Heal(GameState.KillHeal);
    }

    private void HandleRegeneration()
    {
        if (GameState.Regeneration <= 0f || currentHealth >= maxHealth) return;
        if (Time.time - lastDamageTime < RegenerationDelay) return;
        Heal(GameState.Regeneration * Time.deltaTime);
    }

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

    // ---------- Пауза и скорость времени ----------

    // Кнопка меню переключает паузу: время стоит, звук мира на паузе, импланты не тикают
    private void HandleMenu()
    {
        bool pressed = MenuButton.action.ReadValue<float>() >= 0.7f;
        if (pressed && !menuButtonWasPressed) SetPaused(!IsPaused);
        menuButtonWasPressed = pressed;
    }

    /// <summary>Продолжить игру (для кнопки «Продолжить» в меню паузы).</summary>
    public void Resume() => SetPaused(false);

    private void SetPaused(bool paused)
    {
        if (IsPaused == paused) return;
        IsPaused = paused;
        if (MenuCanvas != null) MenuCanvas.SetActive(paused);
        AudioListener.pause = paused;
        ApplyTimeScale();
        GameEvents.RaisePauseChanged(paused);
    }

    /// <summary>Сандевистан замедляет время (включает и выключает PlayerImplants).</summary>
    public void SetSandevistan(bool active)
    {
        if (sandevistanRunning == active) return;
        sandevistanRunning = active;
        ApplyTimeScale();
        GameEvents.RaiseSandevistanChanged(active);
    }

    // Итоговая скорость времени: пауза важнее Сандевистана
    private void ApplyTimeScale()
    {
        // Без нижней границы карточка с пустым замедлением остановила бы время совсем
        float gameScale = sandevistanRunning ? Mathf.Clamp(GameState.SandevistanTimeSlower, MinSandevistanTimeScale, 1f) : 1f;
        Time.timeScale = IsPaused ? 0f : gameScale;
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

    // ---------- Ускорение после урона ----------

    // Таймер вместо корутины, чтобы повторные попадания продлевали эффект, а не обрывали его.
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

    private void OnDestroy()
    {
        if (Current == this) Current = null;
        GameEvents.EnemyHit -= OnEnemyHit;

        // Если сцена выгрузилась на паузе или во время Сандевистана — не оставляем остановленное время и звук.
        AudioListener.pause = false;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = defaultFixedDeltaTime;
    }
}
