using UnityEngine.UI;
using UnityEngine;
using System;

[RequireComponent(typeof(Button))]
public class NavigatorCompononent : MonoBehaviour
{
    public static Action<Button> OnButtonActivated;

    [Tooltip("True for single button menus.")]
    [SerializeField] private bool _isSendingActivationSignal = true;

    private Button _myButton;

    private void Awake() { _myButton = GetComponent<Button>(); }
    private void OnEnable() { if (_isSendingActivationSignal) OnButtonActivated?.Invoke(_myButton); }

    public void EnableMyButton()
    {
        _myButton.interactable = true;
        UserInterfaceNavigator.SetActiveButton(_myButton);
    }
    public void PlayHoverAnimation(bool isSelected)
    {
        if (isSelected)
        {
            EnableMyButton();
        }
        else
        {
            _myButton.interactable = false;
        }
    }

}