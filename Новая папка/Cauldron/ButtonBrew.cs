using UnityEngine;

public class ButtonBrew : MonoBehaviour, IInteractable
{
    public GameObject point;
    public void Interact(PlayerInteractor interactor)
    {
        point.GetComponent<CauldronState>().Brew();
        Debug.Log("Нажата кнопка варки");
    }
}
