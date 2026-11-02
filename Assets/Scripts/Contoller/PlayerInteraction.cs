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
    private float savedMaxAngularVelocity;
    public Transform holdPoint;

    [Header("Rotating a held object")]
    [Tooltip("Hold this key (or the right mouse button) to tip the held object forward, e.g. to pour the bucket.")]
    public Key tiltKey = Key.R;
    [Tooltip("How far the object tips forward while the key is held, in degrees.")]
    public float maxTiltAngle = 110f;
    [Tooltip("Degrees per second while the key is held.")]
    public float tiltSpeed = 140f;
    [Tooltip("Degrees per second when the key is released and the object straightens up.")]
    public float straightenSpeed = 220f;
    [Tooltip("How quickly the held object follows the wanted rotation.")]
    public float heldRotationSpeed = 8f;
    [Tooltip("Top spin of a held object, radians per second.")]
    public float maxHeldAngularSpeed = 10f;
    [Tooltip("Lets a script (or a test) tip the held object without a key.")]
    public bool forceTilt;
    private float tilt;

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

    // The rays skip the "Ignore Raycast" (2) and "Water" (4) layers.
    int layerMask = ~((1 << 2) | (1 << 4));

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

        InteractNow();
    }

    // E was pressed: with an item in hand, hand it to the visitor you point at or drop it;
    // with empty hands, interact with whatever is pointed at.
    public void InteractNow()
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        // Shop delivery boxes: a box in the hands is put down and opened, and pointing exactly
        // at a box opens it.
        var heldBox = heldObject != null ? heldObject.GetComponent<MageTower.Shop.DeliveryBox>() : null;

        if (heldBox != null)
        {
            Drop();
            heldBox.Interact(this);
            return;
        }

        if (heldObject == null && Physics.Raycast(ray, out RaycastHit boxHit, interactDistance, layerMask, QueryTriggerInteraction.Ignore))
        {
            var deliveryBox = boxHit.collider.GetComponentInParent<MageTower.Shop.DeliveryBox>();

            if (deliveryBox != null)
            {
                deliveryBox.Interact(this);
                return;
            }
        }

        var interactable = FindTarget(ray);

        if (heldObject != null)
        {
            // A visitor takes the item you hold (a quest hand-in) instead of it being dropped.
            if (interactable is NPCQuest)
            {
                Debug.Log("[1c] отдаём предмет из рук посетителю");
                interactable.Interact(this);
                return;
            }

            Debug.Log("[1b] держим объект -> Drop");
            Drop();
            return;
        }

        Debug.Log($"[3] IInteractable = {(interactable != null ? interactable.GetType().Name : "NULL")}");
        interactable?.Interact(this);
    }

    // What the player is pointing at. Items win over other things near the crosshair (a sword
    // lying on the enchanting table, a firecracker on the floor), but nothing is picked through a wall.
    IInteractable FindTarget(Ray ray)
    {
        // 1. the exact ray: the nearest thing that is really there
        RaycastHit[] rayHits = Physics.RaycastAll(ray, interactDistance, layerMask, QueryTriggerInteraction.Ignore);
        System.Array.Sort(rayHits, (a, b) => a.distance.CompareTo(b.distance));

        IInteractable exact = null;
        float wall = interactDistance;

        foreach (var rayHit in rayHits)
        {
            // the item in your own hands is not what you are pointing at
            if (IsHeld(rayHit.collider))
                continue;

            exact = InteractableOf(rayHit.collider);
            wall = rayHit.distance;
            break;
        }

        if (exact is IGrabbable)
            return exact;

        // 2. a little aim assist: an item inside the aim radius, not behind the thing the ray hit
        IInteractable bestItem = null;
        IInteractable bestOther = null;
        float bestItemDistance = float.MaxValue;
        float bestOtherDistance = float.MaxValue;

        foreach (var sphereHit in Physics.SphereCastAll(ray, interactRadius, interactDistance, layerMask, QueryTriggerInteraction.Ignore))
        {
            if (IsHeld(sphereHit.collider) || sphereHit.distance > wall + 0.15f)
                continue;

            var target = InteractableOf(sphereHit.collider);

            if (target == null)
                continue;

            if (target is IGrabbable)
            {
                if (sphereHit.distance < bestItemDistance)
                {
                    bestItem = target;
                    bestItemDistance = sphereHit.distance;
                }
            }
            else if (sphereHit.distance < bestOtherDistance)
            {
                bestOther = target;
                bestOtherDistance = sphereHit.distance;
            }
        }

        if (bestItem != null)
            return bestItem;

        return exact != null ? exact : bestOther;
    }

    bool IsHeld(Collider collider)
    {
        return heldRb != null && collider.attachedRigidbody == heldRb;
    }

    // What a collider stands for. A grabbable with a kinematic body cannot be held (it is a display
    // sample lying in a spawner box), so pointing at it means pointing at the box it belongs to.
    static IInteractable InteractableOf(Collider collider)
    {
        var target = collider.GetComponent<IInteractable>();

        // an interactable object whose collider is on a child (a visitor, a shop piece)
        if (target == null)
            target = collider.GetComponentInParent<IInteractable>();

        if (target is IGrabbable grabbable
            && grabbable.TryGetComponent(out Rigidbody body)
            && body.isKinematic)
        {
            return grabbable.transform.parent != null
                ? grabbable.transform.parent.GetComponentInParent<IInteractable>()
                : null;
        }

        return target;
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
        Rigidbody body = obj.GetComponent<Rigidbody>();

        // a kinematic body cannot be moved by setting its velocity
        if (body == null || body.isKinematic)
        {
            Debug.LogWarning($"[Interactor] {obj.name} cannot be held: its Rigidbody is missing or kinematic.");
            return;
        }

        heldObject = obj;
        heldRb = body;

        heldRb.useGravity = false;
        savedLinearDamping = heldRb.linearDamping;
        savedAngularDamping = heldRb.angularDamping;
        savedMaxAngularVelocity = heldRb.maxAngularVelocity;
        heldRb.linearDamping = 10f;
        heldRb.angularDamping = heldAngularDamping;
        heldRb.maxAngularVelocity = maxHeldAngularSpeed;
        tilt = 0f;

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

    bool IsTiltPressed()
    {
        if (forceTilt)
            return true;

        if (PlayerInputHandler.fromInventory)
            return false;

        bool key = Keyboard.current != null && Keyboard.current[tiltKey].isPressed;
        bool button = Mouse.current != null && Mouse.current.rightButton.isPressed;
        return key || button;
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

        // Manual rotation: tip the object forward while the key is held, straighten it when released
        bool pressed = IsTiltPressed();
        tilt = Mathf.MoveTowards(tilt, pressed ? maxTiltAngle : 0f, (pressed ? tiltSpeed : straightenSpeed) * Time.deltaTime);
    }
    void FixedUpdate()
    {
        if (heldRb == null) return;

        Vector3 dir = holdPoint.position - heldRb.position;
        heldRb.linearVelocity = Vector3.ClampMagnitude(dir * 20f, maxHoldSpeed);

        // Turn towards the wanted rotation: upright, facing the way the player looks, tipped by 'tilt'.
        // The object follows smoothly instead of spinning when it bumps into something.
        Vector3 forward = cam.transform.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;

        Quaternion heading = Quaternion.LookRotation(forward.normalized, Vector3.up);
        Quaternion wanted = Quaternion.AngleAxis(tilt, heading * Vector3.right) * heading;

        Quaternion delta = wanted * Quaternion.Inverse(heldRb.rotation);
        delta.ToAngleAxis(out float angle, out Vector3 axis);

        if (angle > 180f)
            angle -= 360f;

        if (float.IsNaN(axis.x) || float.IsInfinity(axis.x) || Mathf.Abs(angle) < 0.01f)
            heldRb.angularVelocity = Vector3.zero;
        else
            heldRb.angularVelocity = Vector3.ClampMagnitude(axis * (angle * Mathf.Deg2Rad * heldRotationSpeed), maxHeldAngularSpeed);
    }

    void Drop()
    {
        heldRb.useGravity = true;
        heldRb.linearDamping = savedLinearDamping;
        heldRb.angularDamping = savedAngularDamping;
        heldRb.maxAngularVelocity = savedMaxAngularVelocity;
        tilt = 0f;

        heldObject = null;
        heldRb = null;
    }
}
