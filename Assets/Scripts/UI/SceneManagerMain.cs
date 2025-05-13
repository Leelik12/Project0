using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManagerMain : MonoBehaviour
{
    [SerializeField] public GameObject Main;
    [SerializeField] public GameObject Setting;
    [SerializeField] public GameObject Achievm;
    [SerializeField] public GameObject End;
    public void Begin()
    {
        SceneManager.LoadSceneAsync("TheFirstLevel");
        Debug.Log("Загрузка первого уровня");
    }
    public void Setti()
    {
        Main.SetActive(false);
        Setting.SetActive(true);
        Debug.Log("Переход в настройки");
    }
    public void Achie()
    {
        Main.SetActive(false);
        Achievm.SetActive(true);
        Debug.Log("Переход в достижения");
    }
    public void Bach2Main()
    {
        Main.SetActive(true);
        Achievm.SetActive(false);
        Setting.SetActive(false);
        Debug.Log("Переход в меню");
    }
    public void Exi()
    {
        Debug.Log("Выход");
        Application.Quit();
    }
    public void EndClose()
    {
        Main.SetActive(true);
        End.SetActive(false);
    }
    private void Update()
    {
        if (StaticHolder.GameOver)
        {
            StaticHolder.GameOver = false;
            Main.SetActive(false);
            Achievm.SetActive(false);
            Setting.SetActive(false);
            End.SetActive(true);
        }
    }
}
