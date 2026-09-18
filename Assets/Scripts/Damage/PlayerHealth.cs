using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using XR.Interaction.Toolkit.Samples;
using UnityEngine.InputSystem;
using TMPro;

public class PlayerHealth : MonoBehaviour
{
    [Header("Настройки здоровья")]
    public float maxHealth;
    public float currentHealth;
    [Header("Настройки баффов")]
    public InputActionProperty HealButton;
    public InputActionProperty SandewistanButton;
    [Header("Настройки меню")]
    public InputActionProperty MenuButton;
    public GameObject MenuCanvas;
    public Slider healthSlider; // Ссылка на UI-слайдер здоровья
    public GameObject MainCheck;
    public GameObject SecCheck;
    public TextMeshProUGUI hpText;
    [Header("Не трогать")]
    public GameObject controller;
    float oldSpeed;
    DynamicMoveProvider speed = null;
    [Header("Katana")]
    public GameObject katana;
    private void Awake()
    {
        HealButton.action.Enable();
        SandewistanButton.action.Enable();
        MenuButton.action.Enable();
        speed = controller.GetComponent<DynamicMoveProvider>();
        speed.moveSpeed = StaticHolder.PlayerBasicSpeed;
        oldSpeed = StaticHolder.PlayerBasicSpeed;
        if (StaticHolder.StrongLegs)
        {
            speed.moveSpeed = speed.moveSpeed * StaticHolder.StrongLegsKoef;
        }
        maxHealth = maxHealth + StaticHolder.PlayerHPBuff;
        katana.SetActive(StaticHolder.Katana);
    }
    private void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();
        Debug.Log("Здоровье игрока = " + currentHealth);
    }

    public void PlayerTakeDamage(float damage)
    {
        if (StaticHolder.SpeedBuffAfterDamage)
        {
            StartCoroutine(SpeedAfterDamage());
        }
        currentHealth -= damage;
        UpdateHealthUI();
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void UpdateHealthUI()
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
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Body") && collision.collider.name.Contains("Hand"))
        {
            Debug.Log("Больно");
        }
    }
    // Выход в главное меню с потерей прогресса забега (кнопка в меню паузы)
    public void back2Main()
    {
        Debug.Log("Выход в мэйн");
        StaticHolder.ResetRun();
        SceneManager.LoadSceneAsync(0);
    }
    void Die()
    {
        Debug.Log("Игрок погиб!");
        StaticHolder.DiedinCyberpunk = true;
        StaticHolder.PlayerLost = true; // в меню покажется экран поражения
        StaticHolder.ResetRun();
        SceneManager.LoadSceneAsync(0);
    }
    void Update()
    {
        if (HealButton.action.ReadValue<float>() >= 0.7f && StaticHolder.PropitalHeal && currentHealth < maxHealth && !StaticHolder.PropitalHealActive)
        {
            StartCoroutine(Propital());
        }
        if (SandewistanButton.action.ReadValue<float>() >= 0.7 && StaticHolder.Sandevistan && !StaticHolder.SandevistanActive)
        {
            StartCoroutine(Sandewistan());
        }
        bool menuPressed = MenuButton.action.ReadValue<float>() >= 0.7;
        if (MenuCanvas.activeSelf != menuPressed)
        {
            MenuCanvas.SetActive(menuPressed);
        }
        if (StaticHolder.levelCheksComplete && !MainCheck.activeSelf)
        {
            MainCheck.SetActive(true);
        }
        if (StaticHolder.ItemPickedUp && !SecCheck.activeSelf)
        {
            SecCheck.SetActive(true);
        }
    }
    public float GetCurrentHealth() => currentHealth;

    public float GetHealthPercent() => currentHealth / maxHealth;
    IEnumerator SpeedAfterDamage()
    {
        // Вызов начального действия
        Debug.Log("Ускорение после получения урона началось");
        speed.moveSpeed = StaticHolder.PlayerBasicSpeed * StaticHolder.SpeedAfterDamageValue;

        yield return new WaitForSeconds(StaticHolder.SpeedTimeAfterDamage);

        // Действие завершено
        Debug.Log("Ускорение после получения урона завершено");
        speed.moveSpeed = oldSpeed;
    }
    IEnumerator Propital()
    {
        Debug.Log("Отхил начат");
        float wastedTime = 0f;
        StaticHolder.PropitalHealActive = true;
        while (wastedTime < StaticHolder.PropitalHealValue)
        {
            if (currentHealth < maxHealth) { currentHealth++; }
            UpdateHealthUI();
            wastedTime += 1f;
            yield return new WaitForSecondsRealtime(1f); // ждём 1 секунду
        }
        Debug.Log("Отхил завершён");
    }
    IEnumerator Sandewistan()
    {
        Debug.Log("Замедление времени началось");
        StaticHolder.SandevistanActive = true;
        // Замедляем время
        float normalFixedDelta = Time.fixedDeltaTime;
        Time.timeScale = StaticHolder.SandevistanTimeSlower;
        Time.fixedDeltaTime = normalFixedDelta * Time.timeScale; // важно для корректной работы физики
        yield return new WaitForSecondsRealtime(StaticHolder.SandevistanTime); // ждём 
        // Возвращаем время к нормальному состоянию
        Time.timeScale = 1f;
        Time.fixedDeltaTime = normalFixedDelta;
        Debug.Log("Замедление времени окончилось");
    }
    public void ToMain()
    {
        StaticHolder.SaveData();
        SceneManager.LoadSceneAsync(0);
    }
}
