using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;

/// <summary>Передаёт значения триггера и грипа контроллера в аниматор руки.</summary>
public class HandAnimation : MonoBehaviour
{
    [SerializeField] private XRInputValueReader<float> m_TriggerInput;
    [SerializeField] private XRInputValueReader<float> m_GripInput;
    [SerializeField] private Animator animator;

    private static readonly int TriggerHash = Animator.StringToHash("Trigger");
    private static readonly int GripHash = Animator.StringToHash("Grip");

    private void Update()
    {
        animator.SetFloat(TriggerHash, m_TriggerInput.ReadValue());
        animator.SetFloat(GripHash, m_GripInput.ReadValue());
    }
}
