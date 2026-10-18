using UnityEngine;

[System.Serializable]
public struct ItemInformation
{
    public GameObject itemOBJ;
    public GameObject itemUI;
    public string itemDescription;
}


public class ItemData : MonoBehaviour
{
    public static ItemData Instance;

    [Header("Items GameObjects")]
    [Space]
    public ItemInformation DecreaseDeathChanceInfo;
    public ItemInformation DrawBloodFromOpponentInfo;
    public ItemInformation DrawCursedBoxTwiceInfo;
    public ItemInformation ForceDrawEveryoneInfo;
    public ItemInformation ForceDrawInfo;
    public ItemInformation IncraseDeathChanceInfo;
    public ItemInformation PeakInsideInfo;
    public ItemInformation SkipTurnInfo;
    public ItemInformation TakeTwoLivesInfo;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    public ItemInformation GetItemInfoFromName(string name)
    {
        ItemInformation itemInfo = new ItemInformation();

        switch (name)
        {
            case "DecreaseDeathChance":
                itemInfo = DecreaseDeathChanceInfo;
                break;
            case "DrawBloodFromOpponent":
                itemInfo = DrawBloodFromOpponentInfo;
                break;
            case "DrawCursedBoxTwice":
                itemInfo = DrawCursedBoxTwiceInfo;
                break;
            case "ForceDrawEveryone":
                itemInfo = ForceDrawEveryoneInfo;
                break;
            case "ForceDraw":
                itemInfo = ForceDrawInfo;
                break;
            case "IncreaseDeathChance":
                itemInfo = IncraseDeathChanceInfo;
                break;
            case "PeekInside":
                itemInfo = PeakInsideInfo;
                break;
            case "SkipTurn":
                itemInfo = SkipTurnInfo;
                break;
            case "TakeTwoLives":
                itemInfo = TakeTwoLivesInfo;
                break;

        }


        return itemInfo;
    }
}
