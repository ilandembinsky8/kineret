using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class GameDestinationLoader : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private PlayerMovementHandler player;
    [SerializeField] private WaypointPathController path;
    [SerializeField] private Terrain terrain;

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

        GenerateDestinations();
        GenerateInterestPoints();
        GenerateRoute();
        gameManager.Destinations = _destinations;
    }

    private void GenerateDestinations()
    {
        _destinations = new DestinationHandler[LocationsManager.DestinationsCount];
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
        float[] segemnts = new float[_destinations.Length];
        for (int i = 0; i < segemnts.Length; i++)
        {
            if(i == segemnts.Length - 1)
            {
                segemnts[i] = (_destinations[i].transform.position - _destinations[0].transform.position).sqrMagnitude;
                continue;
            }

            segemnts[i] = (_destinations[i].transform.position - _destinations[i+1].transform.position).sqrMagnitude;
        }

        int minIndex = Array.IndexOf(segemnts, segemnts.Min());
        int middleDestinationIndex;
        if (minIndex - 1 < 0)
        {
            middleDestinationIndex = _destinations.Length - 1;
        }
        else
        {
            middleDestinationIndex = minIndex - 1;
        }

        DestinationHandler temp = _destinations[1];
        _destinations[1] = _destinations[middleDestinationIndex];
        _destinations[middleDestinationIndex] = temp;

        //Initialize destinations
        for (int i = 0; i < _destinations.Length; i++)
        {
            DestinationCollectableData destinationCollectable = LocationsManager.DestinationCollectables[i];
            _destinations[i].Leg = i;
            _destinations[i].Init(destinationCollectable.RangeData, destinationCollectable.CollectionPopup);
        }

        //Place Player
        Vector3 startPosition,direction;
        Vector3 firstDestinationPosition = _destinations[0].transform.position;


        if (GameSettingsManager.GetInt("Route Settings", "IsCenterStart", 0) == 0)
        {
            //Start in destinations line direction
            direction = (firstDestinationPosition - _destinations[1].transform.position).normalized;
            startPosition = firstDestinationPosition + (direction * GameSettingsManager.GetFloat("Route Settings", "FirstLegDistance", 20000));
        }
        else
        {
            //Start in center direction
            Vector3 terrainCenter = new Vector3(terrain.transform.position.x + (terrain.terrainData.size.x / 2f), firstDestinationPosition.y, terrain.transform.position.z + (terrain.terrainData.size.z / 2f));
            direction = (terrainCenter - firstDestinationPosition).normalized;
            startPosition = firstDestinationPosition + (direction * GameSettingsManager.GetFloat("Route Settings", "FirstLegDistance", 20000));
        }

        
        player.transform.position = startPosition + (Vector3.up * GameSettingsManager.GetFloat("Route Settings", "PlayerStartingYBonus", 2000));
        player.YawBody.LookAt(new Vector3(firstDestinationPosition.x, player.transform.position.y, firstDestinationPosition.z));

        //Initialize the path
        List<Vector3> waypoints = new(_destinations.Length + 1)
        {
            startPosition
        };

        for (int i = 0; i < _destinations.Length; i++)
        {
            waypoints.Add(_destinations[i].transform.position);
        }
        path.Init(waypoints);

        //Generate Collectables
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

        float[] clearLegPercentagers = new float[_destinations.Length];
        for (int i = 0; i < clearLegPercentagers.Length; i++)
        {
            clearLegPercentagers[i] = GameSettingsManager.GetFloat("Route Settings", $"Leg{i + 1}ClearPercentage", 0.3f);
        }


        for (int i = 0; i < _destinations.Length; i++)
        {          
            end = waypoints[i + 1];
            start = waypoints[i] + (end - waypoints[i]) * clearLegPercentagers[i];
            bonusIndex = bonusIndices[UnityEngine.Random.Range(0, bonusIndices.Count)];
            challengeIndex = challengeIndices[UnityEngine.Random.Range(0, challengeIndices.Count)];
            bonusIndices.Remove(bonusIndex); 
            challengeIndices.Remove(challengeIndex);
            GenerateLegCollectables(start, end, bonusIndex, challengeIndex, _destinations[i].Destination,i);
        }
    }

    private void GenerateLegCollectables(Vector3 start, Vector3 end, int bonus,int challenge, int destinationID, int leg)
    {
        //Indices set up
        CollectableHandler[] collectables = new CollectableHandler[5];
        List<int> collectablesIndices = new List<int>();
        for (int i = 0; i < collectables.Length; i++)
        {
            collectablesIndices.Add(i);
        }

        //Bonus creation
        int bonusIndex = collectablesIndices[UnityEngine.Random.Range(0, collectablesIndices.Count)];
        collectablesIndices.Remove(bonusIndex);
        CollectableHandler bonusPoint = Instantiate(bonusPrefab);
        bonusPoint.Leg = leg;
        BonusCollectableData bonusCollectableData = LocationsManager.BonusCollectables[bonus];
        bonusPoint.Init(bonusCollectableData.CollectableData, bonusCollectableData.CollectionPopup, bonusCollectableData.NotificationPopup);
        collectables[bonusIndex] = bonusPoint;

        //Challenge creation
        int challengeIndex = collectablesIndices[UnityEngine.Random.Range(0, collectablesIndices.Count)];
        collectablesIndices.Remove(challengeIndex);
        ChallengeHandler challengePoint = Instantiate(challengePrefab);
        challengePoint.Leg = leg;
        ChallengeCollectableData challengeCollectableData = LocationsManager.Challenges[challenge];
        ChallengeData challengeData = new ChallengeData() { Challenge = (ChallengeType)challenge , Duration = challengeCollectableData.Duration };
        challengePoint.Init(challengeData, challengeCollectableData.FailPopupData, challengeCollectableData.CollectableData, challengeCollectableData.CollectionPopup, challengeCollectableData.NotificationPopup);
        collectables[challengeIndex] = challengePoint;

        //Info points creation
        for (int i = 0; i < 3; i++)
        {
            InfoPointHandler infoPoint = Instantiate(infoPointPrefab);
            infoPoint.Leg = leg;
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
        Vector3 orthogonalDirection = new Vector3(-direction.z, direction.y, direction.x);
        Vector3 segmentPosition;
        float maxVarianceDistance = GameSettingsManager.GetFloat("Route  Settings", "MaxVariancePointDistance", 1000);
        float randomDirection;
        for (int i = 0; i < collectables.Length; i++)
        {
            segmentPosition = start + (direction * (gap * (i + 1)));
            randomDirection = UnityEngine.Random.Range(0, 2) == 0 ? 1 : -1;
            collectables[i].transform.position = segmentPosition + (orthogonalDirection * randomDirection * UnityEngine.Random.Range(-maxVarianceDistance, maxVarianceDistance));
        }        
    }
}
