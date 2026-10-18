using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;
using System.Linq;


public class GameplayUIHandler : NetworkBehaviour
{
    [Header("Main References")]
    public GameObject gameplayUICanvas;
    public PlayerGameState playerGameState;


    [Header("Gameplay Panels")]
    public GameObject waitingPanel;
    public GameObject tabletopPanel;
    public GameObject dungeonPanel;
    public List<GameObject> gameplayUI = new List<GameObject>();


    [Header("Tabletop Panels")]
    public GameObject nonTurnTabletopPanel;
    public GameObject itemBoxPanel;
    public GameObject cursedBoxPanel;
    public List<GameObject> tableTopUIPanels = new List<GameObject>();

    [Header("Itembox UI")]

    public GameObject sacrificeBloodPanel;
    public GameObject addBloodPackPanel;
    public List<GameObject> itemboxUIPanels = new List<GameObject>();

    [Space]
    public TextMeshProUGUI bloodAmountText;
    public TextMeshProUGUI bloodPackAmountText;

    [Header("Cursedbox UI")]
    public UIItemCarousel itemCarousel;
    public GameObject addItemsPanel;
    public GameObject drawCursedCardPanel;
    public List<GameObject> cursedboxUIPanels = new List<GameObject>();



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
    // Main Panels
    public void ToggleWaitingPanel()
    {
        TogglePanel(waitingPanel, gameplayUI);
    }
    public void ToggleTabletopPanel()
    {
        TogglePanel(tabletopPanel, gameplayUI);
    }
    public void ToggleDungeonPanel()
    {
        TogglePanel(dungeonPanel, gameplayUI);
    }



    // Tabletop Panels
    public void ToggleNonTabletopTurnPanel()
    {
        TogglePanel(nonTurnTabletopPanel, tableTopUIPanels);
    }
    public void ToggleItemBoxPanel()
    {
        TogglePanel(itemBoxPanel, tableTopUIPanels);
    }
    public void ToggleCursedBoxPanel()
    {
        TogglePanel(cursedBoxPanel, tableTopUIPanels);
    }



    // Itembox Panels
    public void ToggleSacrificeBloodPanel()
    {
        TogglePanel(sacrificeBloodPanel, itemboxUIPanels);
    }
    public void ToggleAddBloodPackPanel()
    {
        TogglePanel(addBloodPackPanel, itemboxUIPanels);
    }



    // Cursedbox Panels
    public void ToggleAddItemsPanel()
    {
        TogglePanel(addItemsPanel, cursedboxUIPanels);
    }
    public void ToggleDrawCursedCardPanel()
    {
        TogglePanel(drawCursedCardPanel, cursedboxUIPanels);
    }




    private void TogglePanel(GameObject panel, List<GameObject> panels)
    {
        foreach (GameObject curr_panel in panels) 
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
    public void HideAllPanels(List<GameObject> panels)
    {
        foreach (GameObject curr_panel in panels)
        {
            curr_panel.SetActive(false);
        }
    }

    #endregion


    #region Non Turn UI

    #endregion

    public void UpdateItemsUI()
    {
        List<string> items = playerGameState.specialItemsList.ToList<string>();

        UpdateBloodPackUI(items);
        itemCarousel.UpdateItemCarousel(items);

    }


    #region Itembox UI
    public void UpdateBloodLevel(int newBloodLevel)
    {
        bloodAmountText.text = $"{newBloodLevel} blood sacrifices left.";
    }

    private void UpdateBloodPackUI(List<string> items)
    {
        int bloodPackCount = 0;

        foreach (string curr_item in items)
        {
            if (curr_item == "BloodPack") bloodPackCount++;
        }

        bloodPackAmountText.text = $"{bloodPackCount}";
    }

    #endregion


    #region Cursedbox UI

    #endregion

}
