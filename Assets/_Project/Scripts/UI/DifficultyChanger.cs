using UnityEngine;

/// <summary>Переключатель сложности в главном меню. Имя класса и методы привязаны к кнопкам — не переименовывать.</summary>
public class DifficultyChanger : MonoBehaviour
{
    public GameObject EasyDiffGal; // галочка «Легко»
    public GameObject HardDiffGal; // галочка «Сложно»

    private void Start()
    {
        Apply(GameState.Difficulty);
    }

    public void EasyDif() => Apply(false);
    public void HardDif() => Apply(true);

    private void Apply(bool hard)
    {
        GameState.Difficulty = hard;
        if (EasyDiffGal != null) EasyDiffGal.SetActive(!hard);
        if (HardDiffGal != null) HardDiffGal.SetActive(hard);
    }
}
