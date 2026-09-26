using UnityEngine;

public class GameTypeManager : MonoBehaviour
{
    public static GameTypeManager Instance;

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

    public void ExitMatch()
    {
        isSingleplayerGame = false;
        isMultiplayerGame = false;
    }
}
