using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class MainMenuManager : MonoBehaviour
{
    public static bool IsLoadingDestinationSelection = false;
    public static bool IsDestinationSelectionActive { get; private set; } = false;

    [SerializeField] private GameSettings gameSettings;
    [SerializeField] private MainMenuDestinationLoader menuDestinationLoader;
    [SerializeField] private float showTutorialDelay;
    [SerializeField] private float showGameButtonDelay;

    [Header("Canvases")]
    [SerializeField] private GameObject mainMenuCanvas;
    [SerializeField] private GameObject destinationSelectCanvas;
    [SerializeField] private GameObject controlButtonsCanvas;

    [Header("UI Elements")]
    [SerializeField] private CanvasGroup parentCanvasGroup;
    [SerializeField] private GameObject explanationPopup;
    [SerializeField] private PopupTweenHandler destinationsSummaryPopup;
    [SerializeField] private TMP_Text firstDestinationText;
    [SerializeField] private TMP_Text secondDestinationText;
    [SerializeField] private TMP_Text thirdDestinationText;
    [SerializeField] private TMP_Text highscoreText;
    [SerializeField] private TMP_Text usernameText;
    [SerializeField] private PopupTweenHandler toturialPopup;
    [SerializeField] private Button changeLanguageButton;
    [SerializeField] private Button toggleTutorialButton;

    [SerializeField] private GameObject showToturialButton;
    [SerializeField] private GameObject startGameButton;
    [SerializeField] private Animator blackPanel;

    [Header("Values")]
    [SerializeField] private float blackFadeDuration;

    private int _selectedDestinationsCount;
    private bool _isWaitingForInstructionText;

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

        changeLanguageButton.onClick.AddListener(ToggleLanguageChange);
        toggleTutorialButton.onClick.AddListener(ToggleTutorialPopup);
        toggleTutorialButton.gameObject.SetActive(true);
        changeLanguageButton.gameObject.SetActive(true);
    }

    private void OnEnable()
    {
        EventsRelay.OnDestinationSelected += HandleDestinationSelection;
        EventsRelay.OnDestinationDeselected += HandleDestinationDeselection;
        EventsRelay.OnTextStarted += PlayInstructionNarration;
    }

    private void OnDisable()
    {
        EventsRelay.OnDestinationSelected -= HandleDestinationSelection;
        EventsRelay.OnDestinationDeselected -= HandleDestinationDeselection;
        EventsRelay.OnTextStarted -= PlayInstructionNarration;
    }

    private void Start()
    {
        if (IsLoadingDestinationSelection) StartDestinationSelection();
    }

    public void OnStartGameButton()
    {
        parentCanvasGroup.alpha = 0f;
        IdleManager.IsTicking = true;
        HighscoresManager.Instance.AddUser();

        changeLanguageButton.gameObject.SetActive(false);
        toggleTutorialButton.gameObject.SetActive(false);
        if (toturialPopup.gameObject.activeInHierarchy) { DeactivateTutorialPopup(); }

        StartDestinationSelection();
        StartCoroutine(FadeInButtons());
    }

    public void StartDestinationSelection()
    {
        IsDestinationSelectionActive = true;
        _isWaitingForInstructionText = true;
        _selectedDestinationsCount = 0;
        EventsRelay.OnEnableDestinationSelection.Invoke(true);
        menuDestinationLoader.NextDestination(0);
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
        AudioManager.Instance.StopNarration();
        IsDestinationSelectionActive = false;
        EventsRelay.OnEnableDestinationSelection.Invoke(false);
        StartCoroutine(LoadToturial());
        destinationsSummaryPopup.gameObject.SetActive(true);

        firstDestinationText.text = LocationsManager.GetDestination(LocationsManager.SelectedDestinations[0]).Data.UIDestinationInfoText.HebTitle;
        secondDestinationText.text = LocationsManager.GetDestination(LocationsManager.SelectedDestinations[1]).Data.UIDestinationInfoText.HebTitle;
        thirdDestinationText.text = LocationsManager.GetDestination(LocationsManager.SelectedDestinations[2]).Data.UIDestinationInfoText.HebTitle;

        //Need to find better solution for english hebrew for this
        usernameText.text = $"{HighscoresManager.Instance.GetUsername(HighscoresManager.Instance.CurrentUserID)} :'סמ ןקחש";
        highscoreText.text = $"{HighscoresManager.Instance.GetHighscore(HighscoresManager.Instance.CurrentUserID):0000} :דוקינ";

        destinationsSummaryPopup.Play((int)PlayMode.Default);
        //destinationsSummaryPopup.StartCoroutine(destinationsSummaryPopup.PlayIconYoyo(showTutorialDelay));
        explanationPopup.SetActive(false);
    }
    private IEnumerator LoadToturial()
    {
        yield return new WaitForSeconds(showTutorialDelay);
        destinationsSummaryPopup.gameObject.SetActive(false);
        ActivateTutorialPopup();
        yield return new WaitForSeconds(showGameButtonDelay);
        startGameButton.SetActive(true);
    }
    private void ActivateTutorialPopup()
    {
        toturialPopup.gameObject.SetActive(true);
        toturialPopup.Play((int)PlayMode.Default);
    }
    private void DeactivateTutorialPopup()
    {
        toturialPopup.Play(0);
        toturialPopup.gameObject.SetActive(false);
    }
    public void ToggleTutorialPopup()
    {
        if (toturialPopup.gameObject.activeInHierarchy)
            DeactivateTutorialPopup();
        else
            ActivateTutorialPopup();
    }
    public void StartGame()
    {
        AudioManager.Instance.StopNarration();
        blackPanel.gameObject.SetActive(true);
        StartCoroutine(BlackFade());
    }
    private IEnumerator BlackFade()
    {
        yield return new WaitForSeconds(2.5f);
        SceneManager.LoadScene("Game Scene");
    }

    private void PlayInstructionNarration()
    {
        if (_isWaitingForInstructionText)
        {
            _isWaitingForInstructionText = false;
            AudioManager.Instance.PlayInstructionNarration();
        }
    }

    private void ToggleLanguageChange()
    {

    }
    private IEnumerator FadeInButtons()
    {
        yield return new WaitForSeconds(3.5f);
        parentCanvasGroup.DOFade(1f, 1f);
    }

}