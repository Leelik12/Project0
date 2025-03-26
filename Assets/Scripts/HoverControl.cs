using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class AtachTransform : MonoBehaviour
{
    [SerializeField] Transform m_RightAttachTransform;
    [SerializeField] Transform m_LeftAttachTransform;
    public XRGrabInteractable XRGrabAttach;
    public void Attachhand(HoverEnterEventArgs args)
    {
        Transform attTransform;
        if (args.interactorObject.transform.CompareTag("RightHand"))
        {
            attTransform = m_RightAttachTransform;
        }
        else
        {
            attTransform = m_LeftAttachTransform;
        }
        XRGrabAttach.attachTransform = attTransform;
    }
}