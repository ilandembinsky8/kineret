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
    [SerializeField] private GameObject detailedImage;
    [SerializeField] private GameObject selectedImage;
    [SerializeField] private GameObject overImage;
    [SerializeField] private GameObject selectedOver;
    [SerializeField] private GameObject unselectedImage;
    [SerializeField] private GameObject tipTMP;

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
            detailedImage.SetActive(false);
            unselectedImage.SetActive(true);
            return;
        }

        if (_isSelected)
        {
            Debug.Log("Selected Destination: " + gameObject.name);
            overImage.SetActive(false);
            detailedImage.SetActive(true);
            selectedImage.SetActive(false);
            unselectedImage.SetActive(false);
            selectedOver.SetActive(true);
            tipTMP.SetActive(false);
            return;
        }

        gameObject.SetActive(false);
    }

    private void Select()
    {
        if (!_isSelectable) return;

        _isSelected = true;
        overImage.SetActive(false);
        detailedImage.SetActive(true);
        //selectedImage.SetActive(true);
        selectedOver.SetActive(true);
        tipTMP.SetActive(false);
        destinationSelected_EC.RaiseEvent();
    }
    private void Deselect()
    {
        if (!_isSelectable) return;
        _isSelected = false;
        tipTMP.SetActive(true);
        destinationDeselected_EC.RaiseEvent();
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_isSelectable) return;
        if (_isSelected)
        {
            selectedOver.SetActive(true);
            selectedImage.SetActive(false);
        }
        else
        {
            overImage.SetActive(true);
        }
        detailedImage.SetActive(true);
        unselectedImage.SetActive(false);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!_isSelectable) return;
        if (!_isSelected)
        {
            unselectedImage.SetActive(true);
        }
        else
            selectedImage.SetActive(true);
        detailedImage.SetActive(false);
        selectedOver.SetActive(false);
        overImage.SetActive(false);
    }
}
