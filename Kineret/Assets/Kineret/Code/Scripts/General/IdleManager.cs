using UnityEngine.InputSystem.Utilities;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine;
using System;
public enum JoystickInputType
{
    Main, Mini
}
public enum Axis
{
    Horizontal, Vertical
}

public struct JoystickInput
{
    public JoystickInputType InputType;
    public Axis Axis;
    public int Sign;
}

public class IdleManager : MonoBehaviour
{
    public static IdleManager Instance { get; private set; }
    public static Action<JoystickInput> OnAnyJoystickInput { get; set; }
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

        if (Input.GetKeyDown(KeyCode.LeftArrow)) { OnAnyJoystickInput?.Invoke(new JoystickInput { InputType = JoystickInputType.Main, Axis = Axis.Horizontal, Sign = -1 });}
        if (Input.GetKeyDown(KeyCode.RightArrow)) { OnAnyJoystickInput?.Invoke(new JoystickInput { InputType = JoystickInputType.Main, Axis = Axis.Horizontal, Sign = 1 }); }
        if (Input.GetKeyDown(KeyCode.DownArrow)) { OnAnyJoystickInput?.Invoke(new JoystickInput { InputType = JoystickInputType.Main, Axis = Axis.Vertical, Sign = -1 }); }
        if (Input.GetKeyDown(KeyCode.UpArrow)) { OnAnyJoystickInput?.Invoke(new JoystickInput { InputType = JoystickInputType.Main, Axis = Axis.Vertical, Sign = 1 }); }
        //If Joystick Moved
        if (horizontalAxis != 0 || verticalAxis != 0 ||
            miniHorizontalAxis != 0 || miniVerticalAxis != 0)
        {
            _timer = 0;

            if (Mathf.Abs(horizontalAxis) > JoystickManager.StickDeadzone) { OnAnyJoystickInput?.Invoke( new JoystickInput { InputType = JoystickInputType.Main, Axis = Axis.Horizontal, Sign = (int)Mathf.Sign(horizontalAxis) } ); }
            if (Mathf.Abs(verticalAxis) > JoystickManager.StickDeadzone) { OnAnyJoystickInput?.Invoke(new JoystickInput { InputType = JoystickInputType.Main, Axis = Axis.Vertical, Sign = (int)Mathf.Sign(verticalAxis) }); }
            if (Mathf.Abs(miniHorizontalAxis) > JoystickManager.HatDeadzone) { OnAnyJoystickInput?.Invoke(new JoystickInput { InputType = JoystickInputType.Mini, Axis = Axis.Horizontal, Sign = (int)Mathf.Sign(miniHorizontalAxis) }); }
            if (Mathf.Abs(miniVerticalAxis) > JoystickManager.HatDeadzone) { OnAnyJoystickInput?.Invoke(new JoystickInput { InputType = JoystickInputType.Mini, Axis = Axis.Vertical, Sign = (int)Mathf.Sign(miniVerticalAxis) }); }          
        }

        //If Button Pressed
        if (Input.GetButtonDown(JoystickManager.JoystickControls.Trigger) ||
            Input.GetButtonDown(JoystickManager.JoystickControls.RedButton) || 
            Input.GetKeyDown(KeyCode.Space))
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
