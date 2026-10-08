using Mirror;
using UnityEngine;


public enum PlayerType
{
    NONE,
    PlayerControlled,
    AIControlled
}
public enum PlayerState
{
    NONE,
    Tabletop,
    DungeonHunter,
    DungeonVictim
}
public enum PlayerTabletopState
{
    NONE,
    Itembox,
    Cursedbox
}


public class PlayerGameState : NetworkBehaviour
{
    [Header("Main References")]
    public GameObject playerCam;
    public AIController AIController;
    public PlayerController playerController;
    public GameplayUIHandler gameplayUIHandler;


    [Header("Player Type States")]
    [SyncVar] public PlayerType playerType;

    [Space]

    [SyncVar] public bool playerStartedInGame;


    [Header("Game States")]
    public PlayerState playerState;
    [SyncVar] public bool playerInTurn;


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
        playerCam.SetActive(true);

        // Local enabling
        playerType = PlayerType.PlayerControlled;

        playerController.playerControllerEnabled = true;
        AIController.AIControllerEnabled = false;
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
        playerType = PlayerType.AIControlled;

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
        playerType = PlayerType.PlayerControlled;

        playerController.playerControllerEnabled = true;
        AIController.AIControllerEnabled = false;
    }

    #endregion

    #region PlayerStartedGame
    [Server]
    public void ServerOnPlayerGameStarted()
    {
        playerStartedInGame = true;

        playerState = PlayerState.Tabletop;

        RpcOnPlayerGameStarted();
    }
    [ClientRpc]
    private void RpcOnPlayerGameStarted()
    {
        playerStartedInGame = true;
        playerState = PlayerState.Tabletop;

        if (isLocalPlayer && playerType == PlayerType.PlayerControlled)
        {
            playerController.OnPlayerControllerStarted();
        }
    }
    #endregion

    #region Player Turn Management
    [Server]
    public void ServerEndPlayerTurn()
    {
        playerInTurn = false;
        playerTabletopState = PlayerTabletopState.NONE;
    }

    [Server]
    public void ServerStartPlayerTurn()
    {
        playerInTurn = true;
        playerTabletopState = PlayerTabletopState.Itembox;
    }

    #endregion



    #region Tabletop Gameplay
    [Header("Player Tabletop Gameplay")]
    [SyncVar(hook = nameof(OnTabletopStateChanged))] public PlayerTabletopState playerTabletopState;

    [SyncVar] public int usedSpecialItemsCount;


    [Command]
    public void CmdFinishedWithItemBox()
    {
        playerTabletopState = PlayerTabletopState.Cursedbox;
    }


    private void OnTabletopStateChanged(PlayerTabletopState oldValue, PlayerTabletopState newValue)
    {
        Transform interactableFocusPT = null;
        switch (newValue)
        {
            case PlayerTabletopState.Itembox:
                interactableFocusPT = GameManager.Instance.cursedBoxStation.itemBox.transform;
                break;
            case PlayerTabletopState.Cursedbox:
                interactableFocusPT = GameManager.Instance.cursedBoxStation.cursedBox.transform;
                break;
            default:
                interactableFocusPT = null;
                break;
        }

        if (playerType == PlayerType.PlayerControlled)
        {
            if (interactableFocusPT != null)
            {
                playerController.FocusOnInteractable(interactableFocusPT, 50f, .2f);
            }
            else
            {
                playerController.ExitFocusOnInteractable();
            }
        }


    }

    // Special item gambling


    // Cursed Box

    #endregion

    #region Dungeon Gameplay



    #endregion
}
