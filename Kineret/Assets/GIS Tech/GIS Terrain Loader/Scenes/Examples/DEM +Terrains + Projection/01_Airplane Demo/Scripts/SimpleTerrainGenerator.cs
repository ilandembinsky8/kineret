/*     Unity GIS Tech 2020-2023      */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace GISTech.GISTerrainLoader
{

    public class SimpleTerrainGenerator : MonoBehaviour
    {
        public WayPoints GeoWaypoints;
        public AirplaneDemo AirPlane;
        public Text UIText;
        //Enable it to place empty gameobjects 
        public bool InstantiateGameObjects;

        public string TerrainFilePath;

        private RuntimeTerrainGenerator RuntimeGenerator;

        private GISTerrainLoaderPrefs Prefs;
        private GISTerrainLoaderRuntimePrefs RuntimePrefs;

        [HideInInspector]
        public bool TerrainGenerated;

        public bool GenerateTerrainBackground;
        void Start()
        {
            var filePath = Application.streamingAssetsPath + TerrainFilePath;

            RuntimePrefs = GISTerrainLoaderRuntimePrefs.Get;
            Prefs = RuntimePrefs.Prefs;
            Prefs.TerrainFilePath = filePath;

            RuntimeGenerator = RuntimeTerrainGenerator.Get;

            RuntimeTerrainGenerator.OnFinish += OnTerrainGeneratingCompleted;
            StartCoroutine(GenerateTerrain(filePath));
        }
        void Update()
        {
             if (TerrainGenerated)
                UIText.text = "Latitude: " + AirPlane.GetAirPlaneLatLonElevation().y + " \n" + "Longitude: " + AirPlane.GetAirPlaneLatLonElevation().x + " \n" + "Elevation:" + Math.Round(AirPlane.GetAirPlaneLatLonElevation().z, 2) + " m";
        }
        private IEnumerator GenerateTerrain(string TerrainPath)
        {
            yield return new WaitForSeconds(2f);

            if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer)
            {
                if (string.IsNullOrEmpty(TerrainPath) || !System.IO.File.Exists(TerrainPath))
                {
                 
                    Debug.LogError("Terrain file null or not supported.. Try againe");         
                    yield break;
                }
            }

            InitializingRuntimePrefs(TerrainPath);
            StartCoroutine(RuntimeGenerator.StartGenerating(Prefs));

        }
        private void InitializingRuntimePrefs(string TerrainPath)
        {
            RuntimeGenerator.enabled = true;
            Prefs.TerrainFilePath = TerrainPath;
            Prefs.RemovePrvTerrain = OptionEnabDisab.Enable;

            //Load Real Terrain elevation values
            Prefs.TerrainElevation = TerrainElevation.RealWorldElevation;
            //RuntimePrefs.terrainScale = new Vector3(1, 1, 1);

            Prefs.terrainDimensionMode = TerrainDimensionsMode.AutoDetection;

            //RuntimePrefs.heightmapResolution = 513;
            Prefs.textureloadingMode = TexturesLoadingMode.AutoDetection;

            Prefs.vectorType = VectorType.OpenStreetMap;

            if(GenerateTerrainBackground)
            {
                Prefs.TerrainBackground = OptionEnabDisab.Enable;
                Prefs.TerrainBackgroundHeightmapResolution = 513;
                Prefs.TerrainBackgroundTextureResolution = 512;
            }

#if UNITY_6000_0_OR_NEWER

            //Fix Rendering issues with URP pipeline  when using standard shader, you can remove this if you are using custom shaders or the default rendering pipeline
            if (Prefs.terrainMaterialMode == TerrainMaterialMode.Standard)
            {
                Prefs.terrainMaterialMode = TerrainMaterialMode.Custom;
                Prefs.terrainMaterial = Resources.Load<Material>("Materials/URP");
            }
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

        private void OnTerrainGeneratingCompleted(GISTerrainContainer m_container)
        {
            //Convert Geo Lat/Lon Way Points to Unity World Space

            GeoWaypoints.ConvertLatLonToSpacePosition(RuntimeGenerator.GeneratedContainer, InstantiateGameObjects);

            AirPlane.OnTerrainGeneratingCompleted();

            TerrainGenerated = true;
        }
        private void OnDisable()
        {
            RuntimeTerrainGenerator.OnFinish -= OnTerrainGeneratingCompleted;
        }
    }
}
