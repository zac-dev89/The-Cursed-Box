using UnityEngine;
using Mirror;
using Unity.VisualScripting;
using System.Collections.Generic;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    [Header("Main References")]
    public GameObject lobbyCam;
    public GameObject botPlayerPrefab;

    [Header("Game States")]
    [SyncVar] public bool hasGameStarted = false;

    [Header("Players")]
    public List<NetworkIdentity> players = new List<NetworkIdentity>();


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        if (!NetworkServer.active) return;

        if (GameTypeManager.Instance.isSingleplayerGame)
        {
            SetUpSingleplayerGame();
        }
        else if (GameTypeManager.Instance.isMultiplayerGame)
        {
            SetUpMultiplayerGame();
        }
    }

    #region Set Up Games
    public void SetUpSingleplayerGame()
    {
        hasGameStarted = true;

        // Spawn Bots
        SpawnSingleplayerAI();
    }
    private void SpawnSingleplayerAI()
    {
        for (int i = 0; i < 3; i++)
        {
            Transform seatSpawn = TabletopSpawning.Instance.ServerAddPlayerFromSpawn();
            GameObject bot = Instantiate(botPlayerPrefab, seatSpawn.position, seatSpawn.rotation);
            NetworkServer.Spawn(bot);

            PlayerGameState botGameState = bot.GetComponent<PlayerGameState>();
            botGameState.ServerSwitchToAIController();
            botGameState.UpdateCameraSetUp();

        }
    }



    public void SetUpMultiplayerGame()
    {
        hasGameStarted = false;
    }
    #endregion

}
