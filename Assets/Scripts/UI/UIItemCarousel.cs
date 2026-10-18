using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;


public class UIItemCarousel : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform viewport;
    [SerializeField] private RectTransform content;

    [Header("Settings")]
    [SerializeField] private float scrollDuration = 0.25f;
    public float itemWidth = 300f;
    public float itemSpacing = 10f;

    private int selectedIndex = 0;
    private Coroutine scrollCoroutine;
    private int ItemCount;



    private void Start()
    {
        if (!isLocalPlayer) return;
        InitializeItemCarousel();
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

        if (Input.GetKeyDown(KeyCode.A))
            SelectItem(selectedIndex - 1);

        if (Input.GetKeyDown(KeyCode.D))
            SelectItem(selectedIndex + 1);
    }

    public void InitializeItemCarousel()
    {
        ItemCount = content.childCount;

        if (ItemCount == 0) return;

        for (int i = 0; i < content.childCount; i++)
        {
            Debug.Log($"{i}: {content.GetChild(i).name}");
        }

        selectedIndex = -1;
        SelectItem(0);
    }

    public void UpdateItemCarousel(List<string> items)
    {
        // Stop any ongoing scroll animation.
        if (scrollCoroutine != null)
        {
            StopCoroutine(scrollCoroutine);
            scrollCoroutine = null;
        }

        // Remove all existing items from the Content immediately.
        while (content.childCount > 0)
        {
            Transform child = content.GetChild(0);
            child.SetParent(null);
            Destroy(child.gameObject);
        }

        // Create the new items.
        foreach (string itemName in items)
        {
            var itemData = ItemData.Instance.GetItemInfoFromName(itemName);

            if (itemData.itemUI == null)
                continue;

            Instantiate(itemData.itemUI, content, false);
        }

        // Update the item count and reset the selection.
        ItemCount = content.childCount;
        selectedIndex = -1;

        // Recalculate the layout immediately.
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);

        // Select and center the first item.
        if (ItemCount > 0)
            SelectItem(0);
    }



    public void SelectItem(int index)
    {
        if (ItemCount <= 0)
            return;

        // Clamp the requested index to valid bounds.
        index = Mathf.Clamp(index, 0, ItemCount - 1);

        Debug.Log($"Index: {index} || ItemCount: {ItemCount}");

        selectedIndex = index;

        UpdateItemAppearance();
        CenterOnSelectedItem(true);
    }

    private void CenterOnSelectedItem(bool animate)
    {
        if (ItemCount == 0)
            return;

        float itemWidth = ((RectTransform)content.GetChild(0)).rect.width;
        float spacing = content.GetComponent<HorizontalLayoutGroup>().spacing;

        float itemStep = itemWidth + spacing;

        // Total width occupied by items and spacing
        float totalWidth = (ItemCount * itemWidth) + ((ItemCount - 1) * spacing);

        // Position selected item relative to the content center
        float targetX = (totalWidth / 2f)
                      - (itemWidth / 2f)
                      - (selectedIndex * itemStep);

        Vector2 target = new Vector2(targetX, content.anchoredPosition.y);

        if (scrollCoroutine != null)
        {
            StopCoroutine(scrollCoroutine);
            scrollCoroutine = null;
        }

        if (animate)
            scrollCoroutine = StartCoroutine(AnimateScroll(target));
        else
            content.anchoredPosition = target;
    }

    private void UpdateItemAppearance()
    {
        for (int i = 0; i < ItemCount; i++)
        {
            RectTransform item = content.GetChild(i).GetComponent<RectTransform>();
            if (item != null) item.localScale = Vector3.one * (i == selectedIndex ? 1.05f : 0.9f);

            if (i == selectedIndex - 1 || i == selectedIndex || i == selectedIndex + 1)
            {
                content.GetChild(i).GetComponent<CanvasGroup>().alpha = 1f;
            }
            else
            {
                content.GetChild(i).GetComponent<CanvasGroup>().alpha = 0f;
            }
        }


    }

    private IEnumerator AnimateScroll(Vector2 target)
    {
        Vector2 start = content.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < scrollDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / scrollDuration);
            t = t * t * (3f - 2f * t);

            content.anchoredPosition = Vector2.Lerp(start, target, t);
            yield return null;
        }

        content.anchoredPosition = target;
        scrollCoroutine = null;
    }
}
