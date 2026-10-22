using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class UIItemCarousel : NetworkBehaviour
{
    [Header("Main References")]
    public PlayerGameState PGS;

    [Header("Cursed Box UI Carousel")]
    [Space]
    public RectTransform CB_Viewport;
    public RectTransform CB_Content;
    private Coroutine CB_ScrollCoroutine;
    public int CB_SelectedIndex;
    public int CB_ItemCount;

    [Header("Item Box UI Carousel")]
    [Space]
    public RectTransform IB_Viewport;
    public RectTransform IB_Content;
    private Coroutine IB_ScrollCoroutine;
    public int IB_SelectedIndex;
    public int IB_ItemCount;


    [Header("Settings")]
    [SerializeField] private float scrollDuration = 0.25f;
    public float itemWidth = 300f;
    public float itemSpacing = 10f;



    private void Start()
    {
        if (!isLocalPlayer) return;
        InitializeCarousel(ref IB_Content, ref IB_ItemCount, ref IB_SelectedIndex, ref IB_ScrollCoroutine);
        InitializeCarousel(ref CB_Content, ref CB_ItemCount, ref CB_SelectedIndex, ref CB_ScrollCoroutine);
    }

    private void Update()
    {
        if (!isLocalPlayer) return;
        
        // Item Box Carousel
        if (PGS.playerTabletopTurn.playerTabletopState == PlayerTabletopState.Itembox && PGS.playerTabletopTurn.inItemBoxInsertMode)
        {
            if (Input.GetKeyDown(Controls.selectLeftItem))
            {
                SelectItem(IB_SelectedIndex - 1, ref IB_Content, ref IB_ItemCount, ref IB_SelectedIndex, ref IB_ScrollCoroutine);
            }
            else if (Input.GetKeyDown(Controls.selectRightItem))
            {
                SelectItem(IB_SelectedIndex + 1, ref IB_Content, ref IB_ItemCount, ref IB_SelectedIndex, ref IB_ScrollCoroutine);
            }
        }

        // Cursed Box Carousel
        else if (PGS.playerTabletopTurn.playerTabletopState == PlayerTabletopState.Cursedbox && PGS.playerTabletopTurn.inCursedBoxInsertMode)
        {
            if (Input.GetKeyDown(Controls.selectLeftItem))
            {
                SelectItem(CB_SelectedIndex - 1, ref CB_Content, ref CB_ItemCount, ref CB_SelectedIndex, ref IB_ScrollCoroutine);
            }
            else if (Input.GetKeyDown(Controls.selectRightItem))
            {
                SelectItem(CB_SelectedIndex + 1, ref CB_Content, ref CB_ItemCount, ref CB_SelectedIndex, ref IB_ScrollCoroutine);
            }
        }
    }

    public void UpdateCursedBoxUI(List<string> items)
    {
        UpdateItemCarousel(items, ref CB_Content, ref CB_ItemCount, ref CB_SelectedIndex, ref CB_ScrollCoroutine);
    }
    public void UpdateItemBoxUI(List<string> items)
    {
        UpdateItemCarousel(items, ref IB_Content, ref IB_ItemCount, ref IB_SelectedIndex, ref IB_ScrollCoroutine);
    }



    public void InitializeCarousel(ref RectTransform carouselContent, ref int itemCount, ref int selectedIndex, ref Coroutine scrollCoroutine)
    {
        itemCount = carouselContent.childCount;

        if (itemCount == 0) return;

        SelectItem(0, ref carouselContent, ref itemCount, ref selectedIndex, ref scrollCoroutine);
    }

    private void UpdateItemCarousel(List<string> items, ref RectTransform carouselContent, ref int itemCount, ref int selectedIndex, ref Coroutine scrollCoroutine)
    {
        // Stop any ongoing scroll animation.
        if (scrollCoroutine != null)
        {
            StopCoroutine(scrollCoroutine);
            scrollCoroutine = null;
        }

        // Remove all existing items from the Content immediately.
        while (carouselContent.childCount > 0)
        {
            Transform child = carouselContent.GetChild(0);
            child.SetParent(null);
            Destroy(child.gameObject);
        }

        // Create the new items.
        for (int i = 0; i < items.Count; i++)
        {
            ItemInformation itemData = ItemData.Instance.GetItemInfoFromName(items[i]);

            if (itemData.itemUI == null)
            {
                Debug.Log($"Couldn't load item: {items[i]}");
                continue;
            }

            Debug.Log(carouselContent.name);
            GameObject tempItem = Instantiate(itemData.itemUI, carouselContent, false);
            UIItem tempItemUI = tempItem.GetComponent<UIItem>();

            tempItemUI.SetPlayerOwner(netIdentity);
            tempItemUI.inventoryIndex = i;
        }

        // Update the item count and reset the selection.
        itemCount = carouselContent.childCount;
        selectedIndex = -1;

        // Recalculate the layout immediately.
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(carouselContent);

        // Select and center the first item.
        if (itemCount > 0)
            SelectItem(0, ref carouselContent, ref itemCount, ref selectedIndex, ref scrollCoroutine);
    }



    public void SelectItem(int index, ref RectTransform carouselContent, ref int itemCount, ref int selectedIndex, ref Coroutine scrollCoroutine)
    {
        if (itemCount <= 0)
            return;

        // Clamp the requested index to valid bounds.
        index = Mathf.Clamp(index, 0, itemCount - 1);
        selectedIndex = index;

        UpdateItemAppearance(ref carouselContent, ref itemCount, ref selectedIndex);
        CenterOnSelectedItem(true, ref carouselContent, ref itemCount, ref selectedIndex, ref scrollCoroutine);
    }

    private void CenterOnSelectedItem(bool animate, ref RectTransform carouselContent, ref int itemCount, ref int selectedIndex, ref Coroutine scrollCoroutine)
    {
        if (itemCount == 0)
            return;

        float itemWidth = ((RectTransform)carouselContent.GetChild(0)).rect.width;
        float spacing = carouselContent.GetComponent<HorizontalLayoutGroup>().spacing;

        float itemStep = itemWidth + spacing;

        // Total width occupied by items and spacing
        float totalWidth = (itemCount * itemWidth) + ((itemCount - 1) * spacing);

        // Position selected item relative to the content center
        float targetX = (totalWidth / 2f)
                      - (itemWidth / 2f)
                      - (selectedIndex * itemStep);



        Vector2 target = new Vector2(targetX, carouselContent.anchoredPosition.y);

        if (scrollCoroutine != null)
        {
            StopCoroutine(scrollCoroutine);
            scrollCoroutine = null;
        }

        if (animate)
            scrollCoroutine = StartCoroutine(AnimateScroll(target, carouselContent));
        else
            carouselContent.anchoredPosition = target;
    }

    private void UpdateItemAppearance(ref RectTransform carouselContent, ref int itemCount, ref int selectedIndex)
    {
        for (int i = 0; i < itemCount; i++)
        {
            RectTransform item = carouselContent.GetChild(i).GetComponent<RectTransform>();
            if (item != null) item.localScale = Vector3.one * (i == selectedIndex ? 1.1f : 0.6f);

            UIItem itemUI = item.GetComponent<UIItem>();

            itemUI.UpdateItemUsable();
            itemUI.UpdateItemAppearance(selectedIndex);
        }

    }

    private IEnumerator AnimateScroll(Vector2 target, RectTransform carouselContent)
    {
        Vector2 start = carouselContent.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < scrollDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / scrollDuration);
            t = t * t * (3f - 2f * t);

            carouselContent.anchoredPosition = Vector2.Lerp(start, target, t);
            yield return null;
        }

        carouselContent.anchoredPosition = target;
    }
}
