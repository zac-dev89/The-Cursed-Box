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

        if (playerGameState.playerState == PlayerState.Tabletop)
        {
            TabletopGameplay();
        }
        else if (playerGameState.playerState == PlayerState.DungeonHunter)
        {
            DungeonHunterGameplay();
        }
        else if (playerGameState.playerState == PlayerState.DungeonVictim)
        {
            DungeonVictimGameplay();
        }
    }

    #region Table Top Gameplay
    private void TabletopGameplay()
    {

    }
    #endregion



    #region Dungeon Gameplay
    private void DungeonHunterGameplay()
    {

    }
    private void DungeonVictimGameplay()
    {

    }
    #endregion
}
