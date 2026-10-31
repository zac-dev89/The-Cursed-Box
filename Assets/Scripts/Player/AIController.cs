using Mirror;
using System.Collections;
using UnityEngine;

public class AIController : NetworkBehaviour
{
    [Header("Main References")]
    public PlayerGameState playerGameState;

    [Header("Enabling Gameplay")]
    [SyncVar] public bool AIControllerEnabled;

    [Header("AI Actions Timer")]
    public float minActionTime = 1f;
    public float maxActionTime = 3f;
    private float currentActionTime = 0f;
    private float elapsedActionTimer = 0f;

    [Server]
    public void ServerInitializeAIController()
    {
        currentActionTime = Random.Range(minActionTime, maxActionTime);
    }

    private void Update()
    {
        if (!NetworkServer.active) return;
        if (!AIControllerEnabled) return;
        if (!playerGameState.playerStartedInGame) return;

        if (playerGameState.playerState == PlayerState.Tabletop)
        {
            ServerTabletopGameplay();
        }
        else if (playerGameState.playerState == PlayerState.DungeonHunter)
        {
            ServerDungeonHunterGameplay();
        }
        else if (playerGameState.playerState == PlayerState.DungeonVictim)
        {
            ServerDungeonVictimGameplay();
        }
    }

    #region Table Top Gameplay
    [Server]
    private void ServerTabletopGameplay()
    {
        if (!playerGameState.playerTabletopTurn.inTurn) return;

        elapsedActionTimer += Time.deltaTime;

        if (elapsedActionTimer >= currentActionTime)
        {
            elapsedActionTimer = 0f;
            ServerTableTopGameplayAction();
        }
    }

    [Server]
    private void ServerTableTopGameplayAction()
    {
        if (playerGameState.playerTabletopTurn.finishedCursedBoxDraw) return;

        GameManager.Instance.cursedBoxStation.ServerDrawCursedCard(netIdentity);
    }

    #endregion



    #region Dungeon Gameplay
    [Server]
    private void ServerDungeonHunterGameplay()
    {

    }
    [Server]
    private void ServerDungeonVictimGameplay()
    {

    }
    #endregion
}
