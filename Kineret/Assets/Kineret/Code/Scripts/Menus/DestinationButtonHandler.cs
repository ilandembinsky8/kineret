using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DestinationButtonHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GameObject selectedImage;
    [SerializeField] private GameObject unselectedImage;

    private bool _isSelected;

    private void Awake()
    {
        selectedImage.gameObject.SetActive(false);
        unselectedImage.gameObject.SetActive(true);
    }

    public void OnClick()
    {
        if (_isSelected) Deselect();
        else Select();
    }

    private void Select()
    {
        _isSelected = true;
    }
    private void Deselect()
    {
        _isSelected = false;
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        selectedImage.gameObject.SetActive(true);
        unselectedImage.gameObject.SetActive(false);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!_isSelected)
        {
            selectedImage.gameObject.SetActive(false);
            unselectedImage.gameObject.SetActive(true);
        }
    }
}
