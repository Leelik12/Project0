using UnityEngine;
using UnityEngine.SceneManagement;

public class ElevatorController : MonoBehaviour
{
    [Header("Настройки лифта")]
    public float waitTime = 3f;               // Время ожидания в лифте перед загрузкой сцены
    private bool playerInElevator = false;
    private float timer = 0f;
    public int CurrentBuildScene; //Текущая сцена по билду

    private void Start()
    {
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
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
        if (playerInElevator && StaticHolder.levelCheksComplete)
        {
            timer += Time.deltaTime;

            if (timer >= waitTime)
            {
                LoadNextScene();
            }
        }
    }

    void LoadNextScene()
    {
        Debug.Log("Загрузка новой сцены");
        StaticHolder.CurrentLevel = CurrentBuildScene;
        StaticHolder.levelCheksComplete = false;
        SceneManager.LoadScene(1);


    }
}
