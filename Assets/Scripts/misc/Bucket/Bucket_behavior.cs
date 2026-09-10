using Unity.VisualScripting;
using UnityEngine;

public class Bucket_behavior : MonoBehaviour, IInteractable
{
    public int angle;
    private bool isFull;
    [SerializeField]
    private GameObject waterLevel1;
    [SerializeField]
    private GameObject waterLevel2;
    [SerializeField]
    private GameObject waterLevel3;
    [SerializeField]
    private CauldronState cauldronState;
    private IGrabbable grabbableComponent;
    private RaycastHit hit;

    public void Awake()
    {
        isFull = false;
        grabbableComponent = GetComponent<IGrabbable>();
    }

    public void Update()
    {
        Debug.DrawRay(transform.position, Vector3.down, Color.red, 10f);
        Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit);
        if (hit.collider != null && hit.collider.CompareTag("Cauldron's_bottom"))
        {
            Debug.Log("Попали в: " + hit.collider.name);
        }
    }

    public void Interact(PlayerInteractor interactor)
    {
        interactor.Grab(grabbableComponent);
        Vector3 dir = Quaternion.Euler(angle, 0, 0) * Vector3.down;

    }
}
