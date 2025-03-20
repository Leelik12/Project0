using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class GrabParentner : MonoBehaviour
{
    public void OnGrab(SelectEnterEventArgs args)
    {
        args.interactableObject.transform.SetParent(args.interactorObject.transform);
    }
    public void OnUngrab(SelectExitEventArgs args) 
    {
        args.interactableObject.transform.SetParent(null);
    }
}
