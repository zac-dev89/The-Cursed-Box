using UnityEngine;
using Mirror;

public class PlayerGameState : NetworkBehaviour
{
    [Header("Main References")]
    public AIController AIController;
    public PlayerController playerController;


    [Header("Game States")]
    public bool inTabletopGameplay;
    public bool inDungeonGameplay;


    [Server]
    public void ServerSwitchToAIController()
    {
        playerController.playerControllerEnabled = false;
        AIController.AIControllerEnabled = true;
    }

    [Server]
    public void ServerSwitchToPlayerController()
    {
        playerController.playerControllerEnabled = true;
        AIController.AIControllerEnabled = false;
    }
}
