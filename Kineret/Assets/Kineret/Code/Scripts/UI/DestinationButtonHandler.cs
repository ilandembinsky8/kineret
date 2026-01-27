using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DestinationButtonHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Event Channels")]
    [SerializeField] private VoidEventChannel destinationSelected_EC;
    [SerializeField] private VoidEventChannel destinationDeselected_EC;
    [SerializeField] private BoolEventChannel enableDestinationSelection_EC;

    [Header("UI Elements")]
    [SerializeField] private GameObject selectedImage;
    [SerializeField] private GameObject unselectedImage;

    private bool _isSelectable;
    private bool _isSelected;

    private void OnEnable()
    {
        enableDestinationSelection_EC.OnEventRaised += HandleEnableDestinationSelection;
    }

    private void OnDisable()
    {
        enableDestinationSelection_EC.OnEventRaised -= HandleEnableDestinationSelection;
    }

    public void OnClick()
    {
        if (_isSelected) Deselect();
        else Select();
    }

    private void HandleEnableDestinationSelection(bool isEnabled)
    {
        _isSelectable = isEnabled;
        if (isEnabled)
        {
            _isSelected = false;
            gameObject.SetActive(true);
            selectedImage.SetActive(false);
            unselectedImage.SetActive(true);
            return;
        }

        if (_isSelected)
        {
            selectedImage.SetActive(true);
            unselectedImage.SetActive(false);
            return;
        }

        gameObject.SetActive(false);
    }

    private void Select()
    {
        if (!_isSelectable) return;

        _isSelected = true;
        destinationSelected_EC.RaiseEvent();
    }
    private void Deselect()
    {
        if (!_isSelectable) return;
        _isSelected = false;
        destinationDeselected_EC.RaiseEvent();
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_isSelectable) return;
        selectedImage.SetActive(true);
        unselectedImage.SetActive(false);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!_isSelectable) return;
        if (!_isSelected)
        {
            selectedImage.SetActive(false);
            unselectedImage.SetActive(true);
        }
    }
}
