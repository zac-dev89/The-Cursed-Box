using Mirror;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CursedBoxStation : Interactable
{
    [Header("Main References")]
    public Animator stationAnimator;
    public List<GameObject> stationObjects;

    [Space]

    public GameObject itemBox;
    public GameObject cursedBox;


    [Header("Station Gameplay Settings")]
    public float stationTurnAnimDuration;
    public bool stationInteractCooldown;

    [Space]

    public List<string> allItems = new List<string>();

    [Space]

    public int cursedBoxItemsCap = 4;
    public float deathCardChanceIncrement = 10f;


    [Header("Current Station Gameplay State")]
    [SyncVar] public bool isDeathCard;
    [SyncVar] public float currentDeathCardChance;
    private readonly SyncList<string> usedItemsInTurn = new SyncList<string>();

    // Item States
    [HideInInspector, SyncVar] public bool increaseDeathChanceItemInserted;
    [HideInInspector, SyncVar] public bool takeTwoLivesItemInserted;
    [HideInInspector, SyncVar] public bool skipTurnItemInserted;
    [HideInInspector, SyncVar] public bool drawTwiceItemInserted;
    [HideInInspector, SyncVar] public bool forceDrawItemInserted;
    [HideInInspector, SyncVar] public bool everyoneForceDrawItemInserted;

    [Space]

    [HideInInspector, SyncVar] public bool playerMustDrawTwice;
    [HideInInspector, SyncVar] public bool playerMustForceDraw;
    [HideInInspector, SyncVar] public bool everyoneMustForceDraw;
    private int playerForceDrawCount;


    [Header("Interaction Triggers")]
    public GameObject gambleForItemTrigger;
    public GameObject insertItemBoxItemTrigger;
    public GameObject insertCursedBoxItemTrigger;
    public GameObject drawCursedBoxCardTrigger;

    [Header("Other Look At PTs")]
    public GameObject peekInsideCursedBox;

    [Header("Cursed Box UI Screen")]
    public TextMeshProUGUI peekInsideCursedBoxText;

    [Header("Coroutines")]
    private Coroutine facePlayerCoroutine;
    private Coroutine playerTurnCoroutine;

    private void Start()
    {
        ShouldShowStation(false);
    }



    #region Initializing Cursed Box Station

    [Server]
    public void ServerInitializeCursedBoxStation()
    {
        currentDeathCardChance = deathCardChanceIncrement;
        ServerRefreshCursedBox();
        Invoke(nameof(ServerStartCursedBoxStation), 1.5f);
    }

    [Server]
    private void ServerStartCursedBoxStation()
    {
        ServerStartPlayerTurnCooldown(1f);
        RpcStartCursedBoxStation(GameManager.Instance.currentPlayerTurn);
    }

    [ClientRpc]
    private void RpcStartCursedBoxStation(int currentPlayerTurn)
    {
        FaceTowardPlayer(currentPlayerTurn);

        stationAnimator.PlayInFixedTime("StartCursedBoxStation", 0, 0);
        stationAnimator.Update(0f);

        peekInsideCursedBoxText.gameObject.SetActive(false);
        ShouldShowStation(true);
    }

    public void ShouldShowStation(bool value)
    {
        for (int i = 0; i < stationObjects.Count; i++)
        {
            stationObjects[i].SetActive(value);
        }
    }
    #endregion


    #region Player Turns

    [Server]
    public void ServerStartSwitchPlayerTurn(bool newRound)
    {
        if (newRound)
        {
            ServerIncreaseDeathChance(deathCardChanceIncrement);
        }

        // Incrase death chance
        if (increaseDeathChanceItemInserted)
        {
            ServerIncreaseDeathChance(deathCardChanceIncrement);
            increaseDeathChanceItemInserted = false;
        }

        // Drawing Twice
        if (drawTwiceItemInserted)
        {
            drawTwiceItemInserted = false;
            playerMustDrawTwice = true;
        }

        // Force Drawing
        if (forceDrawItemInserted)
        {
            forceDrawItemInserted = false;
            playerMustForceDraw = true;
        }
        else if (everyoneForceDrawItemInserted)
        {
            everyoneForceDrawItemInserted = false;
            everyoneMustForceDraw = true;
        }

        if (playerMustForceDraw && playerForceDrawCount == 1)
        {
            playerMustForceDraw = false;
            playerForceDrawCount = 0;
            Debug.Log("Force Draw Finished");
        }
        else if (everyoneMustForceDraw && playerForceDrawCount == PlayerManager.Instance.players.Count - 1)
        {
            everyoneMustForceDraw = false;       
            playerForceDrawCount = 0;
            Debug.Log("Everyone Force Draw Finished");
        }
        

        usedItemsInTurn.Clear();
        ServerRefreshCursedBox();
        ServerStartPlayerTurnCooldown(1f);
        RpcStartSwitchPlayerTurn(GameManager.Instance.currentPlayerTurn);
    }

    [ClientRpc]
    private void RpcStartSwitchPlayerTurn(int currentPlayerTurn)
    {
        FaceTowardPlayer(currentPlayerTurn);
    }

    private void FaceTowardPlayer(int playerIndex)
    {
        Vector3 targetRot = Vector3.zero;

        switch (playerIndex)
        {
            case 0:
                targetRot = Vector3.zero;
                break;
            case 1:
                targetRot = new Vector3(0f, 90f, 0f);
                break;
            case 2:
                targetRot = new Vector3(0f, 180f, 0f);
                break;
            case 3:
                targetRot = new Vector3(0f, 270f, 0f);
                break;
            default:
                targetRot = Vector3.zero;
                break;
        }

        if (facePlayerCoroutine != null) StopCoroutine(facePlayerCoroutine);
        facePlayerCoroutine = StartCoroutine(FaceTowardPlayerTransition(targetRot));
    }

    IEnumerator FaceTowardPlayerTransition(Vector3 targetRot)
    {
        float elapsedTime = 0f;

        Quaternion initialRot = transform.rotation;

        while (elapsedTime < stationTurnAnimDuration)
        {
            elapsedTime += Time.deltaTime;
            transform.rotation = Quaternion.Slerp(initialRot, Quaternion.Euler(targetRot), elapsedTime / stationTurnAnimDuration);
            yield return null;
        }
    }

    [Server]
    private void ServerStartPlayerTurnCooldown(float duration)
    {
        if (playerTurnCoroutine != null) StopCoroutine(playerTurnCoroutine);
        playerTurnCoroutine = StartCoroutine(StartPlayerTurnCooldownTransition(duration));
    }

    IEnumerator StartPlayerTurnCooldownTransition(float duration)
    {
        yield return new WaitForSeconds(duration);
        GameManager.Instance.ServerStartNewPlayersTurn();
    }

    [Server]
    public void ServerHideCursedBoxStation()
    {
        RpcHideCursedBoxStation();
    }
    [ClientRpc]
    private void RpcHideCursedBoxStation()
    {
        stationAnimator.Play("EndCursedBoxStation");
    }
    

    #endregion


    #region Player Only Interaction

    public override CanInteractResult CanInteract(GameObject hitObject, NetworkIdentity playerRef)
    {
        PlayerGameState PGS = playerRef.GetComponent<PlayerGameState>();

        CanInteractResult interactResult = new CanInteractResult();

        interactResult.canInteract = false;
        interactResult.interactType = "";

        if (hitObject == gambleForItemTrigger &&
            PGS.playerTabletopTurn.playerTabletopState == PlayerTabletopState.Itembox &&
            !PGS.playerTabletopTurn.hasGambledForItem &&
            !PGS.playerTabletopTurn.hasUsedItemBoxItem &&
            !PGS.playerTabletopTurn.inInteractionCooldown &&
            !playerMustForceDraw &&
            !everyoneMustForceDraw &&
            PGS.currentBloodAmount > 0)
        {
            interactResult.canInteract = true;
            interactResult.interactType = "GambleForItem";
        }

        else if (hitObject == insertItemBoxItemTrigger &&
            PGS.playerTabletopTurn.playerTabletopState == PlayerTabletopState.Itembox &&
            !PGS.playerTabletopTurn.hasGambledForItem &&
            !PGS.playerTabletopTurn.hasUsedItemBoxItem &&
            !PGS.playerTabletopTurn.inInteractionCooldown &&
            !playerMustForceDraw &&
            !everyoneMustForceDraw &&
            !PGS.playerTabletopTurn.inItemBoxInsertMode)
        {
            interactResult.canInteract = true;
            interactResult.interactType = "AddBloodPackForItem";
        }

        else if (hitObject == insertCursedBoxItemTrigger &&
            PGS.playerTabletopTurn.playerTabletopState == PlayerTabletopState.Cursedbox &&
            !PGS.playerTabletopTurn.finishedCursedBoxDraw &&
            PGS.playerTabletopTurn.insertedItems < cursedBoxItemsCap &&
            !PGS.playerTabletopTurn.inInteractionCooldown &&
            !playerMustForceDraw &&
            !everyoneMustForceDraw &&
            !PGS.playerTabletopTurn.inCursedBoxInsertMode)
        {
            interactResult.canInteract = true;
            interactResult.interactType = "InsertItemCard";
        }

        else if (hitObject == drawCursedBoxCardTrigger &&
            PGS.playerTabletopTurn.playerTabletopState == PlayerTabletopState.Cursedbox &&
            !PGS.playerTabletopTurn.finishedCursedBoxDraw &&
            !PGS.playerTabletopTurn.inInteractionCooldown &&
            !PGS.playerTabletopTurn.inCursedBoxInsertMode)
        {
            interactResult.canInteract = true;
            interactResult.interactType = "DrawCursedCard";
        }

        return interactResult;
    }

    [Server]
    public override void ServerInteract(GameObject hitObject, NetworkIdentity playerRef)
    {
        CanInteractResult interactResult = CanInteract(hitObject, playerRef);

        if (!interactResult.canInteract) return;

        PlayerGameState PGS = playerRef.GetComponent<PlayerGameState>();

        switch (interactResult.interactType)
        {
            case "GambleForItem":
                ServerGambleForItem(playerRef);
                break;
            case "AddBloodPackForItem":
                // Open button of blood packs player can use
                ServerEnterBloodPackMode(playerRef);
                break;
            case "InsertItemCard":
                // Open menu of items player can use
                ServerEnterItemCardMode(playerRef);
                break;
            case "DrawCursedCard":
                ServerDrawCursedCard(playerRef);
                break;
        }

    }

    #endregion


    public bool CanInsertItem(NetworkIdentity playerRef, string itemName)
    {
        ItemInformation itemInfo = ItemData.Instance.GetItemInfoFromName(itemName);
        PlayerGameState PGS = playerRef.GetComponent<PlayerGameState>();

        if (PGS.playerTabletopTurn.inInteractionCooldown) return false;

        bool canInsert = false;

        if (itemInfo.isCursedBoxItem && PGS.playerTabletopTurn.playerTabletopState == PlayerTabletopState.Cursedbox)
        {
            canInsert = CanInsertCursedBoxItem(playerRef, itemName);
        }
        else if (itemInfo.isItemBoxItem && PGS.playerTabletopTurn.playerTabletopState == PlayerTabletopState.Itembox)
        {
            canInsert = CanInsertItemBoxItem(playerRef, itemName);
        }

        return canInsert;
    }

    [Server]
    public void ServerInsertItem(NetworkIdentity playerRef, string itemName)
    {
        ItemInformation itemInfo = ItemData.Instance.GetItemInfoFromName(itemName);
        PlayerGameState PGS = playerRef.GetComponent<PlayerGameState>();

        if (CanInsertCursedBoxItem(playerRef, itemName) && itemInfo.isCursedBoxItem && PGS.playerTabletopTurn.playerTabletopState == PlayerTabletopState.Cursedbox)
        {
            ServerInsertCursedBoxItem(playerRef, itemName);
        }
        else if (CanInsertItemBoxItem(playerRef, itemName) && itemInfo.isItemBoxItem && PGS.playerTabletopTurn.playerTabletopState == PlayerTabletopState.Itembox)
        {
            ServerInsertItemBoxItem(playerRef, itemName);
        }
    }


    #region Item Box Gameplay
    [Server]
    private void ServerGambleForItem(NetworkIdentity playerRef)
    {
        PlayerGameState PGS = playerRef.GetComponent<PlayerGameState>();

        PlayerTabletopTurn tempTabletopTurn = PGS.playerTabletopTurn;
        tempTabletopTurn.hasGambledForItem = true;
        tempTabletopTurn.playerTabletopState = PlayerTabletopState.Cursedbox;

        GetRandomItems(playerRef);

        PGS.playerTabletopTurn = tempTabletopTurn;
        PGS.currentBloodAmount--;
    }


    private void ServerEnterBloodPackMode(NetworkIdentity playerRef)
    {
        PlayerGameState PGS = playerRef.GetComponent<PlayerGameState>();

        PlayerTabletopTurn tempTabletopTurn = PGS.playerTabletopTurn;
        tempTabletopTurn.inItemBoxInsertMode = true;

        PGS.playerTabletopTurn = tempTabletopTurn;
    }

    [Server]
    public void ServerExitBloodPackMode(NetworkIdentity playerRef)
    {
        PlayerGameState PGS = playerRef.GetComponent<PlayerGameState>();

        PlayerTabletopTurn tempTabletopTurn = PGS.playerTabletopTurn;
        tempTabletopTurn.inItemBoxInsertMode = false;

        PGS.playerTabletopTurn = tempTabletopTurn;
    }

    [Server]
    public void ServerAddBloodPack(NetworkIdentity playerRef)
    {
        PlayerGameState PGS = playerRef.GetComponent<PlayerGameState>();

        PlayerTabletopTurn tempTabletopTurn = PGS.playerTabletopTurn;
        tempTabletopTurn.hasUsedItemBoxItem = true;
        tempTabletopTurn.playerTabletopState = PlayerTabletopState.Cursedbox;

        GetRandomItems(playerRef);

        PGS.playerTabletopTurn = tempTabletopTurn;
        PGS.ServerRemoveItemBoxItem("BloodPack");

        ServerExitBloodPackMode(playerRef);

    }

    private void GetRandomItems(NetworkIdentity playerRef)
    {
        PlayerGameState PGS = playerRef.GetComponent<PlayerGameState>();

        int randomNumberOfItems = Random.Range(1, 4);

        for (int i = 0; i < randomNumberOfItems; i++)
        {
            string gainedSpecialItem = allItems[Random.Range(0, allItems.Count)];
            PGS.ServerRetrievedItem(gainedSpecialItem);
        }
    }

    public bool CanInsertItemBoxItem(NetworkIdentity playerRef, string itemName)
    {
        PlayerGameState PGS = playerRef.GetComponent<PlayerGameState>();

        if (PGS.playerTabletopTurn.hasUsedItemBoxItem) return false;

        bool canInsert = false;

        if (itemName == "BloodPack")
        {
            canInsert = true;
        }

        return canInsert;

    }
    [Server]
    public void ServerInsertItemBoxItem(NetworkIdentity playerRef, string itemName)
    {
        switch (itemName)
        {
            case "BloodPack":
                ServerAddBloodPack(playerRef);
                break;
        }
    }

    #endregion


    #region Cursed Box Gameplay
    [Server]
    public void ServerIncreaseDeathChance(float deathChanceAddition)
    {
        currentDeathCardChance += deathCardChanceIncrement;
    }


    [Server]
    private void ServerRefreshCursedBox()
    {
        Debug.Log("Death Card Chance: " + currentDeathCardChance);
        float randomChance = Random.Range(0f, 100f);
        isDeathCard = randomChance < currentDeathCardChance;

        //Debug.Log("Is Death Card: " + isDeathCard);
        //Debug.Log("Death card chance: " + randomChance + " / 100");      
    }


    [Server]
    private void ServerEnterItemCardMode(NetworkIdentity playerRef)
    {
        PlayerGameState PGS = playerRef.GetComponent<PlayerGameState>();

        PlayerTabletopTurn tempTabletopTurn = PGS.playerTabletopTurn;
        tempTabletopTurn.inCursedBoxInsertMode = true;

        PGS.playerTabletopTurn = tempTabletopTurn;
    }

    [Server]
    public void ServerExitItemCardMode(NetworkIdentity playerRef)
    {
        PlayerGameState PGS = playerRef.GetComponent<PlayerGameState>();

        PlayerTabletopTurn tempTabletopTurn = PGS.playerTabletopTurn;
        tempTabletopTurn.inCursedBoxInsertMode = false;

        PGS.playerTabletopTurn = tempTabletopTurn;
    }

    public bool CanInsertCursedBoxItem(NetworkIdentity playerRef, string itemName)
    {
        // Don't allow the same card used twice
        if (usedItemsInTurn.Contains(itemName))
        {
            return false;
        }

        PlayerGameState PGS = playerRef.GetComponent<PlayerGameState>();

        if (PGS.playerTabletopTurn.insertedItems >= cursedBoxItemsCap) return false;

        bool canInsertItem = false;

        if (itemName == "DecreaseDeathChance")
        {
            if (currentDeathCardChance > deathCardChanceIncrement)
            {
                canInsertItem = true;
            }
        }
        else if (itemName == "IncreaseDeathChance")
        {
            if (currentDeathCardChance < 100f)
            {
                canInsertItem = true;
            }
        }
        else if (itemName == "DrawBloodFromOpponent")
        {
            if (GetValidPlayerToStealBlood() != null)
            {
                canInsertItem = true;
            }
        }
        else if (itemName == "DrawCursedBoxTwice")
        {
            canInsertItem = true;
        }
        else if (itemName == "ForceDrawEveryone")
        {
            if (!forceDrawItemInserted && !everyoneForceDrawItemInserted)
            {
                canInsertItem = true;
            }
        }
        else if (itemName == "ForceDraw")
        {
            if (!forceDrawItemInserted && !everyoneForceDrawItemInserted)
            {
                canInsertItem = true;
            }
        }
        else if (itemName == "PeekInside")
        {
            canInsertItem = true;
        }
        else if (itemName == "SkipTurn")
        {
            canInsertItem = true;
        }
        else if (itemName == "TakeTwoLives")
        {
            if (!takeTwoLivesItemInserted)
            {
                canInsertItem = true;
            }
        }
        else if (itemName == "ReverseDirection")
        {
            canInsertItem = true;
        }

        return canInsertItem;
    }

    [Server]
    public void ServerInsertCursedBoxItem(NetworkIdentity playerRef, string itemName)
    {
        switch (itemName)
        {
            case "DecreaseDeathChance":
                ServerDecreaseDeathChanceItem(playerRef);
                break;
            case "IncreaseDeathChance":
                ServerIncreaseDeathChanceItem(playerRef);
                break;
            case "DrawBloodFromOpponent":
                ServerDrawBloodFromOpponentItem(playerRef);
                break;
            case "DrawCursedBoxTwice":
                ServerDrawTwiceItem(playerRef);
                break;
            case "ForceDrawEveryone":
                ServerForceDrawEveryoneItem(playerRef);
                break;
            case "ForceDraw":
                ServerForceDrawItem(playerRef);
                break;
            case "PeekInside":
                StartCoroutine(PeekInsideBehaviour(playerRef));
                break;
            case "SkipTurn":
                ServerSkipTurnItem(playerRef);
                break;
            case "TakeTwoLives":
                ServerTakeTwoLivesItem(playerRef);
                break;
            case "ReverseDirection":
                ServerReverseDirectionItem(playerRef);
                break;
            default:
                break;
        }

        usedItemsInTurn.Add(itemName);

        PlayerGameState PGS = playerRef.GetComponent<PlayerGameState>();

        PlayerTabletopTurn tempTabletopTurn = PGS.playerTabletopTurn;
        tempTabletopTurn.insertedItems++;

        PGS.playerTabletopTurn = tempTabletopTurn;
        PGS.ServerRemoveItem(itemName);


    }

    [Server]
    public void ServerDrawCursedCard(NetworkIdentity playerRef)
    {
        PlayerGameState PGS = playerRef.GetComponent<PlayerGameState>();

        PlayerTabletopTurn tempTabletopTurn = PGS.playerTabletopTurn;

        if (skipTurnItemInserted)
        {
            skipTurnItemInserted = false;
            GameManager.Instance.ServerStartSwitchPlayerTurn();
            return;
        }

        tempTabletopTurn.cursedBoxDrawCounter++;
        //Debug.Log($"Player drew {tempTabletopTurn.cursedBoxDrawCounter} times. He must draw {(playerMustDrawTwice ? 2 : 1)} times");

        if (tempTabletopTurn.cursedBoxDrawCounter == 1 && !playerMustDrawTwice)
        {
            tempTabletopTurn.finishedCursedBoxDraw = true;
        }
        else if (tempTabletopTurn.cursedBoxDrawCounter == 2 && playerMustDrawTwice)
        {
            tempTabletopTurn.finishedCursedBoxDraw = true;
            playerMustDrawTwice = false;
        }

        PGS.playerTabletopTurn = tempTabletopTurn;

        if ((playerMustForceDraw || everyoneMustForceDraw) && tempTabletopTurn.finishedCursedBoxDraw)
        {
            playerForceDrawCount++;
            Debug.Log($"Force Draw Count: {playerForceDrawCount}");
        }

        if (isDeathCard)
        {
            // Add life logic here
            int livesToDeduct = takeTwoLivesItemInserted ? 2 : 1;
            PGS.ServerRetrieveDeathCard(livesToDeduct);
        }
        else if (tempTabletopTurn.finishedCursedBoxDraw)
        {
            GameManager.Instance.ServerStartSwitchPlayerTurn();
        }
    }


    [Server]
    private void ServerDecreaseDeathChanceItem(NetworkIdentity playerRef)
    {
        Debug.Log("DecreaseDeathChance");

        currentDeathCardChance -= deathCardChanceIncrement;
        currentDeathCardChance = Mathf.Clamp(currentDeathCardChance, deathCardChanceIncrement, 100f);
    }

    [Server]
    private void ServerIncreaseDeathChanceItem(NetworkIdentity playerRef)
    {
        Debug.Log("IncreaseDeathChance");
        increaseDeathChanceItemInserted = true;
    }


    private NetworkIdentity GetValidPlayerToStealBlood()
    {
        int targetPlayerIndex = GameManager.Instance.currentPlayerTurn;
        NetworkIdentity target_player = null;

        for (int i = 0; i < PlayerManager.Instance.players.Count; i++)
        {
            targetPlayerIndex += GameManager.Instance.direction;
            targetPlayerIndex = GameManager.Instance.GetCorrectPlayerTurn(targetPlayerIndex);

            NetworkIdentity temp_player = PlayerManager.Instance.players[targetPlayerIndex];

            if (netIdentity != temp_player)
            {
                PlayerGameState temp_player_PGS = temp_player.GetComponent<PlayerGameState>();
                if (temp_player_PGS.currentBloodAmount > 0)
                {
                    target_player = temp_player;
                }
            }
        }

        return target_player;
    }

    [Server]
    private void ServerDrawBloodFromOpponentItem(NetworkIdentity robberRef)
    {
        Debug.Log("DrawBloodFromOpponent");
        NetworkIdentity victim = GetValidPlayerToStealBlood();

        if (victim == null) return;

        PlayerGameState robber_PGS = robberRef.GetComponent<PlayerGameState>();
        PlayerGameState victim_PGS = victim.GetComponent<PlayerGameState>();

        robber_PGS.ServerRetrievedItem("BloodPack");
        victim_PGS.currentBloodAmount--;
        RpcStealBloodFromVictim(robberRef, victim);

    }
    [ClientRpc]
    private void RpcStealBloodFromVictim(NetworkIdentity robberRef, NetworkIdentity victimRef)
    {
        Debug.Log($"{robberRef.name} Stole From {victimRef.name}");
    }


    [Server]
    private void ServerDrawTwiceItem(NetworkIdentity playerRef)
    {
        Debug.Log("DrawTwice");
        drawTwiceItemInserted = true;
    }

    [Server]
    private void ServerForceDrawEveryoneItem(NetworkIdentity playerRef)
    {
        Debug.Log("ForceDrawEveryone");
        everyoneForceDrawItemInserted = true;
    }

    [Server]
    private void ServerForceDrawItem(NetworkIdentity playerRef)
    {
        Debug.Log("ForceDraw");
        forceDrawItemInserted = true;
    }


    private IEnumerator PeekInsideBehaviour(NetworkIdentity playerRef)
    {
        ServerPeekInsideItem(playerRef);
        yield return new WaitForSeconds(2f);
        ServerFinishedPeeking(playerRef);
    }
    [Server]
    private void ServerPeekInsideItem(NetworkIdentity playerRef)
    {
        Debug.Log("PeekInside");
        PlayerGameState PGS = playerRef.GetComponent<PlayerGameState>();

        PlayerTabletopTurn tempTabletopTurn = PGS.playerTabletopTurn;
        tempTabletopTurn.inInteractionCooldown = true;
        tempTabletopTurn.peekingAtBox = true;

        PGS.playerTabletopTurn = tempTabletopTurn;

        RpcShouldDisplayPeekInsideVisuals(true);
    }
    [Server]
    private void ServerFinishedPeeking(NetworkIdentity playerRef)
    {
        PlayerGameState PGS = playerRef.GetComponent<PlayerGameState>();

        PlayerTabletopTurn tempTabletopTurn = PGS.playerTabletopTurn;
        tempTabletopTurn.inInteractionCooldown = false;
        tempTabletopTurn.peekingAtBox = false;

        PGS.playerTabletopTurn = tempTabletopTurn;

        RpcShouldDisplayPeekInsideVisuals(false);
    }
    [ClientRpc]
    private void RpcShouldDisplayPeekInsideVisuals(bool isPeeking)
    {
        if (isPeeking)
        {
            peekInsideCursedBoxText.gameObject.SetActive(true);
            peekInsideCursedBoxText.text = isDeathCard ? "DEATH" : "SAFE";
        }
        else
        {
            peekInsideCursedBoxText.gameObject.SetActive(false);
        }
    }


    [Server]
    private void ServerSkipTurnItem(NetworkIdentity playerRef)
    {
        Debug.Log("SkipTurn");
        skipTurnItemInserted = true;
    }

    [Server]
    private void ServerTakeTwoLivesItem(NetworkIdentity playerRef)
    {
        Debug.Log("TakeTwoLives");
        takeTwoLivesItemInserted = true;
    }

    [Server]
    private void ServerReverseDirectionItem(NetworkIdentity playerRef)
    {
        Debug.Log("Reverse Direction");
        GameManager.Instance.direction *= -1;
    }
    #endregion
}
