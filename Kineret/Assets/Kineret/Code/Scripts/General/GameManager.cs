using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameSettings gameSettings;
    [SerializeField] private InfoScreenDataEventChannel destinationReached_EC;
    [SerializeField] private InfoScreenDataEventChannel loadInfoScreen_EC;
    [SerializeField] private FloatEventChannel moveSpeedChange_EC;

    [SerializeField] private IntEventChannel scoreChanged_EC;
    [SerializeField] private IntEventChannel gotScore_EC;
    [SerializeField] private VoidEventChannel gameOver_EC;

    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private GameObject summmaryCanvas;

    [SerializeField] private Transform[] destinations;
    private int _destinationsReachedCount;
    private int _score;
    [SerializeField] float legTime;

    private void Awake()
    {
        summmaryCanvas.SetActive(false);
        legTime = IniManager.GetFloat("Flight Settings", "SecondsForLeg",15);
    }

    private void OnEnable()
    {
        gotScore_EC.OnEventRaised += HandleGotScore;
        destinationReached_EC.OnEventRaised += HandleDestinationReached;
        gameOver_EC.OnEventRaised += HandleGameOver;
    }

    private void OnDisable()
    {
        gotScore_EC.OnEventRaised -= HandleGotScore;
        destinationReached_EC.OnEventRaised -= HandleDestinationReached;
        gameOver_EC.OnEventRaised -= HandleGameOver;
    }

    private void Start()
    {
        HandleGotScore(0);
        ChangeMoveSpeedByLeg();
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

    private void HandleDestinationReached(InfoScreenData data)
    {
        ChangeMoveSpeedByLeg();
        _destinationsReachedCount++;
        data.IsFinal = _destinationsReachedCount == gameSettings.DestinationCount;
        loadInfoScreen_EC.RaiseEvent(data);       
    }
    private void ChangeMoveSpeedByLeg()
    {
        Vector3 currentDesY0 = destinations[_destinationsReachedCount].position;
        currentDesY0.y = 0;
        Vector3 nextDesY0;
        if (_destinationsReachedCount < gameSettings.DestinationCount-1)
        {
            nextDesY0 = destinations[_destinationsReachedCount + 1].position;
            nextDesY0.y = 0;
            float distance = Vector3.Distance(currentDesY0, nextDesY0);
            float newMoveSpeed = distance / legTime;
            moveSpeedChange_EC.RaiseEvent(newMoveSpeed);
            Debug.Log("distance from current to next destination: " + distance);
        }
    }

    private void HandleGameOver()
    {
        finalScoreText.text = string.Format("{0:0000}", _score);
        summmaryCanvas.SetActive(true);
    }
}
