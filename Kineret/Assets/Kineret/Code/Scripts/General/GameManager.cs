using TMPro;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static bool IsGamePaused = false;
    public static Transform CurrentDestination;

    [SerializeField] private Transform player;
    [Header("Event Channels")]
    [SerializeField] private FloatEventChannel moveSpeedChange_EC;

    [SerializeField] private IntEventChannel scoreChanged_EC;
    [SerializeField] private IntEventChannel gotScore_EC;

    [Header("UI Elements")]
    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private SummaryPanelHandler summmaryCanvas;

    public DestinationHandler[] Destinations { get; set; }
    private int _destinationsReachedCount;
    private int _score;
    private float _legDuration;

    private void Awake()
    {
        summmaryCanvas.gameObject.SetActive(false);
        _legDuration = GameSettingsManager.GetFloat("Game Settings", "LegDuration", 15);
    }

    private void OnEnable()
    {
        gotScore_EC.OnEventRaised += HandleGotScore;
        EventsRelay.OnDestinationReached += HandleDestinationReached;
        EventsRelay.OnGameOver += HandleGameOver;
        EventsRelay.OnGamePause += HandleGamePaused;
    }

    private void OnDisable()
    {
        gotScore_EC.OnEventRaised -= HandleGotScore;
        EventsRelay.OnDestinationReached -= HandleDestinationReached;
        EventsRelay.OnGameOver -= HandleGameOver;
        EventsRelay.OnGamePause -= HandleGamePaused;
    }

    private void Start()
    {
        HandleGotScore(0);
        ChangeMoveSpeedByLeg(player.position, Destinations[0].transform.position);
        CurrentDestination = Destinations[0].transform;
        EventsRelay.OnGamePause.Invoke(true);
    }

    public void GoToMainMenu(bool _isLoadingDestinationSelection)
    {
        MainMenuManager.IsLoadingDestinationSelection = _isLoadingDestinationSelection;
        SceneManager.LoadScene("Main Menu Scene");
    }

    private void HandleGotScore(int score)
    {
        _score += score;
        scoreChanged_EC.RaiseEvent(_score);
    }

    private void HandleDestinationReached(int destination)
    {
        bool isFinal = _destinationsReachedCount == LocationsManager.DestinationsCount;
        InfoScreenData data = LocationsManager.GetInfoScreenData(destination, isFinal);
        ChangeMoveSpeedByLeg(Destinations[_destinationsReachedCount].transform.position, Destinations[_destinationsReachedCount+1].transform.position);
        _destinationsReachedCount++;

        if(!isFinal) CurrentDestination = Destinations[_destinationsReachedCount].transform;

        EventsRelay.OnLoadInfoScreen(data);       
    }
    private void ChangeMoveSpeedByLeg(Vector3 positionA, Vector3 positionB)
    {
        Vector3 currentPosition = positionA;
        currentPosition.y = 0;
        Vector3 nextPosition;
        if (_destinationsReachedCount < LocationsManager.DestinationsCount -1)
        {
            nextPosition = positionB;
            nextPosition.y = 0;
            float distance = Vector3.Distance(currentPosition, nextPosition);
            float newMoveSpeed = distance / _legDuration;
            moveSpeedChange_EC.RaiseEvent(newMoveSpeed);
            Debug.Log("distance from current to next destination: " + distance);
        }
    }

    private void HandleGamePaused(bool isPaused)
    {
        IsGamePaused = isPaused;
    }

    private void HandleGameOver()
    {
        finalScoreText.text = string.Format("{0:0000}", _score);
        summmaryCanvas.gameObject.SetActive(true);
        summmaryCanvas.StartCoroutine(summmaryCanvas.EnterAnimation());
    }
}
