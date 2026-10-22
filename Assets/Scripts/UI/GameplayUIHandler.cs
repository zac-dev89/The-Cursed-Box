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
    public PlayerController playerController;


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

    [Header("Mouse Cursors")]
    public Texture2D pointerHand;
    public Texture2D interactHand;
    private Texture2D currentHandCursor;
    private bool isCursorOnInteractableButton;

    public void Start()
    {
        if (playerGameState.playerType != PlayerType.PlayerControlled || !isLocalPlayer) return;
        TogglePointerHandCursor();
    }

    public void DisableUI()
    {
        gameplayUICanvas.SetActive(false);
    }
    public void EnableUI()
    {
        gameplayUICanvas.SetActive(true);
    }

    private void Update()
    {
        if (playerGameState.playerType != PlayerType.PlayerControlled || !isLocalPlayer) return;
        CursorBehaviour();
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

    #region Itembox UI
    public void UpdateBloodLevel(int newBloodLevel)
    {
        bloodAmountText.text = $"{newBloodLevel} blood sacrifices left.";
    }

    #endregion

    #region Cursedbox UI

    #endregion

    public void UpdateCursedBoxItemsUI()
    {
        itemCarousel.UpdateCursedBoxUI(playerGameState.cursedBoxItemsList.ToList<string>());
    }
    public void UpdateItemBoxItemsUI() 
    {
        itemCarousel.UpdateItemBoxUI(playerGameState.itemBoxItemsList.ToList<string>());
    }

    #region Cursors

    public void CursorOnInteractableButton(bool isOnButton)
    {
        isCursorOnInteractableButton = isOnButton;
    }

    private void CursorBehaviour()
    {
        if (playerController.isCursorOnInteractable || isCursorOnInteractableButton)
        {
            ToggleInteractionCursor();
        }
        else
        {
            TogglePointerHandCursor();
        }
    }

    public void TogglePointerHandCursor()
    {
        ChangeCursor(pointerHand);
    }
    public void ToggleInteractionCursor()
    {
        ChangeCursor(interactHand);
    }

    public void ChangeCursor(Texture2D newCursor)
    {
        if (newCursor == currentHandCursor) return;

        Vector2 cursorHostpot = new Vector2(0, 0);
        Cursor.SetCursor(newCursor, cursorHostpot, CursorMode.Auto);
        currentHandCursor = newCursor;
    }
    #endregion
}
