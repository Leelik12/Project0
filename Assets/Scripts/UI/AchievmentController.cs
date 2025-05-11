using UnityEngine;

public class AchievmentController : MonoBehaviour
{
    public GameObject s100;
    public GameObject s500;
    public GameObject h100;
    public GameObject h500;
    public GameObject d1000;
    public GameObject d5000;
    public GameObject Cyborg;
    public GameObject FirstDeath;
    void Update()
    {
        if (StaticHolder.countShots >= 100)
        {
            s100.SetActive(true);
        }
        if (StaticHolder.countShots >= 500)
        {
            s500.SetActive(true);
        }
        if (StaticHolder.countHits >= 100)
        {
            h100.SetActive(true);
        }
        if (StaticHolder.countHits >= 500)
        {
            h500.SetActive(true);
        }
        if (StaticHolder.Damage >= 1000)
        {
            d1000.SetActive(true);
        }
        if (StaticHolder.Damage >= 5000)
        {
            d5000.SetActive(true);
        }
        if (StaticHolder.Ciborg)
        {
            Cyborg.SetActive(true);
        }
        if (StaticHolder.DiedinCyberpunk)
        {
            FirstDeath.SetActive(true);
        }
    }
}
