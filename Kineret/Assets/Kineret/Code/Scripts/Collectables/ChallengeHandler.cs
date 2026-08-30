using System.Collections;
using Kamgam.SkyClouds;
using UnityEngine;
using System;

// Order matters: GameDestinationLoader maps a challenge by its index in
// LocationsManager.Challenges via (ChallengeType)challenge, so this must match
// the order of ChallengeDataList in GameData.json.
public enum ChallengeType { Clouds, Birds, SideWind }
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
    [SerializeField] protected BirdFlockChallengeVisual _birdsVisualPrefab;
    private Transform _playerTransform;
    private ChallengeData _challengeData;
    private Challenge _challenge;
    private BirdFlockChallengeVisual _birdsVisual;

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

    /// <summary>
    /// Hitting any bird fails the challenge right away and throws the player back to behind where
    /// the challenge started, so the setback is unmistakable and they have to fly the stretch again.
    /// </summary>
    private void HandleBirdsHit()
    {
        if (_challenge == null || _challenge.HasFailed) { return; }

        _challenge.OnPlayerCollided();

        float pushback = GameSettingsManager.GetFloat("Game Settings", "BirdsFailPushbackDistance", 2000f);
        _playerTransform.position = _challenge.PlayerStartPosition - GetRouteDirection() * pushback;
    }

    /// <summary>Flat direction from the player toward the destination they are flying to.</summary>
    private Vector3 GetRouteDirection()
    {
        Vector3 routeDirection = GameManager.CurrentDestination.position - _playerTransform.position;
        routeDirection.y = 0f;

        return routeDirection.sqrMagnitude < 0.001f ? Vector3.zero : routeDirection.normalized;
    }

    /// <summary>
    /// Puts the flock on the route directly ahead of the player, at their current altitude, so
    /// staying on course flies straight into it.
    ///
    /// The challenge marker's own position is not used: it sits off to the side of the route line
    /// by a random Min/MaxVariancePointDistance (2000-4500 units per config.ini, in
    /// GameDestinationLoader.GenerateLegCollectables), and it is a full NotificationRange away when
    /// the challenge fires. The player travels legDistance/LegDuration,
    /// roughly 670 units per second, so a flock placed at the marker would still be ahead of them
    /// when the challenge was already scored. The spawn distance below is instead matched to the
    /// challenge Duration in GameData.json, so the encounter always lands inside the window.
    /// Raising one without the other breaks the challenge, so keep them in step.
    /// </summary>
    private Vector3 GetFlockPosition()
    {
        Vector3 playerPosition = _playerTransform.position;
        Vector3 routeDirection = GetRouteDirection();

        if (routeDirection == Vector3.zero)
        {
            return new Vector3(transform.position.x, playerPosition.y, transform.position.z);
        }

        float spawnDistance = GameSettingsManager.GetFloat("Game Settings", "BirdsSpawnDistance", 6750f);

        return playerPosition + routeDirection * spawnDistance;
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
                _challenge = new BirdChallenge(_playerTransform.position);
                _birdsVisual = Instantiate(_birdsVisualPrefab, GetFlockPosition(), Quaternion.identity, transform);
                _birdsVisual.OnPlayerHit += HandleBirdsHit;
                break;
        }

        AudioManager.Instance.PlayChallengeNarration(_challengeData.Challenge);

        float timePassed = 0;

        while (true)
        {
            if (duration <= timePassed) break;
            if (_challenge.HasFailed) break;
            if (!GameManager.IsGamePaused)
            {
                timePassed += Time.deltaTime;
            }

            yield return null;
        }

        bool result = _challenge.WasSuccessful(_playerTransform.position);
        //Debug.LogError(@$"Challenge {_challengeData.Challenge} completed with result: {result}");
        _wasCollected = true;

        if (_birdsVisual != null)
        {
            _birdsVisual.OnPlayerHit -= HandleBirdsHit;
            Destroy(_birdsVisual.gameObject);
            _birdsVisual = null;
        }

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

    /// <summary>Where the player was when the challenge was triggered.</summary>
    public Vector3 PlayerStartPosition => _playerStartPosition;

    /// <summary>True once the challenge is already lost, so it can end before its duration runs out.</summary>
    public virtual bool HasFailed => false;

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
    private bool _playerHitFlock;

    public BirdChallenge(Vector3 playerStartPosition) : base(playerStartPosition, Vector3.zero)
    {
        _playerHitFlock = false;
    }

    public override bool HasFailed => _playerHitFlock;

    public override void OnPlayerCollided()
    {
        if (_playerHitFlock) { return; }

        _playerHitFlock = true;
    }
    public override bool WasSuccessful(Vector3 playerEndPosition)
    {
        return !_playerHitFlock;
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