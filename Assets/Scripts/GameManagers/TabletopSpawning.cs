using UnityEngine;
using Mirror;

public class TabletopSpawning : NetworkBehaviour
{
    [Header("Tabletop Spawn Points")]
    public Transform[] spawnPoints;
    public int playerSpawnIndex = 0;

    [Server]
    public Transform ServerAddPlayerFromSpawn()
    {
        Transform spawnPT = spawnPoints[playerSpawnIndex];
        playerSpawnIndex++;
        return spawnPT;
    }

    [Server]
    public void ServerRemovePlayerFromSpawn()
    {
        playerSpawnIndex--;
    }
}
