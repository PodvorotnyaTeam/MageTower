using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AppliedEnchant
{
    public EnchantSymbolData symbol;
    public RuneData rune;

    [Range(0f, 1f)]
    [Tooltip("How cleanly the symbol was cut. 1 = no stray shadows.")]
    public float quality = 1f;
}

/// <summary>
/// Equipment that has rune slots. The item id comes from the QuestItem on
/// the same object, so enchant quests and hand-in work through it.
/// </summary>
[RequireComponent(typeof(QuestItem))]
public class EnchantableItem : MonoBehaviour
{
    [Min(0)] public int runeSlots = 1;
    public List<AppliedEnchant> enchants = new();

    private QuestItem questItem;

    public string ItemID
    {
        get
        {
            if (questItem == null)
                questItem = GetComponent<QuestItem>();

            return questItem.itemID;
        }
    }

    public int FreeSlots => runeSlots - enchants.Count;

    /// <summary>
    /// The blacksmith adds a slot to gear that has none left.
    /// </summary>
    public bool TryAddRuneSlot(int maxSlots)
    {
        if (runeSlots >= maxSlots)
            return false;

        runeSlots++;
        return true;
    }
}
