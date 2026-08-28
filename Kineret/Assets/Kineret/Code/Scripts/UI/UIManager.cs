using System.Collections;
using UnityEngine;
using TMPro;
using System;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Canvas canvas;

    [SerializeField] private InfoScreenHandler infoScreenPrefab;
    [SerializeField] private PopupHandler infoPopupPrefab;
    [SerializeField] private PopupHandler titleOnlyPopupPrefab;
    [SerializeField] private PopupHandler fullPopupPrefab;
    [SerializeField] private PopupHandler highFullPopupPrefab;

    [SerializeField] private PopupDataEventChannel loadPopup_EC;

    [SerializeField] private TMP_Text usernameText;

    [SerializeField] private PopupData routePopupData;
    [SerializeField] private float routePopupDelay;

    [SerializeField] private PopupData leftArrowPopupData;
    [SerializeField] private PopupData rightArrowPopupData;

    [SerializeField] private Animator blackPanel;
    [SerializeField] private float blackFadeDuration;

    private PopupHandler _currentPopup;

    private void OnEnable()
    {
        loadPopup_EC.OnEventRaised += HandleLoadPopup;
        EventsRelay.OnLoadInfoScreen += LoadInfoScreen;
        EventsRelay.OnLoadDirectionPopup += HandleDirectionPopup;
        KineretTerrainBootstrap.OnTerrainReady += OnTerrainReady;
    }
    private void OnDisable()
    {
        loadPopup_EC.OnEventRaised -= HandleLoadPopup;
        EventsRelay.OnLoadInfoScreen -= LoadInfoScreen;
        EventsRelay.OnLoadDirectionPopup -= HandleDirectionPopup;
        KineretTerrainBootstrap.OnTerrainReady -= OnTerrainReady;
    }
    private void Start()
    {
        usernameText.text = $"| {HighscoresManager.Instance.GetUsername(HighscoresManager.Instance.CurrentUserID)}";
        blackPanel.gameObject.SetActive(true);
        StartCoroutine(BlackFade());
        EventsRelay.OnGameStart?.Invoke();
    }

    private IEnumerator BlackFade()
    {
        //blackPanel.GetCurrentAnimatorClipInfo(0)[0].clip.length
        yield return new WaitForSeconds(2.1f);
        EventsRelay.OnGamePause.Invoke(false);
        EventsRelay.OnStartScoreCountdown.Invoke();
        AudioManager.Instance.PlayFlightMusic();
        StartCoroutine(LoadRoutePopup());
    }
    private void HandleDirectionPopup(string direction)
    {
        if (direction == "Left")
        {
            LoadPopup(leftArrowPopupData, 0.6f);
            return;
        }

        LoadPopup(rightArrowPopupData, 0.6f);
    }

    private void HandleLoadPopup(PopupData data)
    {
        LoadPopup(data, 0);
    }

    private IEnumerator LoadRoutePopup()
    {
        yield return new WaitForSeconds(routePopupDelay);
        LoadPopup(routePopupData, 0);
    }

    private void LoadPopup(PopupData data, float scaleMultiplier)
    {
        PopUpType popupType = (PopUpType)data.PopupTextData.Type;

        if (_currentPopup != null)
        {
            bool sameFullPopup = popupType == PopUpType.Full;

            if (sameFullPopup)
            {
                _currentPopup.ChangeDataAndReplayText(data);
                return;
            }

            Destroy(_currentPopup.gameObject);
        }

        switch (popupType)
        {
            case PopUpType.Info:
                _currentPopup = Instantiate(infoPopupPrefab, canvas.transform);
                break;

            case PopUpType.TitleOnly:
                _currentPopup = Instantiate(titleOnlyPopupPrefab, canvas.transform);
                break;

            case PopUpType.Full:
                _currentPopup = Instantiate(fullPopupPrefab, canvas.transform);
                break;

            case PopUpType.HighFull:
                _currentPopup = Instantiate(highFullPopupPrefab, canvas.transform);
                break;
        }

        _currentPopup.LoadData(data);

        if (scaleMultiplier != 0)
        {
            _currentPopup.ScaleIconeSize(scaleMultiplier);
        }
    }

    private void LoadInfoScreen(InfoScreenData data)
    {
        bool isSubtitleEmpty = data.Subtitle == "";

        InfoScreenHandler handler = Instantiate(infoScreenPrefab, canvas.transform);
        handler.LoadData(data, isSubtitleEmpty);
    }
    private void OnTerrainReady()
    {
        blackPanel.SetTrigger("FadeOut");
    }

}