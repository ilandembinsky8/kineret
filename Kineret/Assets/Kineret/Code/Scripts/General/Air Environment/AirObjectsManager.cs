using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class AirObjectsManager : MonoBehaviour
{
    [Header("Air Objects Manager Settings")]
    [SerializeField] private List<AirEnvironmentObject> aircraftsEnvironmentPrefabs = new List<AirEnvironmentObject>();
    [SerializeField] private List<AirEnvironmentObject> birdsEnvironmentPrefabs = new List<AirEnvironmentObject>();
    [SerializeField] private GameObject airEnvironmentParent;
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject playerForwardDirectionReference;

    [SerializeField] private float angleOffset = 22.5f; //angle offset for the crossing direction

    //private variables
    private float _birdsSpawnTimer = 0f;
    private float _aircraftsSpawnTimer = 0f;

    // IMPORTANT - can also be found in the PlayerColliderHandler script. Might be better to make a static class for all of the tags in the game
    private const string TERRAIN_TAG = "Terrain";

    //birds spawning settings
    private Vector2 _birdsMovementSpeedRange;
    private Vector2 _birdsLifetimeRange;
    private Vector2 _birdsFadeDurationRange;
    private Vector2 _birdsSpawningAmountRange;
    private Vector2 _birdsSpawnIntervalRange;//wait time duration between birds spawning
    private Vector2 _birdsSpawningHeightOffsetRange;
    private Vector2 _birdsForwardSpawnDistanceRange;
    private Vector2 _birdsSideSpawnDistanceRange;

    //aircrafts spawning settings
    private Vector2 _aircraftsMovementSpeedRange;
    private Vector2 _aircraftsLifetimeRange;
    private Vector2 _aircraftsFadeDurationRange;
    private Vector2 _aircraftsSpawningAmountRange;
    private Vector2 _aircraftsSpawnIntervalRange;//wait time duration between aircrafts spawning
    private Vector2 _aircraftsSpawningHeightOffsetRange;
    private Vector2 _aircraftsForwardSpawnDistanceRange;
    private Vector2 _aircraftsSideSpawnDistanceRange;

    private void Awake()
    {
        LoadBirdsSettings();
        LoadAircraftsSettings();
    }

    private void Start()
    {
        StartCoroutine(SpawnBirdsRoutine());//first call, then it keeps on calling itself
        StartCoroutine(SpawnAircraftsRoutine());
    }

    private IEnumerator SpawnBirdsRoutine()
    {
        _birdsSpawnTimer = Random.Range(_birdsSpawnIntervalRange.x, _birdsSpawnIntervalRange.y);
        yield return new WaitForSeconds(_birdsSpawnTimer);
        SpawnBirds();
        StartCoroutine(SpawnBirdsRoutine());//recursion
    }

    private IEnumerator SpawnAircraftsRoutine()
    {
        _aircraftsSpawnTimer = Random.Range(_aircraftsSpawnIntervalRange.x, _aircraftsSpawnIntervalRange.y);
        SpawnAircrafts();
        yield return new WaitForSeconds(_aircraftsSpawnTimer);
        StartCoroutine(SpawnAircraftsRoutine());//recursion
    }

    private void SpawnBirds()
    {
        if (birdsEnvironmentPrefabs.Count == 0)
        {
            Debug.LogWarning("No bird prefabs assigned in the AirObjectsManager. Please assign at least one bird prefab.");
            return;
        }
        int randomBirdIndex = Random.Range(0, birdsEnvironmentPrefabs.Count);

        AirEnvironmentObject birdPrefab = birdsEnvironmentPrefabs[randomBirdIndex];
        int randomBirdsAmount = (int)Random.Range(_birdsSpawningAmountRange.x, _birdsSpawningAmountRange.y);
        for (int i = 0; i < randomBirdsAmount; i++)
        {
            if (birdPrefab != null)
            {
                //now we will randomize all of the needed settings for the bird prefab before spawning it
                //starting with the movement speed
                float selectedMovementSpeed = Random.Range(_birdsMovementSpeedRange.x, _birdsMovementSpeedRange.y);
                float selectedLifetime = Random.Range(_birdsLifetimeRange.x, _birdsLifetimeRange.y);
                float selectedFadeDuration = Random.Range(_birdsFadeDurationRange.x, _birdsFadeDurationRange.y);
                float selectedHeightOffset = Random.Range(_birdsSpawningHeightOffsetRange.x, _birdsSpawningHeightOffsetRange.y);
                //we have two different distances for the X and Z axis to make it more random and natural
                float selectedForwardDistance = Random.Range(_birdsForwardSpawnDistanceRange.x, _birdsForwardSpawnDistanceRange.y);
                float selectedSideDistance = Random.Range(_birdsSideSpawnDistanceRange.x, _birdsSideSpawnDistanceRange.y);
                float sideSign = Random.value < 0.5f ? -1f : 1f;

                Vector3 spawnPosition = GetSpawnPosition(selectedForwardDistance, selectedSideDistance, selectedHeightOffset, sideSign);
                //now we can spawn the bird prefab with the randomized settings
                AirEnvironmentObject spawnedBird = Instantiate(birdPrefab, spawnPosition, Quaternion.identity, airEnvironmentParent.transform);
                Vector3 crossingDirection = GetCrossingDirection(sideSign);

                Vector2 movementDirection = new Vector2(crossingDirection.x, crossingDirection.z);
                spawnedBird.Initialize(selectedMovementSpeed, selectedLifetime, selectedFadeDuration, movementDirection);
                spawnedBird.tag = TERRAIN_TAG;
            }
        }
    }

    private void SpawnAircrafts()
    {
        if (aircraftsEnvironmentPrefabs.Count == 0)
        {
            Debug.LogWarning("No aircraft prefabs assigned in the AirObjectsManager. Please assign at least one aircraft prefab.");
            return;
        }
        int randomAircraftIndex = Random.Range(0, aircraftsEnvironmentPrefabs.Count);

        AirEnvironmentObject aircraftPrefab = aircraftsEnvironmentPrefabs[randomAircraftIndex];
        int randomAircraftsAmount = (int)Random.Range(_aircraftsSpawningAmountRange.x, _aircraftsSpawningAmountRange.y);
        for (int i = 0; i < randomAircraftsAmount; i++)
        {
            if (aircraftPrefab != null)
            {
                //now we will randomize all of the needed settings for the aircraft prefab before spawning it
                //starting with the movement speed
                float selectedMovementSpeed = Random.Range(_aircraftsMovementSpeedRange.x, _aircraftsMovementSpeedRange.y);
                float selectedLifetime = Random.Range(_aircraftsLifetimeRange.x, _aircraftsLifetimeRange.y);
                float selectedFadeDuration = Random.Range(_aircraftsFadeDurationRange.x, _aircraftsFadeDurationRange.y);
                float selectedHeightOffset = Random.Range(_aircraftsSpawningHeightOffsetRange.x, _aircraftsSpawningHeightOffsetRange.y);
                //we have two different distances for the X and Z axis to make it more random and natural
                float selectedForwardDistance = Random.Range(_aircraftsForwardSpawnDistanceRange.x, _aircraftsForwardSpawnDistanceRange.y);
                float selectedSideDistance = Random.Range(_aircraftsSideSpawnDistanceRange.x, _aircraftsSideSpawnDistanceRange.y);
                float sideSign = Random.value < 0.5f ? -1f : 1f;

                Vector3 spawnPosition = GetSpawnPosition(selectedForwardDistance, selectedSideDistance, selectedHeightOffset, sideSign);
                //now we can spawn the aircraft prefab with the randomized settings
                AirEnvironmentObject spawnedAircraft = Instantiate(aircraftPrefab, spawnPosition, Quaternion.identity, airEnvironmentParent.transform);
                Vector3 crossingDirection = GetCrossingDirection(sideSign);

                Vector2 movementDirection = new Vector2(crossingDirection.x, crossingDirection.z);
                spawnedAircraft.Initialize(selectedMovementSpeed, selectedLifetime, selectedFadeDuration, movementDirection);
                spawnedAircraft.tag = TERRAIN_TAG;
            }
        }
    }

    private void LoadBirdsSettings()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, "Air Environment/EnvironmentBirdsSettings.json");
        if (!File.Exists(filePath))
        {
            Debug.LogWarning("EnvironmentBirdsSettings.json file not found. Using default settings.");
            return;
        }
        string json = File.ReadAllText(filePath);
        EnvironmentBirdsSettings settings = JsonUtility.FromJson<EnvironmentBirdsSettings>(json);
        //now load the settings into the private variables
        _birdsMovementSpeedRange = new Vector2(settings.MinMovementSpeed, settings.MaxMovementSpeed);
        _birdsLifetimeRange = new Vector2(settings.MinLifeTime, settings.MaxLifeTime);
        _birdsFadeDurationRange = new Vector2(settings.MinFadeDuration, settings.MaxFadeDuration);
        _birdsSpawningAmountRange = new Vector2(settings.MinBirdsPerSpawn, settings.MaxBirdsPerSpawn);
        _birdsSpawnIntervalRange = new Vector2(settings.MinSpawnInterval, settings.MaxSpawnInterval);
        _birdsSpawningHeightOffsetRange = new Vector2(settings.MinSpawnHeightOffset, settings.MaxSpawnHeightOffset);
        _birdsForwardSpawnDistanceRange = new Vector2(settings.MinForwardSpawnDistance, settings.MaxForwardSpawnDistance);
        _birdsSideSpawnDistanceRange = new Vector2(settings.MinSideSpawnDistance, settings.MaxSideSpawnDistance);
        Debug.Log("Birds settings loaded successfully");
    }

    private void LoadAircraftsSettings()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, "Air Environment/EnvironmentAircraftSettings.json");
        if (!File.Exists(filePath))
        {
            Debug.LogWarning("EnvironmentAircraftSettings.json file not found. Using default settings.");
            return;
        }

        string json = File.ReadAllText(filePath);
        EnvironmentAircraftSettings settings = JsonUtility.FromJson<EnvironmentAircraftSettings>(json);

        //now load the settings into the private variables
        _aircraftsMovementSpeedRange = new Vector2(settings.MinMovementSpeed, settings.MaxMovementSpeed);
        _aircraftsLifetimeRange = new Vector2(settings.MinLifeTime, settings.MaxLifeTime);
        _aircraftsFadeDurationRange = new Vector2(settings.MinFadeDuration, settings.MaxFadeDuration);
        _aircraftsSpawningAmountRange = new Vector2(settings.MinAircraftPerSpawn, settings.MaxAircraftPerSpawn);
        _aircraftsSpawnIntervalRange = new Vector2(settings.MinSpawnInterval, settings.MaxSpawnInterval);
        _aircraftsSpawningHeightOffsetRange = new Vector2(settings.MinSpawnHeightOffset, settings.MaxSpawnHeightOffset);
        _aircraftsForwardSpawnDistanceRange = new Vector2(settings.MinForwardSpawnDistance, settings.MaxForwardSpawnDistance);
        _aircraftsSideSpawnDistanceRange = new Vector2(settings.MinSideSpawnDistance, settings.MaxSideSpawnDistance);
        Debug.Log("Aircrafts settings loaded successfully.");
    }


    private Vector3 GetCrossingDirection(float spawnSideSign)
    {
        Transform directionReference = playerForwardDirectionReference != null ? playerForwardDirectionReference.transform : player.transform;
        Vector3 flatForward = Vector3.ProjectOnPlane(directionReference.forward, Vector3.up).normalized;
        Vector3 flatRight = Vector3.Cross(Vector3.up, flatForward).normalized;
        //spawn to the left -> moves to the right, vice versa.
        Vector3 crossingDirection = -spawnSideSign * flatRight;

        float angleVariation = Random.Range(-angleOffset, angleOffset);

        return (Quaternion.AngleAxis(angleVariation, Vector3.up) * crossingDirection).normalized;
    }

    private Vector3 GetSpawnPosition(float forwardDistance, float sideDistance, float heightOffset, float sideSign)
    {
        Transform directionReference = playerForwardDirectionReference != null ? playerForwardDirectionReference.transform : player.transform;

        Vector3 flatForward = Vector3.ProjectOnPlane(directionReference.forward, Vector3.up).normalized;
        Vector3 flatRight = Vector3.Cross(Vector3.up, flatForward).normalized;

        return player.transform.position + flatForward * forwardDistance + flatRight * sideDistance * sideSign + Vector3.up * heightOffset;
    }
}
