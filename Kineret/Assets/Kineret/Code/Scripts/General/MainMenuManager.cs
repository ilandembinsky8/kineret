using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public static bool IsLoadingDestinationSelection = false;

    [SerializeField] private GameSettings gameSettings;
    [SerializeField] private float showTutorialDelay;
    [SerializeField] private float showGameButtonDelay;
    [Header("Event Channels")]
    [SerializeField] private BoolEventChannel enableDestinationSelection_EC;

    [Header("Canvases")]
    [SerializeField] private GameObject mainMenuCanvas;
    [SerializeField] private GameObject destinationSelectCanvas;
    [SerializeField] private GameObject controlButtonsCanvas;

    [Header("UI Elements")]
    [SerializeField] private GameObject explanationPopup;
    [SerializeField] private PopupTweenHandler destinationsSummaryPopup;
    [SerializeField] private TMP_Text firstDestinationText;
    [SerializeField] private TMP_Text secondDestinationText;
    [SerializeField] private TMP_Text thirdDestinationText;
    [SerializeField] private PopupTweenHandler toturialPopup;
   
    [SerializeField] private GameObject showToturialButton;
    [SerializeField] private GameObject startGameButton;
    [SerializeField] private Animator blackPanel;

    [Header("Values")]
    [SerializeField] private float blackFadeDuration;

    private int _selectedDestinationsCount;

    private void Awake()
    {
        mainMenuCanvas.SetActive(!IsLoadingDestinationSelection);
        destinationSelectCanvas.SetActive(IsLoadingDestinationSelection);
        controlButtonsCanvas.SetActive(true);
        explanationPopup.SetActive(true);
        destinationsSummaryPopup.gameObject.SetActive(false);
        toturialPopup.gameObject.SetActive(false);

        showToturialButton.SetActive(false);
        startGameButton.SetActive(false);
    }

    private void OnEnable()
    {
        EventsRelay.OnDestinationSelected += HandleDestinationSelection;
        EventsRelay.OnDestinationDeselected += HandleDestinationDeselection;
    }

    private void OnDisable()
    {
        EventsRelay.OnDestinationSelected -= HandleDestinationSelection;
        EventsRelay.OnDestinationDeselected -= HandleDestinationDeselection;
    }

    private void Start()
    {
        if (IsLoadingDestinationSelection) StartDestinationSelection();
    }

    public void OnStartGameButton()
    {
        IdleManager.IsTicking = true;
        StartDestinationSelection();
    }

    public void StartDestinationSelection()
    {
        _selectedDestinationsCount = 0;
        enableDestinationSelection_EC.RaiseEvent(true);
    }

    private void HandleDestinationSelection(int destination)
    {  
        LocationsManager.SelectedDestinations[_selectedDestinationsCount] = destination;
        _selectedDestinationsCount++;
        if (_selectedDestinationsCount == LocationsManager.DestinationsSelectCount)
        {
            EndDestinationSelection();
        }
    }

    private void HandleDestinationDeselection()
    {
        _selectedDestinationsCount--;
    }

    private void EndDestinationSelection()
    {
        enableDestinationSelection_EC.RaiseEvent(false);
        StartCoroutine(LoadToturial());
        destinationsSummaryPopup.gameObject.SetActive(true);

        firstDestinationText.text = LocationsManager.GetDestination(LocationsManager.SelectedDestinations[0]).Data.UIDestinationInfoText.HebTitle;
        secondDestinationText.text = LocationsManager.GetDestination(LocationsManager.SelectedDestinations[1]).Data.UIDestinationInfoText.HebTitle;
        thirdDestinationText.text = LocationsManager.GetDestination(LocationsManager.SelectedDestinations[2]).Data.UIDestinationInfoText.HebTitle;

        destinationsSummaryPopup.Play((int)PlayMode.Default);
        //destinationsSummaryPopup.StartCoroutine(destinationsSummaryPopup.PlayIconYoyo(showTutorialDelay));
        explanationPopup.SetActive(false);
    }
    private IEnumerator LoadToturial()
    {
        yield return new WaitForSeconds(showTutorialDelay);
        destinationsSummaryPopup.gameObject.SetActive(false);
        toturialPopup.gameObject.SetActive(true);
        toturialPopup.Play((int)PlayMode.Default);
        yield return new WaitForSeconds(showGameButtonDelay);
        startGameButton.SetActive(true);
    }
    public void StartGame()
    {
        blackPanel.gameObject.SetActive(true);
        StartCoroutine(BlackFade());
    }
    private IEnumerator BlackFade()
    {
        yield return new WaitForSeconds(2.5f);
        SceneManager.LoadScene("Game Scene");
    }
    


}
