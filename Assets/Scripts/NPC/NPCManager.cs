using UnityEngine;

public class NPCManager : MonoBehaviour
{
    public static NPCManager Instance;

    [Header("NPC")]
    [SerializeField] private GameObject npcPrefab;

    [Header("Points")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform counterPoint;
    [SerializeField] private Transform exitPoint;

    [SerializeField] private QuestData testQuest;

    private QuestData pendingNextQuest;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        QuestManager.Instance.OnQuestReadyToTurnIn += HandleQuestReady;

        GameEvents.OnQuestTurnedIn += HandleQuestTurnedIn;

        SpawnNPC(testQuest);
    }

    public GameObject SpawnNPC()
    {
        return SpawnNPC(null);
    }

    public GameObject SpawnNPC(QuestData quest)
    {
        if (npcPrefab == null)
        {
            Debug.LogError("NPCManager: NPC Prefab не назначен!");
            return null;
        }

        if (spawnPoint == null)
        {
            Debug.LogError("NPCManager: Spawn Point не назначен!");
            return null;
        }

        if (counterPoint == null)
        {
            Debug.LogError("NPCManager: Counter Point не назначен!");
            return null;
        }

        if (exitPoint == null)
        {
            Debug.LogError("NPCManager: Exit Point не назначен!");
            return null;
        }

        GameObject npcObject = Instantiate(
            npcPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        NPCController controller = npcObject.GetComponent<NPCController>();

        if (controller == null)
        {
            Debug.LogError(
                $"NPCManager: {npcObject.name} does not have NPCController!"
            );

            Destroy(npcObject);
            return null;
        }

        controller.Initialize(counterPoint, exitPoint);

        NPCQuest npcQuest = npcObject.GetComponent<NPCQuest>();
        Debug.Log($"NPCManager received quest = {quest}");

        if (npcQuest == null)
        {
            Debug.LogError(
                $"NPCManager: {npcObject.name} does not have NPCQuest!"
            );

            Destroy(npcObject);
            return null;
        }

        if (quest != null)
        {
            npcQuest.Initialize(quest);
        }

        controller.StartVisit();

        //Debug.Log($"NPC spawned: {npcObject.name}");

        return npcObject;
    }

    //private void OnEnable()
    //{
    //    if (QuestManager.Instance != null)
    //        QuestManager.Instance.OnQuestReadyToTurnIn += HandleQuestReady;
    //}

    private void OnDestroy()
    {
        GameEvents.OnQuestTurnedIn -= HandleQuestTurnedIn;
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnQuestReadyToTurnIn -= HandleQuestReady;
    }
    // When a quest is handed in, the next quest of the chain (QuestData.nextQuest) arrives with a new visitor.
    private void HandleQuestTurnedIn(string questID)
    {
        QuestInstance done = QuestManager.Instance.completedQuests.Find(q => q.data.id == questID);

        if (done == null || done.data.nextQuest == null)
            return;

        pendingNextQuest = done.data.nextQuest;
        Invoke(nameof(SpawnPendingQuestNpc), 4f);
    }

    private void SpawnPendingQuestNpc()
    {
        SpawnNPC(pendingNextQuest);
        pendingNextQuest = null;
    }

    private void HandleQuestReady(QuestInstance quest)
    {
        Debug.Log($"NPCManager: квест можно сдавать: {quest.data.id}");

        SpawnNPC(quest.data);
    }
}