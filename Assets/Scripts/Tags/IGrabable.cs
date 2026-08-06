using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class IGrabbable : MonoBehaviour, IInteractable
{
    public void Interact(PlayerInteractor interactor)
    {
        interactor.Grab(this);
    }
}