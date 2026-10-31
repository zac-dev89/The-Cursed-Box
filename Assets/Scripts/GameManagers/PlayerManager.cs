using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerManager : NetworkBehaviour
{
    public static PlayerManager Instance;
    public readonly SyncList<NetworkIdentity> players = new SyncList<NetworkIdentity>();
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
        if (players.Contains(identity)) return;

        if (identity.connectionToClient == NetworkServer.localConnection)
        {
            players.Insert(0, identity);
        }
        else
        {
            players.Add(identity);
        }
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
