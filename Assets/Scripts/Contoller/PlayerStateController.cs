using System;
using System.Collections;
using UnityEngine;

public class PlayerStateController : MonoBehaviour
{
    [Header("Ground Check")]
    [SerializeField]
    private float raycastCheckDistance;
    private Vector3 spherecastOrigin;
    [SerializeField]
    private float spherecastRadius;
    private CharacterController characterController;
    private RaycastHit sphereCastInfo;
    [SerializeField]
    private LayerMask layerForColission; 
    private bool isGroundedSpherecast;
    [SerializeField]
    [Min(0)]
    private float cayoteTime;
    private float cayoteCountdown = 0;

    public enum PlayerState
    {
        Idle,
        Walking,
        Airborne,
        Dashing
    }

    public PlayerState currentState;
    public bool isStateLocked;
    public bool isMoving;
    public bool isGrounded;
    public bool isCayoteTimeActive;
    public bool isDashing;
    public bool isVulnerable;

    void Start()
    {
        characterController = gameObject.GetComponent<CharacterController>();
        currentState = PlayerState.Idle;
        isStateLocked = false;
        isMoving = false;
        isGrounded = true;
        isCayoteTimeActive = true;
        isDashing = false;
        isVulnerable = true;
    }

    void Update()
    {
        spherecastRadius = characterController.radius * 0.95f;
        spherecastOrigin = transform.position + characterController.center + Vector3.up * (characterController.height * 0.5f - spherecastRadius + 0.02f);
        isGroundedSpherecast = Physics.SphereCast(spherecastOrigin, spherecastRadius, Vector3.down, out sphereCastInfo, 
            raycastCheckDistance, layerForColission);
        if (!isGroundedSpherecast)
        {
            isGrounded = false;
            if (cayoteCountdown > 0)
            {
                cayoteCountdown -= Time.deltaTime;
            }
            else
            {
                isCayoteTimeActive = false;
            }
        }
        else
        {
            cayoteCountdown = cayoteTime;
            isGrounded = true;
            isCayoteTimeActive = true;
        }
    }

    private void LateUpdate()
    {
        if (isDashing)
        {
            currentState = PlayerState.Dashing;
        }
        else if (!isGrounded)
        {
            currentState = PlayerState.Airborne;
        }
        else if (isMoving)
        {
            currentState = PlayerState.Walking;
        }
        else
        {
            currentState = PlayerState.Idle;
        }
    }
}
