using UnityEngine.InputSystem.Utilities;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine;
using System;

public class IdleManager : MonoBehaviour
{
    public static IdleManager Instance { get; private set; }
    public static Action<int> OnAnyJoystickInput { get; set; }
    public static Action OnAnyJoystickClick { get; set; }

    public static bool IsTicking = false;

    private float _timer;
    private int _allowedIdleTime;

    // Start is called before the first frame update
    void Awake()
    {    
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        _allowedIdleTime = GameSettingsManager.GetInt("Game Settings", "MaxIdleDurationInSeconds", 600);
        InputSystem.onAnyButtonPress.Call(ctrl => _timer = 0);
    }

    // Update is called once per frame
    void Update()
    {
        float horizontalAxis = Input.GetAxis(JoystickManager.JoystickControls.HorizontalAxis);
        float verticalAxis = Input.GetAxis(JoystickManager.JoystickControls.VerticalAxis);
        float miniHorizontalAxis = Input.GetAxis(JoystickManager.JoystickControls.MiniHorizontalAxis);
        float miniVerticalAxis = Input.GetAxis(JoystickManager.JoystickControls.MiniVerticalAxis);

        //If Joystick Moved
        if (horizontalAxis != 0 || verticalAxis != 0 ||
            miniHorizontalAxis != 0 || miniVerticalAxis != 0)
        {
            _timer = 0;

            if (Mathf.Abs(horizontalAxis) > JoystickManager.StickDeadzone) { OnAnyJoystickInput?.Invoke((int)Mathf.Sign(horizontalAxis)); }
            if (Mathf.Abs(verticalAxis) > JoystickManager.StickDeadzone) { OnAnyJoystickInput?.Invoke((int)Mathf.Sign(verticalAxis)); }
            if (Mathf.Abs(miniHorizontalAxis) > JoystickManager.HatDeadzone) { OnAnyJoystickInput?.Invoke((int)Mathf.Sign(miniHorizontalAxis)); }
            if (Mathf.Abs(miniVerticalAxis) > JoystickManager.HatDeadzone) { OnAnyJoystickInput?.Invoke((int)Mathf.Sign(miniVerticalAxis)); }
        }
        //If Button Pressed
        if (Input.GetButtonDown(JoystickManager.JoystickControls.Trigger) ||
            Input.GetButtonDown(JoystickManager.JoystickControls.RedButton))
        {
            _timer = 0;
            OnAnyJoystickClick?.Invoke();
        }

        if (IsTicking)
        {
            _timer += Time.deltaTime;
            if (_allowedIdleTime <= _timer)
            {
                ResetGame();
            }
        }
    }

    private void ResetGame()
    {
        //Do stuff relating to user management
        _timer = 0;
        IsTicking = false;
        MainMenuManager.IsLoadingDestinationSelection = false;
        SceneManager.LoadScene("Main Menu Scene");
    }
}
