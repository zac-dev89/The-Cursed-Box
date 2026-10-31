using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Mirror;

public class DungeonSpawning : NetworkBehaviour
{
    public static DungeonSpawning Instance;

    [Header("Dungeon Spawn PTs")]
    public List<Transform> spawnPoints;



    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }


    public List<Transform> GetDungeonSpawnPTs()
    {
        List<Transform> newPlayerSpawnPoints = new List<Transform>();
        List<Transform> tempSpawnPoints = new List<Transform>(spawnPoints);

        int dungeonVictimIndex = Random.Range(0, spawnPoints.Count);

        newPlayerSpawnPoints.Add(tempSpawnPoints[dungeonVictimIndex]);
        tempSpawnPoints.RemoveAt(dungeonVictimIndex);

        for (int i = 0; i < PlayerManager.Instance.players.Count - 1; i++)
        {
            int dungeonHunterIndex = Random.Range(0, tempSpawnPoints.Count);
            newPlayerSpawnPoints.Add(tempSpawnPoints[dungeonHunterIndex]);
            tempSpawnPoints.RemoveAt(dungeonHunterIndex);
        }
        
        return newPlayerSpawnPoints;
    }
   
}
