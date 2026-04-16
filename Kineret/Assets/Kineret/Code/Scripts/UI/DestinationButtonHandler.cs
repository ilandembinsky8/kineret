using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine;
using TMPro;

public class DestinationButtonHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI Elements")]

    [SerializeField] private Image availableIcon;
    [SerializeField] private Image selectedIcon;

    [SerializeField] private RectTransform upperPanel;
    [SerializeField] private Image titleBackground;
    [SerializeField] private RectTransform lowerPanel;
    [SerializeField] private TMP_Text upperText;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text lowerText;

    [SerializeField] private RectMask2D descriptionPanelMask;

    [Header("Values")]
    [SerializeField] private float topMaskPaddingTarget;
    [SerializeField] private float botMaskPaddingTarget;

    private int _destination;
    private bool _isSelectable;
    private bool _isSelected;

    private Color32 selectedColor = new Color32(40, 215, 245, 255);
    private Vector2 upperPanelOriginalSize;
    private Vector2 lowerPanelOriginalSize;

    private void Awake()
    {
        upperPanelOriginalSize = upperPanel.sizeDelta;
        lowerPanelOriginalSize = lowerPanel.sizeDelta;
    }
    private void OnEnable() 
    { 
        EventsRelay.OnEnableDestinationSelection += HandleEnableDestinationSelection;
    }
    private void OnDisable() 
    {
        EventsRelay.OnEnableDestinationSelection -= HandleEnableDestinationSelection;
    }
    private void OnDestroy() { selectedIcon.DOKill(); }

    public bool GetIsSelected() { return _isSelected; }
    public void LoadDestination(int destination)
    {
        _destination = destination;
        DestinationData destinationData = LocationsManager.GetDestination(destination);
        transform.localPosition = destinationData.Data.UiPosition;
        titleText.text = destinationData.Data.UIDestinationInfoText.HebTitle;
        lowerText.text = destinationData.Data.UIDestinationInfoText.HebDescription;
    }

    public void OnClick()
    {
        if (_isSelected) Deselect();
        else Select();
    }

    private void SetUp()
    {
        upperText.color = new Color(upperText.color.r, upperText.color.g, upperText.color.b, 0f);
        lowerText.color = new Color(lowerText.color.r, lowerText.color.g, lowerText.color.b, 0f);
        selectedIcon.color = new Color(selectedIcon.color.r, selectedIcon.color.g, selectedIcon.color.b, 0f);
        availableIcon.color = new Color(availableIcon.color.r, availableIcon.color.g, availableIcon.color.b, 1f);
        upperPanel.sizeDelta = new Vector2(upperPanel.sizeDelta.x, 0);
        lowerPanel.sizeDelta = new Vector2(lowerPanel.sizeDelta.x, 0);
        descriptionPanelMask.padding = Vector4.zero;
    }

    private void HandleEnableDestinationSelection(bool isEnabled)
    {
        _isSelectable = isEnabled;
        if (isEnabled)
        {
            _isSelected = false;
            gameObject.SetActive(true);
            SetUp();
            return;
        }

        if (_isSelected)
        {
            return;
        }

        gameObject.SetActive(false);
    }

    private void Select()
    {
        if (!_isSelectable) return;
        _isSelected = true;

        PlaySelectStatusAnimation(true, 0.5f);
        EventsRelay.OnDestinationSelected.Invoke(_destination);
    }
    private void Deselect()
    {
        if (!_isSelectable) return;
        _isSelected = false;

        PlaySelectStatusAnimation(false, 0.5f);
        EventsRelay.OnDestinationDeselected.Invoke();
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_isSelectable || _isSelected) return;

        PlayHoverAnimation(true, true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!_isSelectable || _isSelected) return;

        PlayHoverAnimation(false, false);
    }

    public void PlayHoverAnimation(bool isEnter, bool isSelected)
    {
        StopAllCoroutines();
        selectedIcon.DOKill();
        availableIcon.DOKill();
        upperPanel.DOKill();
        lowerPanel.DOKill();
        upperText.DOKill();
        lowerText.DOKill();

        //Move to values
        StartCoroutine(StatusIconFade(isSelected, 0.5f, 1f, 0.5f));
        StartCoroutine(PanelsAnimation(isEnter, 0.5f, 0.3f));
    }

    private IEnumerator StatusIconFade(bool isEnter, float fadeDuration, float pulseDuration, float minPulse)
    {
        if (isEnter)
        {
            Tween tween = selectedIcon.DOFade(1, fadeDuration);
            availableIcon.DOFade(0, fadeDuration);

            yield return tween.WaitForCompletion();

            selectedIcon.DOFade(minPulse, pulseDuration).SetLoops(-1, LoopType.Yoyo);
        }
        else
        {
            selectedIcon.DOFade(0, fadeDuration);
            availableIcon.DOFade(1, fadeDuration);
        }
    }
    private IEnumerator PanelsAnimation(bool isEnter, float panelDuration, float textDuration)
    {
        if (isEnter)
        {
            upperPanel.DOSizeDelta(upperPanelOriginalSize, panelDuration).SetEase(Ease.OutBack);
            lowerPanel.DOSizeDelta(lowerPanelOriginalSize, panelDuration).SetEase(Ease.OutBack);

            yield return new WaitForSeconds(panelDuration);

            upperText.DOFade(1, textDuration);
            lowerText.DOFade(1, textDuration);
        }
        else
        {
            upperText.DOFade(0, textDuration);
            lowerText.DOFade(0, textDuration);

            yield return new WaitForSeconds(textDuration);
            upperPanel.DOSizeDelta(new Vector2(upperPanelOriginalSize.x, 0), panelDuration);
            lowerPanel.DOSizeDelta(new Vector2(lowerPanelOriginalSize.x, 0), panelDuration);
        }
    }

    private void PlaySelectStatusAnimation(bool _isSelectted, float duration)
    {
        PlayHoverAnimation(false, _isSelectted);
        titleBackground.DOColor(_isSelectted ? selectedColor : Color.white, duration);

        /*float padding;
        if (_isSelectted)
        {
            padding = descriptionPanelMask.rectTransform.sizeDelta.y / 2;
        }
        else
        {
            padding = 0;
        }
        DOTween.To(
            () => descriptionPanelMask.padding,
            v => descriptionPanelMask.padding = v,
            new Vector4(0, padding, 0, padding),
            duration);*/
    }
}
