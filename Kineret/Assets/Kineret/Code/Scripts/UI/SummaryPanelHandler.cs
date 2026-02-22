using DG.Tweening;
using System.Collections;
using TMPro;
using Unity.VisualScripting;
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

        titleText.color = titleText.color.WithAlpha(0f);
        scoreTitleText.color = scoreText.color.WithAlpha(0f);
        scoreText.color = scoreText.color.WithAlpha(0f);
        endText.color = endText.color.WithAlpha(0f);
        scoreImage.color = scoreImage.color.WithAlpha(0f);
        mainMenuButton.image.color = mainMenuButton.image.color.WithAlpha(0f);
        destinationsButton.image.color = destinationsButton.image.color.WithAlpha(0f);
        mainMenuButtonText.color = mainMenuButtonText.color.WithAlpha(0f);
        destinationsButtonText.color = destinationsButtonText.color.WithAlpha(0f);

        Vector2 size = popupBackground.sizeDelta;
        popupBackground.sizeDelta = new Vector2(size.x, 0f);

        parent.anchoredPosition = new Vector2(parent.anchoredPosition.x, parent.sizeDelta.y);
        Tween tween = parent.DOMoveY(parent.sizeDelta.y / 2f, enterDuration);
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
    }
}
