
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

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
        if(_mouseDirection != Vector2.zero) { MoveMouse(); }
    }

    private void HandleMoveMouseInput(InputAction.CallbackContext context)
    {
        _mouseDirection = context.ReadValue<Vector2>().normalized;
    }
    private void HandlMouseClickInput(InputAction.CallbackContext context)
    {
        _mouse.press.QueueValueChange<float>(1f);
        _mouse.press.QueueValueChange<float>(0f);
    }

    private void MoveMouse()
    {
        _mousePosition += Time.deltaTime * _mouseSpeed * _mouseDirection;

        _mousePosition.x = Mathf.Clamp(_mousePosition.x, 0, _maxX);
        _mousePosition.y = Mathf.Clamp(_mousePosition.y, 0, _maxY);
        _mouse.WarpCursorPosition(_mousePosition);
    }
}
