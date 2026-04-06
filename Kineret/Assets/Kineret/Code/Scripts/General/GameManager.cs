using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static bool IsGamePaused { get; set; }
    public static Transform CurrentDestination;

    [SerializeField] private Transform player;
    [Header("Event Channels")]
    [SerializeField] private FloatEventChannel moveSpeedChange_EC;

    [Header("UI Elements")]
    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private SummaryPanelHandler summmaryCanvas;

    public DestinationHandler[] Destinations { get; set; }
    private int _destinationsReachedCount;
    private int _currentDestinationScore;
    private int _totalScore;
    private float _legDuration;
    private Camera _camera;

    private Coroutine _scoreCoroutine;

    private void Awake()
    {
        _camera = Camera.main;
        summmaryCanvas.gameObject.SetActive(false);
        _legDuration = GameSettingsManager.GetFloat("Game Settings", "LegDuration", 15);
    }

    private void OnEnable()
    {
        EventsRelay.OnScoreGain += HandleGotScore;
        EventsRelay.OnDestinationReached += HandleDestinationReached;
        EventsRelay.OnGameOver += HandleGameOver;
        EventsRelay.OnGamePause += HandleGamePaused;
        EventsRelay.OnStartScoreCountdown += HandleStartScoreCountdown;
        EventsRelay.OnShowDirection += HandleShowDirection;
    }

    private void OnDisable()
    {
        EventsRelay.OnScoreGain -= HandleGotScore;
        EventsRelay.OnDestinationReached -= HandleDestinationReached;
        EventsRelay.OnGameOver -= HandleGameOver;
        EventsRelay.OnGamePause -= HandleGamePaused;
        EventsRelay.OnStartScoreCountdown -= HandleStartScoreCountdown;
        EventsRelay.OnShowDirection -= HandleShowDirection;
    }

    private void Start()
    {
        HandleGotScore(0);
        ChangeMoveSpeedByLeg(player.position, Destinations[0].transform.position);
        CurrentDestination = Destinations[0].transform;
        _currentDestinationScore = Destinations[0].MaxScore;
        EventsRelay.OnGamePause.Invoke(true);
        EventsRelay.OnLegStart.Invoke(_destinationsReachedCount);     
    }

    public void GoToMainMenu(bool _isLoadingDestinationSelection)
    {
        MainMenuManager.IsLoadingDestinationSelection = _isLoadingDestinationSelection;
        SceneManager.LoadScene("Main Menu Scene");
    }

    private void HandleGotScore(int score)
    {
        _totalScore += score;
        EventsRelay.OnScoreChange.Invoke(_totalScore);
    }
   
    private void HandleDestinationReached(int destination)
    {
        _destinationsReachedCount++;

        if (_scoreCoroutine != null)
        {
            StopCoroutine(_scoreCoroutine);
            _scoreCoroutine = null;
        }
           
        EventsRelay.OnScoreGain.Invoke(_currentDestinationScore);

        bool isFinal = _destinationsReachedCount == LocationsManager.DestinationsCount;
        InfoScreenData data = LocationsManager.GetInfoScreenData(destination, isFinal);
        if (!isFinal)
        {
            _currentDestinationScore = Destinations[_destinationsReachedCount].MaxScore;
            ChangeMoveSpeedByLeg(Destinations[_destinationsReachedCount - 1].transform.position, Destinations[_destinationsReachedCount].transform.position);
            CurrentDestination = Destinations[_destinationsReachedCount].transform;
            EventsRelay.OnLegStart.Invoke(_destinationsReachedCount);
        }

        EventsRelay.OnLoadInfoScreen(data);
    }
    private void HandleShowDirection()
    {
        Vector2 flatPlayerForward = new Vector2(_camera.transform.forward.x, _camera.transform.forward.z);
        float peripheralAngle = GameSettingsManager.GetFloat("Player Settings", "PeripheralAngle", 45f);
        
        Vector3 nextDestinationDirection = (Destinations[_destinationsReachedCount].transform.position - _camera.transform.position).normalized;
        Vector2 flatNextDestinationDirection = new Vector2(nextDestinationDirection.x, nextDestinationDirection.z);

      /*  Debug.DrawLine(_camera.transform.position, _camera.transform.position + new Vector3(flatPlayerForward.x, 0f, flatPlayerForward.y), Color.blue, 500000f);
        Debug.DrawLine(_camera.transform.position, _camera.transform.position + new Vector3(flatNextDestinationDirection.x, 0f, flatNextDestinationDirection.y), Color.yellow, 500000f);*/

        float destinationAngle = Vector2.Angle(flatPlayerForward, flatNextDestinationDirection);

        if(destinationAngle < peripheralAngle)
        {
            return;
        }

        Vector2 leftPeripheral = RotateVector(flatPlayerForward, peripheralAngle);
        Vector2 rightPeripheral = RotateVector(flatPlayerForward, -peripheralAngle);

       /* Debug.DrawLine(_camera.transform.position, _camera.transform.position + new Vector3(leftPeripheral.x, 0f, leftPeripheral.y), Color.red, 500000f);
        Debug.DrawLine(_camera.transform.position, _camera.transform.position + new Vector3(rightPeripheral.x, 0f, rightPeripheral.y), Color.green, 500000f); */

        float leftAngle = Vector2.Angle(leftPeripheral, flatNextDestinationDirection);
        float rightAngle = Vector2.Angle(rightPeripheral, flatNextDestinationDirection);

        string direction = leftAngle < rightAngle ? "Left" : "Right";
        EventsRelay.OnLoadDirectionPopup.Invoke(direction);
    }

    private void ChangeMoveSpeedByLeg(Vector3 positionA, Vector3 positionB)
    {
        Vector3 currentPosition = positionA;
        currentPosition.y = 0;
        Vector3 nextPosition;
        if (_destinationsReachedCount < LocationsManager.DestinationsCount)
        {
            nextPosition = positionB;
            nextPosition.y = 0;
            float distance = Vector3.Distance(currentPosition, nextPosition);
            float newMoveSpeed = distance / _legDuration;
            moveSpeedChange_EC.RaiseEvent(newMoveSpeed);
        }
    }

    private void HandleGamePaused(bool isPaused)
    {
        IsGamePaused = isPaused;
    }
 
    private void HandleGameOver()
    {
        finalScoreText.text = string.Format("{0:0000}", _totalScore);
        summmaryCanvas.gameObject.SetActive(true);
        summmaryCanvas.StartCoroutine(summmaryCanvas.EnterAnimation());
    }

    private void HandleStartScoreCountdown()
    {
        _scoreCoroutine = StartCoroutine(ScoreCoroutine());
    }

    private IEnumerator ScoreCoroutine()
    {
        WaitForSeconds timer = new(GameSettingsManager.GetFloat("Score Settings", "TimeForScoreDeduction", 1f));
        float safeTime = _legDuration * GameSettingsManager.GetFloat("Score Settings", "DestinationMaxScoreMultiplier", 1.1f);

        //safe time to get max score
        while (safeTime > 0)
        {
            if (IsGamePaused) { yield return null; }

            safeTime -= Time.deltaTime;
            yield return null;
        }

        int scoreDeduction = GameSettingsManager.GetInt("Score Settings", "ScoreDeductionValue", 1);

        //losing score each deduction period
        while (_currentDestinationScore > 0)
        {
            if (IsGamePaused) { yield return null; }

            _currentDestinationScore -= scoreDeduction;
            yield return timer;
        }
    }
    private Vector2 RotateVector(Vector2 vector, float angle)
    {
        Vector2 normalVector = vector.normalized;
        float vectorAngle = Vector2.SignedAngle(Vector2.right, normalVector) + angle;

        float x = Mathf.Cos(Mathf.Deg2Rad * vectorAngle);
        float y = Mathf.Sin(Mathf.Deg2Rad * vectorAngle);
        return new Vector2(x, y);
    }

}
