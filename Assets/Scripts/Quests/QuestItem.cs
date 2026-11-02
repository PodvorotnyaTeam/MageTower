using UnityEngine;

/// <summary>
/// Marks a physical object that can be handed over to an NPC for a quest
/// (a brewed potion now, an enchanted item later). itemID matches the
/// targetID of the quest objective.
/// </summary>
public class QuestItem : MonoBehaviour
{
    public string itemID;
}
