using UnityEngine;

public class RecipeBehavior : MonoBehaviour, IInteractable
{
    public void Interact(PlayerInteractor playerInteractor)
    {
        playerInteractor.Grab(gameObject.GetComponent<IGrabbable>());
        gameObject.GetComponent<Rigidbody>().useGravity = true;
    }
}
