using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    public List<QuestInstance> activeQuests = new();
    public List<QuestInstance> completedQuests = new();

    public event Action<QuestInstance> OnQuestAdded;
    public event Action<QuestInstance> OnQuestUpdated;
    public event Action<QuestInstance> OnQuestCompleted;
    public event Action<QuestInstance> OnQuestReadyToTurnIn;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        GameEvents.OnItemCollected += HandleItemCollected;
    }

    private void OnDisable()
    {
        GameEvents.OnItemCollected -= HandleItemCollected;
    }
    public void AddQuest(QuestData data)
    {
        if (activeQuests.Any(q => q.data.id == data.id))
            return;

        QuestInstance quest = new QuestInstance
        {
            data = data,
            state = QuestState.Active,
            objectives = data.objectives
                .Select(o => new ObjectiveInstance
                {
                    data = o,
                    currentAmount = 0,
                    isCompleted = false
                })
                .ToList(),
            startTime = Time.time
        };

        activeQuests.Add(quest);

        Debug.Log($"Quest added: {data.id}");

        OnQuestAdded?.Invoke(quest);
    }

    public bool CanTurnIn(string questID)
    {
        var quest = activeQuests.FirstOrDefault(q => q.data.id == questID);

        if (quest == null) return false;

        return quest.objectives.All(o => o.isCompleted);
    }

    public void TurnInQuest(string questID)
    {
        var quest = activeQuests.FirstOrDefault(q => q.data.id == questID);

        if (quest == null) return;

        if (!CanTurnIn(questID))
        {
            Debug.Log("Quest not completed yet");
            return;
        }

        GiveReward(quest);

        activeQuests.Remove(quest);
        completedQuests.Add(quest);

        quest.state = QuestState.Completed;

        Debug.Log($"Quest completed: {questID}");

        OnQuestCompleted?.Invoke(quest);
    }
    private void HandleItemCollected(string itemID, int amount)
    {
        foreach (var quest in activeQuests)
        {
            foreach (var obj in quest.objectives)
            {
                if (obj.data.type == ObjectiveType.CollectItem &&
                    obj.data.targetID == itemID &&
                    !obj.isCompleted)
                {
                    obj.currentAmount += amount;

                    if (obj.currentAmount >= obj.data.requiredAmount)
                    {
                        obj.isCompleted = true;
                        Debug.Log($"Objective completed: {itemID}");
                    }

                    OnQuestUpdated?.Invoke(quest);
                }
            }

            CheckQuestCompletion(quest);
        }
    }
    private void CheckQuestCompletion(QuestInstance quest)
    {
        if (quest.objectives.All(o => o.isCompleted))
        {
            Debug.Log($"Quest ready to turn in: {quest.data.id}");

            OnQuestReadyToTurnIn?.Invoke(quest);
        }
    }

    private void GiveReward(QuestInstance quest)
    {
        Debug.Log($"Reward given: {quest.data.rewardGold} gold");
        Debug.Log($"Reward given: {quest.data.reputationReward} rep");

        ReputationManager.Instance.AddReputation(
            quest.data.faction,
            quest.data.reputationReward);

        // позже через сто мильонов лет туту будет:
        // EconomyManager.AddGold(...)
    }

    public void FailQuest(QuestInstance quest)
    {
        activeQuests.Remove(quest);

        ReputationManager.Instance.AddReputation(
            quest.data.faction,
            -quest.data.reputationPenalty);

        Debug.Log("Quest failed loh");
    }

    public bool HasQuest(string questID)
    {
        return activeQuests.Exists(q => q.data.id == questID);
    }

    public bool IsQuestCompleted(string questID)
    {
        return completedQuests.Exists(q => q.data.id == questID);
    }
}