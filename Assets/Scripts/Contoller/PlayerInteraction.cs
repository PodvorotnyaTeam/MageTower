using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    public float interactDistance = 3f;

    [Tooltip("Aim tolerance: a thin ray is hard to point at small items such as the firecracker.")]
    public float interactRadius = 0.1f;

    [Header("Held object physics")]
    [Tooltip("Top speed of a held object, so it cannot be driven into things hard enough to spin wildly.")]
    public float maxHoldSpeed = 6f;
    [Tooltip("Rotation damping while the object is held.")]
    public float heldAngularDamping = 10f;
    private float savedLinearDamping;
    private float savedAngularDamping;
    public Transform holdPoint;

    [SerializeField]
    InputActionReference interactAction;
    [SerializeField]
    InputActionReference scrollAction;
    Camera cam;

    IGrabbable heldObject;
    Rigidbody heldRb;

    public float minHoldDistance = 0.5f;
    public float maxHoldDistance = 3.0f;
    public float holdAdjustSpeed = 2.0f;

    float currentHoldDistance = 1.5f;

    public IGrabbable HeldObject => heldObject;

    // Lets go of the held object and hands it over (used to put it in the pocket).
    public GameObject ReleaseHeld()
    {
        if (heldObject == null)
            return null;

        GameObject released = heldObject.gameObject;
        Drop();
        return released;
    }

    void Awake()
    {
        cam = Camera.main; // <-- КЛЮЧЕВО
    }

    public void OnInteract()
    {
        // a menu is open: E belongs to the UI, not to the world behind it
        if (PlayerInputHandler.fromInventory)
            return;

        Debug.Log("[1] OnInteract вызван");

        if (!interactAction.action.triggered) { Debug.Log("[1a] action НЕ triggered"); return; }

        if (heldObject != null)
        {
            Debug.Log("[1b] держим объект -> Drop");
            Drop();
            return;
        }

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.SphereCast(ray, interactRadius, out RaycastHit hit, interactDistance))
        {
            Debug.Log($"[2] hit = {hit.collider.name}");
            var interactable = hit.collider.GetComponent<IInteractable>();
            Debug.Log($"[3] IInteractable = {(interactable != null ? interactable.GetType().Name : "NULL")}");
            interactable?.Interact(this);
        }
        else Debug.Log("[2] луч ни во что не попал");
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
    public void Grab(IGrabbable obj)
    {
        heldObject = obj;
        heldRb = obj.GetComponent<Rigidbody>();

        heldRb.useGravity = false;
        savedLinearDamping = heldRb.linearDamping;
        savedAngularDamping = heldRb.angularDamping;
        heldRb.linearDamping = 10f;
        heldRb.angularDamping = heldAngularDamping;

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
        heldRb.linearVelocity = Vector3.ClampMagnitude(dir * 20f, maxHoldSpeed);
    }

    void Drop()
    {
        heldRb.useGravity = true;
        heldRb.linearDamping = savedLinearDamping;
        heldRb.angularDamping = savedAngularDamping;

        heldObject = null;
        heldRb = null;
    }
}