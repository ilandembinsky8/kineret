using System;
using System.Collections;
using UnityEngine;


public enum ChallengeType
{
    FrontWind,SideWind,Birds
}
[Serializable]
public struct ChallengeData
{
    public ChallengeType Challenge;
    public float Duration;
}



public class ChallengeHandler : CollectableHandler
{
    [SerializeField] protected PopupData _failPopupData;
    private Transform _playerTransform;
    private ChallengeData _challengeData;
    
    public void Init(ChallengeData challengeData, PopupTextData failPopupData, CollectableData collectableData, PopupTextData collectPopupData, PopupTextData notificationPopupData = new PopupTextData())
    {
        Init(collectableData, collectPopupData, notificationPopupData);

        _challengeData = challengeData;
        
        _failPopupData.PopupTextData = failPopupData;
        InitPopup(ref _failPopupData, failPopupData);

        /*if (!String.IsNullOrEmpty(failPopupData.PopupIconName))
        {
            LocationsManager.TryGetIconImageData(failPopupData.PopupIconName, out _failPopupData.IconSprite);
        }
        else
        {
            Debug.LogError("Null icon name in a fail popup data");
        }*/   

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

        Challenge challenge = null;

        switch (_challengeData.Challenge)
        {
            case ChallengeType.FrontWind:
            case ChallengeType.SideWind:    
                challenge = new WindChallenge(_playerTransform.position, GameManager.CurrentDestination.position, _challengeData.Challenge);
                break;
            case ChallengeType.Birds:
                float heightToClimb = GameSettingsManager.GetFloat("Game Settings", "BirdsRequiredHeightToRise ", 500);
                challenge = new BirdChallenge(_playerTransform.position, _playerTransform.position.y + heightToClimb);
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

        bool result = challenge.WasSuccessful(_playerTransform.position);

        _wasCollected = true;

        if (result)
        {
            LoadPopup_EC.RaiseEvent(_collectPopupData);
            gotScore_EC.RaiseEvent(_collectableData.MaxScore);
        }
        else
        {
            LoadPopup_EC.RaiseEvent(_failPopupData);
        }

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

    public abstract bool WasSuccessful(Vector3 playerEndPosition);
}

public class BirdChallenge : Challenge
{
    private float _minHeight;
    public BirdChallenge(Vector3 playerStartPosition, float minHeight) : base(playerStartPosition, Vector3.zero)
    {
        _minHeight = minHeight;
    }

    public override bool WasSuccessful(Vector3 playerEndPosition)
    {
        return playerEndPosition.y >= _minHeight;
    }
}
public class WindChallenge : Challenge
{
    public WindChallenge(Vector3 playerStartPosition, Vector3 destinationPosition, ChallengeType type) : base(playerStartPosition, destinationPosition)
    {
        switch (type)
        {
            case ChallengeType.FrontWind:
            case ChallengeType.SideWind:
                EventsRelay.OnWindEvent.Invoke(type,true);
                break;
            default:
                break;
        }
    }
    public override bool WasSuccessful(Vector3 playerEndPosition)
    {
        float requiredTravelDistance = GameSettingsManager.GetFloat("Game Settings", "RequiredTravelDistance",1000);
        EventsRelay.OnWindEvent.Invoke(ChallengeType.FrontWind, false);

        Vector3 startDiff = _destinationPosition - _playerStartPosition;
        Vector3 endDiff = _destinationPosition - playerEndPosition;
        return startDiff.sqrMagnitude > endDiff.sqrMagnitude + requiredTravelDistance;
    }
}
