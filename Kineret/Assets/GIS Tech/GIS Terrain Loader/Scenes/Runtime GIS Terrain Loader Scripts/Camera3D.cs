/*     Unity GIS Tech 2020-2023      */

using GISTech.GISTerrainLoader;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camera3D : MonoBehaviour
{
    public KeyCode RotateLeft;
    public KeyCode RotateRight;
    public KeyCode ResetCameraPosKey;

    [Header("Adapte Camera to GIS Container")]
    public bool AdapteToGISContainer;

    [Header("Camera Movements")]
    public float PanSpeed = 0.1f;
    public float RotationSpeed = 0.15f;
    public float ZoomSpeed;
    public float FollowSpeed = 0.1f;
    public float SmoothingFactor = 0.1f;

    [Header("Camera Parameters")]
    [Space(4)]
    public bool UseKeyboardInput = true;
    public bool UseMouseInput = true;
    public bool AdaptToTerrainHeight = true;
    public bool IncreaseSpeedWhenZoomedOut = true;
    public bool CorrectZoomingOutRatio = true;
    public bool Smoothing = true;

    [Header("Follow a GameObject")]
    public GameObject TargetToFollow;

    [Header("Enable/Disbale Position On Start")]
    public bool UseStartPosition = false;
    public Vector3 StartPosition = new Vector3();
    public Vector3 StartRotation = new Vector3();


    private float mousepanSpeed = 15.0f;
    private float mousezoomSpeed = 100.0f;

    private Vector2 MinMaxZoom = new Vector2(0, 0);

    private float minZoomDistance = 20.0f;
    private float maxZoomDistance = 2.0f;

    private Vector3 cameraTarget;
    private float currentCameraDistance;
    private Vector3 lastMousePos;
    private Vector3 lastPanSpeed = Vector3.zero;
    private Vector3 goingToCameraTarget = Vector3.zero;
    private bool doingAutoMovement = false;
    private Vector3 lastpos;
    private Rect bound;

    private GISTerrainContainer container;

    private Vector2 lastMousePos2;
    private Vector2 lastMousePos3;

    private float minPitch = -80f;
    private float maxPitch = 80f;
    private void OnEnable()
    {
        RuntimeTerrainGenerator.OnFinish += OnTerrainGenerated;
    }
    private void OnDisable()
    {
        RuntimeTerrainGenerator.OnFinish -= OnTerrainGenerated;
    }

    // Use this for initialization
    public void Start()
    {
        currentCameraDistance = minZoomDistance + ((maxZoomDistance - minZoomDistance) / 2.0f);
        this.transform.position = lastpos;

        if (UseStartPosition)
        {
            GoTo(StartPosition);
            this.transform.localEulerAngles = StartRotation;
        }

#if UNITY_6000_0_OR_NEWER
        // Seed last mouse position to avoid large jump on first frame-drag
        if (UnityEngine.InputSystem.Mouse.current != null)
        {
            lastMousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            lastMousePos2 = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            lastMousePos3 = new Vector3(lastMousePos2.x, lastMousePos2.y, 0f);
        }

#else
        // Seed last mouse position to avoid large jump on first frame-drag
        lastMousePos = Input.mousePosition;

#endif

    }
    bool ResetCameraPressed = false;

    // Update is called once per frame
    public void Update()
    {
#if UNITY_6000_0_OR_NEWER
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            ResetCameraPressed = UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame;
        }
#else
        ResetCameraPressed = Input.GetKeyDown(KeyCode.R);
#endif
        if (ResetCameraPressed)
            ResetCameraPosition();

        UpdatePanning();

        UpdateZooming();
        UpdatePosition();
        UpdateRotation();
        UpdateAutoMovement();

#if UNITY_6000_0_OR_NEWER
        lastMousePos2 = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
        lastMousePos3 = new Vector3(lastMousePos2.x, lastMousePos2.y, 0f);
#else
        lastMousePos = Input.mousePosition;
        lastMousePos2 = Input.mousePosition;
#endif

    }

    public void GoTo(Vector3 position)
    {
        doingAutoMovement = true;
        goingToCameraTarget = position;
        TargetToFollow = null;
    }

    public void Follow(GameObject gameObjectToFollow)
    {
        TargetToFollow = gameObjectToFollow;
    }

    bool LeftArrowPressed = false;
    bool DownArrowPressed = false;
    bool RightArrowPressed = false;
    bool UpArrowPressed = false;


    private void UpdatePanning()
    {
        Vector3 moveVector = new Vector3(0, 0, 0);

        if (UseKeyboardInput)
        {
#if UNITY_6000_0_OR_NEWER
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                LeftArrowPressed = UnityEngine.InputSystem.Keyboard.current.leftArrowKey.wasPressedThisFrame;
                DownArrowPressed = UnityEngine.InputSystem.Keyboard.current.downArrowKey.wasPressedThisFrame;
                RightArrowPressed = UnityEngine.InputSystem.Keyboard.current.rightArrowKey.wasPressedThisFrame;
                UpArrowPressed = UnityEngine.InputSystem.Keyboard.current.upArrowKey.wasPressedThisFrame;
            }
#else
            LeftArrowPressed = Input.GetKey(KeyCode.LeftArrow);
            DownArrowPressed = Input.GetKey(KeyCode.DownArrow);
            RightArrowPressed = Input.GetKey(KeyCode.RightArrow);
            UpArrowPressed = Input.GetKey(KeyCode.UpArrow);
#endif

            if (LeftArrowPressed) moveVector.x -= 1;
            if (DownArrowPressed) moveVector.z -= 1;
            if (RightArrowPressed) moveVector.x += 1;
            if (UpArrowPressed) moveVector.z += 1;
        }

        if (UseMouseInput)
        {
#if UNITY_6000_0_OR_NEWER

            if (UnityEngine.InputSystem.Mouse.current != null)
            {
                Vector2 curr = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
                Vector2 delta2 = curr - lastMousePos2;
                Vector3 delta = new Vector3(delta2.x, 0f, delta2.y);

                // Left mouse drag → planar pan (x,z)
                if (UnityEngine.InputSystem.Mouse.current.leftButton.isPressed)
                    moveVector += new Vector3(-delta.x, 0f, -delta.z) * PanSpeed;

                // Middle mouse drag → vertical pan (y)
                if (UnityEngine.InputSystem.Mouse.current.middleButton.isPressed)
                    moveVector += new Vector3(0f, -delta2.y, 0f) * PanSpeed;
            }
#else
            if (Input.GetMouseButton(0))
            {
                Vector3 deltaMousePos = (Input.mousePosition - lastMousePos);
                moveVector += new Vector3(-deltaMousePos.x, 0, -deltaMousePos.y) * PanSpeed;
            }
            if (Input.GetMouseButton(2))
            {
                Vector3 deltaMousePos = (Input.mousePosition - lastMousePos);
                moveVector += new Vector3(0, -deltaMousePos.y, 0) * PanSpeed;
            }
#endif
        }
        if (moveVector != Vector3.zero)
        {
            TargetToFollow = null;
            doingAutoMovement = false;
        }
        Vector3 effectivePanSpeed = moveVector;
        if (Smoothing)
        {
            effectivePanSpeed = Vector3.Lerp(lastPanSpeed, moveVector, SmoothingFactor);
            lastPanSpeed = effectivePanSpeed;
        }
        var oldXRotation = transform.localEulerAngles.x;
        // Set the local X rotation to 0;
        transform.SetLocalEulerAngles(0.0f);
        float panMultiplier = IncreaseSpeedWhenZoomedOut ? (Mathf.Sqrt(currentCameraDistance)) : 1.0f;
        cameraTarget = cameraTarget + transform.TransformDirection(effectivePanSpeed) * mousepanSpeed * panMultiplier * Time.deltaTime;
        // Set the old x rotation.
        transform.SetLocalEulerAngles(oldXRotation);
    }

    bool FPressed = false;
    bool RPressed = false;
    float scroll = 0.0f;

    private void UpdateZooming()
    {
        float deltaZoom = 0.0f;
        if (UseKeyboardInput)
        {

#if UNITY_6000_0_OR_NEWER
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                FPressed = UnityEngine.InputSystem.Keyboard.current.fKey.isPressed;
                RPressed = UnityEngine.InputSystem.Keyboard.current.rKey.isPressed;
            }
#else
            FPressed = Input.GetKey(KeyCode.F);
            RPressed = Input.GetKey(KeyCode.R);
#endif

            if (FPressed) deltaZoom = 1.0f;

            if (RPressed) deltaZoom = -1.0f;
        }


        if (UseMouseInput)
        {

#if UNITY_6000_0_OR_NEWER
            if (UnityEngine.InputSystem.Mouse.current != null)
            {
                // New Input System scroll (Y = vertical wheel)
                float scroll = UnityEngine.InputSystem.Mouse.current.scroll.ReadValue().y; // positive up, negative down on most mice
            }
#else
            scroll = Input.GetAxis("Mouse ScrollWheel");
#endif

            deltaZoom -= scroll * ZoomSpeed;

            var zoomedOutRatio = CorrectZoomingOutRatio ? (currentCameraDistance - minZoomDistance) / (maxZoomDistance - minZoomDistance) : 0.0f;

            if (scroll > 0 || scroll < 0)
            {
                var value = minZoomDistance * scroll * ZoomSpeed * Time.deltaTime;
                if (scroll < 0)
                {
                    if (minZoomDistance - value < MinMaxZoom.y)
                        minZoomDistance -= value;

                }
                if (scroll > 0)
                {

                    if (minZoomDistance + value > MinMaxZoom.x)
                        minZoomDistance -= value;

                }

                if (minZoomDistance <= MinMaxZoom.x)
                    minZoomDistance = MinMaxZoom.x;

                var m_dis = Mathf.Max(minZoomDistance, Mathf.Min(maxZoomDistance, currentCameraDistance + deltaZoom * Time.deltaTime * mousezoomSpeed * (zoomedOutRatio * 2.0f + 1.0f)));
                var dis = Mathf.Clamp(m_dis, MinMaxZoom.x, MinMaxZoom.y);
                currentCameraDistance = dis;


            }
        }

    }

    private void UpdatePosition()
    {

        if (TargetToFollow != null)
        {
            if (FollowSpeed > 0)
                cameraTarget = Vector3.Lerp(cameraTarget, TargetToFollow.transform.position, FollowSpeed);
        }
        if (transform.position != Vector3.zero && cameraTarget != Vector3.zero)
        {
            transform.position = cameraTarget;
            transform.Translate(Vector3.back * currentCameraDistance);

        }
        if (AdapteToGISContainer)
        {
            if (cameraTarget.y >= MinMaxZoom.y)
                cameraTarget = new Vector3(cameraTarget.x, MinMaxZoom.y, cameraTarget.z);

            if (cameraTarget.x <= MinMaxZoom.x)
                cameraTarget = new Vector3(cameraTarget.x, MinMaxZoom.x, cameraTarget.z);

            // Ensure the camera remains within bounds.
            float x = Mathf.Clamp(this.transform.position.x, bound.xMin, bound.xMax);
            float y = Mathf.Clamp(this.transform.position.y, MinMaxZoom.x, MinMaxZoom.y);
            float z = Mathf.Clamp(this.transform.position.z, bound.yMin, bound.yMax);
            this.transform.position = new Vector3(x, y, z);
        }
    }

    private void UpdateAutoMovement()
    {
        if (doingAutoMovement)
        {
            cameraTarget = Vector3.Lerp(cameraTarget, goingToCameraTarget, FollowSpeed);
            if (Vector3.Distance(goingToCameraTarget, cameraTarget) < 1.0f)
            {
                doingAutoMovement = false;
            }
        }

    }

    void OnDrawGizmosSelected()
    {
        if (AdapteToGISContainer)
        {
            //Draw debug lines.
            Vector3 camPos = transform.position;
            Gizmos.DrawLine(new Vector3(bound.xMin, camPos.y, bound.yMin), new Vector3(bound.xMin, camPos.y, bound.yMax));
            Gizmos.DrawLine(new Vector3(bound.xMin, camPos.y, bound.yMax), new Vector3(bound.xMax, camPos.y, bound.yMax));
            Gizmos.DrawLine(new Vector3(bound.xMax, camPos.y, bound.yMax), new Vector3(bound.xMax, camPos.y, bound.yMin));
            Gizmos.DrawLine(new Vector3(bound.xMax, camPos.y, bound.yMin), new Vector3(bound.xMin, camPos.y, bound.yMin));
        }


    }

    private void OnTerrainGenerated(GISTerrainContainer m_container)
    {

        container = m_container;

        ResetCameraPosition();
    }

    public void ResetCameraPosition()
    {
        if (container != null)
        {
            bound = new Rect(new Vector2(-container.ContainerSize.x / 2, -container.ContainerSize.z / 2), new Vector2(container.ContainerSize.x * 2, container.ContainerSize.z * 2));
            var camPos = new Vector3(container.ContainerSize.x / 2, (container.ContainerSize.x + container.ContainerSize.z) / 4, container.ContainerSize.z / 2);
            cameraTarget = camPos;
            transform.position = camPos;
            lastpos = camPos;
            enabled = true;

            PanSpeed = container.Scale.x * 2;
            ZoomSpeed = container.Scale.x * 300;
            MinMaxZoom.x = 40;
            MinMaxZoom.y = (container.ContainerSize.x + container.ContainerSize.z) / 2;
        }
    }

    bool rotPressed = false;
    Vector2 curr = new Vector2();
    private void UpdateRotation()
    {
#if UNITY_6000_0_OR_NEWER
        if (!UseMouseInput || UnityEngine.InputSystem.Mouse.current == null) return;
        rotPressed = UnityEngine.InputSystem.Mouse.current.rightButton.isPressed;

#else
        rotPressed = Input.GetMouseButton(1);

#endif

        if (rotPressed)
        {
            // Mouse delta since last frame
#if UNITY_6000_0_OR_NEWER
            curr = UnityEngine.InputSystem.Mouse.current.position.ReadValue();

#else
            curr = Input.mousePosition;

#endif

            Vector2 delta = curr - lastMousePos2;

            // Convert to yaw/pitch using your RotationSpeed
            float yawDelta = delta.x * RotationSpeed;
            float pitchDelta = -delta.y * RotationSpeed;

            // Stop any auto/follow while user is rotating
            TargetToFollow = null;
            doingAutoMovement = false;

            // Apply yaw/pitch by editing local euler angles, with pitch clamp
            var eul = transform.localEulerAngles;

            // Convert Unity's 0..360 to signed -180..180 for clamping
            float pitch = eul.x;
            if (pitch > 180f) pitch -= 360f;

            pitch = Mathf.Clamp(pitch + pitchDelta, minPitch, maxPitch);
            float yaw = eul.y + yawDelta;

            transform.localEulerAngles = new Vector3(pitch, yaw, 0f);
        }
    }
}

