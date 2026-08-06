using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "QuestData", menuName = "Quests/Quest")]
public class QuestData : ScriptableObject
{
    public string id;
    public string title;
    public string description;

    public QuestType type;

    public List<QuestObjectiveData> objectives = new();

    public FactionData faction;

    public int reputationReward;
    public int reputationPenalty;
    public int rewardGold;

    public float timeLimit;

    public List<string> requiredFlags;
}
