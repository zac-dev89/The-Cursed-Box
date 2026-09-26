using UnityEngine;
using Mirror;

public class PlayerGameState : NetworkBehaviour
{
    [Header("Camera References")]
    public GameObject playerCam;

    [Header("Controller References")]
    public AIController AIController;
    public PlayerController playerController;

    [Header("Player Type States")]
    private bool AIControlled;
    public bool playerControlled;

    [Header("Game States")]
    public bool inTabletopGameplay;
    public bool inDungeonGameplay;


    #region Initializing Player
    public void UpdateCameraSetUp()
    {
        if (AIControlled)
        {
            playerCam.SetActive(false);
            return;
        }

        if (GameManager.Instance.hasGameStarted)
        {
            playerCam.SetActive(true);
            GameManager.Instance.lobbyCam.SetActive(false);
        }
        else
        {
            playerCam.SetActive(false);
            GameManager.Instance.lobbyCam.SetActive(true);
        }
    }


    #endregion


    [Server]
    public void ServerSwitchToAIController()
    {
        AIControlled = true;
        playerControlled = false;

        playerController.playerControllerEnabled = false;
        AIController.AIControllerEnabled = true;
    }

    [Server]
    public void ServerSwitchToPlayerController()
    {
        playerControlled = true;
        AIControlled = false;

        playerController.playerControllerEnabled = true;
        AIController.AIControllerEnabled = false;
    }
}
