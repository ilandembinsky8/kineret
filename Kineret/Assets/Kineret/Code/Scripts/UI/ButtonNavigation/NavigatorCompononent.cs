using UnityEngine.UI;
using UnityEngine;
using System;

[RequireComponent(typeof(Button))]
public class NavigatorCompononent : MonoBehaviour
{
    public static Action<Button> OnButtonActivated;

    private Button _myButton;

    private void Awake() { _myButton = GetComponent<Button>(); }
    private void OnEnable() { OnButtonActivated?.Invoke(_myButton); }

}