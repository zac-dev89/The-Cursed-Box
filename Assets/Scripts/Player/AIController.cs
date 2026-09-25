using Mirror;
using UnityEngine;

public class AIController : NetworkBehaviour
{
    [Header("Main References")]
    public PlayerGameState playerGameState;

    [Header("Enabling Gameplay")]
    [SyncVar] public bool AIControllerEnabled;

    private void Update()
    {
        if (!NetworkServer.active) return;
        if (!AIControllerEnabled) return;

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
