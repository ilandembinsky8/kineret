using DG.Tweening;
using System.Collections;
using TMPro;

using UnityEngine;
using UnityEngine.UI;


public class SummaryPanelHandler : MonoBehaviour
{

    [SerializeField] private RectTransform parent;
    [SerializeField] private RectTransform popupBackground;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text scoreTitleText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text endText;
    [SerializeField] private Image scoreImage;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button destinationsButton;
    [SerializeField] private TMP_Text mainMenuButtonText;
    [SerializeField] private TMP_Text destinationsButtonText;

    [SerializeField] private float enterDuration;
    [SerializeField] private float popupDuration;
    [SerializeField] private float contentFadeDuration;

    public IEnumerator EnterAnimation()
    {
        mainMenuButton.interactable = false;
        destinationsButton.interactable = false;

        titleText.color = new Color(titleText.color.r, titleText.color.g, titleText.color.b, 0);
        scoreTitleText.color = new Color(scoreTitleText.color.r, scoreTitleText.color.g, scoreTitleText.color.b, 0);
        scoreText.color = new Color(scoreText.color.r, scoreText.color.g, scoreText.color.b, 0);
        endText.color = new Color(endText.color.r, endText.color.g, endText.color.b, 0);
        scoreImage.color = new Color(scoreImage.color.r, scoreImage.color.g, scoreImage.color.b, 0);
        mainMenuButton.image.color = new Color(mainMenuButton.image.color.r, mainMenuButton.image.color.g, mainMenuButton.image.color.b, 0);
        destinationsButton.image.color =  new Color(destinationsButton.image.color.r, destinationsButton.image.color.g, destinationsButton.image.color.b, 0);
        mainMenuButtonText.color = new Color(mainMenuButtonText.color.r, mainMenuButtonText.color.g, mainMenuButtonText.color.b, 0);
        destinationsButtonText.color = new Color(destinationsButtonText.color.r, destinationsButtonText.color.g, destinationsButtonText.color.b, 0);

        Vector2 size = popupBackground.sizeDelta;
        popupBackground.sizeDelta = new Vector2(size.x, 0f);

        float height = GameSettingsManager.GetFloat("Game Settings", "ResolutionHeight", 2160f);

        parent.anchoredPosition = new Vector2(parent.anchoredPosition.x, height);
        Tween tween = parent.DOMoveY(height / 2f, enterDuration);
        yield return tween.WaitForCompletion();   
        tween = popupBackground.DOSizeDelta(size, popupDuration).SetEase(Ease.OutBack);
        yield return tween.WaitForCompletion();
        tween = titleText.DOFade(1f, contentFadeDuration);
        yield return tween.WaitForCompletion();
        tween = scoreTitleText.DOFade(1f, contentFadeDuration);
        yield return tween.WaitForCompletion();
        tween = scoreText.DOFade(1f, contentFadeDuration);
        yield return tween.WaitForCompletion();
        tween = scoreImage.DOFade(1f, contentFadeDuration);
        yield return tween.WaitForCompletion();
        tween = endText.DOFade(1f, contentFadeDuration);
        yield return tween.WaitForCompletion();
        tween = mainMenuButton.image.DOFade(1f, contentFadeDuration);
        destinationsButton.image.DOFade(1f, contentFadeDuration);
        mainMenuButtonText.DOFade(1f, contentFadeDuration);
        destinationsButtonText.DOFade(1f, contentFadeDuration);
        yield return tween.WaitForCompletion();
        mainMenuButton.interactable = true;
        destinationsButton.interactable = true;
        EventsRelay.OnSummaryScreenReady?.Invoke();
    }
}
