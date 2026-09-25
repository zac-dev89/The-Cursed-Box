using UnityEngine;
using Mirror;
using Unity.VisualScripting;

public class PlayerController : NetworkBehaviour
{
    [Header("Main References")]
    public PlayerGameState playerGameState;
    public Transform FPSCamera;
    public Camera camera;

    [Header("Enabling Gameplay")]
    [SyncVar] public bool playerControllerEnabled;

    private void Start()
    {
        camera.gameObject.SetActive(isLocalPlayer);

    }

    private void Update()
    {
        if (!isLocalPlayer) return;
        if (!playerControllerEnabled) return;

        if (playerGameState.inTabletopGameplay)
        {
            TabletopGameplay();
        }
        else if (playerGameState.inDungeonGameplay)
        {
            DungeonGameplay();
        }
    }

    #region Table Top Gameplay
    private void TabletopGameplay()
    {

    }
    #endregion



    #region Dungeon Gameplay
    private void DungeonGameplay()
    {

    }
    #endregion
}
