using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public enum PlayMode
{
    Default,WidthOpenFirst
}
public enum TextMode
{
    Run, Fade
}
public class PopupTweenHandler : MonoBehaviour
{
    private const float LETTER_DELAY = 0.01f;
    private const float TEXT_DELAY = 0.2f;
    private const float TEXT_FADE = 0.3f;

    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform icon;
    [SerializeField] private TMP_Text[] TMPtexts;
    [SerializeField] private RectMask2D infoScreenMask;

    [SerializeField] private float backgroundTweenDuration;
    [SerializeField] private float iconTweenDuration;
    [SerializeField] private float contentDelay;
    [SerializeField] private float iconStartingY;
    [SerializeField] private float infoScreenMaskStartingTop;

    [SerializeField] private float startingHeightForWidthFirst;
    [SerializeField] private float iconMaxPulse;

    [SerializeField] private bool playOnStart;
    [SerializeField] private TextMode textMode;
    [SerializeField] private PlayMode startPlayMode;

    [SerializeField] private bool playSFX;

    private Vector2 _originalBGSizeDelta;
    private float _originalIconLocalPositionY;
    private Tween _iconTween;

    public event UnityAction OnTextFinishedLoading;

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

        foreach (var text in TMPtexts)
        {
            text.gameObject.SetActive(false);
        }      
    }

    private void Start()
    {
        if (playOnStart) Play((int)startPlayMode);
    }

    private void OnDestroy()
    {
        if(background != null) background.DOKill();
        if(infoScreenMask != null) infoScreenMask.DOKill();

        foreach (var text in TMPtexts)
        {
            if (text != null) text.DOKill();
        }

        _iconTween?.Kill();
    }
    public void ResetPopup()
    {
        foreach (TMP_Text text in TMPtexts)
        {
            //how to reset so toggle is a thing or avticate without playing animation.
        }
    }
    public void Play(int mode)
    {
        if (playSFX) { AudioManager.Instance.PlayOpenUI(); }

        if(mode == (int)PlayMode.WidthOpenFirst)
        {
            StartCoroutine(PlayWidthFirstAnimation());
            return;
        }

        StartCoroutine(PlayAnimation());
    }

    private IEnumerator PlayAnimation()
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

        PlayText();
    }

    private IEnumerator PlayWidthFirstAnimation()
    {
        if (background == null) yield break;

        background.sizeDelta = new Vector2(0, startingHeightForWidthFirst);
        Tween tween = background.DOSizeDelta(new Vector2(_originalBGSizeDelta.x, startingHeightForWidthFirst), backgroundTweenDuration/2f).SetEase(Ease.OutBack);

        yield return tween.WaitForCompletion();

        background.DOSizeDelta(_originalBGSizeDelta, backgroundTweenDuration/2f).SetEase(Ease.OutBack);


        yield return new WaitForSeconds(contentDelay);
        PlayText();
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

    private void PlayText()
    {
        EventsRelay.OnTextStarted?.Invoke();
        switch (textMode)
        {
            case TextMode.Fade:
                StartCoroutine(PlayFadingText());
                break;
            case TextMode.Run:
                StartCoroutine(PlayRunningText());
                break;
        }
    }

    private IEnumerator PlayFadingText()
    {
        foreach (var text in TMPtexts)
        {
            text.gameObject.SetActive(true);
            text.color = new Color(text.color.r, text.color.g, text.color.b, 0);
            text.DOFade(1f, TEXT_FADE);
            yield return new WaitForSeconds(TEXT_DELAY);
        }
        OnTextFinishedLoading?.Invoke();
    }

    private IEnumerator PlayRunningText()
    {
        Queue<char>[] texts =  new Queue<char>[TMPtexts.Length];
        string currentText;
        for (int i = 0; i < TMPtexts.Length; i++)
        {
            TMPtexts[i].gameObject.SetActive(true);
            currentText = TMPtexts[i].text;
            texts[i] = new Queue<char>(currentText.Length);

            foreach (var ch in currentText)
            {
                texts[i].Enqueue(ch);
            }

            TMPtexts[i].text = "";
        }

        bool isMarkdownActive = false;
        string markdownEnd = string.Empty;
        for (int i = 0; i < TMPtexts.Length; i++)
        {
            while(texts[i].Count > 0)
            {
                char ch = texts[i].Dequeue();

                if(ch == '<' && texts[i].Peek() != '/')
                {
                    string markdownStart = $"{ch}{texts[i].Dequeue()}{texts[i].Dequeue()}";
                    TMPtexts[i].text += markdownStart;
                    markdownEnd += '<';
                    markdownEnd += '/';
                    markdownEnd += markdownStart[1];
                    markdownEnd += '>';
                    TMPtexts[i].text += markdownEnd;
                    isMarkdownActive = true;
                    ch = texts[i].Dequeue();
                }

                if (ch == '<' && texts[i].Peek() == '/')
                {
                    texts[i].Dequeue();
                    texts[i].Dequeue();
                    texts[i].Dequeue();
                    texts[i].Dequeue();       
                    isMarkdownActive = false;
                    markdownEnd = string.Empty;
                    ch = texts[i].Dequeue();
                }

                if (isMarkdownActive)
                {
                    TMPtexts[i].text = TMPtexts[i].text.Substring(0, TMPtexts[i].text.Length - 4);
                    TMPtexts[i].text += ch;
                    TMPtexts[i].text += markdownEnd;
                }
                else
                {
                    TMPtexts[i].text += ch;
                }                
                yield return new WaitForSeconds(LETTER_DELAY);
            }      
        }

        OnTextFinishedLoading?.Invoke();
    }
}
