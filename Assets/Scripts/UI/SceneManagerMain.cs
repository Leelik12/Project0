using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManagerMain : MonoBehaviour
{
    public void Begin()
    {
        SceneManager.LoadSceneAsync("TheFirstLevel");
        Debug.Log("Загрузка первого уровня");
    }
}
