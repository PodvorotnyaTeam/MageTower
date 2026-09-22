using System.Linq;
using TMPro;
using UnityEngine;

/// <summary>
/// Temporary prototype UI for the current NPC order.
/// Attach this component to any Canvas object and assign a TMP text field.
/// </summary>
public class QuestTextUI : MonoBehaviour
{
    [SerializeField] private TMP_Text questText;
    [SerializeField] private bool showWhenThereIsNoActiveQuest = true;
    [SerializeField] private float refreshInterval = 0.2f;

    private float nextRefreshTime;
    private string statusMessage = string.Empty;

    private void Start()
    {
        GameEvents.OnNPCInteracted += HandleQuestChanged;
        GameEvents.OnQuestTurnedIn += HandleQuestTurnedIn;
        GameEvents.OnQuestFailed += HandleQuestFailed;
        GameEvents.OnPotionCrafted += HandlePotionCrafted;

        Refresh();
    }

    private void OnDestroy()
    {
        GameEvents.OnNPCInteracted -= HandleQuestChanged;
        GameEvents.OnQuestTurnedIn -= HandleQuestTurnedIn;
        GameEvents.OnQuestFailed -= HandleQuestFailed;
        GameEvents.OnPotionCrafted -= HandlePotionCrafted;
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefreshTime)
            return;

        nextRefreshTime = Time.unscaledTime + Mathf.Max(0.05f, refreshInterval);
        Refresh();
    }

    private void HandleQuestChanged(string questID)
    {
        statusMessage = string.Empty;
        Refresh();
    }

    private void HandleQuestTurnedIn(string questID)
    {
        statusMessage = "\u0417\u0430\u043a\u0430\u0437 \u0441\u0434\u0430\u043d";
        Refresh();
    }

    private void HandleQuestFailed(string questID)
    {
        statusMessage = "\u0417\u0430\u043a\u0430\u0437 \u043f\u0440\u043e\u0441\u0440\u043e\u0447\u0435\u043d";
        Refresh();
    }

    private void HandlePotionCrafted(string potionID, int amount)
    {
        statusMessage = $"\u0421\u043e\u0437\u0434\u0430\u043d\u043e: {potionID}";
        Refresh();
    }

    private void Refresh()
    {
        if (questText == null)
            return;

        QuestManager questManager = QuestManager.Instance;
        QuestInstance quest = questManager == null
            ? null
            : questManager.activeQuests.FirstOrDefault(q =>
                q != null && q.data != null && q.state == QuestState.Active);

        if (quest == null)
        {
            questText.text = showWhenThereIsNoActiveQuest
                ? "\u0410\u043a\u0442\u0438\u0432\u043d\u044b\u0445 \u0437\u0430\u043a\u0430\u0437\u043e\u0432 \u043d\u0435\u0442"
                : string.Empty;

            if (!string.IsNullOrEmpty(statusMessage) && showWhenThereIsNoActiveQuest)
                questText.text += $"\n\n{statusMessage}";

            return;
        }

        string text = $"{quest.data.title}\n";

        if (!string.IsNullOrWhiteSpace(quest.data.description))
            text += $"{quest.data.description}\n";

        foreach (ObjectiveInstance objective in quest.objectives)
        {
            if (objective == null || objective.data == null)
                continue;

            string objectiveName = GetObjectiveName(objective.data.type);
            string marker = objective.isCompleted ? "[x]" : "[ ]";

            text += $"\n{marker} {objectiveName}: " +
                    $"{objective.currentAmount}/{objective.data.requiredAmount}";

            if (!string.IsNullOrWhiteSpace(objective.data.targetID))
                text += $" ({objective.data.targetID})";
        }

        if (quest.data.timeLimit > 0f)
        {
            float remaining = Mathf.Max(
                0f,
                quest.data.timeLimit - (Time.time - quest.startTime));

            text += $"\n\n\u041e\u0441\u0442\u0430\u043b\u043e\u0441\u044c: {remaining:0.0} \u0441\u0435\u043a.";
        }

        if (!string.IsNullOrEmpty(statusMessage))
            text += $"\n\n{statusMessage}";

        questText.text = text;
    }

    private string GetObjectiveName(ObjectiveType objectiveType)
    {
        switch (objectiveType)
        {
            case ObjectiveType.CraftPotion:
                return "\u0421\u0432\u0430\u0440\u0438\u0442\u044c \u0437\u0435\u043b\u044c\u0435";
            case ObjectiveType.CollectItem:
                return "\u0421\u043e\u0431\u0440\u0430\u0442\u044c \u043f\u0440\u0435\u0434\u043c\u0435\u0442\u044b";
            case ObjectiveType.TalkToNPC:
                return "\u041f\u043e\u0433\u043e\u0432\u043e\u0440\u0438\u0442\u044c \u0441 NPC";
            case ObjectiveType.GoToLocation:
                return "\u0414\u043e\u0431\u0440\u0430\u0442\u044c\u0441\u044f \u0434\u043e \u043c\u0435\u0441\u0442\u0430";
            default:
                return objectiveType.ToString();
        }
    }
}
