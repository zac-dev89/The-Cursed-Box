using UnityEngine;
using Mirror;

public class TabletopSpawning : NetworkBehaviour
{
    public static TabletopSpawning Instance;

    [Header("Tabletop Spawn Points")]
    public Transform[] spawnPoints;
    public int playerSpawnIndex = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }


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
