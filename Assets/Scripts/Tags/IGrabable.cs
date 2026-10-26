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

    private void Awake()
    {
        Rigidbody body = GetComponent<Rigidbody>();
        body.angularDamping = angularDamping;
        body.maxAngularVelocity = maxAngularSpeed;
        body.linearDamping = linearDamping;
        body.interpolation = RigidbodyInterpolation.Interpolate;
    }

    public void Interact(PlayerInteractor interactor)
    {
        interactor.Grab(this);
    }
}
