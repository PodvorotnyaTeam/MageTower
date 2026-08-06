using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementScript : MonoBehaviour
{
    private RotationCameraForYScript cameraScript;

    private const string Horizontal = nameof(Horizontal);
    private const string Vertical = nameof(Vertical);

    private float rotationX = 0f;

    [SerializeField] [Range(1f, 50f)]private float _moveSpeed = 10f;

    private void Start()
    {
        Application.targetFrameRate = 120;
        cameraScript = GetComponentInChildren<RotationCameraForYScript>();
    }

    private void Update()
    {
        if (!GameInputHandler.fromInventory)
            Move();
    }

    private void Move()
    {
        rotationX += Input.GetAxisRaw("Mouse X") * cameraScript.sensitivity;

        transform.rotation = Quaternion.Euler(0f, rotationX, 0f);

        float directionVertical = Input.GetAxisRaw(Vertical);
        float directionHorizontal = Input.GetAxisRaw(Horizontal);
        float distanceHorizontal = directionHorizontal * _moveSpeed * Time.deltaTime;
        float distanceVertical = directionVertical * _moveSpeed * Time.deltaTime;

        Vector3 moveDirection = new Vector3(directionHorizontal, 0, directionVertical);
        if (moveDirection.magnitude > 1f)
        {
            moveDirection.Normalize();
        }
        Vector3 move = moveDirection * _moveSpeed * Time.deltaTime;
        transform.Translate(move);
    }
}
