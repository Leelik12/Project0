using UnityEngine;
using UnityEngine.SceneManagement;

public class UpdateLiftController : MonoBehaviour
{
    [Header("Настройки лифта")]
    public float waitTime = 3f;               // Время ожидания в лифте перед загрузкой сцены
    private bool playerInElevator = false;
    private float timer = 0f;
    int l = StaticHolder.CurrentLevel + 1;


    private void Start()
    {
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && StaticHolder.UpdateLevelEnd)
        {
            playerInElevator = true;
            timer = 0f;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInElevator = false;
            timer = 0f;
        }
    }

    private void Update()
    {
        Debug.Log("Текущий уровень по билду - " + StaticHolder.CurrentLevel);
        //Debug.Log(l);
        if (playerInElevator)
        {
            timer += Time.deltaTime;

            if (timer >= waitTime)
            {
                
                
                LoadNextLevel();
            }
        }
    }

    void LoadNextLevel()
    {
        Debug.Log("Загрузка новой сцены");
        SceneManager.LoadScene(l);
        StaticHolder.CurrentLevel = l;
        StaticHolder.levelCheksComplete = false;
        StaticHolder.UpdateLevelEnd = false;
        StaticHolder.ItemPickedUp = false;
        StaticHolder.PropitalHealActive = true;
        StaticHolder.SandevistanActive = true;
    }
}
