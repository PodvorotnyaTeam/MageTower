using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    public float interactDistance = 3f;
    public Transform holdPoint;

    [SerializeField]
    InputActionReference interactAction;
    [SerializeField]
    InputActionReference scrollAction;
    Camera cam;

    Grabbable heldObject;
    Rigidbody heldRb;

    public float minHoldDistance = 0.5f;
    public float maxHoldDistance = 3.0f;
    public float holdAdjustSpeed = 2.0f;

    float currentHoldDistance = 1.5f;

    void Awake()
    {
        cam = Camera.main; // <-- КЛЮЧЕВО
    }

    public void OnInteract()
    {
        Debug.Log("Interact action triggered");
        if (!interactAction.action.triggered) return;

        if (heldObject != null)
        {
            Drop();
            return;
        }

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
        {
            var interactable = hit.collider.GetComponent<IInteractable>();
            interactable?.Interact(this);
        }
    }

    public void OnScroll()
    {
        if (heldObject == null) return;
        if (!scrollAction.action.triggered) return;
        float scrollInput = scrollAction.action.ReadValue<float>();
        currentHoldDistance += scrollInput * holdAdjustSpeed * Time.deltaTime;
        currentHoldDistance = Mathf.Clamp(currentHoldDistance, minHoldDistance, maxHoldDistance);

        UpdateHoldPoint();
    }
    public void Grab(Grabbable obj)
    {
        heldObject = obj;
        heldRb = obj.GetComponent<Rigidbody>();

        heldRb.useGravity = false;
        heldRb.linearDamping = 10f;

        currentHoldDistance = Vector3.Distance(
        cam.transform.position,
        heldObject.transform.position
    );

        UpdateHoldPoint();
    }

    void UpdateHoldPoint()
    {
        holdPoint.localPosition = new Vector3(0f, 0f, currentHoldDistance);
    }

    void Update()
    {
        if (heldRb == null) return;
        // Adjust hold distance with mouse scroll
        float scrollInput = Mouse.current.scroll.ReadValue().y;
        currentHoldDistance += scrollInput * holdAdjustSpeed * Time.deltaTime;
        currentHoldDistance = Mathf.Clamp(currentHoldDistance, minHoldDistance, maxHoldDistance);
        // Update hold point position based on current hold distance
        holdPoint.localPosition = Vector3.forward * currentHoldDistance;
    }
    void FixedUpdate()
    {
        if (heldRb == null) return;

        Vector3 dir = holdPoint.position - heldRb.position;
        heldRb.linearVelocity = dir * 20f;
    }

    void Drop()
    {
        heldRb.useGravity = true;
        heldRb.linearDamping = 0f;

        heldObject = null;
        heldRb = null;
    }
}