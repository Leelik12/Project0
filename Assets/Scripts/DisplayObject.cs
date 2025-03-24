using UnityEngine;

public class DisplayObject : MonoBehaviour
{
    [SerializeField] GameObject _object;
    
    public void ShowObject()
    {
        _object.SetActive(true);
    }
    public void HideObject()
    {
        _object.SetActive(false);
    }
}
