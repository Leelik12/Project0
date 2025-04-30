using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Настройки здоровья")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("UI")]
    public Slider healthSlider; // Ссылка на UI-слайдер здоровья

    private void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();
    }

    public void PlayerTakeDamage(float damage)
    {
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
            Debug.Log("Пизда");
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

    }
    public float GetCurrentHealth() => currentHealth;

    public float GetHealthPercent() => currentHealth / maxHealth;
}
