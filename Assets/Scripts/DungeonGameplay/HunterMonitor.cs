using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HunterMonitor : MonoBehaviour
{
    public List<GameObject> monitorMeshes;

    public void ShouldShowMonitor(bool show)
    {
        for (int i = 0; i < monitorMeshes.Count; i++)
        {
            monitorMeshes[i].SetActive(show);
        }
    }
}
