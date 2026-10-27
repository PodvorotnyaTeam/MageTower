using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// What an item has to keep when it goes into the pocket and comes out again: the
/// enchantments of an enchanted item, the rarity and the quest id of a potion.
/// A plain item needs nothing and can stack; an item with state never stacks.
/// </summary>
public static class PocketState
{
    public class Payload
    {
        public bool isPotion;
        public string questItemId;
        public string rarity;   // "Rare", "Epic" or null for a common potion
        public object enchant;  // EnchantableItem.SavedState
    }

    /// <summary>The state to keep, or null for a plain item.</summary>
    public static object Capture(GameObject item)
    {
        var payload = new Payload();

        var enchantable = item.GetComponent<EnchantableItem>();

        if (enchantable != null)
            payload.enchant = enchantable.CaptureState();

        // Potions carry their rarity as an active child under "Rarity".
        Transform rarity = item.transform.Find("Rarity");

        if (rarity != null)
        {
            payload.isPotion = true;

            var quest = item.GetComponent<QuestItem>();
            payload.questItemId = quest != null && !string.IsNullOrEmpty(quest.itemID) ? quest.itemID : PrefabName(item);

            foreach (Transform level in rarity)
            {
                if (level.gameObject.activeSelf)
                    payload.rarity = level.name;
            }
        }

        return payload.isPotion || payload.enchant != null ? payload : null;
    }

    public static void Restore(GameObject item, object saved)
    {
        var payload = saved as Payload;

        if (payload == null)
            return;

        if (payload.isPotion)
        {
            var quest = item.GetComponent<QuestItem>();

            if (quest == null)
                quest = item.AddComponent<QuestItem>();

            if (!string.IsNullOrEmpty(payload.questItemId))
                quest.itemID = payload.questItemId;

            Transform rarity = item.transform.Find("Rarity");

            if (rarity != null)
            {
                foreach (Transform level in rarity)
                    level.gameObject.SetActive(level.name == payload.rarity);
            }
        }

        var enchantable = item.GetComponent<EnchantableItem>();

        if (enchantable != null && payload.enchant != null)
            enchantable.RestoreState(payload.enchant);
    }

    /// <summary>The colour an item with this state is tinted in the slot: gold = enchanted, blue = rare, purple = epic.</summary>
    public static Color TintOf(object saved)
    {
        var payload = saved as Payload;

        if (payload == null)
            return Color.white;

        if (payload.enchant != null)
            return new Color(1f, 0.75f, 0.4f);

        if (payload.rarity == "Epic")
            return new Color(0.8f, 0.55f, 1f);

        if (payload.rarity == "Rare")
            return new Color(0.55f, 0.75f, 1f);

        return Color.white;
    }

    private static string PrefabName(GameObject item)
    {
        return Regex.Replace(item.name, @"(\s*\(Clone\)|\s\(\d+\))+$", "").Trim();
    }
}
