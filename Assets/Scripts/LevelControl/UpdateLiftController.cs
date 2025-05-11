using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEngine.Rendering.HDROutputUtils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
public class UpdateLiftController : MonoBehaviour
{
    [Header("Настройки лифта")]
    public float waitTime = 3f;               // Время ожидания в лифте перед загрузкой сцены
    private bool playerInElevator = false;
    private float timer = 0f;
    int l = StaticHolder.CurrentLevel + 1;
    public GameObject loadingUI;
    public Slider progres;
    bool IsLoading = false;
    bool IsLoaded = false;
    AsyncOperation operation;

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
        if (playerInElevator && !IsLoading)
        {
            IsLoading = true;
            LoadNextLevel();
        }
    }

    void LoadNextLevel()
    {
        Debug.Log("Асинхронная загрузка сцены...");
        //SceneManager.LoadSceneAsync(l);
        StaticHolder.CurrentLevel = l;
        StaticHolder.levelCheksComplete = false;
        StaticHolder.UpdateLevelEnd = false;
        StaticHolder.ItemPickedUp = false;
        StaticHolder.PropitalHealActive = false;
        StaticHolder.SandevistanActive = false;
        StartCoroutine(LoadSceneAsync(l));
    }
    IEnumerator LoadSceneAsync(int sceneIndex)
    {
        Debug.Log("загрузка идет");
        if (!IsLoaded)
        {
            //loadingUI.SetActive(true);
            operation = SceneManager.LoadSceneAsync(sceneIndex);
            Debug.Log("Пошла загрузка");
            IsLoaded = true;
        }

        while (!operation.isDone)
        {
            // Здесь можно обновлять прогресс бар, если он есть:
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            progres.value = progress;
            yield return null;
        }
    }
}
