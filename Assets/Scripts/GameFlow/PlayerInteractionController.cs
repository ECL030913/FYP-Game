using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns the player's E key. Nearby objects register themselves and only the
/// nearest valid object receives the interaction, eliminating portal/shop input
/// overlap even if trigger volumes are accidentally moved together later.
/// </summary>
public class PlayerInteractionController : MonoBehaviour
{
    private readonly List<IPlayerInteractable> nearby = new List<IPlayerInteractable>();
    private IPlayerInteractable focused;

    public void Register(IPlayerInteractable interactable)
    {
        if (IsAlive(interactable) && !nearby.Contains(interactable))
        {
            nearby.Add(interactable);
        }
    }

    public void Unregister(IPlayerInteractable interactable)
    {
        nearby.Remove(interactable);
        if (ReferenceEquals(focused, interactable))
        {
            focused.SetInteractionFocus(false);
            focused = null;
        }
    }

    private void Update()
    {
        IPlayerInteractable nearest = FindNearestAvailable();
        if (!ReferenceEquals(nearest, focused))
        {
            focused?.SetInteractionFocus(false);
            focused = nearest;
            focused?.SetInteractionFocus(true);
        }

        if (focused != null
            && focused.IsInteractionAvailable
            && Time.timeScale > 0f
            && Input.GetKeyDown(KeyCode.E))
        {
            focused.Interact(this);
        }
    }

    private void OnDisable()
    {
        focused?.SetInteractionFocus(false);
        focused = null;
        nearby.Clear();
    }

    private IPlayerInteractable FindNearestAvailable()
    {
        IPlayerInteractable nearest = null;
        float nearestSqrDistance = float.PositiveInfinity;

        for (int i = nearby.Count - 1; i >= 0; i--)
        {
            IPlayerInteractable candidate = nearby[i];
            if (!IsAlive(candidate))
            {
                nearby.RemoveAt(i);
                continue;
            }

            if (!candidate.IsInteractionAvailable || candidate.InteractionTransform == null)
            {
                continue;
            }

            float sqrDistance = (candidate.InteractionTransform.position - transform.position).sqrMagnitude;
            if (sqrDistance < nearestSqrDistance)
            {
                nearestSqrDistance = sqrDistance;
                nearest = candidate;
            }
        }

        return nearest;
    }

    private static bool IsAlive(IPlayerInteractable interactable)
    {
        return interactable is Object unityObject && unityObject != null;
    }
}
