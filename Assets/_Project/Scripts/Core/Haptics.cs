using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>Вибрация контроллеров: в руке, которая держит предмет, или в обеих сразу.</summary>
public static class Haptics
{
    /// <summary>Вибрирует контроллер(ы), которые сейчас держат этот предмет.</summary>
    public static void Pulse(IXRSelectInteractable interactable, float amplitude, float duration)
    {
        if (interactable == null || !interactable.isSelected) return;

        foreach (IXRSelectInteractor interactor in interactable.interactorsSelecting)
        {
            if (interactor is XRBaseInputInteractor input) input.SendHapticImpulse(amplitude, duration);
        }
    }

    /// <summary>Вибрирует оба контроллера (получение урона и т.п.).</summary>
    public static void PulseBoth(float amplitude, float duration)
    {
        foreach (HapticImpulsePlayer player in Object.FindObjectsByType<HapticImpulsePlayer>(FindObjectsSortMode.None))
        {
            player.SendHapticImpulse(amplitude, duration);
        }
    }
}
