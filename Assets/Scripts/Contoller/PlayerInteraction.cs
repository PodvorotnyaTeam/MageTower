using NUnit.Framework.Internal.Execution;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    public float interactDistance = 3f;
    public float interactRadius = 0.1f;

    [Header("Held object physics")]
    public float maxHoldSpeed = 6f;
    public float heldAngularDamping = 10f;
    public Transform holdPoint;

    [Header("Rotating a held object")]
    public Key tiltKey = Key.R;
    public float maxTiltAngle = 110f;
    public float tiltSpeed = 140f;
    public float straightenSpeed = 220f;
    public float heldRotationSpeed = 8f;
    public float maxHeldAngularSpeed = 10f;
    public bool forceTilt;
    private float tilt;

    [SerializeField] InputActionReference interactAction;
    [SerializeField] InputActionReference scrollAction;
    Camera cam;

    IGrabbable heldObject;
    Rigidbody heldRb;

    public float minHoldDistance = 0.5f;
    public float maxHoldDistance = 3.0f;
    public float holdAdjustSpeed = 2.0f;
    float currentHoldDistance = 1.5f;

    private float savedLinearDamping;
    private float savedAngularDamping;
    private float savedMaxAngularVelocity;

    public IGrabbable HeldObject => heldObject;

    int layerMask = ~((1 << 2) | (1 << 4));

    void Awake()
    {
        cam = Camera.main;
    }

    public void OnInteract()
    {
        if (PlayerInputHandler.fromInventory) return;
        if (!interactAction.action.triggered) return;

        // Если у нас есть предмет в руках - выбрасываем его (или отдаем NPC)
        if (heldObject != null)
        {
            var interactable = FindTarget(cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)));
            if (interactable is NPCQuest)
            {
                interactable.Interact(this);
                return;
            }
            Drop();
            return;
        }

        // Если руки пусты - подбираем/взаимодействуем
        InteractNow();
    }

    public void InteractNow()
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        var interactable = FindTarget(ray);

        Debug.Log($"InteractNow: interactable = {(interactable != null ? interactable.GetType().Name : "NULL")}");

        if (interactable != null)
        {
            // ИСПОЛЬЗУЕМ GetComponentInParent, так как коллайдер может быть на дочернем объекте
            var worldItem = (interactable as MonoBehaviour)?.GetComponentInParent<WorldItem>();

            if (worldItem != null)
            {
                Debug.Log($"Найден WorldItem ID: {worldItem.itemId}");
                if (Inventory.Instance.TryAddItem(worldItem.itemId, worldItem.count))
                {
                    Destroy(worldItem.gameObject);
                }
                return; // Выходим, чтобы не брать в руки (IGrabbable)
            }

            // Если это не WorldItem, вызываем оригинальное взаимодействие
            // (это сработает для NPC, дверей и даже для взятия в руки через IGrabbable)
            interactable.Interact(this);
        }
    }

    IInteractable FindTarget(Ray ray)
    {
        RaycastHit[] rayHits = Physics.RaycastAll(ray, interactDistance, layerMask, QueryTriggerInteraction.Ignore);
        System.Array.Sort(rayHits, (a, b) => a.distance.CompareTo(b.distance));

        IInteractable exact = null;
        float wall = interactDistance;

        foreach (var rayHit in rayHits)
        {
            if (IsHeld(rayHit.collider)) continue;
            exact = InteractableOf(rayHit.collider);
            wall = rayHit.distance;
            break;
        }

        if (exact is IGrabbable) return exact;

        IInteractable bestItem = null;
        IInteractable bestOther = null;
        float bestItemDistance = float.MaxValue;
        float bestOtherDistance = float.MaxValue;

        foreach (var sphereHit in Physics.SphereCastAll(ray, interactRadius, interactDistance, layerMask, QueryTriggerInteraction.Ignore))
        {
            if (IsHeld(sphereHit.collider) || sphereHit.distance > wall + 0.15f) continue;
            var target = InteractableOf(sphereHit.collider);
            if (target == null) continue;

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

        if (bestItem != null) return bestItem;
        return exact != null ? exact : bestOther;
    }

    bool IsHeld(Collider collider)
    {
        return heldRb != null && collider.attachedRigidbody == heldRb;
    }

    static IInteractable InteractableOf(Collider collider)
    {
        var target = collider.GetComponent<IInteractable>();
        if (target == null) target = collider.GetComponentInParent<IInteractable>();
        if (target is IGrabbable grabbable && grabbable.TryGetComponent(out Rigidbody body) && body.isKinematic)
        {
            return grabbable.transform.parent != null ? grabbable.transform.parent.GetComponentInParent<IInteractable>() : null;
        }
        return target;
    }

    public void Grab(IGrabbable obj)
    {
        Rigidbody body = obj.GetComponent<Rigidbody>();
        if (body == null || body.isKinematic)
        {
            Debug.LogWarning($"[Interactor] {obj.name} cannot be held.");
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

        currentHoldDistance = Vector3.Distance(cam.transform.position, heldObject.transform.position);
        UpdateHoldPoint();
    }

    public GameObject ReleaseHeld()
    {
        if (heldObject == null) return null;
        GameObject released = heldObject.gameObject;
        Drop();
        return released;
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

    void UpdateHoldPoint()
    {
        holdPoint.localPosition = new Vector3(0f, 0f, currentHoldDistance);
    }

    void Update()
    {
        if (heldRb == null) return;
        float scrollInput = Mouse.current.scroll.ReadValue().y;
        currentHoldDistance += scrollInput * holdAdjustSpeed * Time.deltaTime;
        currentHoldDistance = Mathf.Clamp(currentHoldDistance, minHoldDistance, maxHoldDistance);
        holdPoint.localPosition = Vector3.forward * currentHoldDistance;

        bool pressed = (Keyboard.current != null && Keyboard.current[tiltKey].isPressed) || (Mouse.current != null && Mouse.current.rightButton.isPressed);
        tilt = Mathf.MoveTowards(tilt, pressed ? maxTiltAngle : 0f, (pressed ? tiltSpeed : straightenSpeed) * Time.deltaTime);
    }

    void FixedUpdate()
    {
        if (heldRb == null) return;
        Vector3 dir = holdPoint.position - heldRb.position;
        heldRb.linearVelocity = Vector3.ClampMagnitude(dir * 20f, maxHoldSpeed);

        Vector3 forward = cam.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;

        Quaternion heading = Quaternion.LookRotation(forward.normalized, Vector3.up);
        Quaternion wanted = Quaternion.AngleAxis(tilt, heading * Vector3.right) * heading;
        Quaternion delta = wanted * Quaternion.Inverse(heldRb.rotation);
        delta.ToAngleAxis(out float angle, out Vector3 axis);

        if (angle > 180f) angle -= 360f;
        if (float.IsNaN(axis.x) || float.IsInfinity(axis.x) || Mathf.Abs(angle) < 0.01f)
            heldRb.angularVelocity = Vector3.zero;
        else
            heldRb.angularVelocity = Vector3.ClampMagnitude(axis * (angle * Mathf.Deg2Rad * heldRotationSpeed), maxHeldAngularSpeed);
    }

    public IGrabbable FindTargetForGrab()
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        var target = FindTarget(ray);
        return target as IGrabbable;
    }
}