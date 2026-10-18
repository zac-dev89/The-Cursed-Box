using Mirror;
using UnityEngine;

public class DungeonSpawning : NetworkBehaviour
{
    public static DungeonSpawning Instance;

    [Header("Dungeon Spawn PTs")]
    public Transform[] spawnPoints;



    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }


   
}
