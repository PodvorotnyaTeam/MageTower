using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotationCameraForYScript : MonoBehaviour
{
    private float rotationX = 0f;
    private float rotationY = 0f;

    [SerializeField] [Range(1f, 10f)] public float sensitivity = 2f;
    void Update()
    {
        if (!GameInputHandler.fromInventory)
            Rotate();
    }

    private void Rotate()
    {
        rotationY -= Input.GetAxisRaw("Mouse Y") * sensitivity;
        rotationY = Mathf.Clamp(rotationY, -80f, 80f);
        Vector3 rotate = transform.eulerAngles;
        rotate.x = rotationY;

        transform.rotation = Quaternion.Euler(rotate);
    }

}
