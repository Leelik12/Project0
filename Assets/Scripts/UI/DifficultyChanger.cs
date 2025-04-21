using UnityEngine;

public class DifficultyChanger : MonoBehaviour
{
    public GameObject EasyDiffGal;
    public GameObject HardDiffGal;
    public void EasyDif()
    {
        EasyDiffGal.SetActive(true);
        HardDiffGal.SetActive(false);
        StaticHolder.Difficulty = false;
        Debug.Log("Установлена легкая сложность!");
    }
    public void HardDif()
    {
        EasyDiffGal.SetActive(false);
        HardDiffGal.SetActive(true);
        StaticHolder.Difficulty = true;
        Debug.Log("Установлена сложная сложность!");
    }
}
