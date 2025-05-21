using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;


public class HandBasedGrabPoint : XRGrabInteractable
{
    [SerializeField] private Transform rightHandAttachTransform;
    [SerializeField] private Transform leftHandAttachTransform;

    private FixedJoint joint;
    protected override void Awake()
    {
        base.Awake();
        selectMode = InteractableSelectMode.Multiple;
        movementType = MovementType.VelocityTracking;
        trackPosition = true;
        trackRotation = true;


    }
    protected override void OnSelectEntering(SelectEnterEventArgs args)
    {
        // Если уже кто-то держит — не давать захватить ещё раз
        if (isSelected && !IsInteractorAlreadySelected(args.interactorObject))
        {
            // Блокируем второй захват
            return;
        }

        var interactorRb = (args.interactorObject as MonoBehaviour)?.GetComponent<Rigidbody>();
        var thisRb = GetComponent<Rigidbody>();
        if (interactorRb != null && thisRb != null)
        {
            thisRb.isKinematic = false;
            joint = gameObject.AddComponent<FixedJoint>();
            joint.connectedBody = interactorRb;
            joint.breakForce = Mathf.Infinity;
            joint.breakTorque = Mathf.Infinity;
        }

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
    }

    protected override void OnSelectExiting(SelectExitEventArgs args)
    {
        base.OnSelectExiting(args);

        if (joint != null)
        {
            Destroy(joint);
            joint = null;
        }
    }
    private bool IsInteractorAlreadySelected(IXRSelectInteractor interactor)
    {
        foreach (var i in interactorsSelecting)
        {
            if (i == interactor)
                return true;
        }
        return false;
    }
}

