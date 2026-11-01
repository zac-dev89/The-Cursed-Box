using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Mirror;

public class TabletopSpawning : NetworkBehaviour
{
    public static TabletopSpawning Instance;

    [Header("Tabletop Spawn Points")]
    public List<Transform> seatsPoints;
    private int nonHostPlayerSpawnIndex = 1;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }


    [Server]
    public void ServerAddPlayerForSpawn(NetworkIdentity player, bool isHost)
    {
        PlayerGameState PGS = player.GetComponent<PlayerGameState>();

        if (isHost)
        {
            PGS.tabletopSeatIndex = 0;
        }
        else
        {
            PGS.tabletopSeatIndex = nonHostPlayerSpawnIndex;
            nonHostPlayerSpawnIndex++;
        }
    }
    public Transform GetSeatFromIndex(int index)
    {
        return seatsPoints[index];
    }

}
