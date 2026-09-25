using UnityEngine;
using Mirror;

public class CustomNetworkManager : NetworkManager
{
    [Header("Player Types")]
    public GameObject gamePlayer;




    public override void OnServerChangeScene(string newSceneName)
    {
        base.OnServerChangeScene(newSceneName);

        playerPrefab = gamePlayer;

    }


    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        TabletopSpawning tabletopSpawning = FindAnyObjectByType<TabletopSpawning>();

        Transform startPos = tabletopSpawning.ServerAddPlayerFromSpawn();
        GameObject player = startPos != null
            ? Instantiate(playerPrefab, startPos.position, startPos.rotation)
            : Instantiate(playerPrefab);

        player.name = $"{playerPrefab.name} [connId={conn.connectionId}]";

        // Set Up Player Game State
        PlayerGameState playerGameState = player.GetComponent<PlayerGameState>();
        playerGameState.ServerSwitchToPlayerController();

        NetworkServer.AddPlayerForConnection(conn, player);
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        //Debug.Log("Detect client leave");

        if (conn == null) return;
        //Debug.Log("Conn is valid");

        GameManager gameManager = GetGameManager();
        if (gameManager == null) return;
        //Debug.Log("GameManager is valid");

        TabletopSpawning tabletopSpawning = GetTabletopSpawning();
        if (tabletopSpawning == null) return;
        //Debug.Log("TabletopSpawning is valid");

        PlayerGameState targetPlayerGameState = conn.identity.GetComponent<PlayerGameState>();
        if (targetPlayerGameState == null) return;
        //Debug.Log("PlayerGameState is valid");

        if (!gameManager.hasGameStarted)
        {
            //Debug.Log("Clear Spawn");
            tabletopSpawning.ServerRemovePlayerFromSpawn();
            NetworkServer.DestroyPlayerForConnection(conn);
        }
        else
        {
            if (targetPlayerGameState != null)
            {
                //Debug.Log("Switch To AI Controller");
                targetPlayerGameState.ServerSwitchToAIController();
                NetworkServer.RemovePlayerForConnection(conn);
            }
        }
    }

    private GameManager GetGameManager()
    {
        return FindAnyObjectByType<GameManager>();
    }

    private TabletopSpawning GetTabletopSpawning()
    {
        return FindAnyObjectByType<TabletopSpawning>();
    }

}
