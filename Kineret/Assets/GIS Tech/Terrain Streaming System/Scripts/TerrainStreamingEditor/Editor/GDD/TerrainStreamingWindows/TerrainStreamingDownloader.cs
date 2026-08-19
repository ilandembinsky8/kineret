/*     Unity GIS Tech 2020-2022      */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace GISTech.TerrainStreaming
{
 
    public delegate void DownloadingPhase(string phasename, float value);
    public delegate void DownloadEvent();

    public class TerrainStreamingDownloader : EditorWindow
    {
        #region Variables


        public MainOperation MainOperation = MainOperation.DownloadAndGenerate;

        public TerrainStreamingZoneInfo MainZoneInf;
        public List<TerrainStreamingZoneInfo> RasterZoneInf;
 
        private Vector2 scrollPos = Vector2.zero;
        private bool ShowCoordinates;
        private bool ShowDownloadOptions;
        private bool ShowRasterDownloadOptions;
        public static event DownloadEvent OnDownloadStarted;
        public static event DownloadEvent OnDownloadCancelled;

        public string ZoneName = "";
        public DVector2 UpperLeftCoordiante = new DVector2(2.706201465, 33.9171170935667);
        public DVector2 DownRightCoordiante = new DVector2(2.77036813166667, 33.8479504269);

        public static List<TerrainStreamingTileData> SubZonesInfo = new List<TerrainStreamingTileData>();

        public DownloaderState State = DownloaderState.idle;
 
        public string DownloadPath = "";

        public OptionEnabDisab DEMMode = OptionEnabDisab.Enable;

        public GISDataDownloaderDEMProvider DEMSource = GISDataDownloaderDEMProvider.SRTM_90m;
        private int[] HeightmapResolutionsLst = { 33, 65, 129, 257, 513, 1025 };
        private string[] HeightmapResolutionsStr = new string[] { "33", "65", "129", "257", "516", "1025" };
        private int HeightmapResolutions_Selector = 3;
        public int HeightmapResolution = 257;

        public string User = "";
        public string Pass = "";


        public OptionEnabDisab TextureMode = OptionEnabDisab.Enable;

        
        public GISMapSource MapSource = GISMapSource.Mapbox;

        public GISDataDownloaderMapboxType MapBoxType = GISDataDownloaderMapboxType.Satellite;
        public OptionEnabDisab ShowLogo;
        public OptionEnabDisab ShowAttribution;
        public string MapBoxKey = "";

        public GISDataDownloaderArcGISType ArcGISType = GISDataDownloaderArcGISType.Satellite;

        public GISDataDownloaderBingMapType BingmapType = GISDataDownloaderBingMapType.aerial;
        public string BingKey = "";

        public OptionEnabDisab ReplaceExisitingRaster = OptionEnabDisab.Enable;
        public OptionEnabDisab GenerateTabFiles = OptionEnabDisab.Enable;
        public OptionEnabDisab QuitOnRasterError = OptionEnabDisab.Enable;

        private int[] ZoomLevels = { 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18 };
        public string[] ZoomLevelStr = new string[] { "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16", "17", "18" };
        public int ZoomLevel_Selector = 8;
        public int ZoomLevel = 13;

        public Vector2Int Hr_StreamingGridCount = new Vector2Int(4, 4);
        private int[] Hr_ZoomLevels = { 5, 6, 7, 8, 9, 10, 11, 12, 13 };
        public string[] Hr_ZoomLevelStr = new string[] { "5", "6", "7", "8", "9", "10", "11", "12", "13" };
        public int Hr_ZoomLevel_Selector = 7;
        public int Hr_ZoomLevel = 12;

        public OptionEnabDisab Hr_TextureMode = OptionEnabDisab.Enable;
        public OptionEnabDisab Hr_DEMMode = OptionEnabDisab.Enable;

        public OptionEnabDisab VectorMode = OptionEnabDisab.Disable;

        public OptionEnabDisab DownloadTree = OptionEnabDisab.Disable;
        public OptionEnabDisab DownloadGrass = OptionEnabDisab.Disable;

        public OptionEnabDisab ReplaceDownloadedVector = OptionEnabDisab.Enable;
        public OptionEnabDisab ReplaceGeneratedVectorTiles = OptionEnabDisab.Enable;
        private bool ShowVectorDownloadOptions;
        //comming
        public OptionEnabDisab DownloadRoads = OptionEnabDisab.Disable;
        public OptionEnabDisab DownloadBuildings = OptionEnabDisab.Disable;

        public GISDataDownloaderVectorProvider VectorSource = GISDataDownloaderVectorProvider.OpenStreetMap;
        public TerrainStreamingTerrainGrid GridMode = TerrainStreamingTerrainGrid.Auto;


        public OptionEnabDisab HorizonMode = OptionEnabDisab.Disable;
       

        private Vector2Int[] StandardGrid = { new Vector2Int(1, 1), new Vector2Int(2, 2), new Vector2Int(4, 4), new Vector2Int(5, 5), new Vector2Int(6, 6), new Vector2Int(7, 7), new Vector2Int(8, 8), new Vector2Int(10, 10), new Vector2Int(12, 12), new Vector2Int(14, 14), new Vector2Int(15, 15), new Vector2Int(20, 20), new Vector2Int(25, 25), new Vector2Int(30, 30), new Vector2Int(50, 50), new Vector2Int(75, 75), new Vector2Int(100, 100) };
        public string[] StandardGridStr = new string[] { "1x1", "2x2", "4x4", "5x5", "6x6", "7x7", "8x8", "10x10", "12x12", "14x14", "15x15", "20x20", "25x25", "30x30", "50x50", "75x75", "100x100" };
        public int StandardGrid_Selector = 2;
        public Vector2Int StreamingGridCount = new Vector2Int(4, 4);
        

        public OptionEnabDisab WorldPreview = OptionEnabDisab.Disable;
        public OptionEnabDisab RasterAreaPreview = OptionEnabDisab.Disable;
        public Vector3 WorldScale = new Vector3(1, 1, 1);


        private static float s_progress = 0f;
        private static string s_phase = "";
        public static bool Downloading = false;

        private CancellationTokenSource taskSource = null;
        #endregion

        [MenuItem("Tools/GIS Tech /Terrain Streaming/GIS Data Downloader ", false, 2)]
        static void Init()
        {
            TerrainStreamingDownloader window = (TerrainStreamingDownloader)EditorWindow.GetWindow(typeof(TerrainStreamingDownloader), false, "GIS Data Downloader");
            window.Show();
        }
        void OnEnable()
        {
            LoadPrefs();

            State = DownloaderState.idle;

            Application.runInBackground = true;
            TerrainStreamingTiffILoader.OnProgress += DownloadingProgress;
            TerrainStreamingPngRawLoader.OnProgress += DownloadingProgress;

            TerrainStreamingWorldGenerator.OnTilesGenerated += OnTilesGenerated;
            TerrainStreamingWorldGenerator.OnProgress += DownloadingProgress;
 
            TerrainStreamingWebDownloader.OnDownloadProgressChanged += DownloadingProgress;
            TerrainStreamingMultiWebDownloader.OnDownloadProgressChanged += DownloadingProgress;
 

            TerrainStreamingMultiWebDownloader.OnError += OnError;
            TerrainStreamingWebDownloader.OnError += OnError;
            TerrainStreamingZoneInfo.OnError += OnError;
            TerrainStreamingZoneInfo.OnDownloadProgressChanged += DownloadingProgress;

            TerrainStreamingParameters.LoadPrefs();
            
        }
        void OnDisable()
        {
            SavePrefs();
            TerrainStreamingTiffILoader.OnProgress -= DownloadingProgress;
            TerrainStreamingPngRawLoader.OnProgress -= DownloadingProgress;

            TerrainStreamingWorldGenerator.OnTilesGenerated -= OnTilesGenerated;
            TerrainStreamingWorldGenerator.OnProgress -= DownloadingProgress;

            TerrainStreamingWebDownloader.OnDownloadProgressChanged -= DownloadingProgress;
            TerrainStreamingMultiWebDownloader.OnDownloadProgressChanged -= DownloadingProgress;
            TerrainStreamingZoneInfo.OnDownloadProgressChanged -= DownloadingProgress;

            TerrainStreamingMultiWebDownloader.OnError -= OnError;
            TerrainStreamingWebDownloader.OnError -= OnError;
            TerrainStreamingZoneInfo.OnError -= OnError;
            TerrainStreamingZoneInfo.OnDownloadProgressChanged -= DownloadingProgress;

        }

        void OnInspectorUpdate() { Repaint(); }
        void OnGUI()
        {

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            GUIMainToolbar();
            GUICoordinates();
            GUIDownloadingOption();
            GUIGeneratingBtn();
            EditorGUILayout.EndVertical();
        }
        private static void GUIMainToolbar()
        {
            GUIStyle buttonStyle = new GUIStyle(EditorStyles.toolbarButton);

            GUILayout.BeginHorizontal();
            GUILayout.Label("", buttonStyle);

            GUILayout.EndHorizontal();
        }
        private void GUICoordinates()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.BeginVertical(GUI.skin.button);
            ShowCoordinates = EditorGUILayout.Foldout(ShowCoordinates, " Coordinates");
            EditorGUILayout.EndVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent(""), GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.Height(10));
            GUILayout.EndHorizontal();




            if (ShowCoordinates)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent("Zone Name ", " Give a name to the coordiantes bound "), GUILayout.MaxWidth(200));
                ZoneName = EditorGUILayout.TextField("", ZoneName);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(""), GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.Height(10));
                GUILayout.EndHorizontal();

                GUILayout.Label("Upper-Left : ", GUILayout.ExpandWidth(false));

                GUILayout.BeginHorizontal();

                GUILayout.Label("Latitude : ", GUILayout.ExpandWidth(false));
                UpperLeftCoordiante.y = EditorGUILayout.DoubleField(UpperLeftCoordiante.y, GUILayout.ExpandWidth(false));

                GUILayout.Label("Longitude : ", GUILayout.ExpandWidth(false));
                UpperLeftCoordiante.x = EditorGUILayout.DoubleField(UpperLeftCoordiante.x, GUILayout.ExpandWidth(false));

                GUILayout.EndHorizontal();


                GUILayout.Label("Down-Right : ", GUILayout.ExpandWidth(false));

                GUILayout.BeginHorizontal();

                GUILayout.Label("Latitude : ", GUILayout.ExpandWidth(false));
                DownRightCoordiante.y = EditorGUILayout.DoubleField(DownRightCoordiante.y, GUILayout.ExpandWidth(false));

                GUILayout.Label("Longitude : ", GUILayout.ExpandWidth(false));
                DownRightCoordiante.x = EditorGUILayout.DoubleField(DownRightCoordiante.x, GUILayout.ExpandWidth(false));

                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(""), GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.Height(3));
                GUILayout.EndHorizontal();

                if (GUILayout.Button(new GUIContent(" Open Map Helper  ", " Open an online map to easily select the bounding box ''Select CVS RAW Format'' ")))
                    OnOpenOnlineMapGUI();
                if (GUILayout.Button(new GUIContent(" Paste Coordinates ", " Insert the coordinates copied from the online map ")))
                    OnInsertCoordsGUI();
                if (GUILayout.Button(new GUIContent(" Zone Info + Optimized Input ", " Calculate the optimized inputs + Information about that zone ")))
                {
                    var optimizedCount = OnCalculateOptimizedInputersGUI(ZoomLevel,true);
                }
                

                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(""), GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.Height(10));
                GUILayout.EndHorizontal();

                if (GUILayout.Button(new GUIContent(" Save Zone ", " Save Zone Name + bounds ")))
                {
                     var SavePath = EditorUtility.SaveFilePanel("Save Zone to File", "","","zone");

                    if(!string.IsNullOrEmpty(SavePath))
                        OnSaveZoneGUI(SavePath);
                }

                if (GUILayout.Button(new GUIContent(" Load Zone ", " Load Saved Zone ")))
                {
                    var LoadPath = EditorUtility.OpenFilePanel("Load Zone from File", "", "zone");

                    if (!string.IsNullOrEmpty(LoadPath))
                        OnLoadZoneGUI(LoadPath);
                }

            }

            EditorGUILayout.EndVertical();
        }
        private void OnSaveZoneGUI(string Savepath)
        {
            var ZoneData = "";

            ZoneData += "ZoneName  = " + ZoneName +"\n";

            ZoneData += "UpperLeftCoordinate_x  = " + UpperLeftCoordiante.x.ToString() + "\n";
            ZoneData += "UpperLeftCoordinate_y  = " + UpperLeftCoordiante.y.ToString() + "\n";

            ZoneData += "BottomRightCoordiante_x  = " + DownRightCoordiante.x.ToString() + "\n";
            ZoneData += "BottomRightCoordiante_y  = " + DownRightCoordiante.y.ToString() + "\n";

            using (StreamWriter file = new StreamWriter(Savepath))
            {
                file.Write(ZoneData);
            }
        }
        private void OnLoadZoneGUI(string filepath)
        {
            StreamReader DataReader = new StreamReader(filepath);

            string hdrTemp = null;

            hdrTemp = DataReader.ReadLine();

            while (hdrTemp != null)
            {
                hdrTemp.Replace(" ", "");
                string[] lineTemp = hdrTemp.Split('=');

                switch (lineTemp[0].Trim())
                {
                    case "ZoneName":
                        ZoneName = lineTemp[1];
                        break;
                    case "UpperLeftCoordinate_x":
                        UpperLeftCoordiante.x = TerrainStreamingExtensions.ConvertToDouble(lineTemp[1]);
                        break;
                    case "UpperLeftCoordinate_y":
                        UpperLeftCoordiante.y = TerrainStreamingExtensions.ConvertToDouble(lineTemp[1]);
                        break;
                    case "BottomRightCoordiante_x":
                        DownRightCoordiante.x = TerrainStreamingExtensions.ConvertToDouble(lineTemp[1]);
                        break;
                    case "BottomRightCoordiante_y":
                        DownRightCoordiante.y = TerrainStreamingExtensions.ConvertToDouble(lineTemp[1]);
                        break;
                }

                hdrTemp = DataReader.ReadLine();
            }

            DataReader.Close();


        }
        private void OnInsertCoordsGUI()
        {
            try
            {
                string coor = TerrainStreamingClipboardHelper.Clipboard;
                var coors = coor.Split(',');

                float one_tenth = 1f / 10f;

                if (one_tenth.ToString(System.Globalization.CultureInfo.CurrentCulture).Contains('.'))
                {
                    UpperLeftCoordiante = new DVector2(double.Parse(coors[0]), double.Parse(coors[3]));
                    DownRightCoordiante = new DVector2(double.Parse(coors[2]), double.Parse(coors[1]));
                }
                else
                {
                    UpperLeftCoordiante = new DVector2(double.Parse(coors[0].Replace('.', ',')), double.Parse(coors[3].Replace('.', ',')));
                    DownRightCoordiante = new DVector2(double.Parse(coors[2].Replace('.', ',')), double.Parse(coors[1].Replace('.', ',')));
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(" Incorrect coordinates format .. !" + e);
            }
        }
        private void OnOpenOnlineMapGUI()
        {
            if (IsCorrectCoordinates())
                OnWriteCoordinates();
            System.Diagnostics.Process.Start(Application.dataPath + "/GIS Tech/Terrain Streaming System/MapHelper/GISTechMap.html");
        }
        private bool IsCorrectCoordinates()
        {
            bool correct = true;

            if (UpperLeftCoordiante.x >= DownRightCoordiante.x) correct = false;

            if (UpperLeftCoordiante.y <= DownRightCoordiante.y) correct = false;

            if (string.IsNullOrEmpty(UpperLeftCoordiante.x.ToString()) || string.IsNullOrEmpty(UpperLeftCoordiante.y.ToString())) correct = false;

            if (string.IsNullOrEmpty(DownRightCoordiante.x.ToString()) || string.IsNullOrEmpty(DownRightCoordiante.y.ToString())) correct = false;

            return correct;
        }
        public void OnWriteCoordinates()
        {
            var Savepath = Application.dataPath + "/GIS Tech/Terrain Streaming System/MapHelper/GISTechMap_Coords.jscript";

            var Coordinates = "var Coords = {";

            Coordinates += "UpperLeftCoordinate_x:" + UpperLeftCoordiante.x.ToString().Replace(",", ".") + ", ";
            Coordinates += "UpperLeftCoordinate_y:" + UpperLeftCoordiante.y.ToString().Replace(",", ".") + ", ";

            Coordinates += "BottomRightCoordiante_x:" + DownRightCoordiante.x.ToString().Replace(",", ".") + ", ";
            Coordinates += "BottomRightCoordiante_y:" + DownRightCoordiante.y.ToString().Replace(",", ".") + "};"; ;

            using (StreamWriter file = new StreamWriter(Savepath))
            {
                file.Write(Coordinates);
            }
        }
        private Vector2Int OnCalculateOptimizedInputersGUI(int m_ZoomLevel, bool DebugM=false)
        {
            var TL = UpperLeftCoordiante;
            var DR = DownRightCoordiante;

            var DL = new DVector2(TL.x,DR.y);
            var TR = new DVector2(DR.x, TL.y);

            int minpixelX, minpixelY, maxpixelX, maxpixelY;

            TerrainStreamingGeoConversion.LatLongToPixelXY(TL.y, TL.x, m_ZoomLevel, out minpixelX, out minpixelY);
            TerrainStreamingGeoConversion.LatLongToPixelXY(DR.y, DR.x, m_ZoomLevel, out maxpixelX, out maxpixelY);

            DVector2 Dimensions = new DVector2(TerrainStreamingGeoConversion.GetDistance(DL, DR, 'X'), TerrainStreamingGeoConversion.GetDistance(DL, TL, 'Y'));
 
            float Totalwidth = maxpixelX - minpixelX;
            float TotalHeight = maxpixelY - minpixelY;
 
            int CountX = (int)Totalwidth / 700;
            int CountY = (int)TotalHeight / 700;

            if (CountX == 1 && Totalwidth > 1500) CountX = 2;
            if (CountY == 1 && TotalHeight > 2500) CountY = 2;

            if (CountX == 0)
                CountX = 1;
            if (CountY == 0)
                CountY = 1;

            if (DebugM)
                Debug.Log("<color=magenta><size=15> Zone Dimensions [Km] : " + Math.Round(Dimensions.x, 2) + " X " + Math.Round(Dimensions.y, 2) + ", The Best Terrain Count is : " + CountX + "x" + CountY + " For Zoom = " + m_ZoomLevel + "</size></color>");

            return new Vector2Int(CountX, CountY);
         }
        private void GUIDownloadingOption()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.BeginVertical(GUI.skin.button);

            ShowDownloadOptions = EditorGUILayout.Foldout(ShowDownloadOptions, " Download Options");

            if (ShowDownloadOptions)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(""), GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.Height(10));
                GUILayout.EndHorizontal();

                if (GUILayout.Button(" Select Location ", GUI.skin.button))
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent(" Download Path ", " Select the location where data will be downloaded "), GUILayout.MaxWidth(200));
                    DownloadPath = EditorUtility.OpenFolderPanel("Streaming Location", "", "");
                    GUILayout.EndHorizontal();
                }

                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(" Terrain Path ", " Path where data will be downloaded "), GUILayout.MaxWidth(200));
                DownloadPath = EditorGUILayout.TextField("", DownloadPath);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(""), GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.Height(3));
                GUILayout.EndHorizontal();

                Color EnableDEMMode = Color.green;
                if (DEMMode == OptionEnabDisab.Enable)
                    EnableDEMMode = Color.green;
                else
                    EnableDEMMode = Color.red;

                GUI.backgroundColor = EnableDEMMode;
                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(" Download DEM Data ", " Use this option to enable/disable Elevation downloading "), GUILayout.MaxWidth(200));
                DEMMode = (OptionEnabDisab)EditorGUILayout.EnumPopup("", DEMMode);
                GUILayout.EndHorizontal();
                GUI.backgroundColor = Color.white;

                if (DEMMode == OptionEnabDisab.Enable)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent("  DEM Source ", " Select Elevation data source SRTM30-SRTM90 .."), GUILayout.MaxWidth(200));
                    DEMSource = (GISDataDownloaderDEMProvider)EditorGUILayout.EnumPopup("", DEMSource);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent("  Heightmap Resolution ", "The pixel resolution of the Terrain’s Heightmap that will be generated "), GUILayout.MaxWidth(200));
                    HeightmapResolutions_Selector = EditorGUILayout.Popup(HeightmapResolutions_Selector, HeightmapResolutionsStr);
                    HeightmapResolution = HeightmapResolutionsLst[HeightmapResolutions_Selector];
                    GUILayout.EndHorizontal();

                    if (HeightmapResolution >= 513)
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.HelpBox("' Attention ' using high terrain resolution may reduce loading performances ..  ", MessageType.Warning);
                        GUILayout.EndHorizontal();
                    }


                    if(DEMSource == GISDataDownloaderDEMProvider.SRTM_30m)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("  Username ", " Username for EarthData account "), GUILayout.MaxWidth(200));
                        User = EditorGUILayout.TextArea(User);
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("  Password ", " Password for EarthData account "), GUILayout.MaxWidth(200));
                        Pass = EditorGUILayout.TextArea(Pass);
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        EditorGUILayout.HelpBox(" To download SRTM30 data you should to create an account on 'Earthdata' ", MessageType.Info);
                        GUILayout.EndHorizontal();


                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button(new GUIContent("  Registre on Earthdata ", "Link for quick registre on 'Earthdata'")))
                            System.Diagnostics.Process.Start("https://urs.earthdata.nasa.gov/users/new");
                        GUILayout.EndHorizontal();
                    }


                    if (DEMSource == GISDataDownloaderDEMProvider.Mapbox)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("  MapBox Key ", " MapBox API Key for Mapbox account "), GUILayout.MaxWidth(200));
                        MapBoxKey = EditorGUILayout.TextArea(MapBoxKey, GUILayout.MaxWidth(350), GUILayout.ExpandWidth(false));
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button(new GUIContent("  Registre on MapBox ", "Link for quick registre on 'Mapbox' to get Key API")))
                            System.Diagnostics.Process.Start("https://docs.mapbox.com/help/glossary/access-token/");
                        GUILayout.EndHorizontal();
                    }
                }




                //////////////////////////// Raster ////////////////////////////////////////

                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(""), GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.Height(3));
                GUILayout.EndHorizontal();

                Color EnableTextureMode = Color.green;
                if (TextureMode == OptionEnabDisab.Enable)
                    EnableTextureMode = Color.green;
                else
                    EnableTextureMode = Color.red;

                GUI.backgroundColor = EnableTextureMode;
                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(" Download Raster Data ", " Use this option to enable/disable texture downloading "), GUILayout.MaxWidth(200));
                TextureMode = (OptionEnabDisab)EditorGUILayout.EnumPopup("", TextureMode);
                GUILayout.EndHorizontal();
                GUI.backgroundColor = Color.white;

                if (TextureMode == OptionEnabDisab.Enable)
                {
                    

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent("  Map Source ", " Select a server to download maps "), GUILayout.MaxWidth(200));
                    MapSource = (GISMapSource)EditorGUILayout.EnumPopup("", MapSource);
                    GUILayout.EndHorizontal();

                    if (MapSource == GISMapSource.ArcGIS)
                    {

                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("  Map Type ", "Select Raster (Textures) Map Type "), GUILayout.MaxWidth(200));
                        ArcGISType = (GISDataDownloaderArcGISType)EditorGUILayout.EnumPopup("", ArcGISType);
                        GUILayout.EndHorizontal();
  
                    }
                    if (MapSource == GISMapSource.Mapbox)
                    {
 
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("  Map Type ", "Select Raster (Textures) Map Type "), GUILayout.MaxWidth(200));
                        MapBoxType = (GISDataDownloaderMapboxType)EditorGUILayout.EnumPopup("", MapBoxType);
                        GUILayout.EndHorizontal();


                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("  Show MapBox Logo ", " MapBox logo is required according to the terms of service,Disable it may violate the MapBox terms of service : Take a look On https://docs.mapbox.com/help/glossary/attribution/"), GUILayout.MaxWidth(200));
                        ShowLogo = (OptionEnabDisab)EditorGUILayout.EnumPopup("", ShowLogo);
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("  Show Attribution ", " MapBox Attribution is required according to the terms of service,Disable it may violate the MapBox terms of service : Take a look On https://docs.mapbox.com/help/glossary/attribution/"), GUILayout.MaxWidth(200));
                        ShowAttribution = (OptionEnabDisab)EditorGUILayout.EnumPopup("", ShowAttribution);
                        GUILayout.EndHorizontal();


                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("  MapBox Key ", " MapBox API Key for Mapbox account "), GUILayout.MaxWidth(200));
                        MapBoxKey = EditorGUILayout.TextArea(MapBoxKey, GUILayout.MaxWidth(350), GUILayout.ExpandWidth(false));
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button(new GUIContent("  Registre on MapBox ", "Link for quick registre on 'Mapbox' to get Key API")))
                            System.Diagnostics.Process.Start("https://docs.mapbox.com/help/glossary/access-token/");
                        GUILayout.EndHorizontal();



                     
                    }

                    if(MapSource == GISMapSource.Bingmaps)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("  Map Type ", "Select Raster (Textures) Map Type "), GUILayout.MaxWidth(200));
                        BingmapType = (GISDataDownloaderBingMapType)EditorGUILayout.EnumPopup("", BingmapType);
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button(new GUIContent("  Registre on BingMaps ", "Link for quick registre on 'Bingmaps' to get Key API")))
                            System.Diagnostics.Process.Start("https://docs.microsoft.com/en-us/bingmaps/getting-started/bing-maps-dev-center-help/creating-a-bing-maps-account");
                        GUILayout.EndHorizontal();


                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("  BingKey ", " Bing API Key for Bingmaps account "), GUILayout.MaxWidth(200));
                        BingKey = EditorGUILayout.TextArea(BingKey, GUILayout.MaxWidth(350), GUILayout.ExpandWidth(false));
                        GUILayout.EndHorizontal();
                    }


                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent("  Zoom Level ", " Raster zoom level used for terrains"), GUILayout.MaxWidth(200));
                    ZoomLevel_Selector = EditorGUILayout.Popup(ZoomLevel_Selector, ZoomLevelStr);
                    ZoomLevel = ZoomLevels[ZoomLevel_Selector];
                    GUILayout.EndHorizontal();
 
                    ShowRasterDownloadOptions = EditorGUILayout.Foldout(ShowRasterDownloadOptions, "More Options");

                    if (ShowRasterDownloadOptions)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("      Replace exsiting files ", " (Avoid overwriting) Disable this option to avoid downloading some files that already downloaded via the pervious operation "), GUILayout.MaxWidth(200));
                        ReplaceExisitingRaster = (OptionEnabDisab)EditorGUILayout.EnumPopup("", ReplaceExisitingRaster);
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("      Generate Tab Files ", " Enable this option to generate geo-referenced raster files "), GUILayout.MaxWidth(200));
                        GenerateTabFiles = (OptionEnabDisab)EditorGUILayout.EnumPopup("", GenerateTabFiles);
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("      Stop download On Error ", " Enable this to stop downloading while encounter any failed url "), GUILayout.MaxWidth(200));
                        QuitOnRasterError = (OptionEnabDisab)EditorGUILayout.EnumPopup("", QuitOnRasterError);
                        GUILayout.EndHorizontal();


                    }
                }

//////////////////////////// Vector ////////////////////////////////////////

                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(""), GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.Height(3));
                GUILayout.EndHorizontal();

                Color EnableVectorMode = Color.green;
                if (VectorMode == OptionEnabDisab.Enable)
                    EnableVectorMode = Color.green;
                else
                    EnableVectorMode = Color.red;

                GUI.backgroundColor = EnableVectorMode;
                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(" Download Vector Data ", " Use this option to enable/disable downloading OSM vector  "), GUILayout.MaxWidth(200));
                VectorMode = (OptionEnabDisab)EditorGUILayout.EnumPopup("", VectorMode);
                GUILayout.EndHorizontal();
                GUI.backgroundColor = Color.white;

                if (VectorMode == OptionEnabDisab.Enable)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent("  Vector Source ", "Select Vector data source "), GUILayout.MaxWidth(200));
                    VectorSource = (GISDataDownloaderVectorProvider)EditorGUILayout.EnumPopup("", VectorSource);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent("  Download Tree ", " Use this option to enable/disable downloading Tree Vector Data  "), GUILayout.MaxWidth(200));
                    DownloadTree = (OptionEnabDisab)EditorGUILayout.EnumPopup("", DownloadTree);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent("  Download Grass ", " Use this option to enable/disable downloading Grass Vector Data  "), GUILayout.MaxWidth(200));
                    DownloadGrass = (OptionEnabDisab)EditorGUILayout.EnumPopup("", DownloadGrass);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent("  Download Roads ", " Use this option to enable/disable downloading Roads Vector Data  "), GUILayout.MaxWidth(200));
                    DownloadRoads = (OptionEnabDisab)EditorGUILayout.EnumPopup("", DownloadRoads);
                    GUILayout.EndHorizontal();

                    //GUILayout.BeginHorizontal();
                    //GUILayout.Label(new GUIContent(" Download Buildings ", " Use this option to enable/disable downloading Buildings Vector Data  "), GUILayout.MaxWidth(200));
                    //DownloadBuildings = (OptionEnabDisab)EditorGUILayout.EnumPopup("", DownloadBuildings);
                    //GUILayout.EndHorizontal();
                    ShowVectorDownloadOptions = EditorGUILayout.Foldout(ShowVectorDownloadOptions, "More Options");

                    if (ShowVectorDownloadOptions)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("      Replace Downloaded Files ", " (Avoid overwriting) Disable this option to avoid overwriting and re-downloading vector files "), GUILayout.MaxWidth(200));
                        ReplaceDownloadedVector = (OptionEnabDisab)EditorGUILayout.EnumPopup("", ReplaceDownloadedVector);
                        GUILayout.EndHorizontal();

                    }
                    if (ShowVectorDownloadOptions)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("      Replace Generated Tiles ", " (Avoid overwriting) Disable this option to avoid overwriting and re-generating vector tiles "), GUILayout.MaxWidth(200));
                        ReplaceGeneratedVectorTiles = (OptionEnabDisab)EditorGUILayout.EnumPopup("", ReplaceGeneratedVectorTiles);
                        GUILayout.EndHorizontal();

                    }



                }

                //////////////////////////////////////////////////////////////////////////



                //////////////////////////// Horizon ////////////////////////////////////////

                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(""), GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.Height(3));
                GUILayout.EndHorizontal();

                Color EnableHorizonMode = Color.green;
                if (HorizonMode == OptionEnabDisab.Enable)
                    EnableHorizonMode = Color.green;
                else
                    EnableHorizonMode = Color.red;

                GUI.backgroundColor = EnableHorizonMode;
                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(" Download Terrain Horizon ", " Use this option to enable/disable downloading Far (Horizon) Terrain Data"), GUILayout.MaxWidth(200));
                HorizonMode = (OptionEnabDisab)EditorGUILayout.EnumPopup("", HorizonMode);
                GUILayout.EndHorizontal();
                GUI.backgroundColor = Color.white;

                if (HorizonMode == OptionEnabDisab.Enable)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent("  Horizon Zoom Level ", "Zoom level For Hoizon Data"), GUILayout.MaxWidth(200));
                    Hr_ZoomLevel_Selector = EditorGUILayout.Popup(Hr_ZoomLevel_Selector, Hr_ZoomLevelStr);
                    Hr_ZoomLevel = Hr_ZoomLevels[Hr_ZoomLevel_Selector];
                    GUILayout.EndHorizontal();


                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent("  Download DEM Data ", " Use this option to enable/disable Horizon DEM's downloading "), GUILayout.MaxWidth(200));
                    Hr_DEMMode = (OptionEnabDisab)EditorGUILayout.EnumPopup("", Hr_DEMMode);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent("  Download Raster Data ", " Use this option to enable/disable Horizon texture downloading "), GUILayout.MaxWidth(200));
                    Hr_TextureMode = (OptionEnabDisab)EditorGUILayout.EnumPopup("", Hr_TextureMode);
                    GUILayout.EndHorizontal();


 
                }

                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(""), GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.Height(3));
                GUILayout.EndHorizontal();
 
                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(" Grid Mode ", " Use this option to define the number of the world terrain tiles "), GUILayout.MaxWidth(200));
                GridMode = (TerrainStreamingTerrainGrid)EditorGUILayout.EnumPopup("", GridMode);
                GUILayout.EndHorizontal();
 
                if (GridMode == TerrainStreamingTerrainGrid.Custom)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent(" World Tiles ", " Specifie the number of the world terrain tiles , ' Attention '  All  downloaded data (DEM-Raster.. ect) will be divided according to the streaming Grid Int Vector "), GUILayout.MaxWidth(200));
                    StreamingGridCount = EditorGUILayout.Vector2IntField("", StreamingGridCount);
                    GUILayout.EndHorizontal();

                } else
                    if (GridMode == TerrainStreamingTerrainGrid.Standard)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent(" World Terrain Grid ", " All  downloaded data (DEM-Raster.. ect) will be divided according to the streaming Grid Int Vector "), GUILayout.MaxWidth(200));
                    StandardGrid_Selector = EditorGUILayout.Popup(StandardGrid_Selector, StandardGridStr);
                    StreamingGridCount = StandardGrid[StandardGrid_Selector];
                    GUILayout.EndHorizontal();
                }
 
                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(""), GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.Height(3));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(" Generate Preview Raster ", "Enable This Option to Generate Geo-Referenced Raster for your zone / Used to preview that zone in GIS applications "), GUILayout.MaxWidth(200));
                RasterAreaPreview = (OptionEnabDisab)EditorGUILayout.EnumPopup("", RasterAreaPreview);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(" Generate World Preview ", "Enable This Option to Generate and preview the world sectors "), GUILayout.MaxWidth(200));
                WorldPreview = (OptionEnabDisab)EditorGUILayout.EnumPopup("", WorldPreview);
                GUILayout.EndHorizontal();
 
                if (WorldPreview == OptionEnabDisab.Enable)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent(" World Scale ", " Specifies the terrain scale factor in three directions (if terrain is large with 1 value you can set small float value like 0.5f - 0.1f - 0.01f"), GUILayout.MaxWidth(200));
                    WorldScale = EditorGUILayout.Vector3Field("", WorldScale);
                    GUILayout.EndHorizontal();
                }

            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndVertical();

        }
        private void GUIGeneratingBtn()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);

    
            if (State == DownloaderState.Downloading)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(""), GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.Height(3));
                GUILayout.EndHorizontal();

                if (GUILayout.Button(new GUIContent(" Cancel ", "Click To Cancel downloading operation")))
                    OnCancel();
            }else
            if (State == DownloaderState.idle)
            {
                if (GUILayout.Button(new GUIContent(" Download Data ", "Click To Start Downloading Data")))
                {
                    Repaint();
                    STDownload();
                }
            }

            Rect rec = EditorGUILayout.BeginVertical();


            if (State == DownloaderState.Downloading)
            {
                GUILayout.Label("Progress :");
                EditorGUI.ProgressBar(rec, s_progress / 100, s_phase + " " + Mathf.FloorToInt(s_progress) + "%");
            }
            else
            {
                EditorUtility.ClearProgressBar();
                GUILayout.Space(38);
            }

            EditorGUILayout.EndVertical();


            EditorGUILayout.EndScrollView();
        }
        private void STDownload()
        {
            CheckForWorld();
        }
        private void CheckForWorld()
        {
            if (StreamingGridCount.x == 0 || StreamingGridCount.x < 1)
            {
                Debug.LogError("World tiles count must be at least (4x4) ....  ");
                OnError();
            }
            if (StreamingGridCount.y == 0 || StreamingGridCount.y < 1)
            {
                Debug.LogError("World tiles count must be at least (4x4)");
                OnError();
            }

            if (UpperLeftCoordiante.x >= DownRightCoordiante.x)
            {
                Debug.LogError("Bottom-Right Longitude must be greater than Top-Left Longitude");
                OnError();
            }
            if (UpperLeftCoordiante.y <= DownRightCoordiante.y)
            {
                Debug.LogError("Top-Left Latitude must be greater than Bottom-Right Latitude");
                OnError();
            }

            if (string.IsNullOrEmpty(ZoneName))
            {
                Debug.LogError("Set Zone Name");
                OnError();
            }

            if (WorldPreview == OptionEnabDisab.Disable)
                WorldScale = Vector3.one;

            if (MainOperation == MainOperation.DownloadAndGenerate)
            {
                if (!Directory.Exists(DownloadPath))
                    Directory.CreateDirectory(DownloadPath);

                State = DownloaderState.Downloading;
                Phases();

            }

        }
        public async void Phases()
        {           
            if (GridMode == TerrainStreamingTerrainGrid.Auto)
                StreamingGridCount = OnCalculateOptimizedInputersGUI(ZoomLevel);

            taskSource = new CancellationTokenSource();

            if (OnDownloadStarted != null)
                OnDownloadStarted();

            if (State == DownloaderState.Downloading)
            {
                MainZoneInf = new TerrainStreamingZoneInfo(this);

                SubZonesInfo = GetSubZoneInfo(StreamingGridCount, UpperLeftCoordiante, DownRightCoordiante);

                try
                {
                    if(MainOperation == MainOperation.DownloadAndGenerate)
                    {                         
                        if (DEMMode == OptionEnabDisab.Enable && State != DownloaderState.idle)
                        {
                            var DEMURLs = new List<RequestedFileData>();

                            if (DEMSource == GISDataDownloaderDEMProvider.Mapbox)
                                DEMURLs = MainZoneInf.GetDEMURLs_MapBox();

                            if (DEMSource == GISDataDownloaderDEMProvider.SRTM_90m)
                                DEMURLs = MainZoneInf.GetDEMURLs_SRTM90();

                            if (DEMSource == GISDataDownloaderDEMProvider.SRTM_30m)
                                DEMURLs = MainZoneInf.GetDEMURLs_SRTM30();

                            await MainZoneInf.StartDEMDownloading(DEMURLs,taskSource).CancelWith(taskSource.Token);
                        }


                        if (TextureMode == OptionEnabDisab.Enable && State != DownloaderState.idle)
                        {
                            //
                        }


                        if (VectorMode == OptionEnabDisab.Enable && State != DownloaderState.idle)
                        {
                            //
                        }
                    }

                    if (MainOperation == MainOperation.DownloadAndGenerate )
                    {
                        if (DEMMode == OptionEnabDisab.Enable)
                        {
                            if(State != DownloaderState.idle)
                            await MainZoneInf.ExtractDEMsZip(taskSource).CancelWith(taskSource.Token);
                            if (State != DownloaderState.idle)
                                await MainZoneInf.GenerateDEMTilesTasks(taskSource).CancelWith(taskSource.Token);
                        }
                        if (TextureMode == OptionEnabDisab.Enable && State != DownloaderState.idle)
                        {
                            var TotatlRasterURLs = await MainZoneInf.GetRasterURLs(SubZonesInfo, ZoomLevel, "/RasterData", taskSource).CancelWith(taskSource.Token); 
                            await MainZoneInf.DownloadRasterData(TotatlRasterURLs, ReplaceExisitingRaster, taskSource).CancelWith(taskSource.Token);

                        }
                        if (VectorMode == OptionEnabDisab.Enable)
                        {
                            var MainZonesInfo = GetSubZoneInfo(new Vector2Int(1, 1), UpperLeftCoordiante, DownRightCoordiante);

                            if (State != DownloaderState.idle)
                            {
                                var TotatlRasterURLs = MainZoneInf.GetVectorURLs(MainZonesInfo);
                                await MainZoneInf.DownloadVectorData(TotatlRasterURLs, taskSource).CancelWith(taskSource.Token);

                            }

                            if (State != DownloaderState.idle)
                            {
                                SubZonesInfo = GetSubZoneInfo(StreamingGridCount, UpperLeftCoordiante, DownRightCoordiante);
                                await MainZoneInf.GenerateVectorTiles(MainZonesInfo[0], SubZonesInfo, taskSource).CancelWith(taskSource.Token);
                            }
                        }
                      
                        if (HorizonMode == OptionEnabDisab.Enable)
                        {
                            GC.Collect();
                            GC.WaitForPendingFinalizers();

                            DEMSource = GISDataDownloaderDEMProvider.SRTM_90m;

                            MainZoneInf = new TerrainStreamingZoneInfo(this);
                        
                            Hr_StreamingGridCount = OnCalculateOptimizedInputersGUI(Hr_ZoomLevel, false);
                           
                            if (Hr_DEMMode == OptionEnabDisab.Enable)
                            {
                          
                                var DEMURLs = MainZoneInf.GetDEMURLs_SRTM90();
                                if (State != DownloaderState.idle)
                                    await MainZoneInf.StartDEMDownloading(DEMURLs, taskSource).CancelWith(taskSource.Token);
                                if (State != DownloaderState.idle)
                                    await MainZoneInf.ExtractDEMsZip(taskSource).CancelWith(taskSource.Token);
                                if (State != DownloaderState.idle)
                                {
                                    await MainZoneInf.GenerateHorizonTiles(taskSource).CancelWith(taskSource.Token);

                                }
                            }
                            if (Hr_TextureMode == OptionEnabDisab.Enable && State != DownloaderState.idle)
                            {
                                var Hr_SubZonesInfo = GetSubZoneInfo(Hr_StreamingGridCount, UpperLeftCoordiante, DownRightCoordiante);
                                var TotatlRasterURLs = await MainZoneInf.GetRasterURLs(Hr_SubZonesInfo, Hr_ZoomLevel, "/HorizonRasterData", taskSource).CancelWith(taskSource.Token);
                                await MainZoneInf.DownloadRasterData(TotatlRasterURLs, OptionEnabDisab.Enable, taskSource).CancelWith(taskSource.Token);
                            }

                        }


                        if (RasterAreaPreview == OptionEnabDisab.Enable && State != DownloaderState.idle)
                            await MainZoneInf.GeneratePreviewZone(taskSource).CancelWith(taskSource.Token);


                    }

                }
                catch (OperationCanceledException)
                {

                    taskSource.Cancel();
                    throw;
                }
                finally
                {
                    taskSource.Dispose();
                }

            }
            else
            {
                return;
            }

            Finish();

        }
        public void DownloadingProgress(string phasename, float value)
        {
            if (value > 0 && value < 100)
                Downloading = true;
            else
                Downloading = false;


            s_phase = phasename;
            s_progress = value;

        }
        public void DownloadingProgress(string phasename, int value)
        {
            if (value > 0 && value < 100)
                Downloading = true;
            else
                Downloading = false;


            s_phase = phasename;
            s_progress = value;

        }
        private void OnError()
        {

            State = DownloaderState.idle;

            Debug.LogError("Error Occured while Downloading operation...");

            s_phase = "";
            s_progress = 0;

            Repaint();

            if (taskSource != null)
                taskSource.Cancel();

            if (OnDownloadCancelled != null)
                OnDownloadCancelled();

        }
        private void OnCancel()
        {
            if (taskSource != null)
            {
                Debug.Log("Downloading operation Cancelled");

                State = DownloaderState.idle;
                taskSource.Cancel();

                s_phase = "";
                s_progress = 0;

                Repaint();

                if (OnDownloadCancelled != null)
                    OnDownloadCancelled();


            }

        }
        private void Finish()
        {
            s_progress = 0;
            Downloading = false;
            SubZonesInfo = null;

            Debug.Log("<color=magenta><size=14>Downloading Complete </size></color>");
            State = DownloaderState.idle;

        }
        private List<TerrainStreamingTileData> GetTotalTiles()
        {
            List<TerrainStreamingTileData> m_GeneratedTiles = new List<TerrainStreamingTileData>();
 
            var AllSectors = new TerrainStreamingTileData[StreamingGridCount.x, StreamingGridCount.y];
            var LonStep = (DownRightCoordiante.x - UpperLeftCoordiante.x) / StreamingGridCount.x;
            var LatStep = (UpperLeftCoordiante.y - DownRightCoordiante.y) / StreamingGridCount.y;

            for (int x = 0; x < AllSectors.GetLength(0); x++)
            {
                for (int y = 0; y < AllSectors.GetLength(1); y++)
                {
                    var TerrainTileSector = new TerrainStreamingTileData(string.Format("Tile_{0}__{1}", y, x));
                    TerrainTileSector.Number = new Vector2Int(y, x);

                    AllSectors[y, x] = TerrainTileSector;
                }
            }

            for (int x = 0; x < AllSectors.GetLength(0); x++)
            {
                for (int y = 0; y < AllSectors.GetLength(1); y++)
                {
                    var tile = AllSectors[x, y];

                    tile.UpperLeftCoordinate = new DVector2(UpperLeftCoordiante.x + x * LonStep, UpperLeftCoordiante.y - y * LatStep);
                    tile.BottomRightCoordiante = new DVector2(tile.UpperLeftCoordinate.x + LonStep, tile.UpperLeftCoordinate.y - LatStep);

                    m_GeneratedTiles.Add(tile);
                }
            }
            return m_GeneratedTiles;
        }
        private List<TerrainStreamingTileData> GetSubZoneInfo(Vector2Int StreamingGridCount, DVector2 UpperLeftCoordiante, DVector2 DownRightCoordiante)
        {
            List<TerrainStreamingTileData> m_GeneratedTiles = new List<TerrainStreamingTileData>();

            var AllSectors = new TerrainStreamingTileData[StreamingGridCount.x, StreamingGridCount.y];

            var LonStep = (DownRightCoordiante.x - UpperLeftCoordiante.x) / StreamingGridCount.x;
            var LatStep = (UpperLeftCoordiante.y - DownRightCoordiante.y) / StreamingGridCount.y;
 
            for (int x = 0; x < StreamingGridCount.x; x++)
            {
                for (int y = 0; y < StreamingGridCount.y; y++)
                {
                    var TerrainTileSector = new TerrainStreamingTileData(string.Format("Tile_{0}__{1}", x, y));
                    TerrainTileSector.Number = new Vector2Int(x, y);
                    AllSectors[x, y] = TerrainTileSector;
                }
            }

            for (int x = 0; x < StreamingGridCount.x; x++)
            {
                for (int y = 0; y < StreamingGridCount.y; y++)
                {
                    var tile = AllSectors[x, y];

                    tile.UpperLeftCoordinate = new DVector2(UpperLeftCoordiante.x + x * LonStep, UpperLeftCoordiante.y - y * LatStep);
                    tile.BottomRightCoordiante = new DVector2(tile.UpperLeftCoordinate.x + LonStep, tile.UpperLeftCoordinate.y - LatStep);
                    tile.UpperLeftPointMercator = TerrainStreamingGeoConversion.LatLongToMercat(tile.UpperLeftCoordinate.x, tile.UpperLeftCoordinate.y);
                    tile.BottomRightPointMercator = TerrainStreamingGeoConversion.LatLongToMercat(tile.BottomRightCoordiante.x, tile.BottomRightCoordiante.y);

                    m_GeneratedTiles.Add(tile);
                }
            }
            return m_GeneratedTiles;
        }
        private void OnTilesGenerated(List<TerrainStreamingTileData> data)
        {
            SubZonesInfo = data;
        }

        private void ResetPrefs()
        {

        }
        private void SavePrefs()
        {
            
            TerrainStreamingSaveLoadPrefs.SavePref("MainOperation", (int)MainOperation);
            TerrainStreamingSaveLoadPrefs.SavePref("ShowCoordinates", ShowCoordinates);
            TerrainStreamingSaveLoadPrefs.SavePref("ShowDownloadOptions", ShowDownloadOptions);
            /////////////////////////////////////////////////////////////////////

            TerrainStreamingSaveLoadPrefs.SavePref("ZoneName", ZoneName);
            TerrainStreamingSaveLoadPrefs.SavePref("UpperLeftCoordiante", UpperLeftCoordiante);
            TerrainStreamingSaveLoadPrefs.SavePref("DownRightCoordiante", DownRightCoordiante);

            /////////////////////////////////////////////////////////////////////

            TerrainStreamingSaveLoadPrefs.SavePref("DownloadPath", DownloadPath);

            /////////////////////////////////////////////////////////////////////
            
            TerrainStreamingSaveLoadPrefs.SavePref("DEMMode", (int)DEMMode);
            TerrainStreamingSaveLoadPrefs.SavePref("DEMSource", (int)DEMSource);
            TerrainStreamingSaveLoadPrefs.SavePref("HeightmapResolutions_Selector", HeightmapResolutions_Selector);
            TerrainStreamingSaveLoadPrefs.SavePref("User", User);
            TerrainStreamingSaveLoadPrefs.SavePref("Pass", Pass);
            /////////////////////////////////////////////////////////////////////

            TerrainStreamingSaveLoadPrefs.SavePref("TextureMode", (int)TextureMode);
            TerrainStreamingSaveLoadPrefs.SavePref("MapSource", (int)MapSource);

            TerrainStreamingSaveLoadPrefs.SavePref("ArcGISType", (int)ArcGISType);
            TerrainStreamingSaveLoadPrefs.SavePref("MapBoxType", (int)MapBoxType);
            TerrainStreamingSaveLoadPrefs.SavePref("MapBoxKey", MapBoxKey);

            TerrainStreamingSaveLoadPrefs.SavePref("BingmapType", (int)BingmapType);
            TerrainStreamingSaveLoadPrefs.SavePref("BingKey", BingKey);
            TerrainStreamingSaveLoadPrefs.SavePref("ZoomLevel_Selector", ZoomLevel_Selector);

            TerrainStreamingSaveLoadPrefs.SavePref("ShowRasterDownloadOptions", ShowRasterDownloadOptions);

            TerrainStreamingSaveLoadPrefs.SavePref("ReplaceExisitingRaster", (int)ReplaceExisitingRaster);
            TerrainStreamingSaveLoadPrefs.SavePref("GenerateTabFiles", (int)GenerateTabFiles);
            TerrainStreamingSaveLoadPrefs.SavePref("QuitOnRasterError", (int)QuitOnRasterError);



            /////////////////////////////////////////////////////////////////////

            TerrainStreamingSaveLoadPrefs.SavePref("VectorMode", (int)VectorMode);
            TerrainStreamingSaveLoadPrefs.SavePref("VectorSource", (int)VectorSource);
            TerrainStreamingSaveLoadPrefs.SavePref("ReplaceDownloadedVector", (int)ReplaceDownloadedVector);
            TerrainStreamingSaveLoadPrefs.SavePref("ReplaceGeneratedVectorTiles", (int)ReplaceGeneratedVectorTiles);
            TerrainStreamingSaveLoadPrefs.SavePref("ShowVectorDownloadOptions", ShowVectorDownloadOptions);


            TerrainStreamingSaveLoadPrefs.SavePref("DownloadRoads", (int)DownloadRoads);
            TerrainStreamingSaveLoadPrefs.SavePref("DownloadBuildings", (int)DownloadBuildings);
            TerrainStreamingSaveLoadPrefs.SavePref("DownloadGrass", (int)DownloadGrass);
            TerrainStreamingSaveLoadPrefs.SavePref("DownloadTree", (int)DownloadTree);

            /////////////////////////////////////////////////////////////////////
            TerrainStreamingSaveLoadPrefs.SavePref("HorizonMode", (int)HorizonMode);
            TerrainStreamingSaveLoadPrefs.SavePref("Hr_DEMMode", (int)Hr_DEMMode);
            TerrainStreamingSaveLoadPrefs.SavePref("Hr_TextureMode", (int)Hr_TextureMode);
            TerrainStreamingSaveLoadPrefs.SavePref("Hr_ZoomLevel_Selector", Hr_ZoomLevel_Selector);

            /////////////////////////////////////////////////////////////////////
            TerrainStreamingSaveLoadPrefs.SavePref("GridMode", (int)GridMode);
            TerrainStreamingSaveLoadPrefs.SavePref("StandardGrid_Selector", StandardGrid_Selector);
            TerrainStreamingSaveLoadPrefs.SavePref("streamingGridCount", StreamingGridCount);
            TerrainStreamingSaveLoadPrefs.SavePref("streamingGridCount_x", StreamingGridCount.x);
            TerrainStreamingSaveLoadPrefs.SavePref("streamingGridCount_y", StreamingGridCount.y);

            /////////////////////////////////////////////////////////////////////

            TerrainStreamingSaveLoadPrefs.SavePref("RasterAreaPreview", (int)RasterAreaPreview);
            TerrainStreamingSaveLoadPrefs.SavePref("WorldPreview", (int)WorldPreview);
            TerrainStreamingSaveLoadPrefs.SavePref("WorldScale", WorldScale);
            /////////////////////////////////////////////////////////////////////


        }
        private void LoadPrefs()
        {
            MainOperation = (MainOperation)TerrainStreamingSaveLoadPrefs.LoadPref("MainOperation", (int)MainOperation.DownloadAndGenerate);
            ShowCoordinates = TerrainStreamingSaveLoadPrefs.LoadPref("ShowCoordinates", false);
            ShowDownloadOptions = TerrainStreamingSaveLoadPrefs.LoadPref("ShowDownloadOptions", false);
            /////////////////////////////////////////////////////////////////////
            ZoneName = TerrainStreamingSaveLoadPrefs.LoadPref("ZoneName", "");
            UpperLeftCoordiante = TerrainStreamingSaveLoadPrefs.LoadPref("UpperLeftCoordiante", new DVector2(0, 0));
            DownRightCoordiante = TerrainStreamingSaveLoadPrefs.LoadPref("DownRightCoordiante", new DVector2(0, 0));
            /////////////////////////////////////////////////////////////////////
            DownloadPath = TerrainStreamingSaveLoadPrefs.LoadPref("DownloadPath", "");
            /////////////////////////////////////////////////////////////////////
            DEMSource = (GISDataDownloaderDEMProvider)TerrainStreamingSaveLoadPrefs.LoadPref("DEMSource", (int)GISDataDownloaderDEMProvider.SRTM_90m);
            HeightmapResolutions_Selector = TerrainStreamingSaveLoadPrefs.LoadPref("HeightmapResolutions_Selector", 0);
            HeightmapResolution = HeightmapResolutionsLst[HeightmapResolutions_Selector];
            User = TerrainStreamingSaveLoadPrefs.LoadPref("User", "");
            Pass = TerrainStreamingSaveLoadPrefs.LoadPref("Pass", "");
            /////////////////////////////////////////////////////////////////////
            DEMMode = (OptionEnabDisab)TerrainStreamingSaveLoadPrefs.LoadPref("DEMMode", (int)OptionEnabDisab.Enable);
            TextureMode = (OptionEnabDisab)TerrainStreamingSaveLoadPrefs.LoadPref("TextureMode", (int)OptionEnabDisab.Enable);
            MapSource = (GISMapSource)TerrainStreamingSaveLoadPrefs.LoadPref("MapSource", (int)GISMapSource.Mapbox);
 
            ArcGISType = (GISDataDownloaderArcGISType)TerrainStreamingSaveLoadPrefs.LoadPref("ArcGISType", (int)GISDataDownloaderArcGISType.Satellite);

            MapBoxType = (GISDataDownloaderMapboxType)TerrainStreamingSaveLoadPrefs.LoadPref("MapBoxType", (int)GISDataDownloaderMapboxType.Satellite);
            MapBoxKey = TerrainStreamingSaveLoadPrefs.LoadPref("MapBoxKey", "");

            BingmapType = (GISDataDownloaderBingMapType)TerrainStreamingSaveLoadPrefs.LoadPref("BingmapType", (int)GISDataDownloaderBingMapType.aerial);
            BingKey = TerrainStreamingSaveLoadPrefs.LoadPref("BingKey", "");
            ZoomLevel_Selector = TerrainStreamingSaveLoadPrefs.LoadPref("ZoomLevel_Selector", 9);
            ZoomLevel= ZoomLevels[ZoomLevel_Selector];

            ShowRasterDownloadOptions = TerrainStreamingSaveLoadPrefs.LoadPref("ShowRasterDownloadOptions", false);

            ReplaceExisitingRaster = (OptionEnabDisab)TerrainStreamingSaveLoadPrefs.LoadPref("ReplaceExisitingRaster", (int)OptionEnabDisab.Enable);
            GenerateTabFiles = (OptionEnabDisab)TerrainStreamingSaveLoadPrefs.LoadPref("GenerateTabFiles", (int)OptionEnabDisab.Enable);
            QuitOnRasterError = (OptionEnabDisab)TerrainStreamingSaveLoadPrefs.LoadPref("QuitOnRasterError", (int)OptionEnabDisab.Enable);

            /////////////////////////////////////////////////////////////////////

            VectorMode = (OptionEnabDisab)TerrainStreamingSaveLoadPrefs.LoadPref("VectorMode", (int)OptionEnabDisab.Disable);
            VectorSource = (GISDataDownloaderVectorProvider)TerrainStreamingSaveLoadPrefs.LoadPref("VectorSource", (int)GISDataDownloaderVectorProvider.OpenStreetMap);
            ReplaceDownloadedVector = (OptionEnabDisab)TerrainStreamingSaveLoadPrefs.LoadPref("ReplaceDownloadedVector", (int)OptionEnabDisab.Disable);
            ReplaceGeneratedVectorTiles = (OptionEnabDisab)TerrainStreamingSaveLoadPrefs.LoadPref("ReplaceGeneratedVectorTiles", (int)OptionEnabDisab.Disable);
            ShowVectorDownloadOptions = TerrainStreamingSaveLoadPrefs.LoadPref("ShowVectorDownloadOptions", false);
            DownloadTree = (OptionEnabDisab)TerrainStreamingSaveLoadPrefs.LoadPref("DownloadTree", (int)OptionEnabDisab.Disable);
            DownloadGrass= (OptionEnabDisab)TerrainStreamingSaveLoadPrefs.LoadPref("DownloadGrass", (int)OptionEnabDisab.Disable);
            DownloadBuildings = (OptionEnabDisab)TerrainStreamingSaveLoadPrefs.LoadPref("DownloadBuildings", (int)OptionEnabDisab.Disable);
            DownloadRoads = (OptionEnabDisab)TerrainStreamingSaveLoadPrefs.LoadPref("DownloadRoads", (int)OptionEnabDisab.Disable);

            /////////////////////////////////////////////////////////////////////

            HorizonMode = (OptionEnabDisab)TerrainStreamingSaveLoadPrefs.LoadPref("HorizonMode", (int)OptionEnabDisab.Disable);

            Hr_ZoomLevel_Selector = TerrainStreamingSaveLoadPrefs.LoadPref("Hr_ZoomLevel_Selector", 7);
            Hr_ZoomLevel = Hr_ZoomLevels[Hr_ZoomLevel_Selector];

            Hr_DEMMode = (OptionEnabDisab)TerrainStreamingSaveLoadPrefs.LoadPref("Hr_DEMMode", (int)OptionEnabDisab.Disable);
            Hr_TextureMode = (OptionEnabDisab)TerrainStreamingSaveLoadPrefs.LoadPref("Hr_TextureMode", (int)OptionEnabDisab.Disable);

            /////////////////////////////////////////////////////////////////////
            GridMode = (TerrainStreamingTerrainGrid)TerrainStreamingSaveLoadPrefs.LoadPref("GridMode", (int)TerrainStreamingTerrainGrid.Auto);
            StandardGrid_Selector = TerrainStreamingSaveLoadPrefs.LoadPref("StandardGrid_Selector", 2);
            if(GridMode == TerrainStreamingTerrainGrid.Standard)
            StreamingGridCount = TerrainStreamingSaveLoadPrefs.LoadPref("streamingGridCount", StandardGrid[StandardGrid_Selector]);

            if (GridMode == TerrainStreamingTerrainGrid.Custom)
            {
                StreamingGridCount.x = TerrainStreamingSaveLoadPrefs.LoadPref("streamingGridCount_x", 4);
                StreamingGridCount.y = TerrainStreamingSaveLoadPrefs.LoadPref("streamingGridCount_y", 4);
            }
   
            /////////////////////////////////////////////////////////////////////
            RasterAreaPreview = (OptionEnabDisab)TerrainStreamingSaveLoadPrefs.LoadPref("RasterAreaPreview", (int)OptionEnabDisab.Disable);
            WorldPreview = (OptionEnabDisab)TerrainStreamingSaveLoadPrefs.LoadPref("WorldPreview", (int)OptionEnabDisab.Disable);
            WorldScale = TerrainStreamingSaveLoadPrefs.LoadPref("WorldScale", new Vector3(1,1,1));

            /////////////////////////////////////////////////////////////////////

        }

    }

}