using System.Collections;
using UnityEngine;

//Summary Screen
public class ComponentNavigator : MonoBehaviour
{
    [SerializeField] private NavigatorCompononent[] NavigatorContainer;

    private float onButtonChangedTransitionTime = 0.8f;
    private WaitForSeconds waitForSeconds;
    private Coroutine onButtonChanged;
    private int currentIndicator = 0;
    private bool canChangeButton;

    private void Awake()
    {
        waitForSeconds = new WaitForSeconds(onButtonChangedTransitionTime);
    }
    private void OnEnable()
    {
        EventsRelay.OnSummaryScreenReady += EnableButtonChanging;
        IdleManager.OnAnyJoystickInput += TryChangeButton;     
    }
    private void OnDisable()
    {
        EventsRelay.OnSummaryScreenReady -= EnableButtonChanging;
        IdleManager.OnAnyJoystickInput -= TryChangeButton;
    }
    private void EnableButtonChanging()
    {
        canChangeButton = true;
        if (NavigatorContainer[0].isActiveAndEnabled) { NavigatorContainer[0].EnableMyButton(); }
    }

    private void TryChangeButton(JoystickInput joystickInput)
    {
        if (canChangeButton)
        {
            canChangeButton = false;
            onButtonChanged = StartCoroutine(OnButtonChanged(joystickInput.Sign));
        }
    }
    private void NextIndicator(int value)
    {
        currentIndicator += value;

        if (currentIndicator > NavigatorContainer.Length - 1)
            currentIndicator = 0;
        else if (currentIndicator < 0)
            currentIndicator = NavigatorContainer.Length - 1;
    }

    private IEnumerator OnButtonChanged(int inputChangeValue)
    {
        NavigatorContainer[currentIndicator].PlayHoverAnimation(false);
        yield return waitForSeconds;
        NextIndicator(inputChangeValue);
        NavigatorContainer[currentIndicator].PlayHoverAnimation(true);
        yield return waitForSeconds;
        canChangeButton = true;
    }

}