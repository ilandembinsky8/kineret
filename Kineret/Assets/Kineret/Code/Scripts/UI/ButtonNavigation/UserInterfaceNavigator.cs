using UnityEngine.UI;
using UnityEngine;

public class UserInterfaceNavigator : MonoBehaviour
{
    private static Button currentlyActiveButton;

    private void OnEnable() { NavigatorCompononent.OnButtonActivated += SetActiveButton; }
    private void OnDisable() { NavigatorCompononent.OnButtonActivated -= SetActiveButton; }
    private void Update() { if (JoystickManager.JoystickControls.RedButton != null && Input.GetButtonDown(JoystickManager.JoystickControls.RedButton)) { TryActivateButton(); } }

    public static void TryActivateButton()
    {
        if (currentlyActiveButton == null) { Debug.Log("No active button to activate."); return; }

        currentlyActiveButton.onClick.Invoke();
        currentlyActiveButton = null;
    }
    public static Button GetActiveButton() { return currentlyActiveButton; }

    private void SetActiveButton(Button button) { currentlyActiveButton = button; }

}