using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    public bool inItemBoxInsertMode;
    public bool inCursedBoxInsertMode;

    public bool hasGambledForItem;
    public bool hasUsedItemBoxItem;
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
        cursedBoxItemsList.OnAdd += OnCursedBoxItemAdded;
        cursedBoxItemsList.OnRemove += OnCursedBoxItemRemoved;

        itemBoxItemsList.OnAdd += OnItemBoxItemAdded;
        itemBoxItemsList.OnRemove += OnItemBoxItemRemoved;
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
    public readonly SyncList<string> itemBoxItemsList = new SyncList<string>();
    public readonly SyncList<string> cursedBoxItemsList = new SyncList<string>();


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
            int randomAmount = Random.Range(2, 5);

            for (int j = 0; j < randomAmount; j++)
            {
                ServerRetrievedItem(GameManager.Instance.cursedBoxStation.allItems[i]);
            }
        }
    }

    [Server]
    public void ServerRetrievedItem(string item)
    {
        ItemInformation itemInfo = ItemData.Instance.GetItemInfoFromName(item);

        if (itemInfo.isCursedBoxItem)
        {
            ServerRetrievedCursedBoxItem(item);
        }
        else if (itemInfo.isItemBoxItem)
        {
            ServerRetrievedItemBoxItem(item);
        }
    }
    [Server]
    public void ServerRemoveItem(string item)
    {
        ItemInformation itemInfo = ItemData.Instance.GetItemInfoFromName(item);

        if (itemInfo.isCursedBoxItem)
        {
            ServerRemoveCursedBoxItem(item);
        }
        else if (itemInfo.isItemBoxItem)
        {
            ServerRemoveItemBoxItem(item);
        }
    }

    // Cursed Box Items
    [Server]
    public void ServerRetrievedCursedBoxItem(string item)
    {
        cursedBoxItemsList.Add(item);
    }
    [Server]
    public void ServerRemoveCursedBoxItem(string item)
    {
        cursedBoxItemsList.Remove(item);
    }

    void OnCursedBoxItemAdded(int index)
    {
        //Debug.Log($"Cursed Box Item Added {index} {cursedBoxItemsList[index]}");
        if (playerType == PlayerType.PlayerControlled && isLocalPlayer)
        {
            gameplayUIHandler.UpdateCursedBoxItemsUI();
        }
    }
    void OnCursedBoxItemRemoved(int index, string oldValue)
    {
        //Debug.Log($"Element removed at index {index} {oldValue}");
        if (playerType == PlayerType.PlayerControlled && isLocalPlayer)
        {
            gameplayUIHandler.UpdateCursedBoxItemsUI();
        }
    }



    // Item Box Items
    [Server]
    public void ServerRetrievedItemBoxItem(string item)
    {
        itemBoxItemsList.Add(item);
    }
    [Server]
    public void ServerRemoveItemBoxItem(string item)
    {
        itemBoxItemsList.Remove(item);
    }

    [Server]
    void OnItemBoxItemAdded(int index)
    {
        //Debug.Log($"Item Box Item Added {index} {itemBoxItemsList[index]}");
        if (playerType == PlayerType.PlayerControlled && isLocalPlayer)
        {
            gameplayUIHandler.UpdateItemBoxItemsUI();
        }
    }
    [Server]
    void OnItemBoxItemRemoved(int index, string oldValue)
    {
        if (playerType == PlayerType.PlayerControlled && isLocalPlayer)
        {
            gameplayUIHandler.UpdateItemBoxItemsUI();
        }
    }


    // Using and Finding Items
    public bool InventoryContainsItem(string itemName, List<string> itemsInventory)
    {
        foreach (string curr_item in itemsInventory)
        {
            if (curr_item == itemName) return true;
        }
        return false;
    }

    public int GetQuantityOfItem(string itemName, List<string> itemsInventory)
    {
        int quantity = 0;

        foreach (string curr_item in itemsInventory)
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

        if (!InventoryContainsItem("BloodPack", itemBoxItemsList.ToList<string>())) return;
        if (playerTabletopTurn.hasUsedItemBoxItem) return;

        Debug.Log("Can Use Blood Pack");

        CmdTryAddBloodPack();
    }

    [Command]
    private void CmdTryAddBloodPack()
    {
        if (!InventoryContainsItem("BloodPack", itemBoxItemsList.ToList<string>())) return;
        if (playerTabletopTurn.hasUsedItemBoxItem) return;

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
    public void TryUseItem(string itemName)
    {
        if (GameManager.Instance.cursedBoxStation.CanInsertItem(netIdentity, itemName))
        {
            CmdTryUseItem(itemName);
        }
    }

    [Command]
    private void CmdTryUseItem(string itemName)
    {
        if (GameManager.Instance.cursedBoxStation.CanInsertItem(netIdentity, itemName))
        {
            GameManager.Instance.cursedBoxStation.ServerInsertItem(netIdentity, itemName);
        }
    }


    #endregion

    #region Dungeon Gameplay



    #endregion
}
