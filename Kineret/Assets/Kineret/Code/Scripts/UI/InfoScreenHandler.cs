using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InfoScreenHandler : MonoBehaviour
{
    [SerializeField] private BoolEventChannel gamePause_EC;
    [SerializeField] private VoidEventChannel gameOver_EC;

    [SerializeField] private InfoScreenData data;

    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private TMP_Text descriptionText;

    [SerializeField] private Image iconImage;
    [SerializeField] private Image logoImage;
    [SerializeField] private Image backgroundImage;

    [SerializeField] private GameObject continueGameButton;
    [SerializeField] private GameObject endGameButton;

    [SerializeField] private RectTransform parent;
    [SerializeField] private PopupTweenHandler tweenHandler;
    [SerializeField] private float enterDuration;
    private bool _wasLoadedOnAwake;

    private void Awake()
    {
        continueGameButton.SetActive(false);
        endGameButton.SetActive(false);
        if (data != null)
        {
            Debug.Log("Data isnt null at awake");
            LoadData(data);
            _wasLoadedOnAwake = true;
        }

       
    }
    private void Start()
    {
        StartCoroutine(EnterAnimation());
    }
    public void LoadData(InfoScreenData data)
    {
        if(_wasLoadedOnAwake) return;

        if (data == null)
        {
            Destroy(gameObject);
            return;
        }

        continueGameButton.SetActive(!data.IsFinal);
        endGameButton.SetActive(data.IsFinal);

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
        
        if (data.Description != null && descriptionText != null) descriptionText.text = data.Description;

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
        gamePause_EC.RaiseEvent(false);
        Destroy(gameObject);
    }

    public void GameOver()
    {
        gameOver_EC.RaiseEvent();
        Destroy(gameObject);
    }

    private IEnumerator EnterAnimation()
    {
        parent.anchoredPosition = new Vector2(parent.anchoredPosition.x, parent.sizeDelta.y);
        Tween tween = parent.DOMoveY(parent.sizeDelta.y/2f, enterDuration);
        yield return tween.WaitForCompletion();

        tweenHandler.StartCoroutine(tweenHandler.EnterAnimation());
    }
}
