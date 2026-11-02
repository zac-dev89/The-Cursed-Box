using Mirror;
using System.Collections;
using System.Collections.Generic;
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

    public int cursedBoxDrawCounter;
    public bool finishedCursedBoxDraw;

    public bool inInteractionCooldown;
    public bool peekingAtBox;
}

[System.Serializable]
public struct PlayerDungeonHunter
{
    public NetworkIdentity dungeonHunter;
    public NetworkIdentity dungeonHunterMonitor;

    public Vector3 hunterSpawnPOS;
    public Quaternion hunterSpawnROT;
}


public class PlayerGameState : NetworkBehaviour
{
    [Header("Main References")]
    public Transform FPSCamera;
    public GameObject playerCam;
    public AIController AIController;
    public PlayerController playerController;
    public GameplayUIHandler gameplayUIHandler;
    public Animator entirePlayerAnimator;
    public Light flashLight;


    [Header("Player Type States")]
    [SyncVar] public PlayerType playerType;

    [Space]

    [SyncVar] public bool playerStartedInGame;


    [Header("Game States")]
    [SyncVar(hook = nameof(OnTableTopSeatInitialized))] public int tabletopSeatIndex = -1;
    [SyncVar] public PlayerState playerState;
    [SyncVar] public bool isAlive;



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

        flashLight.enabled = false;

        // Local enabling
        playerType = PlayerType.PlayerControlled;

        playerController.playerControllerEnabled = true;
        AIController.AIControllerEnabled = false;
    }

    public void InitializeOtherPlayer()
    {
        gameplayUIHandler.DisableUI();
        playerCam.SetActive(false);

        flashLight.enabled = false;
    }

    void OnTableTopSeatInitialized(int oldValue, int newValue)
    {
        Transform seat = TabletopSpawning.Instance.GetSeatFromIndex(newValue);


        Debug.Log($"Seat Index is {newValue}");
        transform.position = seat.position;
        transform.rotation = seat.rotation;
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

        isAlive = true;
        currentTabletopLives = tabletopLives;

        if (debug_GivePlayerAllItems) DEBUG_ServerGivePlayerAllItems();

        // PLAYER ONLY
        RpcOnPlayerGameStarted();

        // AI ONLY - THIS IS CALLED EVEN THOUGH A PLAYER IS CONTROLLING IT.
        // It is just an edge case to make sure all controller variables are set up if a player leaves the game
        AIController.ServerInitializeAIController();
    }
    [ClientRpc]
    private void RpcOnPlayerGameStarted()
    {
        playerStartedInGame = true;
        playerState = PlayerState.Tabletop;

        flashLight.enabled = false;

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

        if (GameManager.Instance.cursedBoxStation.playerMustForceDraw || GameManager.Instance.cursedBoxStation.everyoneMustForceDraw) 
        {
            // Make animation transition later
            temp.playerTabletopState = PlayerTabletopState.Cursedbox;
        }
        else
        {
            temp.playerTabletopState = PlayerTabletopState.Itembox;
        }

        playerTabletopTurn = temp;
    }

    #endregion


    #region Tabletop Gameplay
    [Header("Tabletop Gameplay")]
    [SyncVar(hook = nameof(OnTabletopTurn))] public PlayerTabletopTurn playerTabletopTurn;

    public int tabletopLives = 4;
    [SyncVar(hook = nameof(OnTabletopLivesChanged))] public int currentTabletopLives;


    public int maxBloodSacrifices = 10;
    [SyncVar(hook = nameof(OnBloodLevelChanged))] public int currentBloodAmount;


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


    private void OnTabletopLivesChanged(int oldValue, int newValue)
    {
        Debug.Log($"{newValue} Lives Left.");
    }
    [Server]
    public void ServerRetrieveDeathCard(int lives)
    {
        currentTabletopLives = Mathf.Clamp(currentTabletopLives - lives, 0, tabletopLives);

        if (currentTabletopLives == 0)
        {
            ServerTabletopDeath();
        }
        else
        {
            GameManager.Instance.ServerDeployVictimIntoDungeon(netIdentity);
        }
    }
    [Server]
    private void ServerTabletopDeath()
    {
        isAlive = false;
        RpcTabletopDeath();
    }
    [ClientRpc]
    private void RpcTabletopDeath()
    {
        Debug.Log("Player Death!");
    }

   

    #region Items

    [Server]
    private void DEBUG_ServerGivePlayerAllItems()
    {
        for (int i = 0; i < GameManager.Instance.cursedBoxStation.allItems.Count; i++)
        {
            int randomAmount = Random.Range(2, 4);

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

    void OnItemBoxItemAdded(int index)
    {
        //Debug.Log($"Item Box Item Added {index} {itemBoxItemsList[index]}");
        if (playerType == PlayerType.PlayerControlled && isLocalPlayer)
        {
            gameplayUIHandler.UpdateItemBoxItemsUI();
        }
    }
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

    #region Item Box

    private void OnBloodLevelChanged(int oldValue, int newValue)
    {
        if (playerType == PlayerType.PlayerControlled && isLocalPlayer)
        {
            gameplayUIHandler.UpdateBloodLevel(newValue);
        }
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

    #region Cursed Box
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

    #endregion



    #region Dungeon Gameplay


    #region Dungeon Victim Gameplay

    [Header("Dungeon Victim Gameplay")]
    [SyncVar] public bool isMovingThroughGap;
    float initialGapTransitionTime = 0.25f;
    public float wallGapMoveTime;
    public float crawlSpaceGapMoveTime;
    public bool isDungeonVictimReady;

    [Space]
    private NetworkIdentity SERVER_CurrentGap; 

    [Server]
    public void ServerSpawnVictimInDungeon(Vector3 spawnPosition, Quaternion spawnRotation)
    {
        playerState = PlayerState.DungeonVictim;
        RpcSpawnVictimInDungeon(spawnPosition, spawnRotation);
    }
    [ClientRpc]
    private void RpcSpawnVictimInDungeon(Vector3 spawnPosition, Quaternion spawnRotation)
    {
        // Dungeon Victim Ready

        transform.position = spawnPosition;
        transform.rotation = spawnRotation;

        playerState = PlayerState.DungeonVictim;
        flashLight.enabled = true;
        
        if (playerType == PlayerType.PlayerControlled && isLocalPlayer)
        {
            playerController.OnDungeonVictimStarted();
            CmdReportVictimReadyInDungeon();
        }
        else if (playerType == PlayerType.AIControlled && NetworkServer.active)
        {
            ServerReportVictimReadyInDungeon();
        }
    }
    [Command]
    private void CmdReportVictimReadyInDungeon()
    {
        isDungeonVictimReady = true;
        GameManager.Instance.ServerDeployHuntersIntoDungeon();
    }
    [Server]
    private void ServerReportVictimReadyInDungeon()
    {
        isDungeonVictimReady = true;
        GameManager.Instance.ServerDeployHuntersIntoDungeon();
    }



    // Gap Traversal

    [Server]
    public void ServerMoveThroughGap(Vector3 facingDirection, Vector3 gapEntrance, NetworkIdentity gapRef)
    {
        isMovingThroughGap = true;
        SERVER_CurrentGap = gapRef;
        RpcMoveThroughGap(facingDirection, gapEntrance, SERVER_CurrentGap.GetComponent<BaseGap>().gapAnimation);
    }
    [ClientRpc]
    public void RpcMoveThroughGap(Vector3 facingDirection, Vector3 gapEntrance, string gapAnimation)
    {
        isMovingThroughGap = true;

        if (isLocalPlayer && playerType == PlayerType.PlayerControlled)
        {
            playerController.characterController.enabled = false;
            playerController.dungeonVictimVelocity = Vector3.zero;
        }

        StartCoroutine(MoveThroughGap(facingDirection, gapEntrance, gapAnimation));
    }
    IEnumerator MoveThroughGap(Vector3 facingDirection, Vector3 gapEntrance, string gapAnimation)
    {
        float elapsedTime = 0f;

        gapEntrance.y = transform.position.y;

        Quaternion initialFacingRotation = transform.rotation;
        Quaternion targetFacingRotation = Quaternion.LookRotation(facingDirection);
        Quaternion initialCameraRotation = FPSCamera.localRotation;

        Vector3 initialPlayerPos = transform.position;
        
        // First move player to gap entrance
        while (elapsedTime < initialGapTransitionTime)
        {
            elapsedTime += Time.deltaTime;

            float t = Mathf.Clamp01(elapsedTime / initialGapTransitionTime);

            transform.rotation = Quaternion.Lerp(initialFacingRotation, targetFacingRotation, t);
            transform.position = Vector3.Lerp(initialPlayerPos, gapEntrance, t);
            FPSCamera.localRotation = Quaternion.Lerp(initialCameraRotation, Quaternion.Euler(0, 0, 0), t);

            yield return null;
        }

        entirePlayerAnimator.Play(gapAnimation, 0, 0f);
    }

    [Server]
    private void ServerFinishedTraversingGap()
    {
        isMovingThroughGap = false;
        SERVER_CurrentGap.GetComponent<BaseGap>().ServerFinishedMovingThroughGap();
        RpcFinishedTraversingGap();
    }
    [ClientRpc]
    private void RpcFinishedTraversingGap()
    {
        isMovingThroughGap = false;
        
        if (isLocalPlayer && playerType == PlayerType.PlayerControlled) playerController.characterController.enabled = true;
    }

    #endregion



    #region Dungeon Hunter Gameplay

    [Header("Dungeon Hunter Gameplay")]
    public bool isDungeonHunterReady;
    [SyncVar(hook = nameof(OnPlayerDungeonHunterChanged))] public PlayerDungeonHunter playerDungeonHunter; 

    [Server]
    public void ServerSpawnHunterInDungeon(PlayerDungeonHunter dungeonHunterData)
    {
        playerState = PlayerState.DungeonHunter;
        playerDungeonHunter = dungeonHunterData;

        RpcSpawnHunterInDungeon(dungeonHunterData);
    }
    [ClientRpc]
    public void RpcSpawnHunterInDungeon(PlayerDungeonHunter dungeonHunterData)
    {
        playerState = PlayerState.DungeonHunter;
        playerDungeonHunter = dungeonHunterData;
        
        HunterMonitor hunterMonitor = dungeonHunterData.dungeonHunterMonitor.GetComponent<HunterMonitor>();
        hunterMonitor.ShouldShowMonitor(true);

        if (playerType == PlayerType.PlayerControlled && isLocalPlayer)
        {
            CmdReportHunterReadyInDungeon();
        }
        else if (playerType == PlayerType.AIControlled && NetworkServer.active)
        {
            ServerReportHunterReadyInDungeon();
        }
    }

    [Command]
    private void CmdReportHunterReadyInDungeon()
    {
        isDungeonHunterReady = true;
        GameManager.Instance.ServerCheckAllDungeonPlayersSetUp();
    }
    [Server]
    private void ServerReportHunterReadyInDungeon()
    {
        isDungeonHunterReady = true;
        GameManager.Instance.ServerCheckAllDungeonPlayersSetUp();
    }

    private void OnPlayerDungeonHunterChanged(PlayerDungeonHunter oldValue, PlayerDungeonHunter newValue)
    {
        
    }

    #endregion

    #endregion
}
