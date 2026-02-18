using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum PlayMode
{
    Default,WidthOpenFirst
}

public class PopupTweenHandler : MonoBehaviour
{

    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform icon;
    [SerializeField] private TMP_Text[] texts;
    [SerializeField] private RectMask2D infoScreenMask;

    [SerializeField] private float backgroundTweenDuration;
    [SerializeField] private float iconTweenDuration;
    [SerializeField] private float contentDelay;
    [SerializeField] private float iconStartingY;
    [SerializeField] private float infoScreenMaskStartingTop;

    [SerializeField] private float startingHeightForWidthFirst;
    [SerializeField] private float iconMaxPulse;

    [SerializeField] private bool playOnStart;

    private Vector2 _originalBGSizeDelta;
    private float _originalIconLocalPositionY;
    private Tween _iconTween;

    private void Awake()
    {
        if (background != null)
        {
            _originalBGSizeDelta = background.sizeDelta;
            background.sizeDelta = new Vector2(background.sizeDelta.x, 0);
        }

        if (icon != null)
        {
            _originalIconLocalPositionY = icon.localPosition.y;
            icon.localPosition = new Vector2(icon.localPosition.x, iconStartingY);
        }

        if (infoScreenMask != null)
        {
            infoScreenMask.padding = new Vector4(0, 0, 0, infoScreenMaskStartingTop);
        }

        foreach (var text in texts)
        {
            text.gameObject.SetActive(false);
        }

        if (playOnStart) StartCoroutine(PlayAnimation());
    }
    private void OnDestroy()
    {
        if(_iconTween != null) _iconTween.Kill();
    }
    public void Play(int mode)
    {
        if(mode == (int)PlayMode.WidthOpenFirst)
        {
            StartCoroutine(PlayWidthFirstAnimation());
            return;
        }

        StartCoroutine(PlayAnimation());
    }

    public IEnumerator PlayAnimation()
    {
        if (background != null)
        {
            background.DOSizeDelta(_originalBGSizeDelta, backgroundTweenDuration).SetEase(Ease.OutBack);
        }
       
        if (icon != null)
        {
            _iconTween =  icon.DOLocalMoveY(_originalIconLocalPositionY, iconTweenDuration).SetEase(Ease.OutBack);
        }

        if (infoScreenMask != null)
        {
            DOTween.To(
            () => infoScreenMask.padding.w,
            w => infoScreenMask.padding = new Vector4(0, 0, 0, w),
            0,
            backgroundTweenDuration).SetEase(Ease.OutBack);
        }

        yield return new WaitForSeconds(contentDelay);

        foreach (var text in texts)
        {
            text.gameObject.SetActive(true);
        }
    }

    public IEnumerator PlayWidthFirstAnimation()
    {
        if (background == null) yield break;

        background.sizeDelta = new Vector2(0, startingHeightForWidthFirst);
        Tween tween = background.DOSizeDelta(new Vector2(_originalBGSizeDelta.x, startingHeightForWidthFirst), backgroundTweenDuration/2f).SetEase(Ease.OutBack);

        yield return tween.WaitForCompletion();

        background.DOSizeDelta(_originalBGSizeDelta, backgroundTweenDuration/2f).SetEase(Ease.OutBack);


        yield return new WaitForSeconds(contentDelay);

        foreach (var text in texts)
        {
            text.gameObject.SetActive(true);
        }
    }

    public IEnumerator PlayIconYoyo(float duration)
    {
        if(_iconTween != null && !_iconTween.IsComplete())
        {
            yield return _iconTween.WaitForCompletion();
        }

        Vector3 originalScale = icon.localScale;
        _iconTween = icon.DOScale(iconMaxPulse, duration/8f).SetLoops(-1,LoopType.Yoyo);

        yield return new WaitForSeconds(duration);

        icon.localScale = originalScale;
    }

}
