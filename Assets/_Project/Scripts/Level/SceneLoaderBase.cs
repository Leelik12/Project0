using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Общая часть лифтов: триггер присутствия игрока и асинхронная загрузка сцены с прогресс-баром.</summary>
public abstract class SceneLoaderBase : MonoBehaviour
{
    public GameObject loadingUI; // экран загрузки (может быть null)
    public Slider progres;       // прогресс-бар (может быть null)

    protected bool playerInElevator;
    private bool isLoading;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) playerInElevator = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) playerInElevator = false;
    }

    private void Update()
    {
        if (isLoading || !playerInElevator || !CanEnter() || !CanDepart()) return;

        isLoading = true;
        OnBeforeLoad();
        StartCoroutine(LoadSceneAsync(TargetSceneIndex));
    }

    /// <summary>Индекс сцены в Build Settings, которую грузит этот лифт.</summary>
    protected abstract int TargetSceneIndex { get; }

    /// <summary>Разрешён ли отъезд с точки зрения условий магазина/этажа (проверяется каждый кадр, пока игрок в лифте).</summary>
    protected virtual bool CanEnter() => true;

    /// <summary>Можно ли ехать (условия этажа).</summary>
    protected abstract bool CanDepart();

    /// <summary>Подготовка GameState перед загрузкой.</summary>
    protected virtual void OnBeforeLoad() { }

    private IEnumerator LoadSceneAsync(int sceneIndex)
    {
        if (loadingUI != null) loadingUI.SetActive(true);

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneIndex);
        while (!operation.isDone)
        {
            if (progres != null) progres.value = Mathf.Clamp01(operation.progress / 0.9f);
            yield return null;
        }
    }
}
