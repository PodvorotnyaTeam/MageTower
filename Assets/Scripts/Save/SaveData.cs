using System;
using System.Collections.Generic;

// The shape of save.json. Everything is plain data, JsonUtility writes it as it is.

[Serializable]
public class SaveData
{
    public int version = 1;
    public string savedAtUtc;

    public int day;
    public int hour;
    public int minute;

    public List<ReputationSave> reputation = new List<ReputationSave>();
    public List<QuestSave> quests = new List<QuestSave>();

    public List<SlotSave> hotbar = new List<SlotSave>();
    public List<SlotSave> inventory = new List<SlotSave>();

    public float[] playerPosition;
}

[Serializable]
public class ReputationSave
{
    public string factionId;
    public int value;
}

[Serializable]
public class QuestSave
{
    public string id;
    public string state;            // "Active" or "Completed"
    public float elapsed;           // seconds since the quest was taken (for the time limit)
    public bool readyToTurnIn;
    public List<ObjectiveSave> objectives = new List<ObjectiveSave>();
}

[Serializable]
public class ObjectiveSave
{
    public string id;
    public int amount;
    public bool done;
}

[Serializable]
public class SlotSave
{
    public int itemId;
    public int count;
    public PayloadSave payload;     // enchantments of a sword, rarity of a potion; null for a plain item
}

[Serializable]
public class PayloadSave
{
    public bool isPotion;
    public string questItemId;
    public string rarity;

    public bool hasEnchant;
    public int runeSlots;
    public List<EnchantSave> enchants = new List<EnchantSave>();
}

[Serializable]
public class EnchantSave
{
    public string symbolId;
    public string runeId;
    public float quality;
}
