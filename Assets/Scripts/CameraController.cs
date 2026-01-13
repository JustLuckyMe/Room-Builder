using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Move")]
    public float moveSpeed = 10f;
    public float fastMoveMultiplier = 2f;

    [Header("Rotate")]
    public float rotationSpeed = 3f;
    public int rotateMouseButton = 1; // 1 = RMB
    public bool lockCursorWhileRotating = false;

    [Header("Keys")]
    public KeyCode forwardKey = KeyCode.W;
    public KeyCode backwardKey = KeyCode.S;
    public KeyCode leftKey = KeyCode.A;
    public KeyCode rightKey = KeyCode.D;
    public KeyCode fastMoveKey = KeyCode.LeftShift;
    public KeyCode upKey = KeyCode.Space;
    public KeyCode downKey = KeyCode.LeftControl;

    private float _yaw;
    private float _pitch;
    private bool _isRotating;

    private void Start()
    {
        Vector3 euler = transform.rotation.eulerAngles;
        _yaw = euler.y;
        _pitch = euler.x;
    }

    private void Update()
    {
        HandleRotation();
        HandleMovement();
    }

    private void HandleMovement()
    {
        float moveX = 0f;
        float moveZ = 0f;
        float moveY = 0f;

        if (Input.GetKey(forwardKey)) moveZ += 1f;
        if (Input.GetKey(backwardKey)) moveZ -= 1f;
        if (Input.GetKey(rightKey)) moveX += 1f;
        if (Input.GetKey(leftKey)) moveX -= 1f;

        if (Input.GetKey(upKey)) moveY += 1f;
        if (Input.GetKey(downKey)) moveY -= 1f;

        Vector3 moveDir = new Vector3(moveX, moveY, moveZ);
        if (moveDir.sqrMagnitude > 1f)
        {
            moveDir.Normalize();
        }

        float speed = moveSpeed;
        if (Input.GetKey(fastMoveKey))
        {
            speed *= fastMoveMultiplier;
        }

        transform.Translate(moveDir * speed * Time.deltaTime, Space.Self);
    }

    private void HandleRotation()
    {
        bool mouseButtonDown = Input.GetMouseButton(rotateMouseButton);

        if (mouseButtonDown && !_isRotating)
        {
            _isRotating = true;

            // Lock to center of screen while rotating
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else if (!mouseButtonDown && _isRotating)
        {
            _isRotating = false;

            // Confine cursor to window when not rotating
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
        }

        if (!_isRotating)
            return;

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        _yaw += mouseX * rotationSpeed;
        _pitch -= mouseY * rotationSpeed;

        _pitch = Mathf.Clamp(_pitch, -89f, 89f);

        transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }
}
