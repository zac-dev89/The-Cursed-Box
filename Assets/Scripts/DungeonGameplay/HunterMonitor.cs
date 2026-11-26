using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class HunterMonitor : MonoBehaviour
{
    public List<GameObject> monitorMeshes;
    public RawImage hunterFeed;

    private void Start()
    {
        ShouldShowMonitor(false);
    }

    public void ShouldShowMonitor(bool show)
    {
        for (int i = 0; i < monitorMeshes.Count; i++)
        {
            monitorMeshes[i].SetActive(show);
        }
    }

    public void DisplayHunterFeed(RenderTexture hunter_RT)
    {
        hunterFeed.texture = hunter_RT;
    }
}
