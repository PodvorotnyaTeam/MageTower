using UnityEngine;

[System.Serializable]
public class QuestObjectiveData
{
    public string id;
    public ObjectiveType type;
    public string targetID;
    public int requiredAmount;

    [Tooltip("EnchantItem only: effect the item must have (fire, ice, steam...). Empty = any enchantment.")]
    public string requiredEffect;
}
