using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Takes the items a quest asks for away from the player when the quest
/// is turned in.
/// </summary>
public static class QuestItemHandIn
{
    /// <summary>
    /// Finds every item the quest needs within radius of origin (anywhere if
    /// origin is null). If all of them are present, destroys them and returns
    /// true. If anything is missing, destroys nothing and returns false.
    /// </summary>
    public static bool TryConsume(QuestInstance quest, Transform origin, float radius)
    {
        var allItems = Object.FindObjectsByType<QuestItem>(FindObjectsSortMode.None);
        var picked = new List<QuestItem>();

        foreach (var objective in quest.objectives)
        {
            if (objective.data == null || !NeedsItem(objective.data.type))
                continue;

            var matches = allItems
                .Where(item => item != null &&
                               !picked.Contains(item) &&
                               item.itemID == objective.data.targetID &&
                               MeetsRequirement(item, objective.data) &&
                               IsInRange(item.transform, origin, radius))
                .OrderBy(item => Distance(item.transform, origin))
                .Take(objective.data.requiredAmount)
                .ToList();

            if (matches.Count < objective.data.requiredAmount)
            {
                Debug.Log($"QuestItemHandIn: not enough {objective.data.targetID} for {quest.data.id} " +
                          $"({matches.Count}/{objective.data.requiredAmount})");
                return false;
            }

            picked.AddRange(matches);
        }

        foreach (var item in picked)
            Object.Destroy(item.gameObject);

        return true;
    }

    private static bool NeedsItem(ObjectiveType type)
    {
        return type == ObjectiveType.CraftPotion || type == ObjectiveType.EnchantItem;
    }

    // An enchanted item is only accepted if it really has the enchantment the quest asks for.
    private static bool MeetsRequirement(QuestItem item, QuestObjectiveData objective)
    {
        if (objective.type != ObjectiveType.EnchantItem)
            return true;

        return EnchantingService.ItemHasEffect(item.GetComponent<EnchantableItem>(), objective.requiredEffect);
    }

    private static bool IsInRange(Transform item, Transform origin, float radius)
    {
        return origin == null || Distance(item, origin) <= radius;
    }

    private static float Distance(Transform item, Transform origin)
    {
        return origin == null ? 0f : Vector3.Distance(item.position, origin.position);
    }
}
