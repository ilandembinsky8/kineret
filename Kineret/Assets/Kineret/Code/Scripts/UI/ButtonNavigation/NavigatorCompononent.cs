using UnityEngine.UI;
using UnityEngine;
using System;


//Single Buttons
[RequireComponent(typeof(Button))]
public class NavigatorCompononent : MonoBehaviour
{
    public static Action<Button> OnButtonActivated { get; set; }

    [Tooltip("True for single button menus.")]
    [SerializeField] private bool _isSendingActivationSignal = true;

    private Button _myButton;

    private void Awake() { _myButton = GetComponent<Button>(); }
    private void OnEnable()
    {    
        if (_isSendingActivationSignal)
        {
            Debug.Log($"Enabling my self: {gameObject.name}");
            OnButtonActivated?.Invoke(_myButton);
        }
    }

    public void EnableMyButton()
    {
        Debug.Log($"Enabling my self: {gameObject.name}");
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