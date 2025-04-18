using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManagerMain : MonoBehaviour
{
    [SerializeField] public GameObject Menu;
    [SerializeField] public GameObject Setting;
    [SerializeField] public GameObject Achievments;
    public void Begin()
    {
        SceneManager.LoadSceneAsync("TheFirstLevel");
        Debug.Log("Загрузка первого уровня");
    }
    public void Settings()
    {
        Menu.SetActive(false);
        Setting.SetActive(true);
    }
    public void Achiev()
    {
        Menu.SetActive(false);
        Achievments.SetActive(true);
    }
    public void Quit()
    {
        Application.Quit();
    }
    public void Back2Menu()
    {
        Achievments.SetActive(false);
        Setting.SetActive(false);
        Menu.SetActive(true);
    }
}
