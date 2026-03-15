using System.Collections.Generic;
using UnityEngine;

public class GameDestinationLoader : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private Transform player;
    [SerializeField] private WaypointPathController path;
    [Header("Prefabs")]
    [SerializeField] private DestinationHandler destinationPrefab;
    [SerializeField] private InterestPointHandler interestPointPrefab;
    [SerializeField] private CollectableHandler bonusPrefab;
    [SerializeField] private InfoPointHandler infoPointPrefab;
    // Add the mission handler prefab

    //Temp
    [Header("Collectiable")]
    [SerializeField] private DestinationCollectableData[] destinationCollectables;
    [SerializeField] private InfoCollectableData infoCollectable;
    [SerializeField] private InfoCollectableData interestCollectable;
    [SerializeField] private BonusCollectableData[] bonusCollectables;

    private DestinationHandler[] _destinations;

    private void Awake()
    {
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
            Vector3 position = LocationsManager.GetDestination(destination).DestinationData.WorldPosition;
            DestinationHandler destinationHandler = Instantiate(destinationPrefab,position,Quaternion.identity);
            destinationHandler.Destination = destination;
            _destinations[i] = destinationHandler;
        }
    }
    private void GenerateInterestPoints()
    {
        for (int i = 0; i < LocationsManager.InterestPointsCount; i++)
        {
            InterestPointSO interestPoint = LocationsManager.GetInterestPoint(i);
            InterestPointHandler interestPointHandler = Instantiate(interestPointPrefab, interestPoint.InterestPointData.WorldPosition, Quaternion.identity);
            interestPointHandler.Init(interestPoint, interestCollectable);
        }
    }

    private void GenerateRoute()
    {
        //Find the longest path
        //reorder the destinations

        for (int i = 0; i < _destinations.Length; i++)
        {
            DestinationCollectableData destinationCollectable = destinationCollectables[i];
            _destinations[i].Init(destinationCollectable.RangeData, destinationCollectable.CollectionPopup);
        }

        Vector3 startPosition = Vector3.zero;
        Vector3 firstDestinationPosition = _destinations[0].transform.position;
        Vector3 direction = (firstDestinationPosition - _destinations[1].transform.position).normalized;
        startPosition = firstDestinationPosition + (direction * 20000);//TODO: Change to be the value from the ini
        player.position = startPosition + (Vector3.up * 2000);
        player.LookAt(new Vector3(firstDestinationPosition.x, player.position.y, firstDestinationPosition.z));

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
        for (int i = 0; i < bonusCollectables.Length; i++)
        {
            bonusIndices.Add(i);
        }

        for (int i = 0; i < _destinations.Length; i++)
        {
            Vector3 start = waypoints[i];
            Vector3 end = waypoints[i + 1];
            int bonusIndex = bonusIndices[Random.Range(0, bonusIndices.Count)];
            bonusIndices.Remove(bonusIndex);
            GenerateLegCollectables(start, end, bonusIndex, _destinations[i].Destination);
        }
    }

    private void GenerateLegCollectables(Vector3 start, Vector3 end, int bonus, int destinationID)
    {
        //TODO: Add missions later
        CollectableHandler[] collectables = new CollectableHandler[4];
        List<int> collectablesIndices = new List<int>();
        for (int i = 0; i < collectables.Length; i++)
        {
            collectablesIndices.Add(i);
        }
        int bonusIndex = collectablesIndices[Random.Range(0, collectablesIndices.Count)];
        collectablesIndices.Remove(bonusIndex);
        CollectableHandler bonusPoint = Instantiate(bonusPrefab);
        bonusPoint.Init(bonusCollectables[bonus].RangeData, bonusCollectables[bonus].CollectionPopup, bonusCollectables[bonus].NotificationPopup);
        collectables[bonusIndex] = bonusPoint;

        for (int i = 0; i < 3; i++)
        {
            InfoPointHandler infoPoint = Instantiate(infoPointPrefab);
            collectables[collectablesIndices[i]] = infoPoint;
            DestinationSO destination = LocationsManager.GetDestination(destinationID);
            switch (i)
            {
                case 0:
                    infoPoint.Init(destination.DestinationData.FirstInfoPoint, infoCollectable);
                    break;
                case 1:
                    infoPoint.Init(destination.DestinationData.SecondInfoPoint, infoCollectable);
                    break;
                case 2:
                    infoPoint.Init(destination.DestinationData.ThirdInfoPoint, infoCollectable);
                    break;
            }
        }

        Vector3 diff = (end - start);
        float gap = diff.magnitude / (collectables.Length+1);
        Vector3 direction = diff.normalized;
        for (int i = 0; i < collectables.Length; i++)
        {
            collectables[i].transform.position = start + (direction * (gap * (i + 1)));
        }
    }
}
