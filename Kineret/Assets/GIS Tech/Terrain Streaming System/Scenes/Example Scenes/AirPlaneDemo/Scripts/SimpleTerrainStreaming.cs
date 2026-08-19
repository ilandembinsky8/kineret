/*     Unity GIS Tech 2020-2022      */

using UnityEngine;
using UnityEngine.UI;
using System.IO;
using GISTech.TerrainStreaming;

 
public class SimpleTerrainStreaming : MonoBehaviour
{
    public enum WayPointMode  {Random,Custom};
    //Create Random Way points 
    public WayPointMode wayPointMode = WayPointMode.Random;
    //Number of Random Points
    public int RandomWayPointsNumber = 10;
    //Pressed Key To Start Generate Terrain
    public KeyCode GenerateKey;
    //Reference to WayPoint Generator Script
    public WayPoints GeoWaypoints;
    //Reference to The AirPlane Controler
    public Airplane AirPlane;
    //Instantiate Some Shperes as a waypoints
    public bool InstantiateGameObjects;
    public Text UIText;
    //Reference to The MainScene Camera
    public Camera MainCamera;
    //TerrainData.dat file path
    private string TerrainFilePath;
    private bool Generated;

    //Reference to TerrainStreamingSystemPrefs Script
    private TerrainStreamingSystemPrefs RuntimePrefs;
    //Reference to TerrainStreamingSystem Script
    private TerrainStreamingSystem RuntimeGenerator;

 
    void Start()
    {
        //Get References
        RuntimePrefs = TerrainStreamingSystemPrefs.Get;
        RuntimeGenerator = TerrainStreamingSystem.Get;

        //TerrainData.dat File Path
        TerrainFilePath = Application.streamingAssetsPath + "/Terrain Streaming/Mojave_Desert/TerrainData.dat";
        //Initializ TerrainStreamingSystem preferences
        InitializingRuntimePrefs();
        //Event Called when TerrainStreamingSystem finish from generating the first basic tiles
        TerrainStreamingSystem.OnFinish += OnTerrainGeneratingCompleted;
    }
    private void InitializingRuntimePrefs()
    {
        RuntimePrefs.terrainScale = new Vector3(1, 1, 1);
     }
    void Update()
    {       
        //Start Generation on KeyPressed
        if (Input.GetKeyDown(GenerateKey))
            OnGenerateTerrain(TerrainFilePath);

        if (Generated)
           UIText.text = "Latitude: " + AirPlane.GetAirPlaneLatLonElevation().y + " \n" + "Longitude: " + AirPlane.GetAirPlaneLatLonElevation().x + " \n" + "Elevation:" + AirPlane.GetAirPlaneLatLonElevation().z + " m";
    }

    private void OnGenerateTerrain(string TerrainMetadatPath)
    {
        if (File.Exists(TerrainMetadatPath))
        {        
            //Load Terrain File
            RuntimeGenerator.LoadTerrainDataFile(TerrainMetadatPath);
            //Start Generation
            StartCoroutine(RuntimeGenerator.GenerateTerrains());
        }
        else
            Debug.LogError("File not found : " + TerrainFilePath);
    }
 
    private void OnTerrainGeneratingCompleted(TerrainStreamingContainer container)
    {
        //This Function used to Create WayPoints Randomly or by Setting Lat-Lon-Elevation of Each point from the inspector
        if(RuntimeGenerator.SectorContainer)
        {
            if (wayPointMode == WayPointMode.Random)
                GeoWaypoints.GenerateRandomWayPoints(container, RandomWayPointsNumber, true);
            else
                GeoWaypoints.ConvertLatLonToSpacePosition(RuntimeGenerator.SectorContainer, InstantiateGameObjects);

            AirPlane.OnTerrainGeneratingCompleted();

            //Disable The mainscene camera 
            MainCamera.gameObject.SetActive(false);
            AirPlane.GetComponent<TerrainStreamingPlayer>().playerCam.gameObject.SetActive(true);
            Generated = true;
        }

    }
    void OnDisable()
    {
        TerrainStreamingSystem.OnFinish -= OnTerrainGeneratingCompleted;

    }
}
