using Mirror;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public static GameController Instance;

    public bool isSingleplayerGame;
    public bool isMultiplayerGame;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        DontDestroyOnLoad(gameObject);
    }

    public void InitiateSingleplayerGame()
    {
        isSingleplayerGame = true;
        isMultiplayerGame = false;
    }

    public void InitiateMultiplayerGame()
    {
        isSingleplayerGame = false;
        isMultiplayerGame = true;
    }

    public void ResetGameTypes()
    {
        isSingleplayerGame = false;
        isMultiplayerGame = false;
    }

    public void ExitMatch()
    {
        ResetGameTypes();

        if (NetworkServer.active)
        {
            NetworkManager.singleton.StopHost();
        }
        else if (NetworkClient.active)
        {
            NetworkManager.singleton.StopClient();
        }

        if (SteamLobby.Instance != null) SteamLobby.Instance.LeaveSteamLobby();
    }
}
