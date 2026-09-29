using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using Mirror;

public class TestingLobbyManager : NetworkBehaviour
{
    public static TestingLobbyManager Instance;
    public TextMeshProUGUI playerCountText;
    public TextMeshProUGUI waitingForHostText;

    public Button startGameButton;

    [SyncVar(hook = nameof(OnPlayerCountChanged))] public int playerCount;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    private void Start()
    {
        if (NetworkServer.active)
        {
            startGameButton.gameObject.SetActive(true);
            waitingForHostText.gameObject.SetActive(false);
        }
        else
        {
            startGameButton.gameObject.SetActive(false);
            waitingForHostText.gameObject.SetActive(true);
        }
    }

    [Server]
    public void ServerRegisterPlayer()
    {
        playerCount++;
    }

    private void OnPlayerCountChanged(int oldValue, int newValue)
    {
        playerCountText.text = "Players: " + newValue + " / 4";
    }

    public void StartGame()
    {
        if (!NetworkServer.active) return;

        NetworkManager.singleton.ServerChangeScene("Gameplay");
    }
}
