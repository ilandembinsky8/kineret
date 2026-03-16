using DG.Tweening;
using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DestinationButtonHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Event Channels")]
    [SerializeField] private BoolEventChannel enableDestinationSelection_EC;

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
        enableDestinationSelection_EC.OnEventRaised += HandleEnableDestinationSelection;
    }

    private void OnDisable()
    {
        enableDestinationSelection_EC.OnEventRaised -= HandleEnableDestinationSelection;
    }

    private void OnDestroy()
    {
        selectedIcon.DOKill();
    }

    public void LoadDestination(int destination)
    {
        _destination = destination;
        DestinationData destinationData = LocationsManager.GetDestination(destination);
        transform.localPosition = destinationData.Data.UiPosition;
        titleText.text = destinationData.Data.Name;
        lowerText.text = destinationData.Data.Description;
    }

    public void OnClick()
    {
        if (_isSelected) Deselect();
        else Select();
    }

    private void SetUp()
    {
        upperText.color = upperText.color.WithAlpha(0);
        lowerText.color = lowerText.color.WithAlpha(0);
        selectedIcon.color = selectedIcon.color.WithAlpha(0);
        availableIcon.color = selectedIcon.color.WithAlpha(1f);
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

        PlaySelectStatusAnimation(true,0.5f);
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

    private void PlayHoverAnimation(bool isEnter, bool isSelected)
    {
        StopAllCoroutines();
        selectedIcon.DOKill();
        availableIcon.DOKill();
        upperPanel.DOKill();
        lowerPanel.DOKill();
        upperText.DOKill();
        lowerText.DOKill();

        //Move to values
        StartCoroutine(StatusIconFade(isSelected, 0.5f,1f, 0.5f));
        StartCoroutine(PanelsAnimation(isEnter, 0.5f, 0.3f));
    }

    private IEnumerator StatusIconFade(bool isEnter,float fadeDuration,float pulseDuration,float minPulse)
    {
        if (isEnter)
        {
            Tween tween = selectedIcon.DOFade(1, fadeDuration);
            availableIcon.DOFade(0, fadeDuration);

            yield return tween.WaitForCompletion();

            selectedIcon.DOFade(minPulse, pulseDuration).SetLoops(-1,LoopType.Yoyo);
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
            upperPanel.DOSizeDelta(new Vector2(upperPanelOriginalSize.x,0), panelDuration);
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
