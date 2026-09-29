using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerManager : NetworkBehaviour
{
    public static PlayerManager Instance;
    public List<NetworkIdentity> players = new List<NetworkIdentity>();
    [SyncVar] public bool allPlayersReady;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    #region Initializing Players
    [Server]
    public void ServerRegister(NetworkIdentity identity)
    {
        if (!NetworkServer.active) return;

        if (!players.Contains(identity))
            players.Add(identity);
    }
    [Server]
    public void ServerUnregister(NetworkIdentity identity)
    {
        if (!NetworkServer.active) return;

        players.Remove(identity);
    }

    // =========================
    // READY CHECK
    // =========================

    [Server]
    public void ServerCheckAllPlayersReady()
    {
        if (!NetworkServer.active) return;
        if (players.Count != NetworkServer.connections.Count) return;

        foreach (NetworkIdentity id in players)
        {
            if (id == null) return;

            PlayerRegistration player = id.GetComponent<PlayerRegistration>();
            if (player == null || !player.isReady)
                return;
        }

        allPlayersReady = true;
        Debug.Log("Everyone Ready, Start Game");
        ServerReadyToStartGame();
    }

    [Server]
    public void ServerReadyToStartGame()
    {
        if (GameController.Instance.isMultiplayerGame)
        {
            GameManager.Instance.ServerStartMultiplayerGame();
        }
        else if (GameController.Instance.isSingleplayerGame)
        {
            GameManager.Instance.ServerStartSingleplayerGame();
        }

    }

    #endregion
}
