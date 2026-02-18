using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PopupTweenHandler : MonoBehaviour
{

    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform icon;
    [SerializeField] private RectTransform titleText;
    [SerializeField] private RectTransform subTitleText;
    [SerializeField] private RectTransform descriptionText;
    [SerializeField] private RectMask2D infoScreenMask;

    [SerializeField] private float backgroundTweenDuration;
    [SerializeField] private float iconTweenDuration;
    [SerializeField] private float contentDelay;
    [SerializeField] private float iconStartingY;
    [SerializeField] private float infoScreenMaskStartingTop;
    [SerializeField] private bool playOnStart;

    private Vector2 _originalBGSizeDelta;
    private float _originalIconLocalPositionY;

    private void Awake()
    {
        _originalBGSizeDelta = background.sizeDelta;
        _originalIconLocalPositionY = icon.localPosition.y;
        icon.localPosition = new Vector2(icon.localPosition.x, iconStartingY);
        
        if (titleText != null) titleText.gameObject.SetActive(false);
        if (subTitleText != null) subTitleText.gameObject.SetActive(false);
        if (descriptionText != null) descriptionText.gameObject.SetActive(false);

        if (infoScreenMask != null) infoScreenMask.padding = new Vector4(0,0 ,0 , infoScreenMaskStartingTop);
        background.sizeDelta = new Vector2 (background.sizeDelta.x, 0);

        if(playOnStart) StartCoroutine(EnterAnimation());
    }

    public IEnumerator EnterAnimation()
    {
        background.DOSizeDelta(_originalBGSizeDelta, backgroundTweenDuration).SetEase(Ease.OutBack);
        icon.DOLocalMoveY(_originalIconLocalPositionY, iconTweenDuration).SetEase(Ease.OutBack);

        if (infoScreenMask != null) DOTween.To(
            () => infoScreenMask.padding.w,
            w => infoScreenMask.padding = new Vector4(0,0, 0, w),
            0,
            backgroundTweenDuration).SetEase(Ease.OutBack);

        yield return new WaitForSeconds(contentDelay);
        if (titleText != null) titleText.gameObject.SetActive(true);
        if (subTitleText != null) subTitleText.gameObject.SetActive(true);
        if (descriptionText != null) descriptionText.gameObject.SetActive(true);
       
    }


}
