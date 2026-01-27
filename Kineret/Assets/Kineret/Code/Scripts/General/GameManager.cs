using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameSettings gameSettings;
    [SerializeField] private InfoScreenDataEventChannel destinationReached_EC;
    [SerializeField] private InfoScreenDataEventChannel loadInfoScreen_EC;

    [SerializeField] private IntEventChannel scoreChanged_EC;
    [SerializeField] private IntEventChannel gotScore_EC;

    [SerializeField] private VoidEventChannel gameOver_EC;

    private int _destinationsReachedCount;
    private int _score;

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
    }

    private void HandleGotScore(int score)
    {
        _score += score;
        scoreChanged_EC.RaiseEvent(_score);
    }

    private void HandleDestinationReached(InfoScreenData data)
    {
        _destinationsReachedCount++;
        data.IsFinal = _destinationsReachedCount == gameSettings.DestinationCount;
        loadInfoScreen_EC.RaiseEvent(data);
    }

    private void HandleGameOver()
    {

    }

}
