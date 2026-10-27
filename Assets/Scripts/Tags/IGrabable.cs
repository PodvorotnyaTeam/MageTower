using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class IGrabbable : MonoBehaviour, IInteractable
{
    [Header("Physics")]
    [Tooltip("How fast spin dies away. Without it a knocked item spins for ages.")]
    [SerializeField] private float angularDamping = 3f;
    [Tooltip("Top spin in radians per second. The Unity default of 7 flings items around.")]
    [SerializeField] private float maxAngularSpeed = 4f;
    [SerializeField] private float linearDamping = 0.1f;
    [Tooltip("How fast overlapping items are pushed apart, in m/s. The Unity default of 10 can throw an item through a thin floor.")]
    [SerializeField] private float maxDepenetrationSpeed = 2f;

    [Header("Safety net")]
    [Tooltip("An item that ends up below this height (it fell through the floor) is put back where it was last safe.")]
    [SerializeField] private float fallLimit = -5f;

    private Rigidbody body;
    private Vector3 lastSafePosition;
    private float nextSafeSave;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.angularDamping = angularDamping;
        body.maxAngularVelocity = maxAngularSpeed;
        body.linearDamping = linearDamping;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.maxDepenetrationVelocity = maxDepenetrationSpeed;

        // a thin floor cannot be crossed even by a fast item (kinematic display samples do not need it)
        if (!body.isKinematic)
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        lastSafePosition = transform.position;
    }

    private void FixedUpdate()
    {
        if (body.isKinematic)
            return;

        if (body.position.y < fallLimit)
        {
            Debug.LogWarning($"[IGrabbable] {name} fell through the world, put back at {lastSafePosition}.");
            body.position = lastSafePosition + Vector3.up * 0.5f;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            return;
        }

        // remember a place where the item was resting above the floor
        if (Time.time >= nextSafeSave && body.position.y > -0.5f && body.linearVelocity.sqrMagnitude < 4f)
        {
            lastSafePosition = body.position;
            nextSafeSave = Time.time + 1f;
        }
    }

    public void Interact(PlayerInteractor interactor)
    {
        interactor.Grab(this);
    }
}
