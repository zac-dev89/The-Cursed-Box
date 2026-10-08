using UnityEngine;
using Mirror;


public struct CanInteractResult
{
    public bool canInteract;
    public string interactType;
}
public class Interactable : NetworkBehaviour
{

    virtual public CanInteractResult CanInteract(GameObject hitObject, NetworkIdentity playerRef)
    {
        return new CanInteractResult();
    }

    [Server]
    virtual public void ServerInteract(GameObject hitObject, NetworkIdentity playerRef)
    {
        Debug.Log("Interact");
    }
}
