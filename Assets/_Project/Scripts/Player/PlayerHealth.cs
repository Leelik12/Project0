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

    [Header("Отклик на урон")]
    [SerializeField] private float damageHapticAmplitude = 0.7f;
    [SerializeField] private float damageHapticDuration = 0.15f;

    private DynamicMoveProvider moveProvider;
    private float baseMoveSpeed;      // скорость с учётом «сильных ног»
    private float speedBoostEndTime;  // до какого момента действует ускорение после урона
    private bool isDead;
    private float defaultFixedDeltaTime;
    private DamageVignette damageVignette;

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
    }

    private void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();
    }

    private void Update()
    {
        HandleImplants();
        HandleMenu();
        HandleMissionChecks();
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

    private void HandleMenu()
    {
        bool menuPressed = MenuButton.action.ReadValue<float>() >= 0.7f;
        if (MenuCanvas != null && MenuCanvas.activeSelf != menuPressed)
        {
            MenuCanvas.SetActive(menuPressed);
        }
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
        float elapsed = 0f;
        while (elapsed < GameState.PropitalHealValue)
        {
            if (currentHealth < maxHealth) currentHealth++;
            UpdateHealthUI();
            elapsed += 1f;
            yield return new WaitForSecondsRealtime(1f);
        }
    }

    // «Сандевистан»: замедление времени на SandevistanTime секунд. Один раз за этаж.
    private IEnumerator SandevistanRoutine()
    {
        GameState.SandevistanActive = true;
        Time.timeScale = GameState.SandevistanTimeSlower;
        Time.fixedDeltaTime = defaultFixedDeltaTime * Time.timeScale; // физика должна замедлиться вместе со временем

        yield return new WaitForSecondsRealtime(GameState.SandevistanTime);

        Time.timeScale = 1f;
        Time.fixedDeltaTime = defaultFixedDeltaTime;
    }

    private void OnDestroy()
    {
        // Если сцена выгрузилась во время Сандевистана — не оставляем замедленное время.
        Time.timeScale = 1f;
        Time.fixedDeltaTime = defaultFixedDeltaTime;
    }
}
