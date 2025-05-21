using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class HandBasedGrabPoint : XRGrabInteractable
{
    [SerializeField] private Transform rightHandAttachTransform;
    [SerializeField] private Transform leftHandAttachTransform;

    protected override void OnSelectEntering(SelectEnterEventArgs args)
    {
        // Выбор attachTransform до базовой логики захвата
        if (args.interactorObject != null)
        {
            var interactorTransform = args.interactorObject.transform;

            if (interactorTransform.CompareTag("RightHand") && rightHandAttachTransform != null)
            {
                attachTransform = rightHandAttachTransform;
            }
            else if (interactorTransform.CompareTag("LeftHand") && leftHandAttachTransform != null)
            {
                attachTransform = leftHandAttachTransform;
            }
        }

        // Важно: вызвать базовую реализацию, чтобы объект действительно захватился
        base.OnSelectEntering(args);
        SetLayerRecursively(gameObject, 2);
    }
    public static void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj == null) return;

        obj.layer = layer;

        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        SetLayerRecursively(gameObject, 0);
        base.OnSelectExited(args);
    }
}

