using UnityEngine;

public class NPCQuest : MonoBehaviour, IInteractable
{
    [SerializeField] private QuestData quest;

    public bool CanInteract(PlayerInteractor interactor) => true;

    public void Interact(PlayerInteractor interactor)
    {
        if (QuestManager.Instance.IsQuestCompleted(quest.id))
        {
            Debug.Log("Quest already completed");
            return;
        }

        if (QuestManager.Instance.HasQuest(quest.id))
        {
            if (QuestManager.Instance.CanTurnIn(quest.id))
            {
                Debug.Log("Quest turned in");

                QuestManager.Instance.TurnInQuest(quest.id);

                GameEvents.OnQuestTurnedIn?.Invoke(quest.id);
            }
            else
            {
                Debug.Log("Quest not completed yet");
            }

            return;
        }
        Debug.Log("Quest given");

        QuestManager.Instance.AddQuest(quest);

        GameEvents.OnNPCInteracted?.Invoke(quest.id);
    }

    public string GetInteractionText()
    {
        if (QuestManager.Instance.IsQuestCompleted(quest.id))
            return "Заказ выполнен";

        if (QuestManager.Instance.HasQuest(quest.id))
        {
            if (QuestManager.Instance.CanTurnIn(quest.id))
                return "Сдать заказ";

            return "Заказ в процессе";
        }

        return "Взять заказ";
    }

    public Transform GetTransform() => transform;

    public void OnFocusEnter(PlayerInteractor interactor) { }

    public void OnFocusExit(PlayerInteractor interactor) { }
}