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
    /// What has to survive when the item goes into the pocket and comes out again.
    /// </summary>
    public class SavedState
    {
        public int runeSlots;
        public List<AppliedEnchant> enchants;
    }

    /// <summary>
    /// The state to keep, or null for an item that has no enchantments (a plain item can stack).
    /// </summary>
    public object CaptureState()
    {
        if (enchants.Count == 0)
            return null;

        return new SavedState { runeSlots = runeSlots, enchants = CopyEnchants(enchants) };
    }

    public void RestoreState(object saved)
    {
        var state = saved as SavedState;

        if (state == null)
            return;

        runeSlots = state.runeSlots;
        enchants = CopyEnchants(state.enchants);
        RefreshVisual();
    }

    private static List<AppliedEnchant> CopyEnchants(List<AppliedEnchant> source)
    {
        var copy = new List<AppliedEnchant>();

        foreach (var enchant in source)
            copy.Add(new AppliedEnchant { symbol = enchant.symbol, rune = enchant.rune, quality = enchant.quality });

        return copy;
    }

    /// <summary>
    /// Makes the item glow in the average colour of the symbols on it, so the
    /// player can see the enchantment.
    /// </summary>
    public void RefreshVisual()
    {
        Renderer itemRenderer = GetComponentInChildren<Renderer>();

        if (itemRenderer == null || enchants.Count == 0)
            return;

        Color sum = Color.black;
        int count = 0;

        foreach (var enchant in enchants)
        {
            if (enchant.symbol == null)
                continue;

            sum += enchant.symbol.color;
            count++;
        }

        if (count == 0)
            return;

        Material material = itemRenderer.material;
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", sum / count * 1.5f);
    }

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
