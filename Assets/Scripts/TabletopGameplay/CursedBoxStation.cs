using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Mirror;

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

    public int specialItemsCap = 4;
    public float deathCardChanceIncrement = 10f;



    [Header("Current Station Gameplay State")]
    [SyncVar] public bool isDeathCard;
    [SyncVar] public float currentDeathCardChance;


    [Header("Interaction Triggers")]
    public GameObject gambleForItemTrigger;
    public GameObject addBloodPackTrigger;
    public GameObject insertItemCardTrigger;
    public GameObject drawCursedBoxCardTrigger;


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
        RefreshCursedBox();
        Invoke(nameof(ServerStartCursedBoxStation), 1.5f);
    }

    [Server]
    private void ServerStartCursedBoxStation()
    {
        StartPlayerTurnCooldown(1f);
        RpcStartCursedBoxStation(GameManager.Instance.currentPlayerTurn);
    }

    [ClientRpc]
    private void RpcStartCursedBoxStation(int currentPlayerTurn)
    {
        FaceTowardPlayer(currentPlayerTurn);

        stationAnimator.PlayInFixedTime("StartCursedBoxStation",0,0);
        stationAnimator.Update(0f);

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
    public void ServerStartSwitchPlayerTurn()
    {
        RefreshCursedBox();
        StartPlayerTurnCooldown(1f);
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


    private void StartPlayerTurnCooldown(float duration)
    {
        if (playerTurnCoroutine != null) StopCoroutine(playerTurnCoroutine);
        playerTurnCoroutine = StartCoroutine(StartPlayerTurnCooldownTransition(duration));
    }

    IEnumerator StartPlayerTurnCooldownTransition(float duration)
    {
        yield return new WaitForSeconds(duration);
        GameManager.Instance.ServerStartNewPlayersTurn();
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
            !PGS.playerTabletopTurn.hasUsedBloodPack &&
            PGS.currentBloodAmount > 0)
        {
            interactResult.canInteract = true;
            interactResult.interactType = "GambleForItem";
        }

        else if (hitObject == addBloodPackTrigger && 
            PGS.playerTabletopTurn.playerTabletopState == PlayerTabletopState.Itembox &&
            !PGS.playerTabletopTurn.hasGambledForItem &&
            !PGS.playerTabletopTurn.hasUsedBloodPack)
        {
            interactResult.canInteract = true;
            interactResult.interactType = "AddBloodPackForItem";
        }

        else if (hitObject == insertItemCardTrigger && 
            PGS.playerTabletopTurn.playerTabletopState == PlayerTabletopState.Cursedbox &&
            !PGS.playerTabletopTurn.hasDrawnCursedCard &&
            PGS.playerTabletopTurn.insertedItems < specialItemsCap)
        {
            interactResult.canInteract = true;
            interactResult.interactType = "InsertItemCard";
        }

        else if (hitObject == drawCursedBoxCardTrigger && 
            PGS.playerTabletopTurn.playerTabletopState == PlayerTabletopState.Cursedbox &&
            !PGS.playerTabletopTurn.hasDrawnCursedCard)
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


    // BLOOD PACKS
    [Server]
    private void ServerEnterBloodPackMode(NetworkIdentity playerRef)
    {
        PlayerGameState PGS = playerRef.GetComponent<PlayerGameState>();

        PlayerTabletopTurn tempTabletopTurn = PGS.playerTabletopTurn;
        tempTabletopTurn.inBloodPackMode = true;

        PGS.playerTabletopTurn = tempTabletopTurn;
    }

    [Server]
    public void ServerExitBloodPackMode(NetworkIdentity playerRef)
    {
        PlayerGameState PGS = playerRef.GetComponent<PlayerGameState>();

        PlayerTabletopTurn tempTabletopTurn = PGS.playerTabletopTurn;
        tempTabletopTurn.inBloodPackMode = false;

        PGS.playerTabletopTurn = tempTabletopTurn;
    }

    [Server]
    public void ServerAddBloodPack(NetworkIdentity playerRef)
    {
        PlayerGameState PGS = playerRef.GetComponent<PlayerGameState>();

        PlayerTabletopTurn tempTabletopTurn = PGS.playerTabletopTurn;
        tempTabletopTurn.hasUsedBloodPack = true;
        tempTabletopTurn.playerTabletopState = PlayerTabletopState.Cursedbox;

        GetRandomItems(playerRef);

        PGS.playerTabletopTurn = tempTabletopTurn;
        PGS.ServerRemoveItem("BloodPack");

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


    #endregion


    #region Cursed Box Gameplay



    [Server]
    private void RefreshCursedBox()
    {
        float randomChance = Random.Range(0f, 100f);
        isDeathCard = randomChance < currentDeathCardChance;

        Debug.Log("Is Death Card: " + isDeathCard);
        Debug.Log("Death card chance: " + randomChance + " / 100");      
    }


    [Server]
    private void ServerEnterItemCardMode(NetworkIdentity playerRef)
    {
        PlayerGameState PGS = playerRef.GetComponent<PlayerGameState>();

        PlayerTabletopTurn tempTabletopTurn = PGS.playerTabletopTurn;
        tempTabletopTurn.inItemCardMode = true;

        PGS.playerTabletopTurn = tempTabletopTurn;
    }

    [Server]
    public void ServerExitItemCardMode(NetworkIdentity playerRef)
    {
        PlayerGameState PGS = playerRef.GetComponent<PlayerGameState>();

        PlayerTabletopTurn tempTabletopTurn = PGS.playerTabletopTurn;
        tempTabletopTurn.inItemCardMode = false;

        PGS.playerTabletopTurn = tempTabletopTurn;
    }


    [Server]
    private void ServerInsertItemCard(NetworkIdentity playerRef, string itemName)
    {
        switch (itemName)
        {
            case "DecreaseDeathChance":
                ServerDecreaseDeathChance(playerRef);
                break;
            case "IncreaseDeathChance":
                ServerIncreaseDeathChance(playerRef);
                break;
            case "DrawBloodFromOpponent":
                ServerDrawBloodFromOpponent(playerRef);
                break;
            case "DrawCursedBoxTwice":
                ServerDrawTwice(playerRef);
                break;
            case "ForceDrawEveryone":
                ServerForceDrawEveryone(playerRef);
                break;
            case "ForceDraw":
                ServerForceDrawEveryone(playerRef);
                break;
            case "PeekInside":
                ServerPeekInside(playerRef);
                break;
            case "SkipTurn":
                ServerSkipTurn(playerRef);
                break;
            case "TakeTwoLives":
                ServerTakeTwoLives(playerRef);
                break;
            default:
                break;
        }

        PlayerGameState PGS = playerRef.GetComponent<PlayerGameState>();

        PlayerTabletopTurn tempTabletopTurn = PGS.playerTabletopTurn;
        tempTabletopTurn.insertedItems++;

        PGS.playerTabletopTurn = tempTabletopTurn;
        PGS.ServerRemoveItem(itemName);

    }

    [Server]
    private void ServerDrawCursedCard(NetworkIdentity playerRef)
    {
        PlayerGameState PGS = playerRef.GetComponent<PlayerGameState>();

        PlayerTabletopTurn tempTabletopTurn = PGS.playerTabletopTurn;
        tempTabletopTurn.hasDrawnCursedCard = true;

        PGS.playerTabletopTurn = tempTabletopTurn;

        if (isDeathCard)
        {
            GameManager.Instance.ServerActivateDungeon(playerRef);
        }
        else
        {
            GameManager.Instance.ServerStartSwitchPlayerTurn();
        }
    }

    #endregion

    #region Item Card Behaviours

    [Server]
    private void ServerDecreaseDeathChance(NetworkIdentity playerRef)
    {

    }

    [Server]
    private void ServerIncreaseDeathChance(NetworkIdentity playerRef)
    {

    }

    [Server]
    private void ServerDrawBloodFromOpponent(NetworkIdentity playerRef)
    {

    }

    [Server]
    private void ServerDrawTwice(NetworkIdentity playerRef)
    {

    }

    [Server]
    private void ServerForceDrawEveryone(NetworkIdentity playerRef)
    {

    }

    [Server]
    private void ServerForceDraw(NetworkIdentity playerRef)
    {

    }

    [Server]
    private void ServerPeekInside(NetworkIdentity playerRef)
    {

    }

    [Server]
    private void ServerSkipTurn(NetworkIdentity playerRef)
    {

    }

    [Server]
    private void ServerTakeTwoLives(NetworkIdentity playerRef)
    {

    }


    #endregion
}
