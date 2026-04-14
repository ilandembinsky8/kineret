

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
    private static float _sensitivityX;
    private static float _sensitivityY;

    public static void Init(float sensitivityX, float sensitivityY, float stickDeadzone,float hatDeadzone, JoystickControls joystickControls)
    {
        _sensitivityX = sensitivityX;
        _sensitivityY = sensitivityY;
        StickDeadzone = stickDeadzone;
        HatDeadzone = hatDeadzone;
        JoystickControls = joystickControls;      
    }

    public static float GetHorizontalAxis()
    {
        return Input.GetAxis(JoystickControls.HorizontalAxis) * _sensitivityX;
    }
    public static float GetVerticalAxis()
    {
        return Input.GetAxis(JoystickControls.VerticalAxis) * _sensitivityY;
    }
}
