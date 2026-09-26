using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Main Menu Buttons")]
    public Button multiplayerButton;
    public Button singleplayerButton;
    public Button optionsButton;
    public Button quitButton;



    public void StartSingleplayerGame()
    {
        NetworkManager.singleton.maxConnections = 1;
        NetworkManager.singleton.StartHost();
        NetworkManager.singleton.ServerChangeScene("Gameplay");
        GameTypeManager.Instance.InitiateSingleplayerGame();
    }

    public void StartMultiplayerGame()
    {
        GameTypeManager.Instance.InitiateMultiplayerGame();
        SteamLobby.Instance.HostLobby();
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
