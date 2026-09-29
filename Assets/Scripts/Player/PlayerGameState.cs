using UnityEngine;
using Mirror;
using Unity.VisualScripting;

public class PlayerGameState : NetworkBehaviour
{
    [Header("Main References")]
    public GameObject playerCam;
    public AIController AIController;
    public PlayerController playerController;
    public GameplayUIHandler gameplayUIHandler;


    [Header("Player Type States")]
    public bool AIControlled;
    public bool playerControlled;

    [Header("Game States")]
    public bool inTabletopGameplay;
    public bool inDungeonGameplay;

    [Header("Lobby")]
    [SyncVar(hook = nameof(OnReady))] public bool isReady;

    [Header("Debug Testing")]
    public Material botMaterial;
    public MeshRenderer botMeshRenderer;

    private void Start()
    {
        if (!isLocalPlayer)
        {
            InitializeOtherPlayer();
        }
    }

    public override void OnStartLocalPlayer()
    {
        InitializeYourPlayer();
    }

    #region Initializing Player
    public void InitializeYourPlayer()
    {
        gameplayUIHandler.EnableUI();

        if (GameManager.Instance.hasGameStarted)
        {
            playerCam.SetActive(true);
            gameplayUIHandler.ToggleTabletopPanel();
        }
    }

    public void InitializeOtherPlayer()
    {
        gameplayUIHandler.DisableUI();
        playerCam.SetActive(false);
    }


    #endregion


    #region Switch Controllers
    [Server]
    public void ServerSwitchToAIController()
    {
        AIControlled = true;
        playerControlled = false;

        playerController.playerControllerEnabled = false;
        AIController.AIControllerEnabled = true;

        RpcSwitchAIController();
    }
    [ClientRpc]
    private void RpcSwitchAIController()
    {
        botMeshRenderer.material = botMaterial;
    }


    [Server]
    public void ServerSwitchToPlayerController()
    {
        playerControlled = true;
        AIControlled = false;

        playerController.playerControllerEnabled = true;
        AIController.AIControllerEnabled = false;
    }
    #endregion

    [Command]
    public void CmdChangeReadyStatus()
    {
        isReady = !isReady;
    }

    private void OnReady(bool oldValue, bool newValue)
    {

    }
}
