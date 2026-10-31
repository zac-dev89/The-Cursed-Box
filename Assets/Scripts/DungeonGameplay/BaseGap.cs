using Mirror;
using UnityEngine;

public class BaseGap : Interactable
{
    [Header("Gap References")]
    public Transform entryPoint1;
    public Transform entryPoint2;

    [Header("Gap Variables")]
    public string gapAnimation;
    [SyncVar] private bool inInteractionCooldown;
    [SyncVar] private bool isGapBoardedUp;



    public override CanInteractResult CanInteract(GameObject hitObject, NetworkIdentity playerRef)
    {
        CanInteractResult canInteract = new CanInteractResult();

        PlayerGameState PGS = playerRef.GetComponent<PlayerGameState>();

        if (!PGS.isMovingThroughGap && !isGapBoardedUp && !inInteractionCooldown)
        {
            canInteract.canInteract = true;
        }

        return canInteract;
    }

    public override void ServerInteract(GameObject hitObject, NetworkIdentity playerRef)
    {
        ServerMoveThroughGap(playerRef);
    }

    [Server]
    private void ServerMoveThroughGap(NetworkIdentity playerRef)
    {
        float distEntryPoint1 = Vector3.Distance(entryPoint1.position, playerRef.transform.position);
        float distEntryPoint2 = Vector3.Distance(entryPoint2.position, playerRef.transform.position);

        Transform gapEntry = null;

        if (distEntryPoint1 < distEntryPoint2)
        {
            gapEntry = entryPoint1;
        }
        else if (distEntryPoint1 >= distEntryPoint2)
        {
            gapEntry = entryPoint2;
        }

        inInteractionCooldown = true;
        playerRef.GetComponent<PlayerGameState>().ServerMoveThroughGap(gapEntry.forward, gapEntry.position, netIdentity);
    }

    [Server]
    public void ServerFinishedMovingThroughGap()
    {
        inInteractionCooldown = false;
    }
}
