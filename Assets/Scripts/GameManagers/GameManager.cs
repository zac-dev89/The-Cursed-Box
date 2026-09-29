using UnityEngine;
using Mirror;
using Unity.VisualScripting;
using System.Collections.Generic;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    [Header("Main References")]
    public GameObject botPlayerPrefab;

    [Header("Game States")]
    [SyncVar] public bool hasGameStarted = false;

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

        // Spawn Bots
        ServerSpawnBots(3);
    }


    [Server]
    public void ServerStartMultiplayerGame()
    {
        hasGameStarted = true;

        int numBots = 4 - PlayerManager.Instance.players.Count;
        ServerSpawnBots(numBots);
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
    #endregion



}
