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


    [Header("Station Turn")]
    public float stationTurnAnimDuration;
    public bool stationInteractCooldown;

    [Space]

    // Current player turn settings
    public int specialItemsCap = 3;


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

 
}
