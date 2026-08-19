/*     Unity GIS Tech 2020-2022      */

using System;
using UnityEditor;
using UnityEngine;

namespace GISTech.TerrainStreaming
{
    public class TerrainStreamingParameters : EditorWindow
    {
        public static string CacheDataPath = "";
        public static TerrainStreamingCachePathType DownloadCachePathType = TerrainStreamingCachePathType.Standard;
        private string m_CacheDataPath = "";
        private static bool ShowCacheFolder;


        public static string srtm_90_tiff_temp_path = "/ElevationData/SRTM_90m_Tiff";
        public static string srtm_30_hgt_temp_path = "/ElevationData/SRTM_30m_HGT";
        public static string Mapbox_pngraw_temp_path = "/ElevationData/MapBox_PNGRAW";

        [MenuItem("Tools/GIS Tech/Terrain Streaming/GIS Data Downloader Parameters", false, 2)]

        public static void Init()
        {
            TerrainStreamingParameters window = GetWindow<TerrainStreamingParameters>(true, "GIS Data Downloader Parameters", true);
            window.minSize = new Vector2(500, 200);
            window.maxSize = new Vector2(800, 300);
        }
        void OnEnable()
        {
            LoadPrefs();
        }
        void OnDisable()
        {
            SavePrefs();
        }

        public void OnGUI()
        {

            EditorGUILayout.BeginVertical(GUI.skin.button);
            ShowCacheFolder = EditorGUILayout.Foldout(ShowCacheFolder, "Cache Folder");
            EditorGUILayout.EndVertical();

            if (ShowCacheFolder)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(" Download Path Type", " The Path where GDD will download data, Standard for PersistancePath and Custom to set a custom path "), GUILayout.MaxWidth(200));
                DownloadCachePathType = (TerrainStreamingCachePathType)EditorGUILayout.EnumPopup("", DownloadCachePathType);
                GUILayout.EndHorizontal();


                if (DownloadCachePathType == TerrainStreamingCachePathType.Custom)
                {
                    if (GUILayout.Button(" Select Location ", GUI.skin.button))
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent(" Download Path ", " Select the location where data will be downloaded "), GUILayout.MaxWidth(200));
                        m_CacheDataPath = EditorUtility.OpenFolderPanel("Streaming Location", "", "");
                        GUILayout.EndHorizontal();
                    }

                    if (!string.IsNullOrEmpty(m_CacheDataPath))
                    {
                        CacheDataPath = m_CacheDataPath;
                    }


                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent(" Cache Path ", " Path where the data will be downloaded "), GUILayout.MaxWidth(200));
                    CacheDataPath = EditorGUILayout.TextField("", CacheDataPath);
                    GUILayout.EndHorizontal();
                }

                if (DownloadCachePathType == TerrainStreamingCachePathType.Standard)
                {
                    CacheDataPath = Application.persistentDataPath;
                    SetCachePath(CacheDataPath);
                }


                EditorGUILayout.BeginHorizontal(GUI.skin.box);

                if (GUILayout.Button(" Apply ", GUI.skin.button))
                {
                    GUILayout.BeginHorizontal();
                    SetCachePath(CacheDataPath);
                    GUILayout.EndHorizontal();
                }

                if (GUILayout.Button(" Cancel ", GUI.skin.button))
                {
                    GUILayout.BeginHorizontal();

                    GUILayout.EndHorizontal();
                }
                EditorGUILayout.EndHorizontal();
            }


            if (GUILayout.Button(" Clear Cache Folder ", GUI.skin.button))
            {
                GUILayout.BeginHorizontal();

                ClearFolder(CacheDataPath);

                GUILayout.EndHorizontal();
            }
        }

        private void SavePrefs()
        {
            TerrainStreamingSaveLoadPrefs.SavePref("DownloadCachePathType", (int)DownloadCachePathType);
            TerrainStreamingSaveLoadPrefs.SavePref("CacheDataPath", CacheDataPath);
            TerrainStreamingSaveLoadPrefs.SavePref("ShowCacheFolder", ShowCacheFolder);
        }
        public static void LoadPrefs()
        {
            DownloadCachePathType = (TerrainStreamingCachePathType)TerrainStreamingSaveLoadPrefs.LoadPref("DownloadCachePathType", (int)TerrainStreamingCachePathType.Standard);
            CacheDataPath = (string)TerrainStreamingSaveLoadPrefs.LoadPref("CacheDataPath", Application.persistentDataPath);
            ShowCacheFolder = (bool)TerrainStreamingSaveLoadPrefs.LoadPref("ShowCacheFolder", true);
            SetCachePath(CacheDataPath);
        }

        public static void SetCachePath(string CachePath)
        {
            if (string.IsNullOrEmpty(CacheDataPath))
                CacheDataPath = Application.persistentDataPath;

            if (!string.IsNullOrEmpty(CachePath))
                CacheDataPath = CachePath;
 
            srtm_90_tiff_temp_path = CacheDataPath + "/ElevationData/SRTM_90m_Tiff";
            srtm_30_hgt_temp_path = CacheDataPath + "/ElevationData/SRTM_30m_HGT";
            Mapbox_pngraw_temp_path = CacheDataPath + "/ElevationData/MapBox_PNGRAW";


        }
        private void ClearFolder(string FolderName)
        {
            if (System.IO.Directory.Exists(FolderName))
            {
                System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(FolderName);

                foreach (System.IO.FileInfo file in di.GetFiles())
                {
                    file.Delete();
                }
                foreach (System.IO.DirectoryInfo dir in di.GetDirectories())
                {
                    dir.Delete(true);
                }
            }

        }
    }
}
