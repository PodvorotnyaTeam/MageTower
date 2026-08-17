using UnityEngine;

public class QuestTestCompleter : MonoBehaviour, IInteractable
{
    [SerializeField] private string itemID = "test_item";

    public bool CanInteract(PlayerInteractor interactor) => true;

    public void Interact(PlayerInteractor interactor)
    {
        GameEvents.OnItemCollected?.Invoke(itemID, 1);
    }

    public string GetInteractionText() => "Собрать тестовый предмет";
    public Transform GetTransform() => transform;

    public void OnFocusEnter(PlayerInteractor interactor) { }
    public void OnFocusExit(PlayerInteractor interactor) { }
}
