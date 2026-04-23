using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Grabbable : MonoBehaviour, IInteractable
{
    public void Interact(PlayerInteractor interactor)
    {
        interactor.Grab(this);
    }
}