

using UnityEngine;

public struct JoystickControls
{
    public string HorizontalAxis;
    public string VerticalAxis;
    public string MiniHorizontalAxis;
    public string MiniVerticalAxis;
    public string Trigger;
    public string RedButton;
}

public static class JoystickManager
{
    public static JoystickControls JoystickControls;

    public static float StickDeadzone;
    public static float HatDeadzone;
    private static float _sensitivity;

    public static void Init(float sensitivity,float stickDeadzone,float hatDeadzone, JoystickControls joystickControls)
    {
        _sensitivity = sensitivity;
        StickDeadzone = stickDeadzone;
        HatDeadzone = hatDeadzone;
        JoystickControls = joystickControls;      
    }

    public static float GetSensitiveAxis(string axisName)
    {
        return Input.GetAxis(JoystickManager.JoystickControls.HorizontalAxis) * _sensitivity;
    }

}
