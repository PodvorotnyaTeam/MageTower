using UnityEngine;

public class Spawner : MonoBehaviour, IInteractable
{
    [SerializeField]
    private GameObject item;
    [SerializeField]
    private GameObject spawnLocation;

    public void Interact(PlayerInteractor interactor)
    {
        Instantiate(item, spawnLocation.transform);
    }
}
