using System.Collections;
using Kamgam.SkyClouds;
using UnityEngine;
using System;

public enum ChallengeType { Clouds, SideWind, Birds }
[Serializable]
public struct ChallengeData
{
    public ChallengeType Challenge;
    public float Duration;
}

public class ChallengeHandler : CollectableHandler
{
    [SerializeField] protected PopupData _failPopupData;
    [SerializeField] protected SkyCloud _cloudVisualPrefab;
    [SerializeField] protected GameObject _birdsVisualPrefab;
    private Transform _playerTransform;
    private ChallengeData _challengeData;
    private Challenge _challenge;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) { return; }

        switch (_challengeData.Challenge)
        {
            case ChallengeType.Clouds:
                _challenge?.OnPlayerCollided();
                break;
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.collider.CompareTag("Player")) { return; }

        switch (_challengeData.Challenge)
        {
            case ChallengeType.Birds:
                _challenge?.OnPlayerCollided();
                break;
        }
    }
    public void Init(ChallengeData challengeData, PopupTextData failPopupData, CollectableData collectableData, PopupTextData collectPopupData, PopupTextData notificationPopupData = new PopupTextData())
    {
        Init(collectableData, collectPopupData, notificationPopupData);

        _challengeData = challengeData;

        _failPopupData.PopupTextData = failPopupData;
        InitPopup(ref _failPopupData, failPopupData);
        _notifyColor = Color.blue;
    }
    protected override void CheckCollectRange(Vector3 delta)
    {
        //Overrides to not run base.CheckCollectRange
    }

    protected override void HandlePlayerMoved(Transform playerTransform)
    {
        if (_playerTransform == null) { _playerTransform = playerTransform; }

        base.HandlePlayerMoved(playerTransform);
    }
    protected override void Notify()
    {
        if (_hasNotified) return;
        _hasNotified = true;
        LoadPopup_EC.RaiseEvent(_notificationPopupData);
        StartCoroutine(ChallengeCoroutine(_challengeData.Duration));
    }

    private IEnumerator ChallengeCoroutine(float duration)
    {
        _challenge = null;
        SkyCloud cloudVisual = null;

        switch (_challengeData.Challenge)
        {
            case ChallengeType.Clouds:
                _challenge = new CloudChallenge(_playerTransform.position);
                cloudVisual = Instantiate(_cloudVisualPrefab, transform.position + Vector3.up * 1300f, Quaternion.identity, transform);
                break;
            case ChallengeType.SideWind:
                _challenge = new WindChallenge(_playerTransform.position, GameManager.CurrentDestination.position, _challengeData.Challenge);
                break;
            case ChallengeType.Birds:
                float heightToClimb = GameSettingsManager.GetFloat("Game Settings", "BirdsRequiredHeightToRise ", 500);
                _challenge = new BirdChallenge(_playerTransform.position, _playerTransform.position.y + heightToClimb);
                break;
        }

        float timePassed = 0;

        while (true)
        {
            if (duration <= timePassed) break;
            if (!GameManager.IsGamePaused)
            {
                timePassed += Time.deltaTime;
            }

            yield return null;
        }

        bool result = _challenge.WasSuccessful(_playerTransform.position);
        Debug.LogError(@$"Challenge {_challengeData.Challenge} completed with result: {result}");
        _wasCollected = true;

        if (result)
        {
            LoadPopup_EC.RaiseEvent(_collectPopupData);
            EventsRelay.OnScoreGain.Invoke(_collectableData.MaxScore);
            AudioManager.Instance.PlayPointCollected();
        }
        else
        {
            LoadPopup_EC.RaiseEvent(_failPopupData);
        }

        visuals.SetActive(false);
        OnDisable();
    }
}

public abstract class Challenge
{
    protected Vector3 _playerStartPosition;
    protected Vector3 _destinationPosition;

    public Challenge(Vector3 playerStartPosition, Vector3 destinationPosition)
    {
        _playerStartPosition = playerStartPosition;
        _destinationPosition = destinationPosition;
    }

    public virtual void OnPlayerCollided()
    {
        // Default implementation does nothing
    }

    public abstract bool WasSuccessful(Vector3 playerEndPosition);
}

public class BirdChallenge : Challenge
{
    private float _minHeight;
    public BirdChallenge(Vector3 playerStartPosition, float minHeight) : base(playerStartPosition, Vector3.zero)
    {
        _minHeight = minHeight;
    }

    public override void OnPlayerCollided()
    {

    }
    public override bool WasSuccessful(Vector3 playerEndPosition)
    {
        return playerEndPosition.y >= _minHeight;
    }
}
public class CloudChallenge : Challenge
{
    public bool _playerEnteredClouds;

    public CloudChallenge(Vector3 playerStartPosition) : base(playerStartPosition, Vector3.zero)
    {
        _playerEnteredClouds = false;
    }

    public override void OnPlayerCollided()
    {
        if (_playerEnteredClouds) { return; }

        _playerEnteredClouds = true;
    }
    public override bool WasSuccessful(Vector3 playerEndPosition)
    {
        return !_playerEnteredClouds;
    }
}
public class WindChallenge : Challenge
{
    public WindChallenge(Vector3 playerStartPosition, Vector3 destinationPosition, ChallengeType type) : base(playerStartPosition, destinationPosition)
    {
        switch (type)
        {
            case ChallengeType.SideWind:
                EventsRelay.OnWindEvent.Invoke(type, true);
                break;
            default:
                break;
        }
    }

    public override bool WasSuccessful(Vector3 playerEndPosition)
    {
        float requiredTravelDistance = GameSettingsManager.GetFloat("Game Settings", "RequiredTravelDistance", 1000);
        EventsRelay.OnWindEvent.Invoke(ChallengeType.SideWind, false);

        Vector3 startDiff = _destinationPosition - _playerStartPosition;
        Vector3 endDiff = _destinationPosition - playerEndPosition;
        return startDiff.sqrMagnitude > endDiff.sqrMagnitude + requiredTravelDistance;
    }
}
