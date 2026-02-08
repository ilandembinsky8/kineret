using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public static bool IsLoadingDestinationSelection = false;

    [SerializeField] private GameSettings gameSettings;
    [SerializeField] private float showTutorialBTNTimer;

    [Header("Event Channels")]
    [SerializeField] private VoidEventChannel destinationSelected_EC;
    [SerializeField] private VoidEventChannel destinationDeselected_EC;
    [SerializeField] private BoolEventChannel enableDestinationSelection_EC;

    [Header("Canvases")]
    [SerializeField] private GameObject mainMenuCanvas;
    [SerializeField] private GameObject destinationSelectCanvas;
    [SerializeField] private GameObject controlButtonsCanvas;

    [Header("UI Elements")]
    [SerializeField] private GameObject explanationPopup;
    [SerializeField] private GameObject destinationsSummaryPopup;
    [SerializeField] private GameObject toturialPopup;
   
    [SerializeField] private GameObject showToturialButton;
    [SerializeField] private GameObject startGameButton;

    private int _selectedDestinationsCount;

    private void Awake()
    {
        mainMenuCanvas.SetActive(!IsLoadingDestinationSelection);
        destinationSelectCanvas.SetActive(IsLoadingDestinationSelection);
        controlButtonsCanvas.SetActive(true);
        explanationPopup.SetActive(true);
        destinationsSummaryPopup.SetActive(false);
        toturialPopup.SetActive(false);

        showToturialButton.SetActive(false);
        startGameButton.SetActive(false);
    }

    private void OnEnable()
    {
        destinationSelected_EC.OnEventRaised += HandleDestinationSelection;
        destinationDeselected_EC.OnEventRaised += HandleDestinationDeselection;
    }

    private void OnDisable()
    {
        destinationSelected_EC.OnEventRaised -= HandleDestinationSelection;
        destinationDeselected_EC.OnEventRaised -= HandleDestinationDeselection;
    }

    private void Start()
    {
        if (IsLoadingDestinationSelection) StartDestinationSelection();
    }

    public void StartDestinationSelection()
    {
        _selectedDestinationsCount = 0;
        enableDestinationSelection_EC.RaiseEvent(true);
    }

    private void HandleDestinationSelection()
    {
        _selectedDestinationsCount++;

        Debug.Log("Destinations Selected" + _selectedDestinationsCount);

        if (_selectedDestinationsCount == gameSettings.DestinationCount) 
            EndDestinationSelection();

    }

    private void HandleDestinationDeselection()
    {
        _selectedDestinationsCount--;
        Debug.Log("Destinations Selected" + _selectedDestinationsCount);
    }

    private void EndDestinationSelection()
    {
        enableDestinationSelection_EC.RaiseEvent(false);
        StartCoroutine(ShowButtomCoro());
        destinationsSummaryPopup.SetActive(true);
        explanationPopup.SetActive(false);
    }
    System.Collections.IEnumerator ShowButtomCoro()
    {
        yield return new WaitForSeconds(showTutorialBTNTimer);
        destinationsSummaryPopup.SetActive(false);
        toturialPopup.SetActive(true);
        yield return new WaitForSeconds(showTutorialBTNTimer);
        showToturialButton.SetActive(true);
    }

    public void StartGame()
    {
        SceneManager.LoadScene("Game Scene");
    }
}
