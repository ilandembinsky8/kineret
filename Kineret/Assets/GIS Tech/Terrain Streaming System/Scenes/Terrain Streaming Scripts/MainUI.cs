/*     Unity GIS Tech 2019-2022      */

using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using SimpleFileBrowser;

namespace GISTech.TerrainStreaming
{
    public class MainUI : MonoBehaviour
    {
        public enum WayPointMode { Random, Custom };

        [Space]

        public WayPointMode wayPointMode = WayPointMode.Random;

        public WayPoints GeoWaypoints;

        public int RandomWayPointsNumber = 10;

        public Airplane AirPlane;

        public Camera MainCamera;

        public Button GenerateTerrain;

        public Button OpenTerrainPath;

        public FileBrowser fileBrowserDiag;

        public Text TerrainPathText;

        private TerrainStreamingSystem RuntimeGenerator;

        private TerrainStreamingSystemPrefs RuntimePrefs;

        public const string version = "2.0";

        void Start()
        {
            FileBrowser.SetFilters(true, new FileBrowser.Filter("Dynamic Terrain File", ".dat"));
            FileBrowser.SetDefaultFilter(".dat");
            FileBrowser.SetExcludedExtensions(".lnk", ".tmp", ".zip", ".rar", ".exe");
            var defaultpath = TerrainPathText.text;

            if (string.IsNullOrEmpty(defaultpath))
                defaultpath = Application.dataPath;

            FileBrowser.AddQuickLink("Data Path", defaultpath, null);

            RuntimeGenerator = TerrainStreamingSystem.Get;

            RuntimePrefs = TerrainStreamingSystemPrefs.Get;

            OpenTerrainPath.onClick.AddListener(OnLoadBtnClicked);

            GenerateTerrain.onClick.AddListener(OnGenerateTerrainBtnClicked);

            QualitySettings.asyncUploadTimeSlice = 4;

            QualitySettings.asyncUploadBufferSize = 16;
#if UNITY_2018 || UNITY_2017
            QualitySettings.asyncUploadPersistentBuffer = true;
#endif
            TerrainStreamingSystem.OnFinish += OnFinish;
        }
        private void OnFinish(TerrainStreamingContainer container)
        {
            if (RuntimeGenerator.SectorContainer)
            {
                if (wayPointMode == WayPointMode.Random)
                    GeoWaypoints.GenerateRandomWayPoints(container, RandomWayPointsNumber, true);
                else
                    GeoWaypoints.ConvertLatLonToSpacePosition(RuntimeGenerator.SectorContainer, false);

                AirPlane.OnTerrainGeneratingCompleted();

            }
        }
        private void OnGenerateTerrainBtnClicked()
        {
            if (File.Exists(TerrainPathText.text))
            {
                RuntimeGenerator.LoadTerrainDataFile(TerrainPathText.text);

                StartCoroutine(RuntimeGenerator.GenerateTerrains());
            
            }

        }
        private void OnLoadBtnClicked()
        {
            StartCoroutine(ShowLoadDialogCoroutine());
        }
        IEnumerator ShowLoadDialogCoroutine()
        {
            var defaultpath = TerrainPathText.text;

            if (string.IsNullOrEmpty(defaultpath))
                defaultpath = Application.dataPath;

            yield return FileBrowser.WaitForLoadDialog(false, Path.GetDirectoryName(defaultpath), "Load Dynamic Terrain Data file", "Load TerrainData");
            TerrainPathText.text = FileBrowser.Result;
        }
    }
}