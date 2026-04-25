using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform camTransform;
    public GameObject camRig;

    public float movementSpeed;
    public float movementTime;
    public float rotAmount;
    public Vector3 zoomAmt;

    public Vector3 newPosition;
    public Quaternion newRotation;
    public Vector3 newZoom;

    public bool isMovementLocked;

    public Camera cam;

    private float zoom;
    private float zoomMultiplier = 4f;
    private float minZoom = 1f;
    private float maxZoom = 10f;
    private float velocity = 0f;
    private float smoothTime = 0.25f;


    void Start()
    {
        newPosition = transform.position;
        newRotation = transform.rotation;
        newZoom = camTransform.localPosition;
        zoom = cam.orthographicSize;
    }

    void LateUpdate()
    {
#if UNITY_6000_0_OR_NEWER
        HandleMovementInputNew();
#else
        HandleMovementInputOld();
#endif
        HandleMouseInput();
    }

    public void LockMovement()
    {
        isMovementLocked = true;
    }

    public void UnlockMovement()
    {
        isMovementLocked = false;
    }

    public void HandleMouseInput()
    {

    }

    public void HandleMovementInputOld()
    {
        if (isMovementLocked) return;

        // --- Movement ---
        Vector3 direction = Vector3.zero;

        // Classic key input works in all versions
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            direction += transform.forward;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            direction -= transform.forward;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            direction += transform.right;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            direction -= transform.right;

        // Normalize to prevent faster diagonal movement
        if (direction != Vector3.zero)
            direction.Normalize();

        // --- Movement speed modifiers ---
        if (Input.GetKey(KeyCode.LeftShift))
            movementSpeed = 0.3f;
        else if (Input.GetKey(KeyCode.LeftControl))
            movementSpeed = 0.05f;
        else
            movementSpeed = 0.2f;

        newPosition += direction * movementSpeed;

        // --- Zoom ---
        float scroll = Input.GetAxis("Mouse ScrollWheel"); // Works in all Unity 6+
        zoom -= scroll * zoomMultiplier;
        zoom = Mathf.Clamp(zoom, minZoom, maxZoom);

        cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, zoom, ref velocity, smoothTime);

        // --- Rotation ---
        if (Input.GetKeyDown(KeyCode.Q))
            newRotation *= Quaternion.AngleAxis(90f, Vector3.up);  // Safe in older Unity
        if (Input.GetKeyDown(KeyCode.E))
            newRotation *= Quaternion.AngleAxis(-90f, Vector3.up);

        // --- Apply movement and rotation smoothly ---
        transform.position = Vector3.Lerp(transform.position, newPosition, Time.deltaTime * movementTime);
        transform.rotation = Quaternion.Lerp(transform.rotation, newRotation, Time.deltaTime * movementTime);
    }

    public void HandleMovementInputNew()
    {
#if UNITY_6000_0_OR_NEWER
        if (isMovementLocked) return;

        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            if (UnityEngine.InputSystem.Keyboard.current.wKey.isPressed || UnityEngine.InputSystem.Keyboard.current.upArrowKey.isPressed)
                newPosition += (transform.forward * movementSpeed);

            if (UnityEngine.InputSystem.Keyboard.current.sKey.isPressed || UnityEngine.InputSystem.Keyboard.current.downArrowKey.isPressed)
                newPosition += (transform.forward * -movementSpeed);

            if (UnityEngine.InputSystem.Keyboard.current.dKey.isPressed || UnityEngine.InputSystem.Keyboard.current.rightArrowKey.isPressed)
                newPosition += (transform.right * movementSpeed);

            if (UnityEngine.InputSystem.Keyboard.current.aKey.isPressed || UnityEngine.InputSystem.Keyboard.current.leftArrowKey.isPressed)
                newPosition += (transform.right * -movementSpeed);

            // speed modifiers
            if (UnityEngine.InputSystem.Keyboard.current.leftShiftKey.isPressed)
                movementSpeed = 0.3f;
            else if (UnityEngine.InputSystem.Keyboard.current.leftCtrlKey.isPressed)
                movementSpeed = 0.05f;
            else
                movementSpeed = 0.2f;

            // rotation
            if (UnityEngine.InputSystem.Keyboard.current.qKey.wasPressedThisFrame)
                newRotation *= Quaternion.Euler(Vector3.up * 90);

            if (UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame)
                newRotation *= Quaternion.Euler(Vector3.up * -90);
        }

        // zoom via scroll wheel
        if (UnityEngine.InputSystem.Mouse.current != null)
        {
            float scroll = UnityEngine.InputSystem.Mouse.current.scroll.ReadValue().y; // ✅ replaces GetAxis("Mouse ScrollWheel")
            zoom -= scroll * zoomMultiplier * Time.deltaTime; // add Time.deltaTime for smoothness
            zoom = Mathf.Clamp(zoom, minZoom, maxZoom);

            cam.orthographicSize = Mathf.SmoothDamp(
                cam.orthographicSize,
                zoom,
                ref velocity,
                smoothTime
            );
        }

        // smooth transitions
        transform.position = Vector3.Lerp(transform.position, newPosition, Time.deltaTime * movementTime);
        transform.rotation = Quaternion.Lerp(transform.rotation, newRotation, Time.deltaTime * movementTime);
#endif
    }
}