using System.Linq;
using UnityEngine;

/// <summary>
/// Connects the alchemy system with the quest system without making either
/// system depend on the other one directly.
/// </summary>
public class QuestCraftingBridge : MonoBehaviour
{
    [SerializeField] private bool autoSpawnReturnNpc = true;
    [SerializeField] private bool enforceTimeLimit = true;

    private QuestManager questManager;

    private void OnEnable()
    {
        GameEvents.OnPotionCrafted += HandlePotionCrafted;
        GameEvents.OnItemEnchanted += HandleItemEnchanted;
    }

    private void Start()
    {
        questManager = QuestManager.Instance;

        if (questManager == null)
            Debug.LogError("QuestCraftingBridge: QuestManager.Instance is null.");
    }

    private void OnDisable()
    {
        GameEvents.OnPotionCrafted -= HandlePotionCrafted;
        GameEvents.OnItemEnchanted -= HandleItemEnchanted;
    }

    private void Update()
    {
        if (questManager == null)
            questManager = QuestManager.Instance;

        if (!enforceTimeLimit || questManager == null)
            return;

        foreach (var quest in questManager.activeQuests.ToList())
        {
            if (quest.data == null || quest.data.timeLimit <= 0f)
                continue;

            // QuestData.timeLimit is measured in real seconds in the current
            // quest prototype (QuestInstance.startTime uses Time.time).
            if (Time.time - quest.startTime < quest.data.timeLimit)
                continue;

            string failedQuestID = quest.data.id;
            quest.state = QuestState.Failed;
            questManager.FailQuest(quest);
            GameEvents.OnQuestFailed?.Invoke(failedQuestID);
        }
    }

    private void HandlePotionCrafted(string potionID, int amount)
    {
        AdvanceObjectives(ObjectiveType.CraftPotion, potionID, amount);
    }

    private void HandleItemEnchanted(string itemID, string symbolID)
    {
        AdvanceObjectives(ObjectiveType.EnchantItem, itemID, 1);
    }

    private void AdvanceObjectives(ObjectiveType type, string targetID, int amount)
    {
        if (questManager == null)
            questManager = QuestManager.Instance;

        if (questManager == null || amount <= 0 || string.IsNullOrWhiteSpace(targetID))
            return;

        foreach (var quest in questManager.activeQuests.ToList())
        {
            if (quest == null || quest.data == null)
                continue;

            bool questUpdated = false;

            foreach (var objective in quest.objectives)
            {
                if (objective.data == null ||
                    objective.data.type != type ||
                    objective.data.targetID != targetID ||
                    objective.isCompleted)
                    continue;

                objective.currentAmount = Mathf.Min(
                    objective.currentAmount + amount,
                    objective.data.requiredAmount);
                objective.isCompleted = objective.currentAmount >= objective.data.requiredAmount;
                questUpdated = true;
            }

            if (!questUpdated)
                continue;

            Debug.Log($"Quest updated: {quest.data.id} -> {targetID}");

            if (quest.readyToTurnIn || !quest.objectives.All(o => o.isCompleted))
                continue;

            quest.readyToTurnIn = true;
            Debug.Log($"Quest ready to turn in: {quest.data.id}");

            // The original NPC leaves after accepting an order. Spawn the
            // return visit when the requested potion has been crafted.
            if (autoSpawnReturnNpc && NPCManager.Instance != null)
                NPCManager.Instance.SpawnNPC(quest.data);
        }
    }
}
