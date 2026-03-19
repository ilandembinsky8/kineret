using System.Collections.Generic;
using UnityEngine;

public class GameDestinationLoader : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private PlayerMovementHandler player;
    [SerializeField] private WaypointPathController path;
    [Header("Prefabs")]
    [SerializeField] private DestinationHandler destinationPrefab;
    [SerializeField] private InterestPointHandler interestPointPrefab;
    [SerializeField] private CollectableHandler bonusPrefab;
    [SerializeField] private InfoPointHandler infoPointPrefab;
    [SerializeField] private ChallengeHandler challengePrefab;

    //Temp
    [Header("Collectiable")]
    [SerializeField] private DestinationCollectableData[] destinationCollectables;
    [SerializeField] private InfoCollectableData infoCollectable;
    [SerializeField] private InfoCollectableData interestCollectable;
    [SerializeField] private BonusCollectableData[] bonusCollectables;
    [SerializeField] private ChallengeCollectableData[] challenges;

    public bool IsTesting;

    private DestinationHandler[] _destinations;

    private void Awake()
    {
        if (IsTesting)
        {
            LocationsManager.DestinationCollectables = destinationCollectables;
            LocationsManager.InfoCollectable = infoCollectable;
            LocationsManager.InterestCollectable = interestCollectable;
            LocationsManager.BonusCollectables = bonusCollectables;
            LocationsManager.Challenges = challenges;
        }
        Debug.Log("Destinations:" + LocationsManager.Destinations.Count); 
        Debug.Log("POI:" + LocationsManager.InterestPoints.Count);
        Debug.Log("DestinationCollectables:" + LocationsManager.DestinationCollectables.Length);
        Debug.Log("BonusCollectables:" + LocationsManager.BonusCollectables.Length);
        Debug.Log("Challenges:" + LocationsManager.Challenges.Length);


        GenerateDestinations();
        GenerateInterestPoints();
        GenerateRoute();
        gameManager.Destinations = _destinations;
    }

    private void GenerateDestinations()
    {
        _destinations = new DestinationHandler[LocationsManager.SELECTABLE_DESTINATIONS_COUNT];
        for (int i = 0; i < _destinations.Length; i++)
        {
            int destination = LocationsManager.SelectedDestinations[i];          
            Vector3 position = LocationsManager.GetDestination(destination).Data.WorldPosition;
            DestinationHandler destinationHandler = Instantiate(destinationPrefab,position,Quaternion.identity);
            destinationHandler.Destination = destination;
            _destinations[i] = destinationHandler;
        }
    }
    private void GenerateInterestPoints()
    {
        for (int i = 0; i < LocationsManager.InterestPoints.Count; i++)
        {
            InterestPointData interestPoint = LocationsManager.GetInterestPoint(i);
            InterestPointHandler interestPointHandler = Instantiate(interestPointPrefab, interestPoint.Data.WorldPosition, Quaternion.identity);
            interestPointHandler.Init(interestPoint, LocationsManager.InterestCollectable);
        }
    }

    private void GenerateRoute()
    {
        //Find the longest path
        //reorder the destinations

        for (int i = 0; i < _destinations.Length; i++)
        {
            DestinationCollectableData destinationCollectable = LocationsManager.DestinationCollectables[i];
            _destinations[i].Init(destinationCollectable.RangeData, destinationCollectable.CollectionPopup);
        }

        Vector3 startPosition = Vector3.zero;
        Vector3 firstDestinationPosition = _destinations[0].transform.position;
        Vector3 direction = (firstDestinationPosition - _destinations[1].transform.position).normalized;
        startPosition = firstDestinationPosition + (direction * 20000);//TODO: Change to be the value from the ini
        player.transform.position = startPosition + (Vector3.up * 2000);
        player.YawBody.LookAt(new Vector3(firstDestinationPosition.x, player.transform.position.y, firstDestinationPosition.z));

        List<Vector3> waypoints = new(4)
        {
            startPosition
        };
        for (int i = 0; i < _destinations.Length; i++)
        {
            waypoints.Add(_destinations[i].transform.position);
        }
        path.Init(waypoints);


        List<int> bonusIndices = new List<int>();
        for (int i = 0; i < LocationsManager.BonusCollectables.Length; i++)
        {
            bonusIndices.Add(i);
        }

        List<int> challengeIndices = new List<int>();
        for (int i = 0; i < LocationsManager.Challenges.Length; i++)
        {
            challengeIndices.Add(i);
        }

        int bonusIndex, challengeIndex;
        Vector3 start, end;
        for (int i = 0; i < _destinations.Length; i++)
        {
            start = waypoints[i];
            end = waypoints[i + 1];
            bonusIndex = bonusIndices[Random.Range(0, bonusIndices.Count)];
            challengeIndex = challengeIndices[Random.Range(0, challengeIndices.Count)];
            bonusIndices.Remove(bonusIndex); 
            challengeIndices.Remove(challengeIndex);
            GenerateLegCollectables(start, end, bonusIndex, challengeIndex, _destinations[i].Destination);
        }
    }

    private void GenerateLegCollectables(Vector3 start, Vector3 end, int bonus,int challenge, int destinationID)
    {
        //Indices set up
        CollectableHandler[] collectables = new CollectableHandler[5];
        List<int> collectablesIndices = new List<int>();
        for (int i = 0; i < collectables.Length; i++)
        {
            collectablesIndices.Add(i);
        }

        //Bonus creation
        int bonusIndex = collectablesIndices[Random.Range(0, collectablesIndices.Count)];
        collectablesIndices.Remove(bonusIndex);
        CollectableHandler bonusPoint = Instantiate(bonusPrefab);
        BonusCollectableData bonusCollectableData = LocationsManager.BonusCollectables[bonus];
        bonusPoint.Init(bonusCollectableData.CollectableData, bonusCollectableData.CollectionPopup, bonusCollectableData.NotificationPopup);
        collectables[bonusIndex] = bonusPoint;

        //Challenge creation
        int challengeIndex = collectablesIndices[Random.Range(0, collectablesIndices.Count)];
        collectablesIndices.Remove(challengeIndex);
        ChallengeHandler challengePoint = Instantiate(challengePrefab);
        ChallengeCollectableData challengeCollectableData = LocationsManager.Challenges[challenge];
        ChallengeData challengeData = new ChallengeData() { Challenge = (ChallengeType)challenge , Duration = challengeCollectableData.Duration };
        Debug.Log(((ChallengeType)challenge).ToString());
        Debug.Log(challengeCollectableData.NotificationPopup.TextData.HebTitle);
        challengePoint.Init(challengeData, challengeCollectableData.FailPopupData, challengeCollectableData.CollectableData, challengeCollectableData.CollectionPopup, challengeCollectableData.NotificationPopup);
        collectables[challengeIndex] = challengePoint;

        //Info points creation
        for (int i = 0; i < 3; i++)
        {
            InfoPointHandler infoPoint = Instantiate(infoPointPrefab);
            collectables[collectablesIndices[i]] = infoPoint;
            DestinationData destination = LocationsManager.GetDestination(destinationID);
            switch (i)
            {
                case 0:
                    infoPoint.Init(destination.Data.FirstInfoPoint, LocationsManager.InfoCollectable);
                    break;
                case 1:
                    infoPoint.Init(destination.Data.SecondInfoPoint, LocationsManager.InfoCollectable);
                    break;
                case 2:
                    infoPoint.Init(destination.Data.ThirdInfoPoint, LocationsManager.InfoCollectable);
                    break;
            }
        }

        //Positioning of the points
        Vector3 diff = (end - start);
        float gap = diff.magnitude / (collectables.Length+1);
        Vector3 direction = diff.normalized;
        for (int i = 0; i < collectables.Length; i++)
        {
            collectables[i].transform.position = start + (direction * (gap * (i + 1)));
        }

        //Randomizing variance from straight route
        float maxVarianceDistance = GameSettingsManager.GetFloat("Game Settings", "Game MaxVariancePointDistance", 500);
    }
}
