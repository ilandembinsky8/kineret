using DG.Tweening;
using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class InfoScreenHandler : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private TMP_Text descriptionText;

    [SerializeField] private Image iconImage;
    [SerializeField] private Image logoImage;
    [SerializeField] private Image backgroundImage;

    [SerializeField] private Button continueGameButton;
    [SerializeField] private Button endGameButton;

    [SerializeField] private RectTransform parent;
    [SerializeField] private PopupTweenHandler tweenHandler;
    [SerializeField] private float enterDuration;

    private void Awake()
    {
        continueGameButton.gameObject.SetActive(false);
        endGameButton.gameObject.SetActive(false);    
    }
    private void Start()
    {
        StartCoroutine(EnterAnimation());
    }
    private void OnEnable()
    {
        tweenHandler.OnTextFinishedLoading += HandleTextFinishedLoading;
    }
    private void OnDisable()
    {
        tweenHandler.OnTextFinishedLoading -= HandleTextFinishedLoading;
    }
    public void LoadData(InfoScreenData data)
    {
        continueGameButton.gameObject.SetActive(!data.isFinal);
        endGameButton.gameObject.SetActive(data.isFinal);

        if (data.Title != null && titleText != null)
        {
            titleText.text = data.Title;
            titleText.fontStyle = FontStyles.Bold;
        }

        if (data.Subtitle != null && subtitleText != null)
        {
            subtitleText.text = data.Subtitle;
            subtitleText.fontStyle = FontStyles.Bold;
        }
        
        if (data.Text != null && descriptionText != null) descriptionText.text = data.Text;

        if (data.Icon != null && iconImage != null)
        {
            iconImage.sprite = data.Icon;
            ((RectTransform)iconImage.transform).sizeDelta = new Vector2(iconImage.sprite.texture.width, iconImage.sprite.texture.height);
        }

        if (data.Logo != null && logoImage != null)
        {
            logoImage.sprite = data.Logo;
            ((RectTransform)logoImage.transform).sizeDelta = new Vector2(logoImage.sprite.texture.width * data.logoSizeMultiplier, logoImage.sprite.texture.height * data.logoSizeMultiplier);
        }

        if (data.Background != null && backgroundImage != null)
        {
            backgroundImage.sprite = data.Background;
        }
    }

   
    public void CloseScreen()
    {
        EventsRelay.OnGamePause.Invoke(false);
        EventsRelay.OnStartScoreCountdown.Invoke();
        Destroy(gameObject);
    }

    public void GameOver()
    {
        EventsRelay.OnGameOver.Invoke();
        Destroy(gameObject,3f);
    }

    private IEnumerator EnterAnimation()
    {
        continueGameButton.interactable = false;
        endGameButton.interactable = false;
        parent.anchoredPosition = new Vector2(parent.anchoredPosition.x, parent.sizeDelta.y);
        Tween tween = parent.DOMoveY(parent.sizeDelta.y/2f, enterDuration);
        yield return tween.WaitForCompletion();
        FlagHandler.EndFlagAnimation.Invoke();
       
        tweenHandler.StartCoroutine(tweenHandler.PlayAnimation());
    }

    private void HandleTextFinishedLoading()
    {
        continueGameButton.interactable = true;
        endGameButton.interactable = true;
    }
}
