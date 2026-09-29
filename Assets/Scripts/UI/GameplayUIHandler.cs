using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;


public class GameplayUIHandler : NetworkBehaviour
{
    [Header("Main References")]
    public GameObject gameplayUICanvas;
    public PlayerGameState playerGameState;

    [Header("Gameplay Panels")]
    public GameObject tabletopPanel;
    public GameObject dungeonPanel;

    public List<GameObject> UI_Panels = new List<GameObject>();

    private void Start()
    {

    }

    public void DisableUI()
    {
        gameplayUICanvas.SetActive(false);
    }
    public void EnableUI()
    {
        gameplayUICanvas.SetActive(true);
    }

    #region Toggle Panels

    public void ToggleTabletopPanel()
    {
        TogglePanel(tabletopPanel);
    }
    public void ToggleDungeonPanel()
    {
        TogglePanel(dungeonPanel);
    }

    private void TogglePanel(GameObject panel)
    {
        foreach (GameObject curr_panel in UI_Panels) 
        {
            if (curr_panel == panel)
            {
                curr_panel.SetActive(true);
            }
            else
            {
                curr_panel.SetActive(false);
            }
        }
    }

    #endregion


}
