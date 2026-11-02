using UnityEngine;
using Mirror;
using System.Collections.Generic;
using System.Collections;

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


    [Header("Tabletop")]
    [SyncVar] public int currentPlayerTurn = 0;
    private int totalTurns = 0;
    [SyncVar] public int currentRound = 1;
    [SyncVar] public int direction = 1;


    [Header("Dungeon")]
    public List<NetworkIdentity> dungeonHunters = new List<NetworkIdentity>();
    public List<NetworkIdentity> dungeonHunterMonitors = new List<NetworkIdentity>();

    [Space]

    public float victimDungeonSurvivalTime = 30f;
    [SyncVar(hook = nameof(OnDungeonTimerUpdate))] private float curr_victimDungeonSurvivalTimer;


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
            GameObject bot = Instantiate(botPlayerPrefab);
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


    #region Tabletop Gameplay
    [Server]
    public void ServerStartSwitchPlayerTurn() // Initializes the start of switch player turn
    {
        if (totalTurns == 0)
        {
            currentPlayerTurn = Random.Range(0, PlayerManager.Instance.players.Count);
            currentPlayerTurn = 0; // Remove this in final game

            direction = Random.Range(0f, 1f) >= 0.5 ? 1 : -1;
            currentRound = 1;
            cursedBoxStation.ServerInitializeCursedBoxStation();
        }
        else
        {
            NetworkIdentity oldPlayer = PlayerManager.Instance.players[currentPlayerTurn];
            oldPlayer.GetComponent<PlayerGameState>().ServerEndPlayerTurn();

            currentPlayerTurn += direction;
            currentPlayerTurn = GetCorrectPlayerTurn(currentPlayerTurn);

            if (totalTurns % PlayerManager.Instance.players.Count == 0)
            {
                currentRound++;
                cursedBoxStation.ServerStartSwitchPlayerTurn(true);
            }
            else
            {
                cursedBoxStation.ServerStartSwitchPlayerTurn(false);
            }

        }

        totalTurns++;

    }

    [Server]
    public void ServerStartNewPlayersTurn()
    {
        NetworkIdentity player = PlayerManager.Instance.players[currentPlayerTurn];
        player.GetComponent<PlayerGameState>().ServerStartPlayerTurn();
    }

    public int GetCorrectPlayerTurn(int turn)
    {
        int correctedPlayerTurn = turn;

        if (correctedPlayerTurn < 0) correctedPlayerTurn = PlayerManager.Instance.players.Count - 1;
        else if (correctedPlayerTurn >= PlayerManager.Instance.players.Count) correctedPlayerTurn = 0;

        return correctedPlayerTurn;


    }
    #endregion


    #region Dungeon Gameplay
    [SyncVar] public bool allDungeonPlayersReady;
    List<Transform> curr_DungeonSpawnPTs = new List<Transform>();
    NetworkIdentity curr_DungeonVictim;

    [Server]
    public void ServerDeployVictimIntoDungeon(NetworkIdentity dungeonVictim)
    {
        curr_DungeonSpawnPTs.Clear();
        curr_DungeonSpawnPTs = DungeonSpawning.Instance.GetDungeonSpawnPTs();

        allDungeonPlayersReady = false;

        curr_DungeonVictim = dungeonVictim;
        PlayerGameState tempPGS = dungeonVictim.GetComponent<PlayerGameState>();

        tempPGS.ServerEndPlayerTurn();
        tempPGS.ServerSpawnVictimInDungeon(curr_DungeonSpawnPTs[0].position, curr_DungeonSpawnPTs[0].rotation);
        
        cursedBoxStation.ServerHideCursedBoxStation();
    }

    [Server]
    public void ServerDeployHuntersIntoDungeon()
    {
        Debug.Log("Hunters Must Be Deployed!");
        int hunterSpawnPTIndex = 1;
        int hunterIndex = 0;

        foreach (NetworkIdentity hunter in PlayerManager.Instance.players)
        {
            if (hunter != curr_DungeonVictim)
            { 
                PlayerGameState hunterPGS = hunter.GetComponent<PlayerGameState>();

                PlayerDungeonHunter tempPDH = new PlayerDungeonHunter();
                
                tempPDH.dungeonHunterMonitor = dungeonHunterMonitors[PlayerManager.Instance.players.IndexOf(hunter)];
                tempPDH.dungeonHunter = dungeonHunters[hunterIndex];
                tempPDH.hunterSpawnPOS = curr_DungeonSpawnPTs[hunterSpawnPTIndex].position;
                tempPDH.hunterSpawnROT = curr_DungeonSpawnPTs[hunterSpawnPTIndex].rotation;

                hunterPGS.ServerSpawnHunterInDungeon(tempPDH);

                hunterIndex++;
                hunterSpawnPTIndex++;
            }
        }
    }

    [Server]
    public void ServerCheckAllDungeonPlayersSetUp()
    {
        Debug.Log("Check Dungeon Hunter Players Ready");

        foreach (NetworkIdentity hunter in PlayerManager.Instance.players)
        {
            PlayerGameState hunterPGS = hunter.GetComponent<PlayerGameState>();

            if (hunterPGS.playerState == PlayerState.DungeonHunter && !hunterPGS.isDungeonHunterReady)
            {
                return;
            }
            

            allDungeonPlayersReady = true;
            ServerStartDungeonGameplay();
        }
    }


    [Server]
    private void ServerStartDungeonGameplay()
    {
        curr_victimDungeonSurvivalTimer = victimDungeonSurvivalTime;
        StartCoroutine(DungeonTimer());
    }

    IEnumerator DungeonTimer()
    {
        while (curr_victimDungeonSurvivalTimer > 0)
        {
            yield return new WaitForSeconds(1f);
            curr_victimDungeonSurvivalTimer = Mathf.Clamp(curr_victimDungeonSurvivalTimer - 1f, 0f, victimDungeonSurvivalTime);
        }
    }
    private void OnDungeonTimerUpdate(float oldValue, float newValue)
    {
        Debug.Log($"{curr_victimDungeonSurvivalTimer} seconds left...");
    }

    #endregion

}
