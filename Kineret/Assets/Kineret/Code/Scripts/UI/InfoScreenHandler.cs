using TMPro;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

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

    private string _destinationName;
    private bool _isWaitingForTextStart;
    private float _maxLogoWidth = 1500;
    private bool _isSubtitleEmpty;

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
        EventsRelay.OnTextStarted += HandleTextStarted;
        tweenHandler.OnTextFinishedLoading += HandleTextFinishedLoading;
    }
    private void OnDisable()
    {
        EventsRelay.OnTextStarted -= HandleTextStarted;
        tweenHandler.OnTextFinishedLoading -= HandleTextFinishedLoading;
    }
    
    public void LoadData(InfoScreenData data, bool isSubtitleEmpty)
    {
        continueGameButton.gameObject.SetActive(!data.isFinal);
        endGameButton.gameObject.SetActive(data.isFinal);

        _destinationName = data.CodeName;
        _isSubtitleEmpty = isSubtitleEmpty;

        if (data.Title != null && titleText != null)
        {
            titleText.text = data.Title;
            titleText.fontStyle = FontStyles.Bold;
        }

        if (data.Subtitle != null && subtitleText != null)
        {
            subtitleText.text = data.Subtitle;
            subtitleText.fontStyle = FontStyles.Bold;
            if (data.CodeName == "BioCastle")
            { subtitleText.enableWordWrapping = false; }
            else { subtitleText.enableWordWrapping = true; }
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
            float calculatedWidth = logoImage.sprite.texture.width * data.logoSizeMultiplier;
            float calculatedHeight = logoImage.sprite.texture.height * data.logoSizeMultiplier;

            if (calculatedWidth > _maxLogoWidth)
            {
                float scale = _maxLogoWidth / calculatedWidth;
                calculatedWidth = _maxLogoWidth;
                calculatedHeight *= scale;
            }

            ((RectTransform)logoImage.transform).sizeDelta = new Vector2(calculatedWidth, calculatedHeight);
        }

        if (data.Background != null && backgroundImage != null)
        {
            backgroundImage.sprite = data.Background;
        }
    }

    public void CloseScreen()
    {
        AudioManager.Instance.StopNarration();
        AudioManager.Instance.SetIncreasedMusicVolume();
        EventsRelay.OnGamePause.Invoke(false);
        EventsRelay.OnStartScoreCountdown.Invoke();
        EventsRelay.OnShowDirection.Invoke();
        Destroy(gameObject);
    }

    public void GameOver()
    {
        AudioManager.Instance.StopNarration();
        AudioManager.Instance.SetIncreasedMusicVolume();
        EventsRelay.OnGameOver.Invoke();
        Destroy(gameObject, 3f);
    }

    private IEnumerator EnterAnimation()
    {
        continueGameButton.interactable = false;
        endGameButton.interactable = false;

        float height = GameSettingsManager.GetFloat("Game Settings", "ResolutionHeight", 2160f);
        parent.anchoredPosition = new Vector2(parent.anchoredPosition.x, height);
        Tween tween = parent.DOMoveY(height / 2f, enterDuration);
        yield return tween.WaitForCompletion();
        FlagHandler.EndFlagAnimation.Invoke();

        if (_isSubtitleEmpty) { descriptionText.rectTransform.localPosition += new Vector3(0, 75, 0); }

        _isWaitingForTextStart = true;
        tweenHandler.Play((int)PlayMode.Default);
    }

    private void HandleTextStarted()
    {
        if (_isWaitingForTextStart)
        {
            _isWaitingForTextStart = false;
            AudioManager.Instance.PlayDestinationNarration(_destinationName);
        }
    }

    private void HandleTextFinishedLoading()
    {
        continueGameButton.interactable = true;
        endGameButton.interactable = true;
    }

}