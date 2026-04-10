using UnityEngine.InputSystem;
using UnityEngine;

public class MouseManager : MonoBehaviour
{
    private float _mouseSpeed;
    private int _maxX;
    private int _maxY;
    private Mouse _mouse;
    private Vector2 _mouseDirection;
    private Vector2 _mousePosition;
    private InputActions _actions;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        _actions = new InputActions();
        _mouseSpeed = GameSettingsManager.GetFloat("Controls", "MouseSpeed", 20f);
        _maxX = GameSettingsManager.GetInt("Controls", "MouseMaxX", 3840);
        _maxY = GameSettingsManager.GetInt("Controls", "MouseMaxY", 2160);
        _mouse = Mouse.current;
        _mousePosition = _mouse.position.value;
    }

    private void OnEnable()
    {
        _actions.Player.Enable();
        _actions.Player.MoveMouse.performed += HandleMoveMouseInput;
        _actions.Player.MoveMouse.canceled += HandleMoveMouseInput;
        _actions.Player.MouseClick.performed += HandlMouseClickInput;
        _mousePosition = new Vector2(1900, 170);
    }
    private void OnDisable()
    {
        _actions.Player.Disable();
        _actions.Player.MoveMouse.performed -= HandleMoveMouseInput;
        _actions.Player.MoveMouse.canceled -= HandleMoveMouseInput;
        _actions.Player.MouseClick.performed -= HandlMouseClickInput;
    }
    private void Update()
    {
        Vector2 joystickDirection = new Vector2(Input.GetAxis("HatX"), Input.GetAxis("HatY"));

        Debug.Log("Mouse:" + _mouseDirection);
        Debug.Log("Joystick:" + joystickDirection);
        if (_mouseDirection != Vector2.zero) { MoveMouse(_mouseDirection); }
        if (joystickDirection != Vector2.zero) { MoveMouse(joystickDirection); }

        if (Input.GetButtonDown("RedButton")) { SimulateMouseClick(); }
    }

    private void HandleMoveMouseInput(InputAction.CallbackContext context)
    {
        _mouseDirection = context.ReadValue<Vector2>().normalized;
    }
    private void HandlMouseClickInput(InputAction.CallbackContext context)
    {
        SimulateMouseClick();
    }

    private void SimulateMouseClick()
    {
        _mouse.press.QueueValueChange<float>(1f);
        _mouse.press.QueueValueChange<float>(0f);
    }

    private void MoveMouse(Vector2 direction)
    {
        _mousePosition += Time.deltaTime * _mouseSpeed * direction;

        _mousePosition.x = Mathf.Clamp(_mousePosition.x, 0, _maxX);
        _mousePosition.y = Mathf.Clamp(_mousePosition.y, 0, _maxY);
        _mouse.WarpCursorPosition(_mousePosition);
    }
}
