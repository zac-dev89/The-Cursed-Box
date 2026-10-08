using UnityEngine;
using Mirror;
using Unity.VisualScripting;
using System.Collections.Generic;

public enum GameState 
{
    NONE,
    Tabletop,
    Dungeon
}

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    [Header("Main References")]
    public GameObject botPlayerPrefab;
    public CursedBoxStation cursedBoxStation;

    [Header("Game States")]
    [SyncVar] public bool hasGameStarted = false;
    [SyncVar] public GameState gameState;

    [Space]

    [SyncVar] public int currentPlayerTurn = 0;
    private int totalTurns = 0;
    [SyncVar] public int currentRound = 1;



    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        if (!NetworkServer.active) return;

        hasGameStarted = false;
    }


    #region Set Up Games

    [Server]
    public void ServerStartSingleplayerGame()
    {
        hasGameStarted = true;

        gameState = GameState.Tabletop;

        // Spawn Bots
        ServerSpawnBots(3);
        ServerSetUpGame();    
    }


    [Server]
    public void ServerStartMultiplayerGame()
    {
        hasGameStarted = true;

        gameState = GameState.Tabletop;

        int numBots = 4 - PlayerManager.Instance.players.Count;
        ServerSpawnBots(numBots);

        ServerSetUpGame();
    }

    [Server]
    private void ServerSpawnBots(int num)
    {
        for (int i = 0; i < num; i++)
        {
            Transform seatSpawn = TabletopSpawning.Instance.ServerAddPlayerFromSpawn();
            GameObject bot = Instantiate(botPlayerPrefab, seatSpawn.position, seatSpawn.rotation);
            NetworkServer.Spawn(bot);

            PlayerGameState botGameState = bot.GetComponent<PlayerGameState>();
            botGameState.ServerSwitchToAIController();

        }
    }

    [Server]
    private void ServerSetUpGame()
    {
        for (int i = 0; i < PlayerManager.Instance.players.Count; i++)
        {
            PlayerGameState tempPlayerGameState = PlayerManager.Instance.players[i].GetComponent<PlayerGameState>();
            tempPlayerGameState.ServerOnPlayerGameStarted();
        }

        ServerStartSwitchPlayerTurn();
    }
    #endregion

    #region Main Gameplay
    [Server]
    public void ServerStartSwitchPlayerTurn() // Initializes the start of switch player turn
    {
        if (totalTurns == 0)
        {
            currentPlayerTurn = Random.Range(0, PlayerManager.Instance.players.Count);
            currentPlayerTurn = 0;
            cursedBoxStation.ServerInitializeCursedBoxStation();
        }
        else
        {
            NetworkIdentity oldPlayer = PlayerManager.Instance.players[currentPlayerTurn];
            oldPlayer.GetComponent<PlayerGameState>().ServerEndPlayerTurn();

            currentPlayerTurn++;
            currentPlayerTurn = currentPlayerTurn >= PlayerManager.Instance.players.Count ? 0 : currentPlayerTurn;

            cursedBoxStation.ServerStartSwitchPlayerTurn();
        }

        totalTurns++;


    }

    [Server]
    public void ServerStartNewPlayersTurn()
    {
        NetworkIdentity player = PlayerManager.Instance.players[currentPlayerTurn];
        player.GetComponent<PlayerGameState>().ServerStartPlayerTurn();
    }

    #endregion

}
