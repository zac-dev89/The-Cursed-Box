using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;


public class CustomNetworkManager : NetworkManager
{
    [Header("Player Types")]
    public GameObject gamePlayer;


    public override void OnServerChangeScene(string newSceneName)
    {
        if (newSceneName == "Gameplay")
        {
            playerPrefab = gamePlayer;
            onlineScene = newSceneName;
        }
        else
        {
            onlineScene = null;
        }

        base.OnServerChangeScene(newSceneName);
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
        playerGameState.UpdateCameraSetUp();

        NetworkServer.AddPlayerForConnection(conn, player);

        GameManager.Instance.players.Add(conn.identity);
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {

        if (conn.identity == null) return;

        if (GameManager.Instance == null) return;

        if (TabletopSpawning.Instance == null) return;

        PlayerGameState targetPlayerGameState = conn.identity.GetComponent<PlayerGameState>();
        if (targetPlayerGameState == null) return;

        GameManager.Instance.players.Remove(conn.identity);

        if (!GameManager.Instance.hasGameStarted)
        {
            //Debug.Log("Clear Spawn");
            TabletopSpawning.Instance.ServerRemovePlayerFromSpawn();
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

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();

        GameTypeManager.Instance.ExitMatch();
        SceneManager.LoadScene("MainMenu");

    }



}
