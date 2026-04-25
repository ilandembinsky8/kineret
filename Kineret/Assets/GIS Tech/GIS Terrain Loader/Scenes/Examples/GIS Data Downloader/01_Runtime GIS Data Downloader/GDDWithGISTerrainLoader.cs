using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

using GISTech.GISTerrainLoader;
#if GISDataDownloader
using GISTech.GISDataDownloader;
#endif
public class GDDWithGISTerrainLoader : MonoBehaviour
{
    private GISTerrainLoaderPrefs Prefs;
    private GISTerrainLoaderRuntimePrefs RuntimePrefs;
    private RuntimeTerrainGenerator RuntimeGenerator;
    void Start()
    {

        //Ref to RuntimeTerrainGenerator.cs Script
        RuntimeGenerator = RuntimeTerrainGenerator.Get;
        //Ref to GISTerrainLoaderRuntimePrefs.cs Script
        RuntimePrefs = GISTerrainLoaderRuntimePrefs.Get;

        //Set GISTerrainLoaderPrefs to GISTerrainLoaderRuntimePrefs script
        Prefs = RuntimePrefs.Prefs;

#if GISDataDownloader
        GISTech.GISDataDownloader.GISDataDownloaderRuntimeDownloader.OnDownloadComplete += OnGDDDownloadComplete;
#endif
    }
    private void Awake()
    {
#if UNITY_6000_0_OR_NEWER
            //Fix Input system conflict with new Unity versions
            UnityEngine.EventSystems.EventSystem es = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include);
            var standalone = es.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            if (standalone != null) Destroy(standalone);
            if (es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
                es.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#endif
    }
    private IEnumerator GenerateTerrain(string TerrainPath)
    {
        yield return new WaitForSeconds(1f);

        if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer)
        {
            if (string.IsNullOrEmpty(TerrainPath) || !System.IO.File.Exists(TerrainPath))
            {
                Debug.LogError("Terrain file null or not supported.. Try againe");
                yield break;
            }
        }
 
        StartCoroutine(RuntimeGenerator.StartGenerating(Prefs));

    }

#if GISDataDownloader
    private void OnGDDDownloadComplete(GISDataDownloaderPrefs GDD_prefs)
    {
        if (Prefs.GenerateTerrainFromDownloadedData == GISTech.GISTerrainLoader.OptionEnabDisab.Enable)
        {
            InitializingRuntimePrefs(GDD_prefs);
            //Coroutine To Start Generating terrains
            StartCoroutine(GenerateTerrain(Prefs.TerrainFilePath));
        }

        if (GDD_prefs.PathType == DownloadPathType.GISTerrainFolder && Prefs.GenerateTerrainFromDownloadedData == GISTech.GISTerrainLoader.OptionEnabDisab.Enable)
        {
            Debug.Log(" Set 'Download Path Type' to 'GIS Terrain Loader' in order to generate a terrain in play mode just after the download is complete");
        }
    }

    private void InitializingRuntimePrefs(GISDataDownloaderPrefs GDD_prefs)
    {
        //Enable RuntimeGenerator if it's not enabled
        RuntimeGenerator.enabled = true;
        //Set the path of the DEM file to GISTerrainLoaderPrefs 
        Prefs.TerrainFilePath = GDD_prefs.DownloadPath + "\\" + GDD_prefs.ZoneName + ".tif";
        //Enable it to Remove any GIS terrains already generated 
        Prefs.RemovePrvTerrain = GISTech.GISTerrainLoader.OptionEnabDisab.Enable;
        //We are loading a GeoTiff DEM File and we need a to read a real Terrain elevation values
        Prefs.TerrainElevation = TerrainElevation.RealWorldElevation;
        //We are loading a GeoTiff DEM File and we need a to read the real world terrain dimensions
        Prefs.terrainDimensionMode = TerrainDimensionsMode.AutoDetection;
        //Set the scale to default Vector3(1, 1, 1)
        Prefs.terrainScale = new Vector3(0.5f, 0.5f, 0.5f);
        //The DEM file Projected in Geographic WGS84 EPSG= 4326 so we can set the projection mode to geographic or AutoDetection
        Prefs.projectionMode = ProjectionMode.Geographic;
        //Terrain Tiles Heightmap resolution
        Prefs.heightmapResolution = 513;
        //Enable this to add textures to terrain
        Prefs.textureMode = TextureMode.WithTexture;

#if UNITY_6000_0_OR_NEWER
            if(Prefs.terrainMaterialMode == TerrainMaterialMode.Standard)
            {
                Prefs.terrainMaterialMode = TerrainMaterialMode.Custom;
                Prefs.terrainMaterial = Resources.Load<Material>("Materials/URP");
            }
#endif

    }
#endif
}
