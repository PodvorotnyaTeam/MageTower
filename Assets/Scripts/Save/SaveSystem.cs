using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Saves and loads the player's progress: game time, reputation, quests (with the progress of their
/// goals and the time left), the hotbar and the inventory (with enchantments and potion rarity) and
/// where the player stands.
///
/// Not saved: loose items lying around and what is in the cauldron; they start as in a new scene.
///
/// The game is saved when the pause menu opens, when the player goes to the menu, when an order is
/// handed in or fails, and when the game quits. "Play" in the menu continues the save if there is one,
/// "New game" deletes it.
/// </summary>
public static class SaveSystem
{
    private const int Version = 1;
    private static bool hooked;

    /// <summary>True from the moment the player chose to continue until the save has been applied to the new scene.</summary>
    public static bool PendingLoad { get; private set; }

    public static string FilePath => Path.Combine(Application.persistentDataPath, "save.json");
    public static bool HasSave => File.Exists(FilePath);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Hook()
    {
        if (hooked)
            return;

        hooked = true;

        SceneManager.sceneLoaded += OnSceneLoaded;
        Application.quitting += () => Save();
        GameEvents.OnQuestTurnedIn += questId => Save();
        GameEvents.OnQuestFailed += questId => Save();
    }

    /// <summary>Called by the menu buttons right before the game scene is loaded.</summary>
    public static void PrepareToStart(bool continueSave)
    {
        if (!continueSave)
            DeleteSave();

        PendingLoad = continueSave && HasSave;
    }

    public static void DeleteSave()
    {
        try
        {
            if (File.Exists(FilePath))
                File.Delete(FilePath);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Save] Could not delete the save: {e.Message}");
        }
    }

    /// <summary>Writes the current game. Does nothing outside the game scene or while a save is still being loaded.</summary>
    public static bool Save()
    {
        QuestManager quests = QuestManager.Instance;
        TimeManager time = TimeManager.Instance;

        if (quests == null || time == null || PendingLoad)
            return false;

        try
        {
            SaveData data = Collect(quests, time);
            string temp = FilePath + ".tmp";

            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            File.WriteAllText(temp, JsonUtility.ToJson(data, true));

            if (File.Exists(FilePath))
                File.Delete(FilePath);

            File.Move(temp, FilePath);
            Debug.Log($"[Save] Saved: day {data.day} {data.hour:00}:{data.minute:00}, {data.quests.Count} quests.");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Save] Save failed: {e.Message}");
            return false;
        }
    }

    private static SaveData Collect(QuestManager quests, TimeManager time)
    {
        var data = new SaveData
        {
            version = Version,
            savedAtUtc = DateTime.UtcNow.ToString("o"),
            day = time.CurrentDay,
            hour = time.CurrentHour,
            minute = time.CurrentMinute
        };

        if (ReputationManager.Instance != null)
        {
            foreach (FactionReputation rep in ReputationManager.Instance.All)
            {
                if (rep.faction != null)
                    data.reputation.Add(new ReputationSave { factionId = GameRegistry.FactionKey(rep.faction), value = rep.reputation });
            }
        }

        foreach (QuestInstance quest in quests.activeQuests)
            AddQuest(data, quest, "Active");

        foreach (QuestInstance quest in quests.completedQuests)
            AddQuest(data, quest, "Completed");

        Inventory inventory = Inventory.Instance;

        if (inventory != null)
        {
            foreach (ItemSlot slot in inventory.HotbarSlots)
                data.hotbar.Add(ToSave(slot));

            foreach (ItemSlot slot in inventory.InventorySlots)
                data.inventory.Add(ToSave(slot));
        }

        PlayerInteractor player = UnityEngine.Object.FindFirstObjectByType<PlayerInteractor>();

        if (player != null)
        {
            Vector3 position = player.transform.root.position;
            data.playerPosition = new[] { position.x, position.y, position.z };
        }

        return data;
    }

    private static void AddQuest(SaveData data, QuestInstance quest, string state)
    {
        if (quest == null || quest.data == null)
            return;

        var saved = new QuestSave
        {
            id = quest.data.id,
            state = state,
            elapsed = Mathf.Max(0f, Time.time - quest.startTime),
            readyToTurnIn = quest.readyToTurnIn
        };

        foreach (ObjectiveInstance objective in quest.objectives)
        {
            saved.objectives.Add(new ObjectiveSave
            {
                id = objective.data != null ? objective.data.id : string.Empty,
                amount = objective.currentAmount,
                done = objective.isCompleted
            });
        }

        data.quests.Add(saved);
    }

    private static SlotSave ToSave(ItemSlot slot)
    {
        return new SlotSave { itemId = slot.itemId, count = slot.count, payload = PayloadToSave(slot.payload) };
    }

    private static PayloadSave PayloadToSave(object payload)
    {
        var state = payload as PocketState.Payload;

        if (state == null)
            return null;

        var saved = new PayloadSave { isPotion = state.isPotion, questItemId = state.questItemId, rarity = state.rarity };
        var enchant = state.enchant as EnchantableItem.SavedState;

        if (enchant != null)
        {
            saved.hasEnchant = true;
            saved.runeSlots = enchant.runeSlots;

            foreach (AppliedEnchant applied in enchant.enchants)
            {
                saved.enchants.Add(new EnchantSave
                {
                    symbolId = applied.symbol != null ? applied.symbol.id : string.Empty,
                    runeId = applied.rune != null ? applied.rune.id : string.Empty,
                    quality = applied.quality
                });
            }
        }

        return saved;
    }

    private static object PayloadFromSave(PayloadSave saved)
    {
        // JsonUtility cannot write null: a plain item comes back as an empty payload, which must stay null
        // (an item with a payload never stacks)
        if (saved == null || (!saved.isPotion && !saved.hasEnchant))
            return null;

        var state = new PocketState.Payload
        {
            isPotion = saved.isPotion,
            questItemId = saved.questItemId,
            rarity = string.IsNullOrEmpty(saved.rarity) ? null : saved.rarity
        };

        if (saved.hasEnchant)
        {
            var enchant = new EnchantableItem.SavedState { runeSlots = saved.runeSlots, enchants = new List<AppliedEnchant>() };

            foreach (EnchantSave applied in saved.enchants)
            {
                enchant.enchants.Add(new AppliedEnchant
                {
                    symbol = GameRegistry.Symbol(applied.symbolId),
                    rune = GameRegistry.Rune(applied.runeId),
                    quality = applied.quality
                });
            }

            state.enchant = enchant;
        }

        return state;
    }

    private static SaveData Read()
    {
        try
        {
            if (!HasSave)
                return null;

            SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));

            if (data == null || data.version != Version)
            {
                Debug.LogWarning("[Save] The save file is from another version, starting a new game.");
                return null;
            }

            return data;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Save] Could not read the save, starting a new game: {e.Message}");
            return null;
        }
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // the pause menu is loaded on top of the game and the main menu is not the game
        if (!PendingLoad || mode != LoadSceneMode.Single || scene.name == "MainMenu")
            return;

        new GameObject("SaveLoader").AddComponent<SaveLoader>();
    }

    /// <summary>Puts the saved state into the freshly loaded game scene.</summary>
    internal static void ApplySaved()
    {
        SaveData data = Read();
        PendingLoad = false;

        if (data == null)
            return;

        if (TimeManager.Instance != null)
            TimeManager.Instance.SetTime(data.day, data.hour, data.minute);

        ApplyReputation(data);
        ApplyQuests(data);
        ApplyInventory(data);
        ApplyPlayer(data);
        RestoreVisitor();

        Debug.Log($"[Save] Loaded: day {data.day} {data.hour:00}:{data.minute:00}, {data.quests.Count} quests.");
    }

    private static void ApplyReputation(SaveData data)
    {
        ReputationManager reputation = ReputationManager.Instance;

        if (reputation == null)
            return;

        reputation.ResetReputations();

        foreach (ReputationSave saved in data.reputation)
        {
            FactionData faction = GameRegistry.Faction(saved.factionId);

            if (faction != null)
                reputation.SetReputation(faction, saved.value);
        }
    }

    private static void ApplyQuests(SaveData data)
    {
        QuestManager quests = QuestManager.Instance;

        if (quests == null)
            return;

        quests.activeQuests.Clear();
        quests.completedQuests.Clear();

        foreach (QuestSave saved in data.quests)
        {
            QuestData definition = GameRegistry.Quest(saved.id);

            if (definition == null)
            {
                Debug.LogWarning($"[Save] Quest '{saved.id}' no longer exists, skipped.");
                continue;
            }

            bool completed = saved.state == "Completed";

            var quest = new QuestInstance
            {
                data = definition,
                state = completed ? QuestState.Completed : QuestState.Active,
                readyToTurnIn = saved.readyToTurnIn,
                startTime = Time.time - saved.elapsed,
                objectives = new List<ObjectiveInstance>()
            };

            for (int i = 0; i < definition.objectives.Count; i++)
            {
                ObjectiveSave objective = saved.objectives.Find(o => o.id == definition.objectives[i].id);

                if (objective == null && i < saved.objectives.Count)
                    objective = saved.objectives[i];

                quest.objectives.Add(new ObjectiveInstance
                {
                    data = definition.objectives[i],
                    currentAmount = objective != null ? objective.amount : 0,
                    isCompleted = objective != null && objective.done
                });
            }

            if (completed)
                quests.completedQuests.Add(quest);
            else
                quests.activeQuests.Add(quest);
        }
    }

    private static void ApplyInventory(SaveData data)
    {
        Inventory inventory = Inventory.Instance;

        if (inventory == null)
            return;

        CopySlots(data.hotbar, inventory.HotbarSlots, inventory.emptyItemId);
        CopySlots(data.inventory, inventory.InventorySlots, inventory.emptyItemId);
        inventory.RefreshAll();
    }

    private static void CopySlots(List<SlotSave> saved, IReadOnlyList<ItemSlot> slots, int emptyItemId)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            SlotSave source = i < saved.Count ? saved[i] : null;

            slots[i].itemId = source != null ? source.itemId : emptyItemId;
            slots[i].count = source != null ? source.count : 0;
            slots[i].payload = source != null ? PayloadFromSave(source.payload) : null;
        }
    }

    private static void ApplyPlayer(SaveData data)
    {
        PlayerInteractor player = UnityEngine.Object.FindFirstObjectByType<PlayerInteractor>();

        if (player == null || data.playerPosition == null || data.playerPosition.Length != 3)
            return;

        Transform root = player.transform.root;
        CharacterController[] controllers = root.GetComponentsInChildren<CharacterController>();

        foreach (CharacterController controller in controllers)
            controller.enabled = false;

        root.position = new Vector3(data.playerPosition[0], data.playerPosition[1], data.playerPosition[2]);

        foreach (CharacterController controller in controllers)
            controller.enabled = true;

        Physics.SyncTransforms();

        PlayerMovement movement = player.GetComponent<PlayerMovement>();

        if (movement != null)
            movement.playerVelocity = Vector3.zero;
    }

    /// <summary>
    /// Who is at the counter follows from the quests: the visitor with the finished order, or, when
    /// nothing is going on, the next order of the chain. A game with no quests yet keeps the first visitor.
    /// </summary>
    private static void RestoreVisitor()
    {
        QuestManager quests = QuestManager.Instance;
        NPCManager visitors = NPCManager.Instance;

        if (quests == null || visitors == null)
            return;

        if (quests.activeQuests.Count == 0 && quests.completedQuests.Count == 0)
            return;

        // the first visitor was already sent in when the scene started
        foreach (NPCController visitor in UnityEngine.Object.FindObjectsByType<NPCController>(FindObjectsSortMode.None))
            UnityEngine.Object.Destroy(visitor.gameObject);

        QuestInstance finished = quests.activeQuests.Find(q => q.readyToTurnIn);

        if (finished != null)
        {
            visitors.SpawnNPC(finished.data);
            return;
        }

        if (quests.activeQuests.Count > 0)
            return;

        foreach (QuestInstance done in quests.completedQuests)
        {
            QuestData next = done.data.nextQuest;

            if (next != null && !quests.IsQuestCompleted(next.id) && !quests.HasQuest(next.id))
            {
                visitors.SpawnNPC(next);
                return;
            }
        }
    }
}

/// <summary>Finds quests, factions and enchantment assets by the id a save file stores (they live in Resources).</summary>
internal static class GameRegistry
{
    private static Dictionary<string, QuestData> quests;
    private static Dictionary<string, FactionData> factions;
    private static Dictionary<string, EnchantSymbolData> symbols;
    private static Dictionary<string, RuneData> runes;

    public static string FactionKey(FactionData faction)
    {
        return string.IsNullOrEmpty(faction.id) ? faction.name : faction.id;
    }

    public static QuestData Quest(string id)
    {
        if (quests == null)
            quests = Index(Resources.LoadAll<QuestData>("Quests"), q => q.id);

        return Find(quests, id);
    }

    public static FactionData Faction(string id)
    {
        if (factions == null)
            factions = Index(Resources.LoadAll<FactionData>("Factions"), FactionKey);

        return Find(factions, id);
    }

    public static EnchantSymbolData Symbol(string id)
    {
        if (symbols == null)
            symbols = Index(Resources.LoadAll<EnchantSymbolData>("Enchanting"), s => s.id);

        return Find(symbols, id);
    }

    public static RuneData Rune(string id)
    {
        if (runes == null)
            runes = Index(Resources.LoadAll<RuneData>("Enchanting"), r => r.id);

        return Find(runes, id);
    }

    private static Dictionary<string, T> Index<T>(T[] assets, Func<T, string> key)
    {
        var index = new Dictionary<string, T>();

        foreach (T asset in assets)
        {
            string id = key(asset);

            if (!string.IsNullOrEmpty(id))
                index[id] = asset;
        }

        return index;
    }

    private static T Find<T>(Dictionary<string, T> index, string id) where T : class
    {
        if (string.IsNullOrEmpty(id))
            return null;

        index.TryGetValue(id, out T asset);
        return asset;
    }
}

/// <summary>Applies the save a couple of frames after the game scene started, once every Start() has run.</summary>
public class SaveLoader : MonoBehaviour
{
    private IEnumerator Start()
    {
        yield return null;
        yield return null;

        SaveSystem.ApplySaved();
        Destroy(gameObject);
    }
}
