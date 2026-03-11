using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public static bool IsLoadingDestinationSelection = false;

    [SerializeField] private GameSettings gameSettings;
    [SerializeField] private float showTutorialDelay;
    [SerializeField] private float showGameButtonDelay;
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
    [SerializeField] private PopupTweenHandler destinationsSummaryPopup;
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
        Debug.Log("Destinations selected:" + _selectedDestinationsCount);
        if (_selectedDestinationsCount == gameSettings.SelectionDestinationCount) 
            EndDestinationSelection();

    }

    private void HandleDestinationDeselection()
    {
        _selectedDestinationsCount--;
        Debug.Log("Destinations selected:" + _selectedDestinationsCount);
    }

    private void EndDestinationSelection()
    {
        Debug.Log("Ending destination selection");
        enableDestinationSelection_EC.RaiseEvent(false);
        StartCoroutine(ShowButtomCoro());
        destinationsSummaryPopup.gameObject.SetActive(true);
        destinationsSummaryPopup.Play((int)PlayMode.Default);
        destinationsSummaryPopup.StartCoroutine(destinationsSummaryPopup.PlayIconYoyo(showTutorialDelay));
        explanationPopup.SetActive(false);
    }
    System.Collections.IEnumerator ShowButtomCoro()
    {
        yield return new WaitForSeconds(showTutorialDelay);
        destinationsSummaryPopup.gameObject.SetActive(false);
        toturialPopup.gameObject.SetActive(true);
        toturialPopup.Play((int)PlayMode.Default);
        yield return new WaitForSeconds(showGameButtonDelay);
        showToturialButton.SetActive(true);
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
