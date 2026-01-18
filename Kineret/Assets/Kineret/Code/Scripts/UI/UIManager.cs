using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Canvas canvas;

    [SerializeField] private InfoScreenHandler infoScreen;
    [SerializeField] private PopupHandler infoPopupPrefab;
    [SerializeField] private PopupHandler titleOnlyPopupPrefab;
    [SerializeField] private PopupHandler fullPopupPrefab;
    [SerializeField] private PopupHandler highFullPopupPrefab;

    [SerializeField] private PopupDataEventChannel loadPopup_EC;
    [SerializeField] private InfoScreenDataEventChannel loadInfoScreen_EC;

    [SerializeField] private PopupData routePopupData;

    private PopupHandler _currentPopup;

    private void Awake()
    {
        LoadPopup(routePopupData);
    }

    private void OnEnable()
    {
        loadPopup_EC.OnEventRaised += LoadPopup;
        loadInfoScreen_EC.OnEventRaised += LoadInfoScreen;
    }

    private void OnDisable()
    {
        loadPopup_EC.OnEventRaised -= LoadPopup;
        loadInfoScreen_EC.OnEventRaised -= LoadInfoScreen;
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
        InfoScreenHandler handler = Instantiate(infoScreen, canvas.transform);
        infoScreen.LoadData(data);
    }
}
