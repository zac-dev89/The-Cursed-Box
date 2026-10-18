using Mirror;
using Unity.Collections.LowLevel.Unsafe;
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


[System.Serializable]
public struct PlayerTabletopTurn
{
    public bool inTurn;
    public PlayerTabletopState playerTabletopState;

    public bool inBloodPackMode;
    public bool inItemCardMode;

    public bool hasGambledForItem;
    public bool hasUsedBloodPack;
    public int insertedItems;
    public bool hasDrawnCursedCard;

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
    [SyncVar(hook = nameof(OnTabletopTurn))] public PlayerTabletopTurn playerTabletopTurn;

    [Header("Blood Sacrificing")]
    public int maxBloodSacrifices = 10;
    [SyncVar(hook = nameof(OnBloodLevelChanged))] public int currentBloodAmount;



    [Header("Debug Testing")]
    public Material botMaterial;
    public MeshRenderer botMeshRenderer;

    [Space]

    public bool debug_GivePlayerAllItems;

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

    public override void OnStartClient()
    {
        specialItemsList.OnAdd += OnItemAdded;
        specialItemsList.OnRemove += OnItemRemoved;
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
        currentBloodAmount = maxBloodSacrifices;


        if (debug_GivePlayerAllItems) DEBUG_ServerGivePlayerAllItems();



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
        PlayerTabletopTurn temp = new PlayerTabletopTurn();
        playerTabletopTurn = temp;
    }

    [Server]
    public void ServerStartPlayerTurn()
    {
        PlayerTabletopTurn temp = new PlayerTabletopTurn();
        temp.inTurn = true;
        temp.playerTabletopState = PlayerTabletopState.Itembox;

        playerTabletopTurn = temp;
    }

    #endregion


    #region Tabletop Gameplay
    [Header("Tabletop Gameplay")]
    public readonly SyncList<string> specialItemsList = new SyncList<string>();


    private void OnTabletopTurn(PlayerTabletopTurn oldValue, PlayerTabletopTurn newValue)
    {
        PlayerTabletopTurn tabletopTurn = newValue;

        if (playerType == PlayerType.PlayerControlled && isLocalPlayer)
        {
            playerController.OnPlayerTabletopTurn(tabletopTurn);
        }
    }

    #region Items

    [Server]
    private void DEBUG_ServerGivePlayerAllItems()
    {
        for (int i = 0; i < GameManager.Instance.cursedBoxStation.allItems.Count; i++)
        {
            int randomAmount = Random.Range(1, 3);

            for (int j = 0; j < randomAmount; j++)
            {
                ServerRetrievedItem(GameManager.Instance.cursedBoxStation.allItems[i]);
            }
        }
    }

    [Server]
    public void ServerRetrievedItem(string item)
    {   
        specialItemsList.Add(item);
    }
    [Server]
    public void ServerRemoveItem(string item)
    {
        specialItemsList.Remove(item);
    }


    // On changed functions
    void OnItemAdded(int index)
    {
        //Debug.Log($"Element added at index {index} {specialItemsList[index]}");
        if (playerType == PlayerType.PlayerControlled && isLocalPlayer)
        {
            gameplayUIHandler.UpdateItemsUI();
        }
    }
    void OnItemRemoved(int index, string oldValue)
    {
        //Debug.Log($"Element removed at index {index} {oldValue}");
        if (playerType == PlayerType.PlayerControlled && isLocalPlayer)
        {
            gameplayUIHandler.UpdateItemsUI();
        }
    }


    // Using and Finding Items
    public bool InventoryContainsItem(string itemName)
    {
        foreach (string curr_item in specialItemsList)
        {
            if (curr_item == itemName) return true;
        }
        return false;
    }

    public int GetQuantityOfItem(string itemName)
    {
        int quantity = 0;

        foreach (string curr_item in specialItemsList)
        {
            if (curr_item == itemName) quantity++;
        }

        return quantity;
    }

    #endregion

    #region Item box

    private void OnBloodLevelChanged(int oldValue, int newValue)
    {
        if (playerType == PlayerType.PlayerControlled && isLocalPlayer)
        {
            gameplayUIHandler.UpdateBloodLevel(newValue);
        }
    }

    public void TryAddBloodPack()
    {
        Debug.Log("Try Add Blood Pack");

        if (!InventoryContainsItem("BloodPack")) return;
        if (playerTabletopTurn.hasUsedBloodPack) return;

        Debug.Log("Can Use Blood Pack");

        CmdTryAddBloodPack();
    }

    [Command]
    private void CmdTryAddBloodPack()
    {
        if (!InventoryContainsItem("BloodPack")) return;
        if (playerTabletopTurn.hasUsedBloodPack) return;

        GameManager.Instance.cursedBoxStation.ServerAddBloodPack(this.netIdentity);
    }


    [Command]
    public void CmdFinishedWithItemBox()
    {
        PlayerTabletopTurn temp = new PlayerTabletopTurn();
        temp.inTurn = true;
        temp.playerTabletopState = PlayerTabletopState.Cursedbox;

        playerTabletopTurn = temp;
    }

    #endregion


    // Cursed Box

    #endregion

    #region Dungeon Gameplay



    #endregion
}
