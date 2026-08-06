using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField]
    [Min(0f)]
    private float moveSpeed = 1f;
    [SerializeField]
    [Min(0)]
    private float jumpHeight = 2f;
    [SerializeField]
    [Min(0)]
    private float airSpeedMultiplier = 0.5f;
    [SerializeField]
    private float gravity = 10f;
    [SerializeField]
    [Min(0)]
    private float airDrag = 0.001f;
    [SerializeField]
    private int framerate;

    [SerializeField]
    private CinemachineCamera cinemachineCamera;
    private CharacterController controller;
    private PlayerStateController playerStateController;
    [HideInInspector]
    public Vector3 playerVelocity;

    [Header("Input Action References")]
    [SerializeField]
    private InputActionReference moveAction;
    [SerializeField]
    private InputActionReference jumpAction;
    [SerializeField]
    private InputActionReference interactAction;

    private bool isGrounded = true;
    private bool isCoyoteTimeActive = true;

    void Start()
    {
        controller = gameObject.GetComponent<CharacterController>();
        playerStateController = gameObject.GetComponent<PlayerStateController>();
        moveAction.action.Enable();
        jumpAction.action.Enable();
        interactAction.action.Enable();
    }

    void Update()
    {
        Application.targetFrameRate = framerate;
        isGrounded = playerStateController.isGrounded;
        isCoyoteTimeActive = playerStateController.isCayoteTimeActive;
        HandlePlayerMovement();
        HandlePlayerJump();
        HandlePlayerPhysics();
        controller.Move(playerVelocity * Time.deltaTime);
    }

    void HandlePlayerMovement()
    {
        Vector2 moveVector = moveAction.action.ReadValue<Vector2>();
        Vector3 moveDirection = (GetRight() * moveVector.x + GetForward() * moveVector.y).normalized;
        if (isGrounded || isCoyoteTimeActive)
        {
            if (moveVector.magnitude > 0f)
            {                
                playerVelocity.x = moveDirection.x * moveSpeed;
                playerVelocity.z = moveDirection.z * moveSpeed;

                playerStateController.isMoving = true;
            }
            else
            {
                playerVelocity.x = 0f;
                playerVelocity.z = 0f;

                playerStateController.isMoving = false;
            }
        }
        else
        {
            if (moveVector.magnitude > 0f)
            {

                playerVelocity.x = moveDirection.x * moveSpeed * airSpeedMultiplier;
                playerVelocity.z = moveDirection.z * moveSpeed * airSpeedMultiplier;

                playerStateController.isMoving = true;
            }
        }
    }

    private Vector3 GetForward()
    {
        Vector3 forward = cinemachineCamera.transform.forward;
        forward.y = 0f;
        return forward.normalized;
    }
    private Vector3 GetRight()
    {
        Vector3 right = cinemachineCamera.transform.right;
        right.y = 0f;
        return right.normalized;
    }
    void HandlePlayerJump()
    {
        if (jumpAction.action.WasPressedThisFrame() && (isGrounded || isCoyoteTimeActive))
        {
            playerVelocity.y = Mathf.Sqrt(jumpHeight * gravity);

            playerStateController.isCayoteTimeActive = false;
        }
    }

    void HandlePlayerPhysics()
    {
        if (isGrounded || isCoyoteTimeActive)
        {
            playerVelocity.y -= 0.05f * Time.deltaTime;
        }
        else
        {
            playerVelocity.y -= gravity * Time.deltaTime;
            playerVelocity.x *= (1 - airDrag);
            playerVelocity.z *= (1 - airDrag);
        }
    }
    
}
