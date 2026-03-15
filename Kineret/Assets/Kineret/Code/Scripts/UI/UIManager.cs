using DG.Tweening;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEditor.MPE;
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

    [SerializeField] private PopupData routePopupData;
    [SerializeField] private float routePopupDelay;

    [SerializeField] private Animator blackPanel;
    [SerializeField] private float blackFadeDuration;

    private PopupHandler _currentPopup;

    private void OnEnable()
    {
        loadPopup_EC.OnEventRaised += LoadPopup;
        EventsRelay.OnLoadInfoScreen += LoadInfoScreen;
    }

    private void OnDisable()
    {
        loadPopup_EC.OnEventRaised -= LoadPopup;
        EventsRelay.OnLoadInfoScreen -= LoadInfoScreen;
    }


    private void Start()
    {
        blackPanel.gameObject.SetActive(true);
        StartCoroutine(BlackFade());
    }

    private IEnumerator BlackFade()
    {
        //blackPanel.GetCurrentAnimatorClipInfo(0)[0].clip.length
        yield return new WaitForSeconds(2.1f);
        EventsRelay.OnGamePause.Invoke(false);
        StartCoroutine(LoadRoutePopup());
        blackPanel.gameObject.SetActive(false);
    }

    private IEnumerator LoadRoutePopup()
    {
        yield return new WaitForSeconds(routePopupDelay);
        LoadPopup(routePopupData);
    }

    private void LoadPopup(PopupData data)
    {
        if(_currentPopup != null) Destroy(_currentPopup.gameObject);

        switch ((PopUpType)data.PopupTextData.Type)
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
