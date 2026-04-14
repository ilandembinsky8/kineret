using UnityEngine.UI;
using UnityEngine;

//Handles the destination buttons
public class UserInterfaceNavigator : MonoBehaviour
{
    private static Button currentlyActiveButton;

    private void OnEnable()
    {
        NavigatorCompononent.OnButtonActivated += SetActiveButton;
        IdleManager.OnAnyJoystickClick += TryActivateButton;
    }
    private void OnDisable()
    {
        NavigatorCompononent.OnButtonActivated -= SetActiveButton;
        IdleManager.OnAnyJoystickClick -= TryActivateButton;
    }

    public static void TryActivateButton()
    {
        if (currentlyActiveButton == null) { Debug.Log("No active button to activate."); return; }
        if (!currentlyActiveButton.IsInteractable()) { Debug.Log("Button is not interactable."); return; }

        currentlyActiveButton.onClick.Invoke();
        currentlyActiveButton = null;
    }
    public static Button GetActiveButton() { return currentlyActiveButton; }
    public static void SetActiveButton(Button button) { currentlyActiveButton = button; }

}