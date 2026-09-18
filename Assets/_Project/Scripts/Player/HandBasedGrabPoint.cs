using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// XRGrabInteractable с отдельными точками хвата для левой и правой руки.
/// На время хвата переводит объект на слой Ignore Raycast, чтобы оружие не «стреляло» само в себя.
/// </summary>
public class HandBasedGrabPoint : XRGrabInteractable
{
    private const int IgnoreRaycastLayer = 2;

    [SerializeField] private Transform rightHandAttachTransform;
    [SerializeField] private Transform leftHandAttachTransform;

    private readonly Dictionary<GameObject, int> originalLayers = new();

    protected override void OnSelectEntering(SelectEnterEventArgs args)
    {
        // attachTransform нужно выбрать до базовой логики захвата
        if (args.interactorObject != null)
        {
            Transform interactor = args.interactorObject.transform;

            if (interactor.CompareTag("RightHand") && rightHandAttachTransform != null)
            {
                attachTransform = rightHandAttachTransform;
            }
            else if (interactor.CompareTag("LeftHand") && leftHandAttachTransform != null)
            {
                attachTransform = leftHandAttachTransform;
            }
        }

        base.OnSelectEntering(args);
        StoreAndSetLayer(gameObject, IgnoreRaycastLayer);
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        RestoreLayers();
        base.OnSelectExited(args);
    }

    private void StoreAndSetLayer(GameObject obj, int layer)
    {
        originalLayers.Clear();
        foreach (Transform t in obj.GetComponentsInChildren<Transform>(true))
        {
            originalLayers[t.gameObject] = t.gameObject.layer;
            t.gameObject.layer = layer;
        }
    }

    private void RestoreLayers()
    {
        foreach (var pair in originalLayers)
        {
            if (pair.Key != null) pair.Key.layer = pair.Value;
        }
        originalLayers.Clear();
    }
}
