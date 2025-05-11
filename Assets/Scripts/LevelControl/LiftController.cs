using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEditor;

public class ElevatorController : MonoBehaviour
{
    [Header("Настройки лифта")]
    private bool playerInElevator = false;
    public int CurrentBuildScene; //Текущая сцена по билду
    public GameObject loadingUI;
    public Slider progres;
    bool IsLoading = false;
    bool IsLoaded2 = false;
    AsyncOperation operation;
    private void Start()
    {
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInElevator = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInElevator = false;
        }
    }

    private void Update()
    {
        Debug.Log("Текущий уровень по билду - " + StaticHolder.CurrentLevel);
        if (playerInElevator && StaticHolder.levelCheksComplete && !IsLoading)
        {
            IsLoading = true;
            LoadNextScene();
        }
    }

    void LoadNextScene()
    {
        Debug.Log("Асинхронная загрузка сцены...");
        StaticHolder.CurrentLevel = CurrentBuildScene;
        StaticHolder.levelCheksComplete = false;
        StaticHolder.PropitalHealActive = false;
        StaticHolder.SandevistanActive = false;
        //SceneManager.LoadScene(1);
        StartCoroutine(LoadSceneAsync(1)); // подставь нужный индекс
    }
    IEnumerator LoadSceneAsync(int sceneIndex)
    {
        if (!IsLoaded2)
        {
            //loadingUI.SetActive(true);
            operation = SceneManager.LoadSceneAsync(sceneIndex);
            IsLoaded2 = true;
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
