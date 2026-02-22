using DG.Tweening;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Canvas canvas;

    [SerializeField] private InfoScreenHandler infoScreenPrefab;
    [SerializeField] private PopupHandler infoPopupPrefab;
    [SerializeField] private PopupHandler titleOnlyPopupPrefab;
    [SerializeField] private PopupHandler fullPopupPrefab;
    [SerializeField] private PopupHandler highFullPopupPrefab;

    [SerializeField] private PopupDataEventChannel loadPopup_EC;
    [SerializeField] private InfoScreenDataEventChannel loadInfoScreen_EC;

    [SerializeField] private PopupData routePopupData;
    [SerializeField] private float routePopupDelay;

    [SerializeField] private RectTransform blackPanel;
    [SerializeField] private float blackFadeDuration;

    private PopupHandler _currentPopup;

    private void OnEnable()
    {
        loadPopup_EC.OnEventRaised += LoadPopup;
        loadInfoScreen_EC.OnEventRaised += LoadInfoScreen;
        GameManager.OnStartGame += HandleStartGame;
    }

    private void OnDisable()
    {
        loadPopup_EC.OnEventRaised -= LoadPopup;
        loadInfoScreen_EC.OnEventRaised -= LoadInfoScreen;
        GameManager.OnStartGame -= HandleStartGame;
    }


    private void Start()
    {
        StartCoroutine(BlackFade());
    }

    private IEnumerator BlackFade()
    {
        blackPanel.gameObject.SetActive(true);
        Tween tween = blackPanel.DOSizeDelta(Vector2.zero, blackFadeDuration);
        yield return tween.WaitForCompletion();
        GameManager.OnStartGame.Invoke();
    }

    private void HandleStartGame()
    {
        StartCoroutine(LoadRoutePopup());
    }
    private IEnumerator LoadRoutePopup()
    {
        yield return new WaitForSeconds(routePopupDelay);
        LoadPopup(routePopupData);
    }

    private void LoadPopup(PopupData data)
    {
        if(_currentPopup != null) Destroy(_currentPopup.gameObject);

        switch (data.Type)
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
    }

    private void LoadInfoScreen(InfoScreenData data)
    {
        InfoScreenHandler handler = Instantiate(infoScreenPrefab, canvas.transform);
        handler.LoadData(data);
    }
}
