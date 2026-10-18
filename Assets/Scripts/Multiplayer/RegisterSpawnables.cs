using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Mirror;

public class RegisterSpawnables : MonoBehaviour
{
    public List<GameObject> spawnableItems;

    private void Start()
    {
        InitializeSpawnables();
    }
    private void InitializeSpawnables()
    {
        for (int i = 0; i < spawnableItems.Count; i++)
        {
            NetworkManager.singleton.spawnPrefabs.Add(spawnableItems[i]);
        }
    }
}
