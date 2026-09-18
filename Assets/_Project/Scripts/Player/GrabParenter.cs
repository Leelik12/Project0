using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Делает взятый объект дочерним к руке и сохраняет его масштаб.
/// Методы OnGrab/OnUngrab привязаны к событиям XRGrabInteractable в префабах — не переименовывать.
/// </summary>
public class GrabParenter : MonoBehaviour
{
    private Vector3 originalScale;

    public void OnGrab(SelectEnterEventArgs args)
    {
        Transform item = args.interactableObject.transform;
        originalScale = item.localScale;
        item.SetParent(args.interactorObject.transform);
        item.localScale = originalScale;
    }

    public void OnUngrab(SelectExitEventArgs args)
    {
        Transform item = args.interactableObject.transform;
        item.SetParent(null);
        item.localScale = originalScale;
    }
}
