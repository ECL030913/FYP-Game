using UnityEngine;

/// <summary>
/// A single interaction contract shared by portals and shop displays. Input is
/// read only by PlayerInteractionController so one E press can trigger at most
/// one nearby object.
/// </summary>
public interface IPlayerInteractable
{
    Transform InteractionTransform { get; }
    bool IsInteractionAvailable { get; }
    void SetInteractionFocus(bool focused);
    void Interact(PlayerInteractionController controller);
}
