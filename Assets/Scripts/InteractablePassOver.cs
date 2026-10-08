using UnityEngine;

public class InteractablePassOver : MonoBehaviour
{
    public Interactable interactable;

    public Interactable GetInteractableTarget()
    {
        return interactable;
    }
}
