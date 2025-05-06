using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using XR.Interaction.Toolkit.Samples;
using UnityEngine.InputSystem;

public class PlayerHealth : MonoBehaviour
{
    [Header("Настройки здоровья")]
    public float maxHealth;
    public float currentHealth;
    public InputActionProperty HealButton;
    public InputActionProperty SandewistanButton;
    public GameObject controller;
    float oldSpeed;
    DynamicMoveProvider speed = null;
    [Header("UI")]
    public Slider healthSlider; // Ссылка на UI-слайдер здоровья
    private void Awake()
    {
        HealButton.action.Enable();
        SandewistanButton.action.Enable();
        speed = controller.GetComponent<DynamicMoveProvider>();
        speed.moveSpeed = StaticHolder.PlayerBasicSpeed;
        oldSpeed = StaticHolder.PlayerBasicSpeed;
        maxHealth = maxHealth + StaticHolder.PlayerHPBuff;
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
        Debug.Log("Игрок получил урон: " + damage + ". Текущее здоровье: " + currentHealth);
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
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Body") && collision.collider.name.Contains("Hand"))
        {
            Debug.Log("Больно");
        }
    }
    void Die()
    {
        Debug.Log("Игрок погиб!");
        StaticHolder.DieStation = true;
        SceneManager.LoadScene(0);
        // Здесь можно вызывать экран Game Over и т.д.
    }
    void Update()
    {
        Debug.Log("Здоровья - " + currentHealth + " Из "+ maxHealth + " Кнопка отхила " + HealButton.action.ReadValue<float>());
        if (HealButton.action.ReadValue<float>() >= 0.7f && StaticHolder.PropitalHeal && currentHealth < maxHealth && !StaticHolder.PropitalHealActive)
        {
            StartCoroutine(Propital());
        }
        Debug.Log("Кнопка сандевистана " + SandewistanButton.action.ReadValue<float>());
        if (SandewistanButton.action.ReadValue<float>() >= 0.7 && StaticHolder.Sandevistan && !StaticHolder.SandevistanActive)
        {
            StartCoroutine(Sandewistan());
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
        Time.timeScale = StaticHolder.SandevistanTimeSlower;
        Time.fixedDeltaTime = 0.02f * Time.timeScale; // важно для корректной работы физики
        yield return new WaitForSecondsRealtime(StaticHolder.SandevistanTime); // ждём 
        // Возвращаем время к нормальному состоянию
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        Debug.Log("Замедление времени окончилось");
    }
}
