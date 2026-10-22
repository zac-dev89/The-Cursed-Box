using UnityEngine;
using UnityEngine.UI;
using Mirror;
using System.Collections;

public class UIItem : MonoBehaviour
{
    public NetworkIdentity playerOwner;

    private GameplayUIHandler gameplayUIHandler;
    private PlayerGameState playerGameState;
    private PlayerController playerController;
    private UIItemCarousel itemCarousel;

    [Space]

    public CanvasGroup itemCG;
    public Button itemButton;

    [Header("Item Information")]
    public string itemName;
    public int inventoryIndex;
    private bool isSelected;
    private bool canUseItem;

    [Header("Item UI")]
    public GameObject crossedOutUI;

    private void Start()
    {
        StartCoroutine(CheckItemUsableBehaviour());
    }

    public void SetPlayerOwner(NetworkIdentity playerRef)
    {
        playerOwner = playerRef;

        gameplayUIHandler = playerOwner.GetComponent<GameplayUIHandler>();
        playerGameState = playerOwner.GetComponent<PlayerGameState>();
        playerController = playerOwner.GetComponent<PlayerController>();
        itemCarousel = playerController.GetComponent<UIItemCarousel>();
    }

    public void SetPlayerHandCursor(bool interactable)
    {
        if (GetComponent<Button>().enabled)
        {
            gameplayUIHandler.CursorOnInteractableButton(interactable);
        }
        else
        {
            gameplayUIHandler.CursorOnInteractableButton(false);
        }
    }

    public void TryUseItem()
    {
        playerGameState.TryUseItem(itemName);
    }

    public IEnumerator CheckItemUsableBehaviour()
    {
        while (true)
        {
            yield return new WaitForSeconds(.5f);

            UpdateItemUsable();
        }
    }

    public void UpdateItemUsable()
    {
        if (GameManager.Instance.cursedBoxStation.CanInsertItem(playerOwner, itemName))
        {
            canUseItem = true;
            crossedOutUI.SetActive(false);
            if (isSelected) itemButton.enabled = true;
        }
        else
        {
            canUseItem = false;
            crossedOutUI.SetActive(true);
            itemButton.enabled = false;
        }
    }

    public void UpdateItemAppearance(int selectedIndex)
    {
        if (inventoryIndex == selectedIndex)
        {
            isSelected = true;
            itemCG.alpha = 1f;
            if (canUseItem) itemButton.enabled = true;
        }
        else if (inventoryIndex == selectedIndex - 1 || inventoryIndex == selectedIndex + 1)
        {
            isSelected = false;
            itemCG.alpha = .1f;
            itemButton.enabled = false;
        }
        else
        {
            isSelected = false;
            itemCG.alpha = 0f;
            itemButton.enabled = false;
        }
    }

}
