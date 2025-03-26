using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class GrabParenter : MonoBehaviour
{
    public void OnGrab(SelectEnterEventArgs args)
    {
        Debug.Log("Взял");
        args.interactableObject.transform.position = args.interactorObject.transform.position;
    }
    public void OnUngrab(SelectExitEventArgs args)
    {
        Debug.Log("Отпустил");
        args.interactableObject.transform.position = args.interactableObject.transform.position;
    }
}
