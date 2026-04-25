/*     Unity GIS Tech 2020-2025      */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

#if GISDataDownloader
using GISTech.GISDataDownloader;
#endif

namespace GISTech.GISTerrainLoader
{
    public class EditorTerrainGenerator : EditorWindow
    {
        public GISTerrainLoaderPrefs Prefs;

        public const string version = "1.9 Pro";

        #region TerrainGenerator

        public GISTerrainTile[,] terrains = new GISTerrainTile[0, 0];
        private GISTerrainContainer GeneratedContainer;

        private string LoadedFileExtension = "";

        public static float s_progress = 0f;
        public static string s_phase = "";

        private static bool ShowProjectionMode = true;
        private bool ShowTiffElevationSourceMode;
        private bool ShowCoordinates;
        private bool ShowSubRegion = false;
        private bool ShowMainTerrainFile = true;
        private bool ShowSetTerrainPref = true;
        private bool ShowTerrainPref = true;
        private bool ShowVectorDataPanel = true;
        private bool ShowSmoothingOpr = true;
        private bool ShowTexturePref = true;
        private bool ShowSplatmapsTerrainLayer = true;
        private bool ShowHelpSupport = true;
        private bool ShowRawParameters = false;

        private Texture2D m_terrain;
        private Texture2D m_downloaExamples;
        private Texture2D m_helpIcon;
        private Texture2D m_resetPrefs;
        private Texture2D m_aboutIcon;

        private Vector2 scrollPos = Vector2.zero;

        private GISTerrainLoaderElevationInfo ElevationInfo;
        private GUIStyle ToolbarSkin = new GUIStyle();

        private Vector2 m_terrainDimensions;
        private UnityEngine.Object m_DEMFile;
        public UnityEngine.Object DEMFile
        {
            get { return m_DEMFile; }
            set
            {
                if (m_DEMFile != value)
                {
                    m_DEMFile = value;
                    OnTerrainFileChanged(DEMFile);
                }
            }
        }

        private static GeneratorState State = GeneratorState.idle;
        private OptionEnabDisab SerializeHeightmap = OptionEnabDisab.Disable;


        private Stopwatch timer = new Stopwatch();



#if GISTechAdvancedVectorGenerator
        private bool ShowAdvancedVectorGenerator = false;
#endif

#if RDST
        private bool ShowRDSTPanel = true;
 #endif

        #endregion

        public static EditorTerrainGenerator window;

        [MenuItem("Tools/GIS Tech/GIS Terrain Loader/Editor GTL", false, 1)]
        static void Init()
        {
            Application.runInBackground = true;
            window = EditorWindow.GetWindow<EditorTerrainGenerator>(false, "GIS Terrain Loader Pro");

            window.ShowUtility();
            window.minSize = new Vector2(400, 500);
            window.Show();
        }

        void OnInspectorUpdate() { Repaint(); }
        void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            OnTerrainFileGUI();

            OnTerrainDimensionScaleGUI();

            OnTerrainPreferencesGUI();

            OnTexturePrefrencesGUI();

            OnTerrainSmoothignOperationGUI();

            OnTerrainVectorGenerationGUI();

            OnHelpToolbarGUI();

            GeneratingBtn();


            EditorGUILayout.EndVertical();
        }

        #region GUIElements
        private void OnTerrainFileGUI()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.BeginVertical(GUI.skin.button);
            GUILayout.Label(new GUIContent("GIS Terrain Loader Pro"), ToolbarSkin);
            EditorGUILayout.EndVertical();

            if (ShowMainTerrainFile)
            {

                GUILayout.BeginHorizontal();

                GUILayout.Label(new GUIContent(" DEM File ", m_terrain, " Importe Your GIS Data into 'Resources/GIS Terrains' Folder, then drag and drop Your DEM File to 'Terrain File Field' "), GUILayout.MaxWidth(200));
                DEMFile = EditorGUILayout.ObjectField(DEMFile, typeof(UnityEngine.Object), true, GUILayout.ExpandWidth(true));
                GUILayout.EndHorizontal();

#if GISTerrainStreamer
                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(" Terrain Loader ", "Loader Type (GIS Terrain Loader or Gdal ...)"), GUILayout.MaxWidth(200));
                Prefs.terrainLoader = (TerrainLoader)EditorGUILayout.EnumPopup("", Prefs.terrainLoader);
                GUILayout.EndHorizontal();
#endif



                EditorGUILayout.HelpBox(" Import Your GIS Data into 'Resources/GIS Terrains' Folder, then drag and drop Your DEM File to 'Terrain File Field', " + "Edit Terrain parameters and click on Generate terrain", MessageType.Info);

                if (ShowSubRegion)
                {


                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent(" File Reading Mode ", "Default is full heightmap mode, used to read whole hightmap file; sub region mode used to import a sub region of the file instead, note that coordinates of sub regions is needed; this option available only for GeoRefenced files (Tiff,HGT,BIL,ASC,FLT) "), GUILayout.MaxWidth(200));
                    Prefs.readingMode = (ReadingMode)EditorGUILayout.EnumPopup("", Prefs.readingMode);
                    GUILayout.EndHorizontal();

                    if (Prefs.readingMode == ReadingMode.SubRegion)
                    {
                        CoordinatesBarGUI();
                    }

                    if (ShowProjectionMode)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent(" Projection Mode ", " Use this option to customize the projection of tiff file by setting EPSG code, if you dont know the EPSG set it to autodetection, note that DotSpatial lib is required for auto and cutome epsg"), GUILayout.MaxWidth(200));
                        Prefs.projectionMode = (ProjectionMode)EditorGUILayout.EnumPopup("", Prefs.projectionMode);
                        GUILayout.EndHorizontal();

                        if (Prefs.projectionMode == ProjectionMode.Custom_EPSG)
                        {
#if DotSpatial
                            GUILayout.BeginHorizontal();
                            GUILayout.Label(new GUIContent("  EPSG Code ", " Set the Projection Code"), GUILayout.MaxWidth(200));
                            Prefs.EPSGCode = EditorGUILayout.IntField("", Prefs.EPSGCode);
                            GUILayout.EndHorizontal();
#else
                            EditorGUILayout.HelpBox(" DotSpatial not instaled,From the toolbar menu click on 'Tools/GIS Tech/GIS Terrains/intall libs/Dotspatial/import_dotspatial'", MessageType.Info);
#endif
                        }
                        else if (Prefs.projectionMode == ProjectionMode.Geographic)
                        {
                            Prefs.EPSGCode = 0;

                        }
                        else
                           if (Prefs.projectionMode == ProjectionMode.AutoDetection)
                        {
#if !DotSpatial
                            EditorGUILayout.HelpBox("EPSG Detection may not work correctly, try to instal DotSpatial, From the toolbar menu click on 'Tools/GIS Tech/GIS Terrains/intall libs/Dotspatial/import_dotspatial'", MessageType.Info);
#endif
                        }
                    }


                    //DEM's Particularity
                    //Tiff
                    if (ShowTiffElevationSourceMode)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent(" Tiff Elevation Source ", " If you are using Tiff based on grayscale use this option to load heightmap data from grayscale color"), GUILayout.MaxWidth(200));
                        Prefs.tiffElevationSource = (TiffElevationSource)EditorGUILayout.EnumPopup("", Prefs.tiffElevationSource);
                        GUILayout.EndHorizontal();
                    }

                    if (ShowTiffElevationSourceMode && Prefs.tiffElevationSource == TiffElevationSource.BandsData)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("  Band Index ", " In which bands the elevation data are located"), GUILayout.MaxWidth(200));
                        Prefs.BandsIndex = EditorGUILayout.IntField("", Prefs.BandsIndex);
                        GUILayout.EndHorizontal();
                    }
                    else
                        Prefs.BandsIndex = 0;


                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent(" Serialize Heightmap ", " Enable This Option to Store Terrain Hightmap data into 'Resource/HeightmapData' Folder, Why? to read elevation data and Get-Set Coordinates in edit mode For more check 'Get_Set_Position_EditMode' Demo Scene"), GUILayout.MaxWidth(200));
                    SerializeHeightmap = (OptionEnabDisab)EditorGUILayout.EnumPopup("", SerializeHeightmap);
                    GUILayout.EndHorizontal();
                }


            }

            //DEM's Particularity
            //Raw
            if (ShowRawParameters)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(" Depth ", " Depth of raw file 8/16 bit"), GUILayout.MaxWidth(200));
                Prefs.Raw_Depth = (RawDepth)EditorGUILayout.EnumPopup("", Prefs.Raw_Depth);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(" Byts Order ", " Raw Byts Order Mac/Windows"), GUILayout.MaxWidth(200));
                Prefs.Raw_ByteOrder = (RawByteOrder)EditorGUILayout.EnumPopup("", Prefs.Raw_ByteOrder);
                GUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }
        private void CoordinatesBarGUI()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.BeginVertical(GUI.skin.button);
            ShowCoordinates = EditorGUILayout.Foldout(ShowCoordinates, "Sub Region Coordinates");
            EditorGUILayout.EndVertical();


            if (ShowCoordinates)
            {
                EditorGUILayout.HelpBox(" Set Sub Region Heightmap coordinates ", MessageType.Info);

                GUILayout.Label("Upper-Left : ", GUILayout.ExpandWidth(false));

                GUILayout.BeginHorizontal();

                GUILayout.Label("Latitude : ", GUILayout.MaxWidth(100), GUILayout.ExpandWidth(false));
                GUI.SetNextControlName("UpperLeftCoordianteLat");
                Prefs.SubRegionUpperLeftCoordiante.y = EditorGUILayout.DoubleField(Prefs.SubRegionUpperLeftCoordiante.y, GUILayout.ExpandWidth(true));

                GUILayout.Label("Longitude : ", GUILayout.MaxWidth(100), GUILayout.ExpandWidth(false));
                GUI.SetNextControlName("UpperLeftCoordianteLon");
                Prefs.SubRegionUpperLeftCoordiante.x = EditorGUILayout.DoubleField(Prefs.SubRegionUpperLeftCoordiante.x, GUILayout.ExpandWidth(true));

                GUILayout.EndHorizontal();


                GUILayout.Label("Down-Right : ", GUILayout.ExpandWidth(false));

                GUILayout.BeginHorizontal();

                GUILayout.Label("Latitude : ", GUILayout.MaxWidth(100), GUILayout.ExpandWidth(false));
                GUI.SetNextControlName("DownRightCoordianteLat");
                Prefs.SubRegionDownRightCoordiante.y = EditorGUILayout.DoubleField(Prefs.SubRegionDownRightCoordiante.y, GUILayout.ExpandWidth(true));

                GUILayout.Label("Longitude : ", GUILayout.MaxWidth(100), GUILayout.ExpandWidth(false));
                GUI.SetNextControlName("DownRightCoordianteLon");
                Prefs.SubRegionDownRightCoordiante.x = EditorGUILayout.DoubleField(Prefs.SubRegionDownRightCoordiante.x, GUILayout.ExpandWidth(true));
                GUILayout.EndHorizontal();

                GUILayout.Label("", GUILayout.ExpandWidth(false));

            }

            EditorGUILayout.EndVertical();
        }
        private void OnTerrainDimensionScaleGUI()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);

            GUI.backgroundColor = Color.green;
            EditorGUILayout.BeginVertical(GUI.skin.button);
            ShowSetTerrainPref = EditorGUILayout.Foldout(ShowSetTerrainPref, " Elevation Mode, Scale, Dimensions ");
            EditorGUILayout.EndVertical();
            GUI.backgroundColor = Color.white;

            if (ShowSetTerrainPref)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(" Terrain Elevation Mode ", "Generate Terrain By Loading Real Elevation Data or By using 'Exaggeration' value to set manualy terrain elevation factor"), GUILayout.MaxWidth(200));
                Prefs.TerrainElevation = (TerrainElevation)EditorGUILayout.EnumPopup("", Prefs.TerrainElevation);
                GUILayout.EndHorizontal();

                if (Prefs.TerrainElevation == TerrainElevation.ExaggerationTerrain)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent(" Exaggeration value ", "Vertical exaggeration can be used to emphasize subtle changes in a surface. This can be useful in creating visualizations of terrain where the horizontal extent of the surface is significantly greater than the amount of vertical change in the surface. A fractional vertical exaggeration can be used to flatten surfaces or features that have extreme vertical variation"), GUILayout.MaxWidth(200));
                    Prefs.TerrainExaggeration = EditorGUILayout.Slider(Prefs.TerrainExaggeration, 0, 1);
                    GUILayout.EndHorizontal();
                }



                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(" Terrain Dimensions Mode ", "This Option let us to load real terrain Width/Length for almost of supported types - We can set it to manual to make terrain small or large as we want by setting new W/L values in 'KM' "), GUILayout.MaxWidth(200));
                Prefs.terrainDimensionMode = (TerrainDimensionsMode)EditorGUILayout.EnumPopup("", Prefs.terrainDimensionMode);
                GUILayout.EndHorizontal();

                if (Prefs.terrainDimensionMode == TerrainDimensionsMode.AutoDetection)
                {

                    if (!Prefs.TerrainHasDimensions)
                    {
                        GUILayout.BeginHorizontal();

                        GUILayout.Label(new GUIContent(" Set Terrain Dimensions [Km] ", "This appear when DEM file not loaded yet or has no real dimensions so we have to set Manualy terrain width/length in KM"), GUILayout.MaxWidth(200));

                        GUILayout.Label(" Width ");
                        Prefs.TerrainDimensions.x = EditorGUILayout.DoubleField(Prefs.TerrainDimensions.x, GUILayout.ExpandWidth(true));

                        GUILayout.Label(" length ");
                        Prefs.TerrainDimensions.y = EditorGUILayout.DoubleField(Prefs.TerrainDimensions.y, GUILayout.ExpandWidth(true));
                        GUILayout.EndHorizontal();

                        if (Prefs.TerrainDimensions.x == 0 || Prefs.TerrainDimensions.y == 0)
                            EditorGUILayout.HelpBox("Can not Detect Terrain bounds,You have to set terrain dimensions in Km", MessageType.Warning);
                    }
                }
                else
                if (Prefs.terrainDimensionMode == TerrainDimensionsMode.Manual)
                {
                    GUILayout.BeginHorizontal();

                    GUILayout.Label(new GUIContent(" Set Terrain Dimensions [Km] ", "Set Manually terrain width/length in KM"), GUILayout.MaxWidth(200));

                    GUILayout.Label(" Width ");
                    Prefs.TerrainDimensions.x = EditorGUILayout.DoubleField(Prefs.TerrainDimensions.x, GUILayout.ExpandWidth(true));

                    GUILayout.Label(" Lenght ");
                    Prefs.TerrainDimensions.y = EditorGUILayout.DoubleField(Prefs.TerrainDimensions.y, GUILayout.ExpandWidth(true));

                    GUILayout.EndHorizontal();


                    if (Prefs.TerrainDimensions.x == 0 || Prefs.TerrainDimensions.y == 0)
                        EditorGUILayout.HelpBox("Can not Detect Terrain bounds,You have to set terrain dimensions in Km", MessageType.Warning);
                }

                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(" UnderWater ", "Enable This Option to load negative values from DEM files "), GUILayout.MaxWidth(200));
                Prefs.UnderWater = (OptionEnabDisab)EditorGUILayout.EnumPopup("", Prefs.UnderWater);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(" Fix Terrain ", " (Only for Real World Data) Use this option to fix terrain Min/Max detecion to used in avoid extrem elevation values in order to generate terrain without any deformation Manually OR Automa "), GUILayout.MaxWidth(200));
                Prefs.TerrainFixOption = (FixOption)EditorGUILayout.EnumPopup("", Prefs.TerrainFixOption);
                GUILayout.EndHorizontal();

                if (Prefs.TerrainFixOption != FixOption.Disable)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent(" Elevation For Null Points ", " (Only for Real World Data) average will set an average elevation value to null points, but manual will give the ability to set manually the elevation value "), GUILayout.MaxWidth(200));
                    Prefs.ElevationForNullPoints = (EmptyPoints)EditorGUILayout.EnumPopup("", Prefs.ElevationForNullPoints);
                    GUILayout.EndHorizontal();

                    if (Prefs.ElevationForNullPoints == EmptyPoints.Manual)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent(" Elevation Value For Null Points ", "Set Manually the Elevation Value For for each null Points in the DEM File"), GUILayout.MaxWidth(200));
                        Prefs.ElevationValueForNullPoints = EditorGUILayout.FloatField(Prefs.ElevationValueForNullPoints);
                        GUILayout.EndHorizontal();
                    }
                }

                if (Prefs.TerrainFixOption == FixOption.ManualFix)
                {
                    GUILayout.BeginHorizontal();

                    GUILayout.Label(new GUIContent(" Elevation [m] ", "Set Manually terrain Max and Min Elevation in [m]"), GUILayout.MaxWidth(200));
                    GUILayout.Label("  Min ");
                    Prefs.TerrainMaxMinElevation.x = EditorGUILayout.FloatField(Prefs.TerrainMaxMinElevation.x, GUILayout.ExpandWidth(true));

                    GUILayout.Label("  Max ");
                    Prefs.TerrainMaxMinElevation.y = EditorGUILayout.FloatField(Prefs.TerrainMaxMinElevation.y, GUILayout.ExpandWidth(true));
                    GUILayout.EndHorizontal();

                    if (Prefs.TerrainDimensions.x == 0 || Prefs.TerrainDimensions.y == 0)
                        EditorGUILayout.HelpBox("Can not Detect Terrain bounds,You have to set terrain dimensions in Km", MessageType.Warning);

                }

                GUILayout.BeginHorizontal();

                GUILayout.Label(new GUIContent(" Terrain Scale ", " Specifies the terrain scale factor in three directions (if terrain is large with 1 value you can set small float value like 0.5f - 0.1f - 0.01f"), GUILayout.MaxWidth(200));
                Prefs.terrainScale = EditorGUILayout.Vector3Field("", Prefs.terrainScale);
                GUILayout.EndHorizontal();

                if (Prefs.terrainScale.x == 0 || Prefs.terrainScale.y == 0 || Prefs.terrainScale.z == 0)
                    EditorGUILayout.HelpBox("Check your Terrain Scale (Terrain Scale is null !)", MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }
        private void OnTerrainPreferencesGUI()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUI.backgroundColor = Color.green;
            EditorGUILayout.BeginVertical(GUI.skin.button);
            ShowTerrainPref = EditorGUILayout.Foldout(ShowTerrainPref, " Terrain Preferences ");
            EditorGUILayout.EndVertical();
            GUI.backgroundColor = Color.white;

            if (ShowTerrainPref)
            {

                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(" Heightmap Resolution ", "The pixel resolution of the Terrain’s heightmap"), GUILayout.MaxWidth(200));
                Prefs.heightmapResolution_index = EditorGUILayout.Popup(Prefs.heightmapResolution_index, Prefs.heightmapResolutionsSrt, GUILayout.ExpandWidth(true));
                GUILayout.EndHorizontal();


                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(" Detail Resolution ", "The number of cells available for placing details onto the Terrain tile used to controls grass and detail meshes. Lower you set this number performance will be better"), GUILayout.MaxWidth(200));
                Prefs.detailResolution_index = EditorGUILayout.Popup(Prefs.detailResolution_index, Prefs.availableHeightSrt, GUILayout.ExpandWidth(true));
                GUILayout.EndHorizontal();


                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(" Resolution Per Patch ", "The number of cells in a single patch (mesh), recommended value is 16 for very large detail object distance "), GUILayout.MaxWidth(200));
                Prefs.resolutionPerPatch_index = EditorGUILayout.Popup(Prefs.resolutionPerPatch_index, Prefs.availableHeightsResolutionPrePectSrt, GUILayout.ExpandWidth(true));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(" Base Map Resolution ", "Resolution of the composite texture used on the terrain when viewed from a distance greater than the Basemap Distance"), GUILayout.MaxWidth(200));
                Prefs.baseMapResolution_index = EditorGUILayout.Popup(Prefs.baseMapResolution_index, Prefs.availableHeightSrt, GUILayout.ExpandWidth(true));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(" Pixel Error ", " The accuracy of the mapping between Terrain maps (such as heightmaps and Textures) and generated Terrain. Higher values indicate lower accuracy, but with lower rendering overhead. "), GUILayout.MaxWidth(200));
                Prefs.PixelError = EditorGUILayout.Slider(Prefs.PixelError, 1, 200, GUILayout.ExpandWidth(true));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(" BaseMap Distance ", " The maximum distance at which Unity displays Terrain Textures at full resolution. Beyond this distance, the system uses a lower resolution composite image for efficiency "), GUILayout.MaxWidth(200));
                Prefs.BaseMapDistance = EditorGUILayout.Slider(Prefs.BaseMapDistance, 1, 20000, GUILayout.ExpandWidth(true));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(" Terrain Material Mode ", "This option used to cutomize terrain material ex : in case of using HDRP "), GUILayout.MaxWidth(200));
                Prefs.terrainMaterialMode = (TerrainMaterialMode)EditorGUILayout.EnumPopup("", Prefs.terrainMaterialMode);
                GUILayout.EndHorizontal();



                if (Prefs.terrainMaterialMode == TerrainMaterialMode.HeightmapColorRampContourLines)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent(" Contour Interval [m] ", " Contour Interval in meter"), GUILayout.MaxWidth(200));
                    Prefs.ContourInterval = EditorGUILayout.Slider(Prefs.ContourInterval, 5, 200, GUILayout.ExpandWidth(true));
                    GUILayout.EndHorizontal();

                }

                if (Prefs.terrainMaterialMode == TerrainMaterialMode.Custom)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent("  Terrain Material ", "Materail that will be used in the generated terrains "), GUILayout.MaxWidth(200));
                    Prefs.terrainMaterial = (Material)EditorGUILayout.ObjectField(Prefs.terrainMaterial, typeof(UnityEngine.Material), true, GUILayout.ExpandWidth(true));
                    GUILayout.EndHorizontal();

                }


                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(" Terrain Baseboards ", "Enable This option to genertate a Terrain Baseboards "), GUILayout.MaxWidth(200));
                Prefs.TerrainBaseboards = (OptionEnabDisab)EditorGUILayout.EnumPopup("", Prefs.TerrainBaseboards);
                GUILayout.EndHorizontal();

                if (Prefs.TerrainBaseboards == OptionEnabDisab.Enable)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent("  Border High ", " Border High value according to the Terrain minimum elevation"), GUILayout.MaxWidth(200));
                    Prefs.BorderHigh = EditorGUILayout.IntSlider(Prefs.BorderHigh, -150, 0, GUILayout.MaxWidth(200), GUILayout.ExpandWidth(true));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent("  Terrain Border Material ", "Materail that will be used for the terrain border "), GUILayout.MaxWidth(200));
                    Prefs.terrainBorderMaterial = (Material)EditorGUILayout.ObjectField(Prefs.terrainBorderMaterial, typeof(UnityEngine.Material), true, GUILayout.ExpandWidth(true));
                    GUILayout.EndHorizontal();

                }

                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(" Terrain Background ", "Enable This option to genertate a Background to your Terrain"), GUILayout.MaxWidth(200));
                Prefs.TerrainBackground = (OptionEnabDisab)EditorGUILayout.EnumPopup("", Prefs.TerrainBackground);
                GUILayout.EndHorizontal();

                if (Prefs.TerrainBackground == OptionEnabDisab.Enable)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent("  Heightmap Resolution ", "The pixel resolution of the Terrain’s heightmap"), GUILayout.MaxWidth(200));
                    Prefs.TerrainBackgroundHeightmapResolution_index = EditorGUILayout.Popup(Prefs.TerrainBackgroundHeightmapResolution_index, Prefs.TerrainBackgroundHeightmapResolutionsSrt, GUILayout.ExpandWidth(true));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent("  Texture Resolution ", "The pixel resolution of the Terrain’s heightmap"), GUILayout.MaxWidth(200));
                    Prefs.TerrainBackgroundTextureResolution_index = EditorGUILayout.Popup(Prefs.TerrainBackgroundTextureResolution_index, Prefs.TerrainBackgroundTextureResolutionsSrt, GUILayout.ExpandWidth(true));
                    GUILayout.EndHorizontal();
                }


                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(" Set Terrain Layer ", "This option cutomize terrain Layer "), GUILayout.MaxWidth(200));
                Prefs.TerrainLayerSet = (OptionEnabDisab)EditorGUILayout.EnumPopup("", Prefs.TerrainLayerSet);
                GUILayout.EndHorizontal();

                if (Prefs.TerrainLayerSet == OptionEnabDisab.Enable)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent("  Terrain Layer ", "Set Terrain Layer"), GUILayout.MaxWidth(200));
                    Prefs.TerrainLayer_index = EditorGUILayout.Popup(Prefs.TerrainLayer_index, Prefs.TerrainLayerSrt, GUILayout.ExpandWidth(true));
                    GUILayout.EndHorizontal();

                }
            }

            EditorGUILayout.EndVertical();
        }
        private void OnFocus()
        {
            Prefs.TerrainLayerSrt = Enumerable.Range(0, 32).Select(index => LayerMask.LayerToName(index)).Where(l => !string.IsNullOrEmpty(l)).ToArray();
        }
        private void OnTexturePrefrencesGUI()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);

            GUI.backgroundColor = Color.green;
            EditorGUILayout.BeginVertical(GUI.skin.button);
            ShowTexturePref = EditorGUILayout.Foldout(ShowTexturePref, " Terrain Textures  ");
            EditorGUILayout.EndVertical();
            GUI.backgroundColor = Color.white;

            if (ShowTexturePref)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(" Texturing Mode ", "Generate Terrain with or without textures (Specifies the count terrains is needed when selecting 'Without' because Texture folder will not readed "), GUILayout.MaxWidth(200));
                Prefs.textureMode = (TextureMode)EditorGUILayout.EnumPopup("", Prefs.textureMode, GUILayout.ExpandWidth(true));
                GUILayout.EndHorizontal();


                switch (Prefs.textureMode)
                {
                    case TextureMode.WithTexture:

                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("  Textures Loading Mode ", " The Creation of terrain tiles is based on the number of texture tiles existing in the terrain texture folder, setting this parameter to Auto means that GTL will load and generate terrains by loading directly textures from texture folder /// if it set to Manually, GTL will make some operations of merging and spliting existing textures to make them simulair to terrain tiles count ' Attention' : this operation may consume memory when textures are larges '"), GUILayout.MaxWidth(200));
                        Prefs.textureloadingMode = (TexturesLoadingMode)EditorGUILayout.EnumPopup("", Prefs.textureloadingMode, GUILayout.ExpandWidth(true));
                        GUILayout.EndHorizontal();

                        if (Prefs.textureloadingMode == TexturesLoadingMode.Manual)
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label(new GUIContent("   Count Tiles ", " Specifie the number of terrain tiles , ' Attention '  Count Tiles set is different than the number of terrain textures tiles (Located in the Terrain texture folder), some operations (Spliting/mergins) textures will excuted so becarful when textures are large"), GUILayout.MaxWidth(200));
                            Prefs.terrainCount = EditorGUILayout.Vector2IntField("", Prefs.terrainCount);
                            GUILayout.EndHorizontal();

                            EditorGUILayout.HelpBox("' Attention Memory ' When terrain Count Tiles is different than the number of terrain textures tiles(Located in the Terrain texture folder), some operations(Spliting / Mergins) textures will excuted so becarful for large textures ", MessageType.Warning);

                        }

                        if (Prefs.AddTextureOffset == OptionEnabDisab.Enable)
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label(new GUIContent("  Texture Offset ", " Texture offset value"), GUILayout.MaxWidth(200));
                            Prefs.TextureOffset = EditorGUILayout.Vector2Field("", Prefs.TextureOffset);
                            GUILayout.EndHorizontal();


                            EditorGUILayout.HelpBox("' Attention Memory ' Works only with terrain count = 1,1", MessageType.Info);
                        }
                        break;

                    case TextureMode.Splatmapping:

                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("  Count Tiles ", " Number of Terrain Tiles in X-Y, This Option is avalaible Only when Texturing mode set to 'Without' or 'Splatmapping' "), GUILayout.MaxWidth(200));
                        Prefs.terrainCount = EditorGUILayout.Vector2IntField("", Prefs.terrainCount);
                        GUILayout.EndHorizontal();



                        GUILayout.BeginVertical();

                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("  Slope ", " Used to normalized the slope in Y dir, The default value = 0"), GUILayout.MaxWidth(200));
                        Prefs.Slope = EditorGUILayout.Slider(Prefs.Slope, 0.0f, 1, GUILayout.MaxWidth(200), GUILayout.ExpandWidth(true));
                        GUILayout.EndHorizontal();
                        GUILayout.EndVertical();

                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("  Merging Radius ", " Used to precise the radius of merging between layers, 0 value means that no merging operation will apply  "), GUILayout.MaxWidth(200));
                        Prefs.MergeRadius = EditorGUILayout.IntSlider(Prefs.MergeRadius, 0, 500, GUILayout.MaxWidth(200), GUILayout.ExpandWidth(true));
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("  Merging Factor ", " Used to precise how many times the merging will applyed on the terrain, the default is 1 "), GUILayout.MaxWidth(200));
                        Prefs.MergingFactor = EditorGUILayout.IntSlider(Prefs.MergingFactor, 1, 5, GUILayout.MaxWidth(200), GUILayout.ExpandWidth(true));
                        GUILayout.EndHorizontal();


                        GUILayout.BeginVertical();
                        GUILayout.Label(" ");
                        GUILayout.Label(new GUIContent("  Base Terrain Map ", " this will be the first splatmap for slope = 0"), GUILayout.MaxWidth(200));
                        Prefs.BaseTerrainLayers.ShowHeight = true;
                        EditorGUILayout.BeginHorizontal();

                        EditorGUILayout.BeginVertical();
                        EditorGUILayout.BeginHorizontal();
                        Prefs.BaseTerrainLayers.Diffuse = TextureField("", Prefs.BaseTerrainLayers.Diffuse);
                        Prefs.BaseTerrainLayers.NormalMap = TextureField("", Prefs.BaseTerrainLayers.NormalMap);
                        GUILayout.Label(new GUIContent(" Size ", "    "), GUILayout.MaxWidth(200));
                        Prefs.BaseTerrainLayers.TextureSize = EditorGUILayout.Vector2Field("", Prefs.BaseTerrainLayers.TextureSize);
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndHorizontal();

                        GUILayout.EndVertical();

                        ShowSplatmapsTerrainLayer = EditorGUILayout.Foldout(ShowSplatmapsTerrainLayer, " Terrain Layers  ");
                        EditorGUILayout.BeginVertical(GUI.skin.box);
                        EditorGUILayout.BeginHorizontal();

                        EditorGUILayout.EndHorizontal();

                        if (ShowSplatmapsTerrainLayer)
                        {
                            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

                            if (Prefs.TerrainLayers == null) Prefs.TerrainLayers = new List<GISTerrainLoaderTerrainLayer>();

                            if (GUILayout.Button(new GUIContent("+", "Add Terrain Layer"), EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                            {
                                Prefs.TerrainLayers.Add(new GISTerrainLoaderTerrainLayer());
                            }

                            GUILayout.Label("Layers");

                            if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.ExpandWidth(false))) Prefs.TerrainLayers = new List<GISTerrainLoaderTerrainLayer>();

                            EditorGUILayout.EndHorizontal();

                            if (Prefs.TerrainLayers.Count == 0)
                            {
                                GUILayout.Label("Terrain Layers is null.");
                            }

                            int LayerIndex = 1;
                            int LayerDeleteIndex = -1;

                            foreach (GISTerrainLoaderTerrainLayer Tlayer in Prefs.TerrainLayers)
                            {
                                EditorGUILayout.BeginHorizontal();
                                EditorGUILayout.BeginVertical();

                                GUILayout.Label(LayerIndex.ToString(), GUILayout.ExpandWidth(false));
                                EditorGUILayout.BeginHorizontal();
                                Tlayer.Diffuse = TextureField("", Tlayer.Diffuse);
                                Tlayer.NormalMap = TextureField("", Tlayer.NormalMap);
                                Tlayer.ShowHeight = true;
                                GUILayout.Label(new GUIContent(" Size ", "    "), GUILayout.MaxWidth(200));
                                Tlayer.TextureSize = EditorGUILayout.Vector2Field("", Tlayer.TextureSize);

                                if (GUILayout.Button(new GUIContent("X", "Delete Layer"), GUILayout.ExpandWidth(false))) LayerDeleteIndex = LayerIndex - 1;

                                EditorGUILayout.EndHorizontal();

                                if (Tlayer.ShowHeight)
                                {

                                    GUILayout.Label(" Height ");
                                    EditorGUILayout.BeginHorizontal();
                                    GUILayout.Label(" From ", GUILayout.MaxWidth(50));
                                    Tlayer.X_Height = EditorGUILayout.Slider(Tlayer.X_Height, 0.0f, 1, GUILayout.ExpandWidth(true));
                                    EditorGUILayout.EndHorizontal();
                                    EditorGUILayout.BeginHorizontal();
                                    GUILayout.Label(" To   ", GUILayout.MaxWidth(50));
                                    Tlayer.Y_Height = EditorGUILayout.Slider(Tlayer.Y_Height, 0.0f, 1, GUILayout.ExpandWidth(true));

                                    EditorGUILayout.EndHorizontal();

                                }
                                EditorGUILayout.EndHorizontal();
                                EditorGUILayout.EndHorizontal();

                                LayerIndex++;
                            }

                            if (LayerDeleteIndex != -1) Prefs.TerrainLayers.RemoveAt(LayerDeleteIndex);

                            EditorGUILayout.Space();

                            if (GUILayout.Button(new GUIContent(" Distribute Values", m_resetPrefs, "Set All Splatmapping values to default and distribute slope values "), new GUIStyle(EditorStyles.toolbarButton), GUILayout.ExpandWidth(true)))
                            {
                                Prefs.Slope = 0.1f;
                                Prefs.MergeRadius = 1;
                                Prefs.MergingFactor = 1;

                                float step = 1f / Prefs.TerrainLayers.Count;

                                for (int i = 0; i < Prefs.TerrainLayers.Count; i++)
                                {
                                    Prefs.TerrainLayers[i].X_Height = i * step;
                                    Prefs.TerrainLayers[i].Y_Height = (i + 1) * step;
                                }

                            }
                        }
                        EditorGUILayout.EndVertical();

                        break;

#if RDST
                    case TextureMode.RasterDrivenSplatmaps:
                        RDST_DrawRDSTPanel();
                        break;
#endif

                    case TextureMode.ShadedRelief:

                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("  Count Tiles ", " Number of Terrain Tiles in X-Y, This Option is avalaible Only when Texturing mode set to 'Without' or 'Splatmapping' "), GUILayout.MaxWidth(200));
                        Prefs.terrainCount = EditorGUILayout.Vector2IntField("", Prefs.terrainCount);
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent(" Shader Type ", " Select terrain shader type "), GUILayout.MaxWidth(200));
                        Prefs.TerrainShaderType = (ShaderType)EditorGUILayout.EnumPopup("", Prefs.TerrainShaderType);
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent(" UnderWater ", "Enable This Option to generate shaders to underwater terrains (Used to avoid blue color) "), GUILayout.MaxWidth(200));
                        Prefs.UnderWaterShader = (OptionEnabDisab)EditorGUILayout.EnumPopup("", Prefs.UnderWaterShader);
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent(" Save Shader Texture ", "Enable This Option to save the generated shaders as textures (For Editor in 'GIS Terrain' Folder), the texture resolution equal to terrain hightmap resolution"), GUILayout.MaxWidth(200));
                        Prefs.SaveShaderTextures = (OptionEnabDisab)EditorGUILayout.EnumPopup("", Prefs.SaveShaderTextures);
                        GUILayout.EndHorizontal();


                        break;

                    case TextureMode.WithoutTexture:

                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("  Count Tiles ", " Number of Terrain Tiles in X-Y, This Option is avalaible Only when Texturing mode set to 'Without' or 'Splatmapping' "), GUILayout.MaxWidth(200));
                        Prefs.terrainCount = EditorGUILayout.Vector2IntField("", Prefs.terrainCount);
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("  Use Custom Terrain Color ", "Enable/Disable customize terrain color "), GUILayout.MaxWidth(200));
                        Prefs.UseTerrainEmptyColor = (OptionEnabDisab)EditorGUILayout.EnumPopup("", Prefs.UseTerrainEmptyColor, GUILayout.ExpandWidth(true));
                        GUILayout.EndHorizontal();

                        if (Prefs.UseTerrainEmptyColor == OptionEnabDisab.Enable)
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label(new GUIContent("  Terrain Color ", "Used to change the main terrain color"), GUILayout.MaxWidth(200));
                            Prefs.TerrainEmptyColor = EditorGUILayout.ColorField("", Prefs.TerrainEmptyColor, GUILayout.ExpandWidth(true));
                            GUILayout.EndHorizontal();
                        }

                        break;
                }


            }
            EditorGUILayout.EndVertical();
        }
        private Texture2D TextureField(string name, Texture2D tex)
        {
            GUILayout.BeginVertical();
            var style = new GUIStyle(GUI.skin.label);
            style.alignment = TextAnchor.UpperCenter;
            style.fixedWidth = 70;
            style.fixedHeight = 20;
            GUILayout.Label(name, style);
            var result = (Texture2D)EditorGUILayout.ObjectField((Texture2D)tex, typeof(Texture2D), false, GUILayout.Width(100), GUILayout.Height(70));
            GUILayout.EndVertical();
            return result;
        }
        private void OnTerrainSmoothignOperationGUI()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);

            GUI.backgroundColor = Color.green;
            EditorGUILayout.BeginVertical(GUI.skin.button);
            ShowSmoothingOpr = EditorGUILayout.Foldout(ShowSmoothingOpr, " Terrain Smoothing  ");
            EditorGUILayout.EndVertical();
            GUI.backgroundColor = Color.white;


            if (ShowSmoothingOpr)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(" Terrain Height Smoother ", "Used to softens the landscape and reduces the appearance of abrupt changes"), GUILayout.MaxWidth(200));
                Prefs.UseTerrainHeightSmoother = (OptionEnabDisab)EditorGUILayout.EnumPopup("", Prefs.UseTerrainHeightSmoother, GUILayout.ExpandWidth(true));
                GUILayout.EndHorizontal();

                if (Prefs.UseTerrainHeightSmoother == OptionEnabDisab.Enable)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("  Terrain Height Smooth Factor ", GUILayout.MaxWidth(200));
                    Prefs.TerrainHeightSmoothFactor = EditorGUILayout.Slider(Prefs.TerrainHeightSmoothFactor, 0.0f, 1f, GUILayout.ExpandWidth(true));
                    GUILayout.EndHorizontal();

                }

                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(" Terrain Surface Smoother ", " this operation is useful when for terrains with unwanted jaggies, terraces,banding and non-smoothed terrain heights. Changing the surface smoother value to higher means more smoothing on surface while 1 value means minimum smoothing"), GUILayout.MaxWidth(200));
                Prefs.UseTerrainSurfaceSmoother = (OptionEnabDisab)EditorGUILayout.EnumPopup("", Prefs.UseTerrainSurfaceSmoother, GUILayout.ExpandWidth(true));
                GUILayout.EndHorizontal();


                if (Prefs.UseTerrainSurfaceSmoother == OptionEnabDisab.Enable)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent("  Terrain Surface Smooth Factor ", ""), GUILayout.MaxWidth(200));
                    Prefs.TerrainSurfaceSmoothFactor = EditorGUILayout.IntSlider(Prefs.TerrainSurfaceSmoothFactor, 1, 15, GUILayout.ExpandWidth(true));
                    GUILayout.EndHorizontal();

                }


            }

            EditorGUILayout.EndVertical();
        }
        private void OnTerrainVectorGenerationGUI()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);

            GUI.backgroundColor = Color.green;
            EditorGUILayout.BeginVertical(GUI.skin.button);
            ShowVectorDataPanel = EditorGUILayout.Foldout(ShowVectorDataPanel, " Vector Data Generator");
            EditorGUILayout.EndVertical();
            GUI.backgroundColor = Color.white;

            if (ShowVectorDataPanel)
            {
                GUI.backgroundColor = Color.cyan;
                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(" Vector Type ", "Select your vector type (Data must added to VectorData folder)"), GUILayout.MaxWidth(200));
                Prefs.vectorType = (VectorType)EditorGUILayout.EnumPopup("", Prefs.vectorType, GUILayout.ExpandWidth(true));
                GUILayout.EndHorizontal();
                GUI.backgroundColor = Color.white;

                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(" Vector Generator ", " Enable 3D Vector generator to generate Trees, Building ...."), GUILayout.MaxWidth(200));
                Prefs.EnableVectorGenerator = (OptionEnabDisab)EditorGUILayout.EnumPopup("", Prefs.EnableVectorGenerator);
                GUILayout.EndHorizontal();

                if (Prefs.EnableVectorGenerator == OptionEnabDisab.Enable)
                {
                    Color EnableGeoPointColor = Color.green;
                    if (Prefs.EnableGeoPointGeneration == OptionEnabDisab.Enable)
                        EnableGeoPointColor = Color.green;
                    else
                        EnableGeoPointColor = Color.red;


                    GUI.backgroundColor = EnableGeoPointColor;
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent(" Generate GeoPoints ", " Enable this option to generate gamebjects according to geo-points coordinates found in the vector file"), GUILayout.MaxWidth(200));
                    Prefs.EnableGeoPointGeneration = (OptionEnabDisab)EditorGUILayout.EnumPopup("", Prefs.EnableGeoPointGeneration);
                    GUILayout.EndHorizontal();
                    GUI.backgroundColor = Color.white;

                    Color EnableTreeColor = Color.green;
                    if (Prefs.EnableTreeGeneration == OptionEnabDisab.Enable)
                        EnableTreeColor = Color.green;
                    else
                        EnableTreeColor = Color.red;

                    GUI.backgroundColor = EnableTreeColor;

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent(" Generate Trees ", "Enable/Disable Loading and Generating Trees from Vector File "), GUILayout.MaxWidth(200));
                    Prefs.EnableTreeGeneration = (OptionEnabDisab)EditorGUILayout.EnumPopup("", Prefs.EnableTreeGeneration);
                    GUILayout.EndHorizontal();
                    GUI.backgroundColor = Color.white;

                    if (Prefs.EnableTreeGeneration == OptionEnabDisab.Enable)
                    {
                        //Grass Scale Factor
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("  Tree Scale Factor ", GUILayout.MaxWidth(200));
                        Prefs.TreeScaleFactor = EditorGUILayout.Slider(Prefs.TreeScaleFactor, 1f, 10, GUILayout.ExpandWidth(true));
                        GUILayout.EndHorizontal();

                        //Tree Dist
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("  Tree Distribution  ", " Distribute Trees in Random or Regular way "), GUILayout.MaxWidth(200));
                        Prefs.TreeDistribution = (PointDistribution)EditorGUILayout.EnumPopup("", Prefs.TreeDistribution);
                        GUILayout.EndHorizontal();

                        //Tree Distance
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("  Tree Distance ", " The distance from the camera beyond which trees are culled "), GUILayout.MaxWidth(200));
                        Prefs.TreeDistance = EditorGUILayout.Slider(Prefs.TreeDistance, 1, 5000, GUILayout.ExpandWidth(true));
                        GUILayout.EndHorizontal();

                        //Tree BillBoard Start Distance
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("  Tree BillBoard Start Distance ", "The distance from the camera at which Billboard images replace 3D Tree objects"), GUILayout.MaxWidth(200));
                        Prefs.BillBoardStartDistance = EditorGUILayout.Slider(Prefs.BillBoardStartDistance, 1, 2000, GUILayout.ExpandWidth(true));
                        GUILayout.EndHorizontal();

                    }

                    Color EnableGrassColor = Color.green;
                    if (Prefs.EnableGrassGeneration == OptionEnabDisab.Enable)
                        EnableGrassColor = Color.green;
                    else
                        EnableGrassColor = Color.red;

                    GUI.backgroundColor = EnableGrassColor;

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent(" Generate Grass ", "Enable/Disable Loading and Generating Grass from Vector File "), GUILayout.MaxWidth(200));
                    Prefs.EnableGrassGeneration = (OptionEnabDisab)EditorGUILayout.EnumPopup("", Prefs.EnableGrassGeneration);
                    GUILayout.EndHorizontal();
                    GUI.backgroundColor = Color.white;

                    if (Prefs.EnableGrassGeneration == OptionEnabDisab.Enable)
                    {
                        //Grass Distribution
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("  Grass Distribution  ", " Distribute Grass in Random or Regular way "), GUILayout.MaxWidth(200));
                        Prefs.GrassDistribution = (PointDistribution)EditorGUILayout.EnumPopup("", Prefs.GrassDistribution);
                        GUILayout.EndHorizontal();

                        //Grass Scale Factor
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("  Grass Scale Factor ", GUILayout.MaxWidth(200));
                        Prefs.GrassScaleFactor = EditorGUILayout.Slider(Prefs.GrassScaleFactor, 0.1f, 100, GUILayout.ExpandWidth(true));
                        GUILayout.EndHorizontal();

                        //Detail Distance
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("  Detail Distance ", "The distance from the camera beyond which details are culled"), GUILayout.MaxWidth(200));
                        Prefs.DetailDistance = EditorGUILayout.Slider(Prefs.DetailDistance, 10f, 1500, GUILayout.ExpandWidth(true));
                        GUILayout.EndHorizontal();
                    }

                    Color EnableRoadColor = Color.green;
                    if (Prefs.EnableRoadGeneration == OptionEnabDisab.Enable)
                        EnableRoadColor = Color.green;
                    else
                        EnableRoadColor = Color.red;

                    GUI.backgroundColor = EnableRoadColor;
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent(" Generate Roads ", "Enable/Disable Loading and Generating Roads from OSM File "), GUILayout.MaxWidth(200));
                    Prefs.EnableRoadGeneration = (OptionEnabDisab)EditorGUILayout.EnumPopup("", Prefs.EnableRoadGeneration);
                    GUILayout.EndHorizontal();
                    GUI.backgroundColor = Color.white;

                    if (Prefs.EnableRoadGeneration == OptionEnabDisab.Enable)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("  Road Generator Type ", "Select whiche type of road will be used (Note that EasyRoad3D must be existing in the project "), GUILayout.MaxWidth(200));
                        Prefs.RoadGenerator = (RoadGeneratorType)EditorGUILayout.EnumPopup("", Prefs.RoadGenerator, GUILayout.ExpandWidth(true));
                        GUILayout.EndHorizontal();

                        if (Prefs.RoadGenerator == RoadGeneratorType.EasyRoad3D)
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label(new GUIContent("   Raise Offset ", " The Distanc above the terrain, (offset to rise up/down the road "), GUILayout.MaxWidth(200));
                            Prefs.RoadRaiseOffset = EditorGUILayout.Slider(Prefs.RoadRaiseOffset, 0.1f, 2f, GUILayout.ExpandWidth(true));
                            GUILayout.EndHorizontal();

                            GUILayout.BeginHorizontal();
                            GUILayout.Label(new GUIContent("   Build Terrains ", "Enable/Disable  Update the terrains OnFinish according to the road network shape"), GUILayout.MaxWidth(200));
                            Prefs.BuildTerrains = (OptionEnabDisab)EditorGUILayout.EnumPopup("", Prefs.BuildTerrains);
                            GUILayout.EndHorizontal();
                        }

                        if (Prefs.vectorType == VectorType.OpenStreetMap)
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label(new GUIContent("  Roads Lable ", "Add Roads name  "), GUILayout.MaxWidth(200));
                            Prefs.EnableRoadName = EditorGUILayout.Toggle("", Prefs.EnableRoadName, GUILayout.ExpandWidth(true));
                            GUILayout.EndHorizontal();
                        }


                    }

                    Color EnableBuildingColor = Color.green;
                    if (Prefs.EnableBuildingGeneration == OptionEnabDisab.Enable)
                        EnableBuildingColor = Color.green;
                    else
                        EnableBuildingColor = Color.red;

                    GUI.backgroundColor = EnableBuildingColor;
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent(" Generate Buildings ", "Enable/Disable Loading and Generating building from Vector File "), GUILayout.MaxWidth(200));
                    Prefs.EnableBuildingGeneration = (OptionEnabDisab)EditorGUILayout.EnumPopup("", Prefs.EnableBuildingGeneration);
                    GUILayout.EndHorizontal();
                    GUI.backgroundColor = Color.white;


                    if (Prefs.EnableBuildingGeneration == OptionEnabDisab.Enable)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("   Generate Building Base ", "Enable this option to generate a base to each building "), GUILayout.MaxWidth(200));
                        Prefs.GenerateBuildingBase = (OptionEnabDisab)EditorGUILayout.EnumPopup("", Prefs.GenerateBuildingBase);
                        GUILayout.EndHorizontal();
                    }


                    Color EnableWaterColor = Color.green;
                    if (Prefs.EnableWaterGeneration == OptionEnabDisab.Enable)
                        EnableWaterColor = Color.green;
                    else
                        EnableWaterColor = Color.red;

                    GUI.backgroundColor = EnableWaterColor;
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent(" Generate Water ", "Enable/Disable this option to generate water areas from vector "), GUILayout.MaxWidth(200));
                    Prefs.EnableWaterGeneration = (OptionEnabDisab)EditorGUILayout.EnumPopup("", Prefs.EnableWaterGeneration);
                    GUILayout.EndHorizontal();
                    GUI.backgroundColor = Color.white;

                    if (Prefs.EnableWaterGeneration == OptionEnabDisab.Enable)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("   Water Data Source ", "Plane To Generate a simple plane for the entier terrain, VectorSource generate complexed geometry water data from vectordata  "), GUILayout.MaxWidth(200));
                        Prefs.WaterDataSource = (WaterSource)EditorGUILayout.EnumPopup("", Prefs.WaterDataSource);
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("   Water Offset Y ", " Rise up water from the terrain by y offset "), GUILayout.MaxWidth(200));
                        Prefs.WaterOffsetY = EditorGUILayout.Slider(Prefs.WaterOffsetY, 0.1f, 5f, GUILayout.ExpandWidth(true));
                        GUILayout.EndHorizontal();
                    }

                    Color EnableLandParcelColor = Color.green;
                    if (Prefs.EnableLandParcelGeneration == OptionEnabDisab.Enable)
                        EnableLandParcelColor = Color.green;
                    else
                        EnableLandParcelColor = Color.red;

                    GUI.backgroundColor = EnableLandParcelColor;
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent(" Generate LandParcel ", "Enable/Disable to generate some 2D plans on terrain from Vector File "), GUILayout.MaxWidth(200));
                    Prefs.EnableLandParcelGeneration = (OptionEnabDisab)EditorGUILayout.EnumPopup("", Prefs.EnableLandParcelGeneration);
                    GUILayout.EndHorizontal();
                    GUI.backgroundColor = Color.white;

                    if (Prefs.EnableLandParcelGeneration == OptionEnabDisab.Enable)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("   LandParcel Elevation Source ", "Default2D: only bounds vector points will be adapted to the terrain elevation , AdaptedToTerrainElevation: Adapte the entire polygon to the terrain elevation by creating more complexed geometry plan"), GUILayout.MaxWidth(200));
                        Prefs.LandParcelElevationMode = (VectorElevationMode)EditorGUILayout.EnumPopup("", Prefs.LandParcelElevationMode);
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("   Y Offset  ", " Rise up LandParcel from the terrain by y offset, Y Unite adapted to terrain container "), GUILayout.MaxWidth(200));
                        Prefs.LandParcelOffsetY = EditorGUILayout.Slider(Prefs.LandParcelOffsetY, 0.1f, 50f, GUILayout.ExpandWidth(true));
                        GUILayout.EndHorizontal();

                        if (Prefs.LandParcelElevationMode == VectorElevationMode.AdaptedToTerrainElevation3D)
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Label(new GUIContent("   Polygon Count ", " Add more subdivisions inside of Polygon to be more adapted to the terrain curves (becareful, the more Polygon Count the higher calculation number) "), GUILayout.MaxWidth(200));
                            Prefs.LandParcelPolygonCount = EditorGUILayout.IntSlider(Prefs.LandParcelPolygonCount, 1, 5, GUILayout.ExpandWidth(true));
                            GUILayout.EndHorizontal();
                        }

                    }

                    /// ---------- ADD-ON------------------//
                    /// 

#if GISTerrainLoaderFenceGenerator

                    DrawRedLabeledSeparator("Add-On Generators");

#endif

                    OnFenceGeneratorGUI();
                }

                OnTerrainAdvancedVectorGenerationGUI();
            }

            EditorGUILayout.EndVertical();



        }

        private void OnFenceGeneratorGUI()
        {
#if GISTerrainLoaderFenceGenerator

            Color EnableFenceGeneratorColor = Color.green;
            if (Prefs.EnableFenceGenerator == OptionEnabDisab.Enable)
                EnableFenceGeneratorColor = Color.green;
            else
                EnableFenceGeneratorColor = Color.red;

            GUI.backgroundColor = EnableFenceGeneratorColor;
            GUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent(" Generate Fence ", "Enable/Disable to generate 3D Fences "), GUILayout.MaxWidth(200));
            Prefs.EnableFenceGenerator = (OptionEnabDisab)EditorGUILayout.EnumPopup("", Prefs.EnableFenceGenerator);
            GUILayout.EndHorizontal();
            GUI.backgroundColor = Color.white;
 
#endif
        }
        private void OnTerrainAdvancedVectorGenerationGUI()
        {


#if GISTechAdvancedVectorGenerator

            EditorGUILayout.BeginVertical(GUI.skin.box);


            GUI.backgroundColor = Color.blue;
            EditorGUILayout.BeginVertical(GUI.skin.button);
            ShowAdvancedVectorGenerator = EditorGUILayout.Foldout(ShowAdvancedVectorGenerator, " 2D Advanced Vector Generator ");
            EditorGUILayout.EndVertical();
            GUI.backgroundColor = Color.white;

            if (ShowAdvancedVectorGenerator)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(" 2D Advanced Vector Generator ", " Enable Advanced Vector Generator ."), GUILayout.MaxWidth(200));
                Prefs.EnableAdvancedVectorGenerator = (OptionEnabDisab)EditorGUILayout.EnumPopup("", Prefs.EnableAdvancedVectorGenerator);
                GUILayout.EndHorizontal();

                if (Prefs.EnableAdvancedVectorGenerator == OptionEnabDisab.Enable)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent("  OOM Symbols ", "Dictionnay for OOM features "), GUILayout.MaxWidth(200));
                    Prefs.SymbolsDic = (GISTerrainLoaderCustomDic)EditorGUILayout.ObjectField(Prefs.SymbolsDic, typeof(GISTerrainLoaderCustomDic), true, GUILayout.ExpandWidth(true));
                    GUILayout.EndHorizontal();


                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent(" Features Enablement ", " Option to select which features will be selected from the dictionary to be used via the generator"), GUILayout.MaxWidth(200));
                    Prefs.FeaturesEnablement = (FeaturesEnablementOption)EditorGUILayout.EnumPopup("", Prefs.FeaturesEnablement);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent(" 2D Advanced Vector Generator ", " Enable Advanced Vector Generator ."), GUILayout.MaxWidth(200));
                    Prefs.EnableAdvancedVectorGenerator = (OptionEnabDisab)EditorGUILayout.EnumPopup("", Prefs.EnableAdvancedVectorGenerator);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent(" Generate Points ", "Enable/Disable Load and Generate Points "), GUILayout.MaxWidth(200));
                    Prefs.EnablePointGenerator = (OptionEnabDisab)EditorGUILayout.EnumPopup("", Prefs.EnablePointGenerator);
                    GUILayout.EndHorizontal();

                    if (Prefs.EnablePointGenerator == OptionEnabDisab.Enable)
                    {

                    }

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent(" Generate Lines ", "Enable/Disable Load and Generate Lines "), GUILayout.MaxWidth(200));
                    Prefs.EnableLineGenerator = (OptionEnabDisab)EditorGUILayout.EnumPopup("", Prefs.EnableLineGenerator);
                    GUILayout.EndHorizontal();

                    if (Prefs.EnableLineGenerator == OptionEnabDisab.Enable)
                    {


                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("  Line Object Type ", " Define in which way the line will be generated"), GUILayout.MaxWidth(200));
                        Prefs.AVG_LineType = (AVG_LineType)EditorGUILayout.EnumPopup("", Prefs.AVG_LineType);
                        GUILayout.EndHorizontal();

                    }

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent(" Generate Polygons ", "Enable/Disable Load and Generate Polygons "), GUILayout.MaxWidth(200));
                    Prefs.EnablePolygonGenerator = (OptionEnabDisab)EditorGUILayout.EnumPopup("", Prefs.EnablePolygonGenerator);
                    GUILayout.EndHorizontal();


                    if (Prefs.EnablePolygonGenerator == OptionEnabDisab.Enable)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("  Polygon Object Type ", " Define in which way the polygon will be generated"), GUILayout.MaxWidth(200));
                        Prefs.PolygonType = (PolygonObjectType)EditorGUILayout.EnumPopup("", Prefs.PolygonType);
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("  Vector Elevation Source ", " Create Flat Line/Mesh. Get Elevation from the terrain. Get the elevation from the vector itseltf"), GUILayout.MaxWidth(200));
                        Prefs.PolygonElevationSource = (VectorElevationMode)EditorGUILayout.EnumPopup("", Prefs.PolygonElevationSource);
                        GUILayout.EndHorizontal();

                    }
                }






                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent("  "), GUILayout.MaxWidth(200));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(" 2D Vector To AlphaMaps ", " Enable this option to draw vector as AlphaMaps on terrain"), GUILayout.MaxWidth(200));
                Prefs.EnableVectorToAlphaMaps = (OptionEnabDisab)EditorGUILayout.EnumPopup("", Prefs.EnableVectorToAlphaMaps);
                GUILayout.EndHorizontal();

                if (Prefs.EnableVectorToAlphaMaps == OptionEnabDisab.Enable)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent("  Alphamaps Resolution ", "The pixel resolution of the Terrain Alphamaps "), GUILayout.MaxWidth(200));
                    Prefs.AlphamapResolution_index = EditorGUILayout.Popup(Prefs.AlphamapResolution_index, Prefs.AlphamapResolutionsSrt, GUILayout.ExpandWidth(true));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent("  Merge Terrain Layers ", " Enable this option to merge all terrain layers into one Layer"), GUILayout.MaxWidth(200));
                    Prefs.AlphamapMergeTerrainLayers = (OptionEnabDisab)EditorGUILayout.EnumPopup("", Prefs.AlphamapMergeTerrainLayers);
                    GUILayout.EndHorizontal();

                    //GUILayout.BeginHorizontal();
                    //GUILayout.Label(new GUIContent("  Draw Points ", " Enable this option to draw Points on terrain"), GUILayout.MaxWidth(200));
                    //Prefs.EnableAlphaMapPoints = (OptionEnabDisab)EditorGUILayout.EnumPopup("", Prefs.EnableAlphaMapPoints);
                    //GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent("  Draw Lines ", " Enable this option to draw Lines on terrain"), GUILayout.MaxWidth(200));
                    Prefs.EnableAlphaMapLines = (OptionEnabDisab)EditorGUILayout.EnumPopup("", Prefs.EnableAlphaMapLines);
                    GUILayout.EndHorizontal();

                    if (Prefs.EnableAlphaMapLines == OptionEnabDisab.Enable)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("   Texture Motif ", " Select whatever the texture will as a motif or a sample blank texture"), GUILayout.MaxWidth(200));
                        Prefs.LineMotifTexture = (AlphamapsTextureMotif)EditorGUILayout.EnumPopup("", Prefs.LineMotifTexture);
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("   Displayed Texture ", " Select whatever the texture for the Line is Symbol/Biom"), GUILayout.MaxWidth(200));
                        Prefs.LineDisplayTexture = (DisplayedTexture)EditorGUILayout.EnumPopup("", Prefs.LineDisplayTexture);
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("   Line Width Factor ", "Use this parameter to increase/decrease the width of drawn lines (Default value= 1)"), GUILayout.MaxWidth(200));
                        Prefs.AlphamapLineWidthFactor = EditorGUILayout.Slider(Prefs.AlphamapLineWidthFactor, 0.1f, 2, GUILayout.ExpandWidth(true));
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("   Alphamaps Smoothness Factor ", "Used to interpolate between points at a finer scale,If the distance between consecutive points is large, it insert additional points to fill gaps"), GUILayout.MaxWidth(200));
                        Prefs.AlphamapSmoothnessFactor = EditorGUILayout.Slider(Prefs.AlphamapSmoothnessFactor, 0f, 10f, GUILayout.ExpandWidth(true));
                        GUILayout.EndHorizontal();
                    }


                    GUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent("  Draw Polygons ", " Enable this option to draw Polygons on terrain"), GUILayout.MaxWidth(200));
                    Prefs.EnableAlphaMapPolygons = (OptionEnabDisab)EditorGUILayout.EnumPopup("", Prefs.EnableAlphaMapPolygons);
                    GUILayout.EndHorizontal();

                    if (Prefs.EnableAlphaMapPolygons == OptionEnabDisab.Enable)
                    {

                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("   Texture Motif ", " Select whatever the texture will as a motif or a sample blank texture"), GUILayout.MaxWidth(200));
                        Prefs.PolygonMotifTexture = (AlphamapsTextureMotif)EditorGUILayout.EnumPopup("", Prefs.PolygonMotifTexture);
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        GUILayout.Label(new GUIContent("   Displayed Texture ", " Select whatever the texture for the polygons is Symbol/Biom"), GUILayout.MaxWidth(200));
                        Prefs.PolygonDisplayTexture = (DisplayedTexture)EditorGUILayout.EnumPopup("", Prefs.PolygonDisplayTexture);
                        GUILayout.EndHorizontal();

                    }
                }


            }
            EditorGUILayout.EndVertical();

#endif
        }
        private void OnHelpToolbarGUI()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);

            GUI.backgroundColor = Color.green;
            EditorGUILayout.BeginVertical(GUI.skin.button);
            ShowHelpSupport = EditorGUILayout.Foldout(ShowHelpSupport, " Help & Support & Addons");
            EditorGUILayout.EndVertical();
            GUI.backgroundColor = Color.white;

            if (ShowHelpSupport)
            {
                GUIStyle buttonStyle = new GUIStyle(EditorStyles.toolbarButton);

                buttonStyle.alignment = TextAnchor.MiddleLeft;
                GUILayout.BeginVertical();

                GUILayout.Label("", buttonStyle);

                if (GUILayout.Button(new GUIContent(" Download 'GIS Examples' Asset", m_downloaExamples, "Link to download 'GIS Terrain Loader Data Examples', Contains all GIS Data that can be loaded by GTL "), buttonStyle, GUILayout.ExpandWidth(true)))
                    System.Diagnostics.Process.Start("https://assetstore.unity.com/packages/tools/integration/gis-terrain-loader-data-exemples-152552");

                if (GUILayout.Button(new GUIContent(" Download 'GIS Data Downloader' Asset", m_downloaExamples, "Link to 'GIS Data Downloader', use this asset to download real world data from online servers "), buttonStyle, GUILayout.ExpandWidth(true)))
                    System.Diagnostics.Process.Start("https://assetstore.unity.com/packages/tools/integration/gis-data-downloader-199112?fbclid=IwAR2dVOdf-vIJp3fO8QGz6Gcepo4_cL0rp144cSUAwGXfFdLr8LFE7QqzcUA#content");

                if (GUILayout.Button(new GUIContent(" Download 'GIS Data Downloader Pro' Asset", m_downloaExamples, "Link to 'GIS Data Downloader Pro', this is the Pro version of GIS Data Downloader asset, it used to download real world data from online servers "), buttonStyle, GUILayout.ExpandWidth(true)))
                    System.Diagnostics.Process.Start("https://assetstore.unity.com/packages/tools/integration/gis-data-downloader-pro-303392");

                if (GUILayout.Button(new GUIContent(" Download 'Fence Generator Add-on' Asset", m_downloaExamples, "Link to 'Fence Generator',  it allows you to quickly and professionally generate fences, walls, and posts on your GIS terrains, using the power of GIS Terrain Loader Pro. "), buttonStyle, GUILayout.ExpandWidth(true)))
                    System.Diagnostics.Process.Start("https://assetstore.unity.com/packages/tools/integration/fence-generator-add-on-for-gis-terrain-loader-pro-326705");

                if (GUILayout.Button(new GUIContent(" Download 'ENC57 (Electronic Navigational Charts) Add-on' Asset", m_downloaExamples, "ENC57 (Electronic Navigational Charts) Reader is an Add-On for GIS Terrain Loader Pro to Bring Real-World Nautical Data Directly into Your Unity Projects!"), buttonStyle, GUILayout.ExpandWidth(true)))
                    System.Diagnostics.Process.Start("https://assetstore.unity.com/packages/tools/integration/enc57-electronic-navigational-charts-add-on-for-gis-terrain-load-322094");
 
                if (GUILayout.Button(new GUIContent(" Report an Issue", m_helpIcon, "Ask a question in forum page "), buttonStyle, GUILayout.ExpandWidth(true)))
                    System.Diagnostics.Process.Start("https://forum.unity.com/threads/released-gis-terrain-loader.726206/");

                if (GUILayout.Button(new GUIContent(" Reset Parameters to default", m_resetPrefs, " Reset all Prefs to default "), buttonStyle, GUILayout.ExpandWidth(true)))
                    ResetPrefs();

                if (GUILayout.Button(new GUIContent(" About", m_aboutIcon, "About GIS Terrain Loader "), buttonStyle, GUILayout.ExpandWidth(true)))
                    GISTerrainLoaderAboutWindow.Init();

                GUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
        }
        private void GeneratingBtn()
        {
            EditorGUILayout.BeginHorizontal(GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent(" Remove Previous Terrain ", "Enable this Option to Remove previous generated terrain existing in your scene"), GUILayout.MaxWidth(200));
            Prefs.RemovePrvTerrain = (OptionEnabDisab)EditorGUILayout.EnumPopup("", Prefs.RemovePrvTerrain);
            GUILayout.EndHorizontal();
            EditorGUILayout.EndHorizontal();

#if GISDataDownloader
            EditorGUILayout.BeginHorizontal(GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent(" Generate Terrain From GDD Data ", "Enable this Option to Automatically generate terrain once the data being downloaded via GIS Data Downloader Pro"), GUILayout.MaxWidth(200));
            Prefs.GenerateTerrainFromDownloadedData = (OptionEnabDisab)EditorGUILayout.EnumPopup("", Prefs.GenerateTerrainFromDownloadedData);
            GUILayout.EndHorizontal();
            EditorGUILayout.EndHorizontal();
#endif

            EditorGUILayout.BeginVertical(GUI.skin.box);

            if (State == GeneratorState.Generating)
            {
                GUI.backgroundColor = Color.blue;

                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(""), GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.Height(3));
                GUILayout.EndHorizontal();

                if (GUILayout.Button(new GUIContent(" Cancel ", "Click To Cancel the operation")))
                {
                    OnError("");

                }
                GUI.backgroundColor = Color.white;
            }
            if (State == GeneratorState.idle)
            {
                if (GUILayout.Button(new GUIContent(" Generate Terrain ", "Click To Start Generating Terrains")))
                {
                    Repaint();

                    switch (Prefs.terrainLoader)
                    {
                        case TerrainLoader.GISTerrainLoader:
                            GTL_Generate();
                            break;
                        case TerrainLoader.GISTerrainStreamer:
#if GISTerrainStreamer
                            GTS_Generate();
#endif
                            break;
                    }

                }
            }



            Rect rec = EditorGUILayout.BeginVertical();


            if (State == GeneratorState.Generating)
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
        #endregion
        #region Phases
        public async Task GTL_Phases()
        {
            if (State == GeneratorState.Generating)
            {
                timer.Reset();
                timer.Start();

                await FreeUpMemory();

                await LoadElevationFile(Prefs.TerrainFilePath);

                if (ElevationInfo != null)
                {
                    await GenerateContainer();

                    if (GeneratedContainer)
                    {
                        for (int i = 0; i < GeneratedContainer.terrains.Length; i++)
                        {
                            if (State != GeneratorState.idle)
                                await GenerateTerrains(i);
                        }


                        for (int i = 0; i < GeneratedContainer.terrains.Length; i++)
                        {
                            if (State != GeneratorState.idle)
                                await GenerateHeightmap(i);
                        }

                        if (State != GeneratorState.idle)
                            RepareTerrains();
                        if (State != GeneratorState.idle)
                            await GenerateTextures();
                        if (State != GeneratorState.idle)
                            await GenerateBackground();
                        if (State != GeneratorState.idle)
                            await GenerateVectorData();
                        if (State != GeneratorState.idle)
                            Finish();

                        timer.Stop();
                    }
                }
                else
                {
                    Finish();
                }

            }
            try
            {


            }
            catch (Exception ex)
            {
                OnError("Couldn't Load Terrain file: " + ex.Message + "  " + Environment.NewLine);
            }
            ;

        }
        private async void GTLCheckFileConfig()
        {
            if (File.Exists(Prefs.TerrainFilePath))
            {
                if (IsTerrainFileInsideGISFolder(Prefs.TerrainFilePath))
                {
                    CheckTerrainTextures();

                    State = GeneratorState.Generating;

                    await Task.Delay(1);

                    await GTL_Phases();
                }
                else
                    Debug.LogError("DEM Not Placed in the Correct Folder : Place your GIS Data into 'GIS Terrain Loader/Recources/GIS Terrains/' Folder");
            }

        }

        public async Task LoadElevationFile(string filepath)
        {
            if (File.Exists(filepath))
            {
                LoadedFileExtension = Path.GetExtension(filepath);

                ElevationInfo = null;

                if (LoadedFileExtension == ".tiff") LoadedFileExtension = ".tif";

                switch (LoadedFileExtension)
                {
                    case ".tif":
                        {
                            ElevationInfo = new GISTerrainLoaderElevationInfo(Prefs);

                            var TiffReader = new GISTerrainLoaderTIFFLoader(Prefs);

                            TiffReader.LoadFile();

                            while (!TiffReader.LoadComplet)
                                await Task.Delay(TimeSpan.FromSeconds(0.01));

                            ElevationInfo.GetData(TiffReader.data);

                            await CheckForDimensionAndTiles(true);

                        }
                        break;
                    case ".las":
                        {
#if GISTerrainLoaderPdal

                            ElevationInfo = new GISTerrainLoaderElevationInfo(Prefs);

                            var lasReader = new GISTerrainLoaderLASLoader();

                            if (!lasReader.LoadComplet)
                                lasReader.LoadLasFile(filepath);

                            while (!lasReader.LoadComplet)
                                await Task.Delay(TimeSpan.FromSeconds(0.01));

                            ElevationInfo.GetData(lasReader.data);

                            if (lasReader.LoadComplet)
                            {
                                Prefs.TerrainFilePath = lasReader.GeneratedFilePath;
                                await Task.Delay(TimeSpan.FromSeconds(1));

                                if (File.Exists(Prefs.TerrainFilePath))
                                {
                                    var TiffReader = new GISTerrainLoaderTIFFLoader(Prefs);
  
                                        TiffReader.LoadFile(ElevationInfo.data);
       
                                    while (!TiffReader.LoadComplet)
                                        await Task.Delay(TimeSpan.FromSeconds(0.01));

                                    ElevationInfo.GetData(TiffReader.data);
                                    await CheckForDimensionAndTiles(true);

                                    lasReader.LoadComplet = false;
                                }
                                else
                                    Debug.LogError("File Not exsiting " + Prefs.TerrainFilePath);
                            }

#else
                            Debug.LogError("Pdal Plugin Not Configured ..");
#endif

                        }
                        break;
                    case ".flt":
                        {
                            ElevationInfo = new GISTerrainLoaderElevationInfo();

                            var floatReader = new GISTerrainLoaderFloatReader(Prefs);
                            floatReader.LoadFile(Prefs.TerrainFilePath);

                            ElevationInfo.GetData(floatReader.data);

                            await CheckForDimensionAndTiles(true);

                            if (floatReader.LoadComplet)
                                floatReader.LoadComplet = false;
                        }
                        break;

                    case ".bin":
                        {

                            ElevationInfo = new GISTerrainLoaderElevationInfo();

                            var binReader = new GISTerrainLoaderBinLoader(Prefs);
                            binReader.LoadFile(Prefs.TerrainFilePath);

                            ElevationInfo.GetData(binReader.data);

                            await CheckForDimensionAndTiles(true);

                            if (binReader.LoadComplet)
                                binReader.LoadComplet = false;
                        }
                        break;
                    case ".bil":
                        {
                            ElevationInfo = new GISTerrainLoaderElevationInfo();

                            var BILReader = new GISTerrainLoaderBILReader(Prefs);
                            BILReader.LoadFile(Prefs.TerrainFilePath);

                            ElevationInfo.GetData(BILReader.data);

                            await CheckForDimensionAndTiles(true);

                            if (BILReader.LoadComplet)
                                BILReader.LoadComplet = false;
                        }
                        break;
                    case ".asc":
                        {
                            ElevationInfo = new GISTerrainLoaderElevationInfo();

                            var ASCIReader = new GISTerrainLoaderASCILoader(Prefs);
                            ASCIReader.LoadFile(Prefs.TerrainFilePath);

                            ElevationInfo.GetData(ASCIReader.data);

                            await CheckForDimensionAndTiles(true);

                            if (ASCIReader.LoadComplet)
                                ASCIReader.LoadComplet = false;
                        }
                        break;
                    case ".hgt":
                        {
                            ElevationInfo = new GISTerrainLoaderElevationInfo();

                            var HGTReader = new GISTerrainLoaderHGTLoader(Prefs);
                            HGTReader.LoadFile(Prefs.TerrainFilePath);

                            ElevationInfo.GetData(HGTReader.data);

                            await CheckForDimensionAndTiles(true);

                            if (HGTReader.LoadComplet)
                                HGTReader.LoadComplet = false;
                        }
                        break;

                    case ".dem":
                        {
                            ElevationInfo = new GISTerrainLoaderElevationInfo();

                            var DemReader = new GISTerrainLoaderDEMLoader(Prefs);
                            DemReader.LoadFile(Prefs.TerrainFilePath);

                            ElevationInfo.GetData(DemReader.data);

                            await CheckForDimensionAndTiles(true);

                            if (DemReader.LoadComplet)
                            {
                                DemReader.LoadComplet = false;
                            }
                        }
                        break;
                    case ".raw":
                        {

                            ElevationInfo = new GISTerrainLoaderElevationInfo();

                            var RawReader = new GISTerrainLoaderRawLoader(Prefs);
                            RawReader.LoadFile(Prefs.TerrainFilePath);

                            while (!RawReader.LoadComplet)
                                await Task.Delay(TimeSpan.FromSeconds(0.01));

                            ElevationInfo.GetData(RawReader.data);

                            await CheckForDimensionAndTiles(false);

                            RawReader.LoadComplet = false;
                        }
                        break;

                    case ".png":
                        {
                            ElevationInfo = new GISTerrainLoaderElevationInfo();

                            var PngReader = new GISTerrainLoaderDEMPngLoader(Prefs);
                            PngReader.LoadFile(Prefs.TerrainFilePath);

                            while (!PngReader.LoadComplet)
                                await Task.Delay(TimeSpan.FromSeconds(0.01));

                            ElevationInfo.GetData(PngReader.data);

                            await CheckForDimensionAndTiles(false);

                            PngReader.LoadComplet = false;
                        }
                        break;

                    case ".ter":
                        {

                            ElevationInfo = new GISTerrainLoaderElevationInfo();

                            var TerReader = new GISTerrainLoaderTerraGenLoade(Prefs);
                            TerReader.LoadFile(filepath);

                            while (!TerReader.LoadComplet)
                                await Task.Delay(TimeSpan.FromSeconds(0.01));

                            ElevationInfo.GetData(TerReader.data);

                            await CheckForDimensionAndTiles(false);

                            TerReader.LoadComplet = false;
                        }
                        break;

                }


            }
        }
        private async Task GenerateContainer()
        {
            if (ElevationInfo == null)
            {
                OnError(" DEM not loaded correctly .. !");
                return;
            }

            if (Prefs.terrainDimensionMode == TerrainDimensionsMode.AutoDetection && (ElevationInfo.data.Dimensions.x == 0 || ElevationInfo.data.Dimensions.y == 0))
            {
                OnError("Terrain Dimension is null ...");
                return;
            }

            string containerName = "Terrains";

            if (Prefs.RemovePrvTerrain == OptionEnabDisab.Enable)
            {
                DestroyImmediate(GameObject.Find(containerName));
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            else
            {
                int index_name = 1;
                while (GameObject.Find(containerName) != null)
                {
                    containerName = containerName + " " + index_name.ToString();
                    index_name++;
                }
            }

            var container = new GameObject(containerName);

            container.transform.position = new Vector3(0, 0, 0);

            Vector2Int tCount = new Vector2Int(Prefs.terrainCount.x, Prefs.terrainCount.y);


            float maxElevation = ElevationInfo.data.MinMaxElevation.y;
            float minElevation = ElevationInfo.data.MinMaxElevation.x;
            float ElevationRange = maxElevation - minElevation;

            if (Prefs.UnderWater == OptionEnabDisab.Enable)
            {
                if (minElevation <= 0 && maxElevation <= 0)
                    ElevationRange = Math.Abs(minElevation) - Math.Abs(maxElevation);
                else
                    if (maxElevation >= 0 && minElevation < 0)
                    ElevationRange = maxElevation + Math.Abs(minElevation);

            }

            var sizeX = Mathf.Floor(m_terrainDimensions.x * Prefs.terrainScale.x * Prefs.ScaleFactor) / Prefs.terrainCount.x;
            var sizeZ = Mathf.Floor(m_terrainDimensions.y * Prefs.terrainScale.z * Prefs.ScaleFactor) / Prefs.terrainCount.y;
            var sizeY = (ElevationRange) / Prefs.ElevationScaleValue * Prefs.TerrainExaggeration * 100 * Prefs.terrainScale.y * 10;


            Vector3 size;

            if (!Prefs.Settings_SO.IsGeoFile(LoadedFileExtension))
            {
                if (Prefs.TerrainElevation == TerrainElevation.RealWorldElevation)
                {
                    sizeY = ((162)) * Prefs.terrainScale.y;
                    size = new Vector3(sizeX, sizeY, sizeZ);
                }
                else
                {
                    sizeY = 300 * Prefs.TerrainExaggeration * Prefs.terrainScale.y;
                    size = new Vector3(sizeX, sizeY, sizeZ);
                }
            }
            else
            {
                if (Prefs.TerrainElevation == TerrainElevation.RealWorldElevation)
                {
                    sizeY = (ElevationRange / Prefs.ElevationScaleValue) * 1000 * Prefs.terrainScale.y;

                    size = new Vector3(sizeX, sizeY, sizeZ);
                }
                else
                {
                    sizeY = sizeY * 10;
                    size = new Vector3(sizeX, sizeY, sizeZ);
                }
            }



            string resultFolder = "Assets/Generated GIS Terrains";
            string resultFullPath = Path.Combine(Application.dataPath, "Generated GIS Terrains");

            if (!Directory.Exists(resultFullPath)) Directory.CreateDirectory(resultFullPath);
            string dateStr = DateTime.Now.ToString("yyyy-MM-dd HH-mm-") + DateTime.Now.Second.ToString();
            resultFolder += "/" + dateStr;
            resultFullPath = Path.Combine(resultFullPath, dateStr);

            if (!Directory.Exists(resultFullPath)) Directory.CreateDirectory(resultFullPath);

            terrains = new GISTerrainTile[tCount.x, tCount.y];

            GISTerrainContainer terrainContainer = container.AddComponent<GISTerrainContainer>();

            terrainContainer.data = new GISTerrainLoaderFileData();

            terrainContainer.data = ElevationInfo.data;

            terrainContainer.TerrainCount = new Vector2Int(Prefs.terrainCount.x, Prefs.terrainCount.y);

            terrainContainer.DestinationTerrainPath = resultFolder;

            terrainContainer.SourceDEMPath = Prefs.TerrainFilePath;

            terrainContainer.Scale = Prefs.terrainScale;

            terrainContainer.ContainerSize = new Vector3(size.x * tCount.x, size.y, size.z * tCount.y);

            terrainContainer.SubTerrainSize = size;

            if (Prefs.Settings_SO.IsGeoFile(LoadedFileExtension))
                terrainContainer.data.Dimensions = ElevationInfo.data.Dimensions;
            else
                terrainContainer.data.Dimensions = Prefs.TerrainDimensions;

            terrainContainer.data.MinMaxElevation = new Vector2((float)ElevationInfo.data.MinMaxElevation.x, (float)ElevationInfo.data.MinMaxElevation.y);

            var centre = new Vector3(terrainContainer.ContainerSize.x / 2, 0, terrainContainer.ContainerSize.z / 2);
            terrainContainer.GlobalTerrainBounds = new Bounds(centre, new Vector3(centre.x + terrainContainer.ContainerSize.x / 2, 0, centre.z + terrainContainer.ContainerSize.z / 2));

            terrainContainer.terrains = terrains;

            GeneratedContainer = terrainContainer;


#if DotSpatial
            if (ElevationInfo.data.EPSG != 0)
                terrainContainer.m_ContainerProjection = ProjectionMode.Custom_EPSG;
#endif


            if (SerializeHeightmap == OptionEnabDisab.Enable)
            {
                terrainContainer.data.Store(Path.GetFileName(Prefs.TerrainFilePath) + "_" + Path.GetRandomFileName());
            }

            if (!GeneratedContainer.IsValidContainer(GeneratedContainer))
            {
                DestroyImmediate(GameObject.Find("Terrains"));
                GC.Collect();
                GC.WaitForPendingFinalizers();
                OnError("Error While loading file : DEM not loaded correctly, Elevation or Dimensions out of Range");
                return;
            }

            CheckTerrainMaterials();


            await Task.Delay(TimeSpan.FromSeconds(0.0005));

        }
        public async Task GenerateTerrains(int index)
        {
            if (index >= terrains.Length)
            {
                s_progress = 0;
                return;
            }

            int x = index % Prefs.terrainCount.x;
            int y = index / Prefs.terrainCount.x;

            OnProgress("Generating Terrains ", (index + 1) * 100 / (terrains.Length));

            var terrain = await CreateTerrain(GeneratedContainer, x, y);
            terrains[x, y] = terrain;
            terrain.container = GeneratedContainer;

        }
        private async Task<GISTerrainTile> CreateTerrain(GISTerrainContainer Container, int x, int y)
        {
            TerrainData tdata = new TerrainData
            {
                baseMapResolution = 32,
                heightmapResolution = 32
            };

            tdata.heightmapResolution = Prefs.heightmapResolution;
            tdata.baseMapResolution = Prefs.baseMapResolution;
            tdata.SetDetailResolution(Prefs.detailResolution, Prefs.resolutionPerPatch);
            tdata.size = GeneratedContainer.SubTerrainSize;


            GameObject GO = Terrain.CreateTerrainGameObject(tdata);
            GO.gameObject.SetActive(true);
            GO.name = string.Format("Tile__{0}__{1}", x, y);
            GO.transform.parent = Container.gameObject.transform;
            GO.transform.position = new Vector3(GeneratedContainer.SubTerrainSize.x * x, 0, GeneratedContainer.SubTerrainSize.z * y);
            GO.isStatic = false;

            if (Prefs.TerrainLayerSet == OptionEnabDisab.Enable)
            {
                GO.gameObject.layer = Prefs.TerrainLayer;
            }


            GISTerrainTile item = GO.AddComponent<GISTerrainTile>();
            item.Number = new Vector2Int(x, y);
            item.size = GeneratedContainer.SubTerrainSize;
            item.ElevationFilePath = Prefs.TerrainFilePath;

            item.terrain = GO.GetComponent<Terrain>();
            item.terrainData = item.terrain.terrainData;

            item.terrain.heightmapPixelError = Prefs.PixelError;
            item.terrain.basemapDistance = Prefs.BaseMapDistance;
            item.terrain.materialTemplate = Prefs.terrainMaterial;

            string filename = Path.Combine(Container.DestinationTerrainPath, GO.name) + ".asset";

            AssetDatabase.CreateAsset(tdata, filename);

            AssetDatabase.SaveAssets();

            await Task.Delay(TimeSpan.FromSeconds(0.01));

            return item;
        }
        private async Task GenerateHeightmap(int index)
        {
            if (index >= terrains.Length)
            {
                s_progress = 0;
                return;
            }

            int x = index % Prefs.terrainCount.x;
            int y = index / Prefs.terrainCount.x;

            OnProgress("Generating Heightmap ", (index + 1) * 100 / (terrains.Length));

            await ElevationInfo.GenerateHeightMap(Prefs, terrains[x, y]);

        }
        public void RepareTerrains()
        {
            List<GISTerrainTile> List_terrainsObj = new List<GISTerrainTile>();

            foreach (var item in terrains)
            {
                if (item != null)
                {
                    List_terrainsObj.Add(item);
                }

            }

            if (Prefs.UseTerrainHeightSmoother == OptionEnabDisab.Enable)
                GISTerrainLoaderTerrainSmoother.SmoothTerrainHeights(List_terrainsObj, 1 - Prefs.TerrainHeightSmoothFactor);

            if (Prefs.UseTerrainSurfaceSmoother == OptionEnabDisab.Enable)
                GISTerrainLoaderTerrainSmoother.SmoothTerrainSurface(List_terrainsObj, Prefs.TerrainSurfaceSmoothFactor);

            if (Prefs.UseTerrainHeightSmoother == OptionEnabDisab.Enable || Prefs.UseTerrainSurfaceSmoother == OptionEnabDisab.Enable)
                GISTerrainLoaderBlendTerrainEdge.StitchTerrain(List_terrainsObj, 50f, 20);


            if (Prefs.TerrainBaseboards == OptionEnabDisab.Enable)
                GISTerrainLoaderBaseboardsGenerator.GenerateTerrainBaseboards(GeneratedContainer, Prefs.BorderHigh, Prefs.terrainBorderMaterial);


        }
        private async Task GenerateTextures()
        {
            switch (Prefs.textureMode)
            {
                case TextureMode.WithTexture:

                    TextureSourceFormat TextureSourceformat = null;

                    bool TextureFolderExist = GISTerrainLoaderTextureGenerator.CheckForTerrainTextureFolder(Prefs.TerrainFilePath, out TextureSourceformat);

                    string ResTextureFolder = GISTerrainLoaderTextureGenerator.GetTextureFolder(Prefs.TerrainFilePath);

                    if (Prefs.textureloadingMode == TexturesLoadingMode.Manual)
                    {
                        var FolderTiles_count = GISTerrainLoaderTextureGenerator.GetTilesNumberInTextureFolder(Prefs.TerrainFilePath);

                        if (Prefs.terrainCount != FolderTiles_count)
                        {
                            if (FolderTiles_count == Vector2.one)
                            {
                                await GISTerrainLoaderTextureGenerator.SplitTex(Prefs.TerrainFilePath, Prefs.terrainCount);
                                AssetDatabase.Refresh();
                            }
                            else
                            {
                                if (FolderTiles_count.x > 1 || FolderTiles_count.y > 1)
                                {
                                    GISTerrainLoaderTextureGenerator.CombienTerrainTextures(Prefs.TerrainFilePath);
                                    AssetDatabase.Refresh();

                                    GISTerrainLoaderTextureGenerator.SplitTex(Prefs.TerrainFilePath, Prefs.terrainCount).Wait();
                                    AssetDatabase.Refresh();

                                    Prefs.textureloadingMode = TexturesLoadingMode.AutoDetection;
                                }

                            }

                        }
                        else
                            Prefs.textureloadingMode = TexturesLoadingMode.AutoDetection;
                    }

                    if (Prefs.textureloadingMode == TexturesLoadingMode.AutoDetection)
                    {
                        if (TextureFolderExist)
                        {
                            var TextureFolderPath = Path.Combine(Path.GetDirectoryName(Prefs.TerrainFilePath), Path.GetFileNameWithoutExtension(Prefs.TerrainFilePath) + Prefs.Settings_SO.TextureFolderName);

                            var Tiles = GISTerrainLoaderTextureGenerator.GetTextureTiles(TextureFolderPath, Prefs.Settings_SO);

                            if (Tiles.Length > 0)
                            {
                                TextureSourceFormat TextureSourceSoft = null;

                                bool IsCorrectFormat = GISTerrainLoaderTextureGenerator.TextureSourceName(Tiles, Prefs.Settings_SO, out TextureSourceSoft);

                                if (IsCorrectFormat)
                                {

                                    for (int index = 0; index < GeneratedContainer.terrains.Length; index++)
                                    {
                                        if (index < terrains.Length)
                                        {
                                            int x = index % Prefs.terrainCount.x;
                                            int y = index / Prefs.terrainCount.x;

                                            OnProgress("Generating Textures ", (index + 1) * 100 / (Prefs.terrainCount.x * Prefs.terrainCount.y));

                                            await GISTerrainLoaderTextureGenerator.AddTextureToTerrain(TextureFolderPath, ResTextureFolder, Prefs, TextureSourceSoft, terrains[x, y]);

                                        }
                                        else
                                        {
                                            s_progress = 0;

                                            return;
                                        }
                                    }
                                }

                            }
                        }

                    }
                    break;

                case TextureMode.MipmapTextures:

                    TextureSourceformat = null;

                    var MipmapTextureFolderExists = GISTerrainLoaderTextureGenerator.CheckForTerrainMipmapTextureFolder(Prefs.TerrainFilePath, out TextureSourceformat);

                    ResTextureFolder = GISTerrainLoaderTextureGenerator.GetTextureFolder(Prefs.TerrainFilePath);

                    if (Prefs.textureloadingMode == TexturesLoadingMode.AutoDetection)
                    {
                        if (MipmapTextureFolderExists)
                        {
                            var TextureFolderPath = Path.Combine(Path.GetDirectoryName(Prefs.TerrainFilePath), Path.GetFileNameWithoutExtension(Prefs.TerrainFilePath) + Prefs.Settings_SO.MipmapTextureFolderName + "/Level_0");

                            string[] Tiles = Directory.GetFiles(TextureFolderPath, "*.*", SearchOption.AllDirectories).Where(f => Prefs.Settings_SO.SupportedTextures.Contains(new FileInfo(f).Extension, StringComparer.OrdinalIgnoreCase)).ToArray();

                            if (Tiles.Length > 0)
                            {
                                TextureSourceFormat TextureSourceSoft = null;

                                bool IsCorrectFormat = GISTerrainLoaderTextureGenerator.TextureSourceName(Tiles, Prefs.Settings_SO, out TextureSourceSoft);

                                if (IsCorrectFormat)
                                {
                                    var MainMipmapFolder = Path.Combine(Path.GetDirectoryName(Prefs.TerrainFilePath), Path.GetFileNameWithoutExtension(Prefs.TerrainFilePath) + Prefs.Settings_SO.MipmapTextureFolderName);
                                    var NewMipmapFolder = Path.Combine(MainMipmapFolder, "MipmapTextures");
                                    if (!Directory.Exists(NewMipmapFolder))
                                    {
                                        Directory.CreateDirectory(NewMipmapFolder);
                                        AssetDatabase.Refresh();
                                    }

                                    for (int index = 0; index < GeneratedContainer.terrains.Length; index++)
                                    {

                                        if (index < terrains.Length)
                                        {
                                            int x = index % Prefs.terrainCount.x;
                                            int y = index / Prefs.terrainCount.x;

                                            List<Texture2D> sources = new List<Texture2D>();

                                            string Format = TextureSourceSoft.Format.Replace("{x}", "{0}").Replace("{y}", "{1}");
                                            string TileName = string.Format(Format, x, y);

                                            for (int level = 0; level < TextureSourceformat.MipmapLevel - 1; level++)
                                            {
                                                var TilePath = Path.Combine(Path.GetDirectoryName(Prefs.TerrainFilePath), Path.GetFileNameWithoutExtension(Prefs.TerrainFilePath) + Prefs.Settings_SO.MipmapTextureFolderName + "/Level_" + level + "/" + TileName + ".png");
                                                var tex = GISTerrainLoaderTextureGenerator.LoadedTextureTile(TilePath);

                                                sources.Add(tex);

                                            }

                                            var Maintexture = new Texture2D(sources[0].width, sources[0].height);
                                            Maintexture.name = sources[0].name;
                                            Maintexture.SetPixels(sources[0].GetPixels());
                                            Maintexture.Apply();

                                            for (int i = 1; i < sources.Count; i++)
                                            {
                                                var pixels = sources[i].GetPixels();
                                                Maintexture.SetPixels(pixels, i);
                                            }

                                            Maintexture.Apply(false, false);
                                            var MipmapTexturePath = Path.Combine(NewMipmapFolder, TileName + ".asset");

                                            String[] extract = Regex.Split(MipmapTexturePath, "Assets");
                                            String main = "Assets" + extract[1].TrimEnd('\\');

                                            AssetDatabase.CreateAsset(Maintexture, main);

                                            AssetDatabase.Refresh();

                                            await GISTerrainLoaderTextureGenerator.EditorAddMipmapTextureToTerrain(NewMipmapFolder, Maintexture, ResTextureFolder, Prefs.Settings_SO, TextureSourceSoft, terrains[x, y]);

                                            Resources.UnloadUnusedAssets();
                                            GC.Collect();
                                            GC.WaitForPendingFinalizers();

                                            OnProgress("Generating MipmapTextures ", (index + 1) * 100 / (Prefs.terrainCount.x * Prefs.terrainCount.y));


                                        }
                                        else
                                        {
                                            s_progress = 0;

                                            return;
                                        }
                                    }
                                }

                            }
                        }

                    }
                    break;

                case TextureMode.MultiTexture:

                    TextureSourceformat = null;

                    bool MainTextureFolderExist = GISTerrainLoaderTextureGenerator.CheckForTerrainTextureFolder(Prefs.TerrainFilePath, out TextureSourceformat);

                    ResTextureFolder = GISTerrainLoaderTextureGenerator.GetTextureFolder(Prefs.TerrainFilePath);

                    List<string> ResTexturesFolders = GISTerrainLoaderTextureGenerator.GetResTextureFolders(Prefs.TerrainFilePath);
                    List<string> FullTexturesFolders = GISTerrainLoaderTextureGenerator.GetFullTextureFolders(Prefs.TerrainFilePath);

                    if (Prefs.textureloadingMode == TexturesLoadingMode.Manual)
                    {
                        var FolderTiles_count = GISTerrainLoaderTextureGenerator.GetTilesNumberInTextureFolder(Prefs.TerrainFilePath);

                        if (Prefs.terrainCount != FolderTiles_count)
                        {
                            if (FolderTiles_count == Vector2.one)
                            {
                                await GISTerrainLoaderTextureGenerator.SplitTex(Prefs.TerrainFilePath, Prefs.terrainCount);
                                AssetDatabase.Refresh();
                            }
                            else
                            {
                                if (FolderTiles_count.x > 1 || FolderTiles_count.y > 1)
                                {
                                    GISTerrainLoaderTextureGenerator.CombienTerrainTextures(Prefs.TerrainFilePath);
                                    AssetDatabase.Refresh();

                                    GISTerrainLoaderTextureGenerator.SplitTex(Prefs.TerrainFilePath, Prefs.terrainCount).Wait();
                                    AssetDatabase.Refresh();

                                    Prefs.textureloadingMode = TexturesLoadingMode.AutoDetection;
                                }

                            }

                        }
                        else
                            Prefs.textureloadingMode = TexturesLoadingMode.AutoDetection;
                    }

                    if (Prefs.textureloadingMode == TexturesLoadingMode.AutoDetection)
                    {
                        if (MainTextureFolderExist)
                        {
                            if (FullTexturesFolders.Count > 0)
                            {
                                int FolderIndex = 0;

                                foreach (var TextureFolderPath in FullTexturesFolders)
                                {

                                    var Tiles = GISTerrainLoaderTextureGenerator.GetTextureTiles(TextureFolderPath, Prefs.Settings_SO);

                                    if (Tiles.Length > 0)
                                    {
                                        TextureSourceFormat TextureSourceSoft = null;

                                        bool IsCorrectFormat = GISTerrainLoaderTextureGenerator.TextureSourceName(Tiles, Prefs.Settings_SO, out TextureSourceSoft);

                                        if (IsCorrectFormat)
                                        {

                                            for (int index = 0; index < GeneratedContainer.terrains.Length; index++)
                                            {
                                                if (index < terrains.Length)
                                                {
                                                    int x = index % Prefs.terrainCount.x;
                                                    int y = index / Prefs.terrainCount.x;

                                                    OnProgress("Generating Textures ", (index + 1) * 100 / (Prefs.terrainCount.x * Prefs.terrainCount.y));

                                                    await GISTerrainLoaderTextureGenerator.AddMultiTextureToTerrain(FolderIndex, TextureFolderPath, ResTexturesFolders.ToArray(), Prefs, TextureSourceSoft, terrains[x, y]);

                                                }
                                                else
                                                {
                                                    s_progress = 0;

                                                    return;
                                                }
                                            }
                                        }

                                    }
                                    FolderIndex++;
                                }
                            }


                        }

                    }
                    break;
                case TextureMode.WithoutTexture:

                    if (Prefs.UseTerrainEmptyColor == OptionEnabDisab.Enable)
                    {
                        Material mat = new Material(Shader.Find("Standard"));
                        mat.SetColor("_Color", Prefs.TerrainEmptyColor);

                        for (int index = 0; index < GeneratedContainer.terrains.Length; index++)
                        {
                            if (index < terrains.Length)
                            {

                                int x = index % Prefs.terrainCount.x;
                                int y = index / Prefs.terrainCount.x;

                                OnProgress("Generating Terrain Color ", index * 100 / (Prefs.terrainCount.x * Prefs.terrainCount.y));
#if UNITY_2018
                                terrains[x, y].terrain.materialType = Terrain.MaterialType.Custom;
#else
                                terrains[x, y].terrain.materialTemplate = mat;
#endif

                            }
                            else
                            {
                                s_progress = 0;
                                return;
                            }
                        }

                    }
                    else
                    {
                        return;
                    }
                    break;

                case TextureMode.Splatmapping:

                    for (int index = 0; index < GeneratedContainer.terrains.Length; index++)
                    {
                        if (index < terrains.Length)
                        {
                            int x = index % Prefs.terrainCount.x;
                            int y = index / Prefs.terrainCount.x;

                            OnProgress("Generating Splatmaps ", index * 100 / (Prefs.terrainCount.x * Prefs.terrainCount.y));

                            GISTerrainLoaderSplatMapping.SetTerrainSpaltMap(Prefs, terrains[x, y]);

                        }
                        else
                        {
                            s_progress = 0;

                            return;
                        }
                    }

                    break;
                case TextureMode.ShadedRelief:

                    for (int index = 0; index < GeneratedContainer.terrains.Length; index++)
                    {
                        if (index < terrains.Length)
                        {
                            int x = index % Prefs.terrainCount.x;
                            int y = index / Prefs.terrainCount.x;

                            OnProgress("Generating Terrain Shader ", index * 100 / (Prefs.terrainCount.x * Prefs.terrainCount.y));

                            var Tile = terrains[x, y];

                            await GISTerrainLoaderTerrainShader.GenerateShadedTextureEditor(Prefs, Tile);

                        }
                        else
                        {
                            s_progress = 0;

                            return;
                        }
                    }


                    break;

#if RDST
                case TextureMode.RasterDrivenSplatmaps:
                    
                    OnProgress("Applying Raster-Driven Splatmaps", 0);
 
                    if (Prefs.RDST_layerData == null || Prefs.RDST_layerData == null)
                    {
                        Debug.LogWarning("RDST: No configuration or layer data found. Skipping splatmap application.");
                        break;
                    }
                    
                    if (Prefs.RDST_layerData.rasterMasks.Count == 0)
                    {
                        Debug.LogWarning("RDST: No raster masks configured. Skipping splatmap application.");
                        break;
                    }
                    
                    for (int index = 0; index < GeneratedContainer.terrains.Length; index++)
                    {
                        if (index < terrains.Length)
                        {
                            int x = index % Prefs.terrainCount.x;
                            int y = index / Prefs.terrainCount.x;
                            
                            OnProgress("Applying Raster Splatmaps", (index + 1) * 100 / (Prefs.terrainCount.x * Prefs.terrainCount.y));
                            
                            var terrain = terrains[x, y];
                            
                            if (terrain != null && terrain.terrain != null)
                            {
                                string errorMessage;
                                bool success = GISTech.GISTerrainLoader.RasterSplatmap.RDSTProcessor.ProcessRasterSplatmaps(
                                    terrain.terrain,
                                    Prefs.RDST_layerData,
                                    Prefs,
                                    out errorMessage);
                                
                                if (!success)
                                {
                                    Debug.LogError($"RDST: Failed to apply splatmaps to terrain [{x},{y}]: {errorMessage}");
                                }
                                //else
                                //{
                                //    Debug.Log($"<color=cyan>RDST: Successfully applied splatmaps to terrain [{x},{y}]</color>");
                                //}
                            }
                            
                            await Task.Yield();
                        }
                        else
                        {
                            s_progress = 0;
                            return;
                        }
                    }
                    
                    break;
#endif
            }


        }
        private async Task GenerateBackground()
        {
            if (Prefs.TerrainBackground == OptionEnabDisab.Enable)
            {
                var MainTexture = GISTerrainLoaderTextureGenerator.CaptureContainerTexture(GeneratedContainer, Prefs);

                Color32[] SourcePix = MainTexture.GetPixels32();
                SourcePix = GISTerrainLoaderTextureGenerator.AdjustBrightnessContrast(SourcePix, 0.73f);

                GISTerrainLoaderPrefs m_Prefs = new GISTerrainLoaderPrefs();
                m_Prefs.terrainCount = new Vector2Int(1, 1);

                var terrain_Bottom = GeneratedContainer.CreateBackgroundTerrainTile(Prefs, 0, -1);
                terrain_Bottom.container = GeneratedContainer;
                terrain_Bottom.Number = new Vector2Int(0, 0);
                await ElevationInfo.GenerateHeightMap(m_Prefs, terrain_Bottom, TerrainSide.Bottom);
                GISTerrainLoaderTextureGenerator.AddTextureToTerrain(GISTerrainLoaderTextureGenerator.OrientedTexture(SourcePix, TerrainSide.Bottom, Prefs.TerrainBackgroundTextureResolution, GeneratedContainer.DestinationTerrainPath), terrain_Bottom, false);

                var terrain_Right = GeneratedContainer.CreateBackgroundTerrainTile(Prefs, 1, 0);
                terrain_Right.container = GeneratedContainer;
                terrain_Right.Number = new Vector2Int(0, 0);
                await ElevationInfo.GenerateHeightMap(m_Prefs, terrain_Right, TerrainSide.Right);
                GISTerrainLoaderTextureGenerator.AddTextureToTerrain(GISTerrainLoaderTextureGenerator.OrientedTexture(SourcePix, TerrainSide.Right, Prefs.TerrainBackgroundTextureResolution, GeneratedContainer.DestinationTerrainPath), terrain_Right, false);

                var terrain_Top = GeneratedContainer.CreateBackgroundTerrainTile(Prefs, 0, 1);
                terrain_Top.container = GeneratedContainer;
                terrain_Top.Number = new Vector2Int(0, 0);
                await ElevationInfo.GenerateHeightMap(m_Prefs, terrain_Top, TerrainSide.Top);
                GISTerrainLoaderTextureGenerator.AddTextureToTerrain(GISTerrainLoaderTextureGenerator.OrientedTexture(SourcePix, TerrainSide.Top, Prefs.TerrainBackgroundTextureResolution, GeneratedContainer.DestinationTerrainPath), terrain_Top, false);

                ////Co
                var terrain_Left = GeneratedContainer.CreateBackgroundTerrainTile(Prefs, -1, 0);
                terrain_Left.container = GeneratedContainer;
                terrain_Left.Number = new Vector2Int(0, 0);
                await ElevationInfo.GenerateHeightMap(m_Prefs, terrain_Left, TerrainSide.Left);
                GISTerrainLoaderTextureGenerator.AddTextureToTerrain(GISTerrainLoaderTextureGenerator.OrientedTexture(SourcePix, TerrainSide.Left, Prefs.TerrainBackgroundTextureResolution, GeneratedContainer.DestinationTerrainPath), terrain_Left, false);

                var terrain_TopRight = GeneratedContainer.CreateBackgroundTerrainTile(Prefs, 1, 1);
                terrain_TopRight.container = GeneratedContainer;
                terrain_TopRight.Number = new Vector2Int(0, 0);
                await ElevationInfo.GenerateHeightMap(m_Prefs, terrain_TopRight, TerrainSide.TopRight);
                GISTerrainLoaderTextureGenerator.AddTextureToTerrain(GISTerrainLoaderTextureGenerator.OrientedTexture(SourcePix, TerrainSide.TopRight, Prefs.TerrainBackgroundTextureResolution, GeneratedContainer.DestinationTerrainPath), terrain_TopRight, false);

                var terrain_TopLeft = GeneratedContainer.CreateBackgroundTerrainTile(Prefs, -1, 1);
                terrain_TopLeft.container = GeneratedContainer;
                terrain_TopLeft.Number = new Vector2Int(0, 0);
                await ElevationInfo.GenerateHeightMap(m_Prefs, terrain_TopLeft, TerrainSide.TopLeft);
                GISTerrainLoaderTextureGenerator.AddTextureToTerrain(GISTerrainLoaderTextureGenerator.OrientedTexture(SourcePix, TerrainSide.TopLeft, Prefs.TerrainBackgroundTextureResolution, GeneratedContainer.DestinationTerrainPath), terrain_TopLeft, false);

                var terrain_BottomLeft = GeneratedContainer.CreateBackgroundTerrainTile(Prefs, -1, -1);
                terrain_BottomLeft.container = GeneratedContainer;
                terrain_BottomLeft.Number = new Vector2Int(0, 0);
                await ElevationInfo.GenerateHeightMap(m_Prefs, terrain_BottomLeft, TerrainSide.BottomLeft);
                GISTerrainLoaderTextureGenerator.AddTextureToTerrain(GISTerrainLoaderTextureGenerator.OrientedTexture(SourcePix, TerrainSide.BottomLeft, Prefs.TerrainBackgroundTextureResolution, GeneratedContainer.DestinationTerrainPath), terrain_BottomLeft, false);

                var terrain_BottomRight = GeneratedContainer.CreateBackgroundTerrainTile(Prefs, 1, -1);
                terrain_BottomRight.container = GeneratedContainer;
                terrain_BottomRight.Number = new Vector2Int(0, 0);
                await ElevationInfo.GenerateHeightMap(m_Prefs, terrain_BottomRight, TerrainSide.BottomRight);
                GISTerrainLoaderTextureGenerator.AddTextureToTerrain(GISTerrainLoaderTextureGenerator.OrientedTexture(SourcePix, TerrainSide.BottomRight, Prefs.TerrainBackgroundTextureResolution, GeneratedContainer.DestinationTerrainPath), terrain_BottomRight, false);
            }
        }
        private async Task GenerateVectorData()
        {
            List<GISTerrainLoaderGeoVectorData> LoadedGeoDataFiles = new List<GISTerrainLoaderGeoVectorData>();

            bool isGeoFile = Prefs.IsVectorGenerationEnabled(LoadedFileExtension, Prefs.EnableVectorGenerator);

#if GISTechAdvancedVectorGenerator
            if (!isGeoFile)
                isGeoFile = Prefs.IsVectorGenerationEnabled(LoadedFileExtension, Prefs.EnableAdvancedVectorGenerator);
#endif

            if (isGeoFile)
                LoadedGeoDataFiles = GISTerrainLoaderVectorParser.LoadVectorFiles(Prefs, GeneratedContainer);


            if (Prefs.EnableVectorGenerator == OptionEnabDisab.Enable)
            {
                if (Prefs.terrainDimensionMode == TerrainDimensionsMode.AutoDetection)
                {
                    if (Prefs.EnableGeoPointGeneration == OptionEnabDisab.Enable)
                        Prefs.LoadAllPointPrefabs();

                    if (Prefs.EnableRoadGeneration == OptionEnabDisab.Enable)
                        Prefs.LoadAllRoadPrefabs(Prefs.RoadGenerator);

                    if (Prefs.EnableTreeGeneration == OptionEnabDisab.Enable)
                        GISTerrainLoaderTreeGenerator.AddTreePrefabsToTerrains(GeneratedContainer, Prefs);

                    if (Prefs.EnableGrassGeneration == OptionEnabDisab.Enable)
                        GISTerrainLoaderGrassGenerator.AddDetailsLayersToTerrains(GeneratedContainer, Prefs);

                    if (Prefs.EnableBuildingGeneration == OptionEnabDisab.Enable)
                        Prefs.LoadAllBuildingPrefabs();

                    if (Prefs.EnableWaterGeneration == OptionEnabDisab.Enable)
                        Prefs.LoadAllWaterPrefabs();

                    if (Prefs.EnableLandParcelGeneration == OptionEnabDisab.Enable)
                        Prefs.LoadAllLandParcelPrefabs();

#if GISTerrainLoaderFenceGenerator
                    if (Prefs.EnableFenceGenerator == OptionEnabDisab.Enable)
                        Prefs.LoadAllFancesPrefabs();
#endif



                    foreach (var GeoDataFile in LoadedGeoDataFiles)
                    {
                        if (Prefs.EnableGeoPointGeneration == OptionEnabDisab.Enable)
                        {
                            var GeoPoints = GISTerrainLoaderDataFilter.GetGeoVectorPointsData(GeoDataFile, Prefs.VectorParameters_SO.Attributes_Points);
                            GISTerrainLoaderGeoPointGenerator.GenerateGeoPoint(GeneratedContainer, GeoPoints, Prefs);
                        }

                        if (Prefs.EnableRoadGeneration == OptionEnabDisab.Enable)
                        {
                            var GeoLines = GISTerrainLoaderDataFilter.GetGeoVectorLinesData(GeoDataFile, Prefs.VectorParameters_SO.Attributes_Roads);
                            GISTerrainLoaderRoadsGenerator.GenerateTerrainRoades(GeneratedContainer, GeoLines, Prefs);
                        }

                        if (Prefs.EnableTreeGeneration == OptionEnabDisab.Enable)
                        {
                            if (Prefs.TreePrefabs.Count > 0)
                            {
                                var GeoPolygons = GISTerrainLoaderDataFilter.GetGeoVectorPolyData(GeoDataFile, Prefs.VectorParameters_SO.Attributes_Trees);

                                GISTerrainLoaderTreeGenerator.GenerateTrees(GeneratedContainer, GeoPolygons, Prefs);
                            }
                            else
                                Debug.LogError("Error : Tree Prefabs List is empty ");
                        }

                        if (Prefs.EnableGrassGeneration == OptionEnabDisab.Enable)
                        {
                            if (Prefs.GrassPrefabs.Count > 0)
                            {
                                var GeoPolygons = GISTerrainLoaderDataFilter.GetGeoVectorPolyData(GeoDataFile, Prefs.VectorParameters_SO.Attributes_Grass);

                                GISTerrainLoaderGrassGenerator.GenerateGrass(GeneratedContainer, GeoPolygons, Prefs);
                            }
                            else
                                Debug.LogError("Error : Grass Prefabs List is empty ");

                        }

                        if (Prefs.EnableBuildingGeneration == OptionEnabDisab.Enable)
                        {
                            var GeoPolygons = GISTerrainLoaderDataFilter.GetGeoVectorPolyData(GeoDataFile, Prefs.VectorParameters_SO.Attributes_Buildings);

                            GISTerrainLoaderBuildingGenerator.GenerateBuildings(GeneratedContainer, GeoPolygons, Prefs);
                        }


                        if (Prefs.EnableWaterGeneration == OptionEnabDisab.Enable)
                        {
                            var GeoPolygons = GISTerrainLoaderDataFilter.GetGeoVectorPolyData(GeoDataFile, Prefs.VectorParameters_SO.Attributes_Water);
                            var m_GeoData = new GISTerrainLoaderGeoVectorData();
                            m_GeoData.GeoPolygons = GeoPolygons;
                            var waterGenerator = new GISTerrainLoaderWaterGenerator();
                            waterGenerator.Generate(Prefs, m_GeoData, GeneratedContainer);

                        }

                        if (Prefs.EnableLandParcelGeneration == OptionEnabDisab.Enable)
                        {
                            var GeoPolygons = GISTerrainLoaderDataFilter.GetGeoVectorPolyData(GeoDataFile, Prefs.VectorParameters_SO.Attributes_LandParcel);

                            var m_GeoData = new GISTerrainLoaderGeoVectorData();
                            m_GeoData.GeoPolygons = GeoPolygons;
                            var LandParcelGenerator = new GISTerrainLoaderLandParcelGenerator();
                            LandParcelGenerator.Generate(Prefs, m_GeoData, GeneratedContainer);
                        }



                        //------------------- Add-ons --------------------------// 


#if GISTerrainLoaderFenceGenerator

                        if (Prefs.EnableFenceGenerator == OptionEnabDisab.Enable)
                        {
                            var GeoLines = GISTerrainLoaderDataFilter.GetGeoVectorLinesData(GeoDataFile, Prefs.VectorParameters_SO.Attributes_Fences);

                            var m_GeoData = new GISTerrainLoaderGeoVectorData();
                            m_GeoData.GeoLines = GeoLines;
                            var FenceGenerator = new GISTerrainLoaderFenceGenerator();
 
                            FenceGenerator.Generate(Prefs, m_GeoData, GeneratedContainer);
                        }
#endif

                    }

                }
                else
                {
                    Debug.LogError("Vector Data Available only with Real World DEM Data (Set Terrain Dimension Mode to Auto)");
                }
            }



#if GISTechAdvancedVectorGenerator

            if (Prefs.EnableAdvancedVectorGenerator == OptionEnabDisab.Enable)
            {
                foreach (var GeoDataFile in LoadedGeoDataFiles)
                {

                    if (Prefs.EnablePointGenerator == OptionEnabDisab.Enable)
                    {
                        if (Prefs.SymbolsDic.PointsFeatures != null)
                        {
                            var GeoPoints = GISTerrainLoaderDataFilter.AVG_FilterGeoPointsData(GeoDataFile, Prefs);
                            var m_GeoData = new GISTerrainLoaderGeoVectorData(GeoPoints);
                            GISTerrainLoaderPointGenerator pointGenerator = new GISTerrainLoaderPointGenerator();
                            pointGenerator.Generate(Prefs, m_GeoData, GeneratedContainer);
                        }
                        else
                        {
                            Debug.LogError("Points Features Dictionary is null");
                        }
                    }

                    if (Prefs.EnableLineGenerator == OptionEnabDisab.Enable)
                    {
                        if (Prefs.SymbolsDic.LineFeatures != null)
                        {
                            if (Prefs.SymbolsDic.LineFeatures.Count > 0)
                            {
                                var GeoLines = GISTerrainLoaderDataFilter.AVG_FilterGeoLinesData(GeoDataFile, Prefs);

                                var m_GeoData = new GISTerrainLoaderGeoVectorData();
                                m_GeoData.GeoLines = GeoLines;

                                var LineGenerator = new GISTerrainLoaderLineGenerator();
                                LineGenerator.Generate(Prefs, m_GeoData, GeneratedContainer);
                            }
                        }
                        else
                        {
                            Debug.LogError("Lines Features Dictionary is null");
                        }
                    }

                    if (Prefs.EnablePolygonGenerator == OptionEnabDisab.Enable)
                    {
                        if (Prefs.SymbolsDic.PolygonFeatures != null)
                        {
                            if (Prefs.SymbolsDic.PolygonFeatures.Count > 0)
                            {

                                var GeoPolygons = GISTerrainLoaderDataFilter.AVG_FilterGeoPolyData(GeoDataFile, Prefs);
                                var m_GeoData = new GISTerrainLoaderGeoVectorData();
                                m_GeoData.GeoPolygons = GeoPolygons;

                                var PolygonGenerator = new GISTerrainLoaderPolygonGenerator();
                                PolygonGenerator.Generate(Prefs, m_GeoData, GeneratedContainer);



                            }
                        }
                        else
                        {
                            Debug.LogError("Polygons Features Dictionary is null");
                        }

                    }
                }

            }
#endif

            await Task.Delay(TimeSpan.FromSeconds(0.001));
        }
        private void DrawRedSeparator()
        {
            var rect = EditorGUILayout.GetControlRect(false, 4);
            EditorGUI.DrawRect(rect, new Color(0.8f, 0.1f, 0.1f)); // RGB for red
        }
        private void DrawRedLabeledSeparator(string label)
        {
            var rect = EditorGUILayout.GetControlRect(false, 20);
            EditorGUI.DrawRect(rect, new Color(0.8f, 0.1f, 0.1f));

            var labelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            EditorGUI.LabelField(rect, label, labelStyle);
        }
        private async Task FreeUpMemory()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Resources.UnloadUnusedAssets();
            await Task.Delay(TimeSpan.FromSeconds(0.001));
        }
        private void Finish()
        {
            foreach (GISTerrainTile item in terrains)
                item.terrain.Flush();

            if (GeneratedContainer != null)
            {
                var time = ((double)(timer.Elapsed.TotalSeconds)).ToString("0.00 s");
                Debug.Log("<color=green><size=14> Terrain Generated Successfully. [</size></color> " + "<color=green><size=14>" + time + "]</size></color>");
                Debug.Log("<color=green><size=14> Bounds : Upper Left " + GeneratedContainer.data.TLPoint_LatLon + ", Lower Right " + GeneratedContainer.data.DRPoint_LatLon + ", Lenght / Width  " + Math.Round(GeneratedContainer.data.Dimensions.x, 2) + " X " + Math.Round(GeneratedContainer.data.Dimensions.y, 2) + " [Km] " + "</size></color>");
            }

            s_phase = "";
            s_progress = 0;

            State = GeneratorState.idle;

#if GISVirtualTexture
            AddGISVirtualTexture();
#endif

#if RDST
            if (Prefs.textureMode == TextureMode.RasterDrivenSplatmaps)
            {
                RDST_ApplyRasterSplatmapsAutomatic();
            }
#endif
        }
        #endregion
        #region Events
        void OnError(string errorMsg)
        {
            if (!Application.isPlaying)
            {
                if (!string.IsNullOrEmpty(errorMsg))
                    Debug.LogError(errorMsg);

                s_phase = "";
                s_progress = 0;

                Repaint();
                State = GeneratorState.idle;
                Repaint();
            }

        }
        void OnProgress(string phasename, float value)
        {
            Repaint();
            s_phase = phasename;
            s_progress = value;
            Repaint();

        }
        private void OnDisable()
        {
            GISTerrainLoaderFloatReader.OnReadError -= OnError;
            GISTerrainLoaderTIFFLoader.OnReadError -= OnError;
            GISTerrainLoaderTerraGenLoade.OnReadError -= OnError;
            GISTerrainLoaderDEMPngLoader.OnReadError -= OnError;
            GISTerrainLoaderRawLoader.OnReadError -= OnError;
            GISTerrainLoaderASCILoader.OnReadError -= OnError;


#if GISTerrainLoaderPdal
            GISTerrainLoaderLASLoader.OnReadError -= OnError;
#endif

            GISTerrainLoaderFloatReader.OnProgress -= OnProgress;
            GISTerrainLoaderTIFFLoader.OnProgress -= OnProgress;
            GISTerrainLoaderTerraGenLoade.OnProgress -= OnProgress;
            GISTerrainLoaderDEMPngLoader.OnProgress -= OnProgress;
            GISTerrainLoaderRawLoader.OnProgress -= OnProgress;
            GISTerrainLoaderASCILoader.OnProgress -= OnProgress;


            SavePrefs();
        }
        private void OnDestroy()
        {
            SavePrefs();
        }
        private void OnEnable()
        {
            if (Prefs == null)
                Prefs = new GISTerrainLoaderPrefs();


            Prefs.LoadSettings();

            OnTerrainFileChanged(DEMFile);

            window = this;

#if GISDataDownloader
            GISTech.GISDataDownloader.GISDataDownloaderDownloader.OnDownloadComplete += OnGDDDownloadComplete;
#endif

            GISTerrainLoaderFloatReader.OnReadError += OnError;
            GISTerrainLoaderTIFFLoader.OnReadError += OnError;
            GISTerrainLoaderTerraGenLoade.OnReadError += OnError;
            GISTerrainLoaderDEMPngLoader.OnReadError += OnError;
            GISTerrainLoaderRawLoader.OnReadError += OnError;
            GISTerrainLoaderASCILoader.OnReadError += OnError;
            GISTerrainLoaderHGTLoader.OnReadError += OnError;
            GISTerrainLoaderBILReader.OnReadError += OnError;

            GISTerrainLoaderDEMLoader.OnReadError += OnError;

#if GISTerrainLoaderPdal
            GISTerrainLoaderLASLoader.OnReadError += OnError;
#endif

            GISTerrainLoaderFloatReader.OnProgress += OnProgress;
            GISTerrainLoaderTIFFLoader.OnProgress += OnProgress;
            GISTerrainLoaderTerraGenLoade.OnProgress += OnProgress;
            GISTerrainLoaderDEMPngLoader.OnProgress += OnProgress;
            GISTerrainLoaderRawLoader.OnProgress += OnProgress;
            GISTerrainLoaderASCILoader.OnProgress += OnProgress;
            GISTerrainLoaderHGTLoader.OnProgress += OnProgress;
            GISTerrainLoaderBILReader.OnProgress += OnProgress;


            LoadPrefs();

            if (m_terrain == null)
                m_terrain = LoadTexture("GTL_Terrain");

            if (m_downloaExamples == null)
                m_downloaExamples = LoadTexture("GTL_DownloaExamples");

            if (m_helpIcon == null)
                m_helpIcon = LoadTexture("GTL_HelpIcon");

            if (m_resetPrefs == null)
                m_resetPrefs = LoadTexture("GTL_ResetPrefs");

            if (m_aboutIcon == null)
                m_aboutIcon = LoadTexture("GTL_AboutPrefs");

            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.grey);
            texture.Apply();

            ToolbarSkin.normal.background = texture;
            ToolbarSkin.normal.textColor = Color.white;
            ToolbarSkin.fontSize = 13;
            ToolbarSkin.fontStyle = FontStyle.Bold;
            ToolbarSkin.alignment = TextAnchor.MiddleCenter;

        }
        private void OnTerrainFileChanged(UnityEngine.Object terrain)
        {

            var TerrainFilePath = Path.Combine(Path.GetDirectoryName(Application.dataPath), AssetDatabase.GetAssetPath(DEMFile));


            if (File.Exists(TerrainFilePath))
            {
                var fileExtension = Path.GetExtension(TerrainFilePath);

                if (Prefs.Settings_SO.GeoFile.Contains(fileExtension))
                {
                    Prefs.TerrainHasDimensions = true;

                    if (fileExtension == ".tif" || fileExtension == ".tiff" || fileExtension == ".las")
                    {
                        ShowSubRegion = true;

#if DotSpatial
                        ShowProjectionMode = true;
#endif
                        if (fileExtension == ".tif" || fileExtension == ".tiff")
                            ShowTiffElevationSourceMode = true;
                    }
                    else
                    {
#if DotSpatial
                        ShowProjectionMode = false;
#endif

                        ShowTiffElevationSourceMode = false;
                    }

                }
                else
                {
                    Prefs.TerrainHasDimensions = false;
                }

                if (!Prefs.TerrainHasDimensions)
                {
                    if (fileExtension == ".raw")
                        ShowRawParameters = true;

                    ShowSubRegion = false;
                    Prefs.terrainDimensionMode = TerrainDimensionsMode.Manual;
                }
                else
                {
                    ShowRawParameters = false;
                    ShowSubRegion = true;
                    Prefs.terrainDimensionMode = TerrainDimensionsMode.AutoDetection;
                }

                Prefs.TerrainFilePath = TerrainFilePath;

            }
            else
            {
                Prefs.TerrainFilePath = "";
                ShowRawParameters = false;
                Prefs.terrainDimensionMode = TerrainDimensionsMode.AutoDetection;
                ShowSubRegion = false;
                Prefs.TerrainHasDimensions = true;
            }
        }
        #endregion
        #region Other
        private void CheckTerrainMaterials()
        {

            if (Prefs.terrainMaterialMode == TerrainMaterialMode.Standard)
            {
                Prefs.terrainMaterial = new Material((Material)Resources.Load("Materials/Default-Terrain-Standard", typeof(Material)));

                if (Prefs.terrainMaterial == null)
                    Debug.LogError("Standard terrain material null or standard terrain material not found in 'Resources/Materials/Default-Terrain-Standard' ");
            }

            if (Prefs.terrainMaterialMode == TerrainMaterialMode.HeightmapColorRamp)
            {
                if (Prefs.textureMode == TextureMode.WithTexture || Prefs.textureMode == TextureMode.MultiTexture || Prefs.textureMode == TextureMode.MultiLayers)
                    Prefs.textureMode = TextureMode.WithoutTexture;

                if (Prefs.UseTerrainEmptyColor == OptionEnabDisab.Enable)
                    Prefs.UseTerrainEmptyColor = OptionEnabDisab.Disable;

                Prefs.terrainMaterial = new Material((Material)Resources.Load("Materials/TerrainShaders/HeightmapColorRamp", typeof(Material)));

                if (Prefs.terrainMaterial)
                {
                    Prefs.terrainMaterial.SetFloat("_TerrainHeight", GeneratedContainer.ContainerSize.y);
                    Prefs.terrainMaterial.SetFloat("_Brightness", GISTerrainLoaderConstants.Brightness);
                    Prefs.terrainMaterial.SetFloat("_Contrast", GISTerrainLoaderConstants.Contrast);
                }
                else
                    Debug.LogError("HeightmapColorRamp terrain material not found in 'Resources/Materials/TerrainShaders/HeightmapColorRamp' ");
            }

            if (Prefs.terrainMaterialMode == TerrainMaterialMode.ElevationGrayScaleGradient)
            {
                if (Prefs.textureMode == TextureMode.WithTexture || Prefs.textureMode == TextureMode.MultiTexture || Prefs.textureMode == TextureMode.MultiLayers)
                    Prefs.textureMode = TextureMode.WithoutTexture;

                if (Prefs.UseTerrainEmptyColor == OptionEnabDisab.Enable)
                    Prefs.UseTerrainEmptyColor = OptionEnabDisab.Disable;

                Prefs.terrainMaterial = new Material((Material)Resources.Load("Materials/TerrainShaders/ElevationGrayScaleGradient", typeof(Material)));
                Prefs.terrainMaterial.SetFloat("_TerrainHeight", GeneratedContainer.ContainerSize.y);
                Prefs.terrainMaterial.SetFloat("_Brightness", GISTerrainLoaderConstants.Brightness);
                Prefs.terrainMaterial.SetFloat("_Contrast", GISTerrainLoaderConstants.Contrast);

                if (Prefs.terrainMaterial == null)
                    Debug.LogError("ElevationGrayScaleGradient terrain material not found in 'Resources/Materials/TerrainShaders/ElevationGrayScaleGradient' ");
            }

            if (Prefs.terrainMaterialMode == TerrainMaterialMode.HeightmapColorRampContourLines)
            {
                if (Prefs.textureMode == TextureMode.WithTexture || Prefs.textureMode == TextureMode.MultiTexture || Prefs.textureMode == TextureMode.MultiLayers)
                    Prefs.textureMode = TextureMode.WithoutTexture;

                if (Prefs.UseTerrainEmptyColor == OptionEnabDisab.Enable)
                    Prefs.UseTerrainEmptyColor = OptionEnabDisab.Disable;

                Prefs.terrainMaterial = new Material((Material)Resources.Load("Materials/TerrainShaders/HeightmapContourLines", typeof(Material)));

                if (Prefs.terrainMaterial)
                {
                    Prefs.terrainMaterial.SetFloat("_ContourInterval", Prefs.ContourInterval);
                    Prefs.terrainMaterial.SetFloat("_TerrainHeight", GeneratedContainer.ContainerSize.y);
                    Prefs.terrainMaterial.SetFloat("_LineWidth", GISTerrainLoaderConstants.LineWidth);
                    Prefs.terrainMaterial.SetFloat("_Brightness", GISTerrainLoaderConstants.Brightness);
                    Prefs.terrainMaterial.SetFloat("_Contrast", GISTerrainLoaderConstants.Contrast);
                }
                else
                    Debug.LogError("HeightmapContourLines terrain material not found in 'Resources/Materials/TerrainShaders/HeightmapContourLines' ");

            }


        }
        private void CheckTerrainTextures()
        {
            if (Prefs.terrainMaterialMode == TerrainMaterialMode.HeightmapColorRamp || Prefs.terrainMaterialMode == TerrainMaterialMode.HeightmapColorRampContourLines || Prefs.terrainMaterialMode == TerrainMaterialMode.ElevationGrayScaleGradient)
                Prefs.textureMode = TextureMode.WithoutTexture;

            if (Prefs.textureMode == TextureMode.WithTexture || Prefs.textureMode == TextureMode.MultiTexture || Prefs.textureMode == TextureMode.MultiLayers)
            {
                if (Prefs.textureloadingMode == TexturesLoadingMode.AutoDetection)
                {
                    var c_count = GISTerrainLoaderTextureGenerator.GetTilesNumberInTextureFolder(Prefs.TerrainFilePath);

                    Prefs.terrainCount = new Vector2Int((int)c_count.x, (int)c_count.y);

                    if (c_count == Vector2.zero)
                    {
                        Prefs.terrainCount = new Vector2Int(1, 1);
                    }

                }
            }
            if (Prefs.textureMode == TextureMode.MipmapTextures)
            {
                var c_count = GISTerrainLoaderTextureGenerator.GetTilesNumberInMipmapTextureFolder(Prefs.TerrainFilePath);

                Prefs.terrainCount = new Vector2Int((int)c_count.x, (int)c_count.y);

                if (c_count == Vector2.zero)
                {
                    Prefs.terrainCount = new Vector2Int(1, 1);
                }

            }
        }
        private async Task CheckForDimensionAndTiles(bool AutoDim)
        {
            if (Prefs.terrainDimensionMode == TerrainDimensionsMode.AutoDetection)
            {
                if (AutoDim)
                {
                    if (ElevationInfo.data.Dimensions.x == 0 || ElevationInfo.data.Dimensions.y == 0)
                    {
                        OnError("Can't detecte terrain dimension (Check your file projection) and please againe ");
                        return;
                    }
                    else
                    if (ElevationInfo.data.Dimensions != new DVector2(0, 0))
                    {
                        m_terrainDimensions = new Vector2((float)ElevationInfo.data.Dimensions.x, (float)ElevationInfo.data.Dimensions.y);
                    }
                    if (ElevationInfo.data.Tiles != Vector2.zero)
                    {
                        Prefs.terrainCount = new Vector2Int((int)ElevationInfo.data.Tiles.x, (int)ElevationInfo.data.Tiles.y);
                    }
                    else
                    {
                        //OnError();
                    }
                }
                else
                {
                    if (Prefs.TerrainDimensions.x == 0 || Prefs.TerrainDimensions.y == 0)
                    {
                        OnError("Reset Terrain dimensions ... try again  ");
                        return;
                    }
                    else
        if (Prefs.TerrainDimensions != new DVector2(0, 0))
                    {
                        m_terrainDimensions = new Vector2((float)Prefs.TerrainDimensions.x, (float)Prefs.TerrainDimensions.y);
                    }

                    if (ElevationInfo.data.Tiles != Vector2.zero)
                    {
                        Prefs.terrainCount = new Vector2Int((int)ElevationInfo.data.Tiles.x, (int)ElevationInfo.data.Tiles.y);
                    }
                    else
                    {
                        if (Prefs.textureMode == TextureMode.WithTexture || Prefs.textureMode == TextureMode.MultiTexture || Prefs.textureMode == TextureMode.MultiLayers)
                            Debug.LogError("Can't detecte terrain textures folder ... try again");

                        OnError("");
                    }
                }
            }
            else
            {

                if (Prefs.TerrainDimensions.x == 0 || Prefs.TerrainDimensions.y == 0)
                {
                    OnError("Reset Terrain dimensions ... try again  ");
                    return;
                }

                else
    if (Prefs.TerrainDimensions != new DVector2(0, 0))
                {
                    m_terrainDimensions = new Vector2((float)Prefs.TerrainDimensions.x, (float)Prefs.TerrainDimensions.y);
                }

                if (ElevationInfo.data.Tiles != Vector2.zero)
                {
                    Prefs.terrainCount = new Vector2Int((int)ElevationInfo.data.Tiles.x, (int)ElevationInfo.data.Tiles.y);


                }
                else
                {
                    ElevationInfo.data.Tiles = Prefs.terrainCount;
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(0.01));
        }
        private Texture2D LoadTexture(string m_iconeName)
        {
            var tex = new Texture2D(35, 35);

            string[] guids = AssetDatabase.FindAssets(m_iconeName + " t:texture");
            if (guids != null && guids.Length > 0)
            {
                string iconPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                tex = (Texture2D)AssetDatabase.LoadAssetAtPath(iconPath, typeof(Texture2D));
            }

            return tex;
        }

        private UnityEngine.Object[] LoadAll(string path)
        {
            return Resources.LoadAll(path);
        }

        private void GTL_Generate()
        {
            Prefs.TerrainFilePath = Path.Combine(Path.GetDirectoryName(Application.dataPath), AssetDatabase.GetAssetPath(DEMFile));

            if (Prefs.Settings_SO.IsValidTerrainFile(Prefs.TerrainFilePath))
            {

                Prefs.heightmapResolution = Prefs.heightmapResolutions[Prefs.heightmapResolution_index];
                Prefs.TerrainBackgroundHeightmapResolution = Prefs.TerrainBackgroundHeightmapResolutions[Prefs.TerrainBackgroundHeightmapResolution_index];
                Prefs.TerrainBackgroundTextureResolution = Prefs.TerrainBackgroundHeightmapResolutions[Prefs.TerrainBackgroundTextureResolution_index];
                Prefs.TerrainLayer = LayerMask.NameToLayer(Prefs.TerrainLayerSrt[Prefs.TerrainLayer_index]);
                Prefs.detailResolution = Prefs.availableHeights[Prefs.detailResolution_index];
                Prefs.resolutionPerPatch = Prefs.availableHeightsResolutionPrePec[Prefs.resolutionPerPatch_index];
                Prefs.baseMapResolution = Prefs.availableHeights[Prefs.baseMapResolution_index];

                if (!string.IsNullOrEmpty(Prefs.TerrainFilePath))
                {
                    GTLCheckFileConfig();
                }
                else
                {
                    OnError(" Please set DEM File .. Try againe");
                }
            }
        }

        #endregion
        #region SaveLoad
        private void SavePrefs()
        {
            GISTerrainLoaderSaveLoadPrefs.SavePref("ShowMainTerrainFile", ShowMainTerrainFile);

            GISTerrainLoaderSaveLoadPrefs.SavePref("TerrainFilePath", Prefs.TerrainFilePath);
            GISTerrainLoaderSaveLoadPrefs.SavePref("readingMode", (int)Prefs.readingMode);
            GISTerrainLoaderSaveLoadPrefs.SavePref("projectionMode", (int)Prefs.projectionMode);
            GISTerrainLoaderSaveLoadPrefs.SavePref("tiffElevationSource", (int)Prefs.tiffElevationSource);
            GISTerrainLoaderSaveLoadPrefs.SavePref("EPSGCode", Prefs.EPSGCode);
            GISTerrainLoaderSaveLoadPrefs.SavePref("BandsIndex", Prefs.BandsIndex);
            GISTerrainLoaderSaveLoadPrefs.SavePref("SerializeHeightmap", (int)SerializeHeightmap);

            GISTerrainLoaderSaveLoadPrefs.SavePref("ShowCoordinates", ShowCoordinates);
            GISTerrainLoaderSaveLoadPrefs.SavePref("SubRegionUpperLeftCoordiante", Prefs.SubRegionUpperLeftCoordiante);
            GISTerrainLoaderSaveLoadPrefs.SavePref("SubRegionDownRightCoordiante", Prefs.SubRegionDownRightCoordiante);

            ////////////////////////////////////////////////////////////////////////////////
            GISTerrainLoaderSaveLoadPrefs.SavePref("ShowSetTerrainPref", ShowSetTerrainPref);

            GISTerrainLoaderSaveLoadPrefs.SavePref("ShowTexturePref", ShowTexturePref);
            GISTerrainLoaderSaveLoadPrefs.SavePref("ShowSplatmapsTerrainLayer", ShowSplatmapsTerrainLayer);
            GISTerrainLoaderSaveLoadPrefs.SavePref("TerrainElevationMode", (int)Prefs.TerrainElevation);
            GISTerrainLoaderSaveLoadPrefs.SavePref("TerrainExaggeration", Prefs.TerrainExaggeration);
            GISTerrainLoaderSaveLoadPrefs.SavePref("TerrainDimensionMode", (int)Prefs.terrainDimensionMode);
            GISTerrainLoaderSaveLoadPrefs.SavePref("TerrainFixOption", (int)Prefs.TerrainFixOption);
            GISTerrainLoaderSaveLoadPrefs.SavePref("ElevationForNullPoints", (int)Prefs.ElevationForNullPoints);
            GISTerrainLoaderSaveLoadPrefs.SavePref("ElevationValueForNullPoints", (float)Prefs.ElevationValueForNullPoints);
            GISTerrainLoaderSaveLoadPrefs.SavePref("TerrainMaxMinElevation", Prefs.TerrainMaxMinElevation);
            GISTerrainLoaderSaveLoadPrefs.SavePref("UnderWater", (int)Prefs.UnderWater);
            GISTerrainLoaderSaveLoadPrefs.SavePref("TerrainDimensions", Prefs.TerrainDimensions);
            GISTerrainLoaderSaveLoadPrefs.SavePref("TerrainScale", Prefs.terrainScale);
            GISTerrainLoaderSaveLoadPrefs.SavePref("TerrainCount", Prefs.terrainCount);

            ////////////////////////////////////////////////////////////////////////////////
            GISTerrainLoaderSaveLoadPrefs.SavePref("ShowTerrainPref", ShowTerrainPref);
            GISTerrainLoaderSaveLoadPrefs.SavePref("heightmapResolution_index", Prefs.heightmapResolution_index);
            GISTerrainLoaderSaveLoadPrefs.SavePref("detailResolution_index", Prefs.detailResolution_index);
            GISTerrainLoaderSaveLoadPrefs.SavePref("resolutionPerPatch_index", Prefs.resolutionPerPatch_index);
            GISTerrainLoaderSaveLoadPrefs.SavePref("baseMapResolution_index", Prefs.baseMapResolution_index);
            GISTerrainLoaderSaveLoadPrefs.SavePref("PixelErro", Prefs.PixelError);
            GISTerrainLoaderSaveLoadPrefs.SavePref("baseMapDistance", Prefs.BaseMapDistance);
            GISTerrainLoaderSaveLoadPrefs.SavePref("terrainMaterialMode", (int)Prefs.terrainMaterialMode);
            GISTerrainLoaderSaveLoadPrefs.SavePref("terrainMaterialPath", AssetDatabase.GetAssetPath(Prefs.terrainMaterial));
            GISTerrainLoaderSaveLoadPrefs.SavePref("TerrainBaseboards", (int)Prefs.TerrainBaseboards);
            GISTerrainLoaderSaveLoadPrefs.SavePref("TerrainLayerSet", (int)Prefs.TerrainLayerSet);
            GISTerrainLoaderSaveLoadPrefs.SavePref("TerrainLayer", Prefs.TerrainLayer);
            GISTerrainLoaderSaveLoadPrefs.SavePref("TerrainLayer_index", Prefs.TerrainLayer_index);

            GISTerrainLoaderSaveLoadPrefs.SavePref("BorderHigh", (int)Prefs.BorderHigh);
            GISTerrainLoaderSaveLoadPrefs.SavePref("terrainBorderMaterial", new List<UnityEngine.Object>() { Prefs.terrainBorderMaterial });

            GISTerrainLoaderSaveLoadPrefs.SavePref("ContourInterval", Prefs.ContourInterval);

            GISTerrainLoaderSaveLoadPrefs.SavePref("TerrainBackground", (int)Prefs.TerrainBackground);
            GISTerrainLoaderSaveLoadPrefs.SavePref("TerrainBackgroundHeightmapResolution_index", Prefs.TerrainBackgroundHeightmapResolution_index);
            GISTerrainLoaderSaveLoadPrefs.SavePref("TerrainBackgroundTextureResolution_index", Prefs.TerrainBackgroundTextureResolution_index);

            GISTerrainLoaderSaveLoadPrefs.SavePref("AddTextureOffset", (int)Prefs.AddTextureOffset);
            GISTerrainLoaderSaveLoadPrefs.SavePref("TextureOffset", Prefs.TextureOffset);

            ////////////////////////////////////////////////////////////////////////////////
            GISTerrainLoaderSaveLoadPrefs.SavePref("textureMode", (int)Prefs.textureMode);
            GISTerrainLoaderSaveLoadPrefs.SavePref("textureEmptyColor", Prefs.TerrainEmptyColor);
            GISTerrainLoaderSaveLoadPrefs.SavePref("useTerrainEmptyColor", (int)Prefs.UseTerrainEmptyColor);



            GISTerrainLoaderSaveLoadPrefs.SavePref("Slope", Prefs.Slope);
            GISTerrainLoaderSaveLoadPrefs.SavePref("MergeRadius", Prefs.MergeRadius);
            GISTerrainLoaderSaveLoadPrefs.SavePref("MergingFactor", Prefs.MergingFactor);
            GISTerrainLoaderSaveLoadPrefs.SavePref("BaseTerrainLayers", Prefs.BaseTerrainLayers);
            GISTerrainLoaderSaveLoadPrefs.SavePref("TerrainLayers", Prefs.TerrainLayers);

            ////////////////////////////////////////////////////////////////////////////////

            GISTerrainLoaderSaveLoadPrefs.SavePref("ShowSmoothingOpr", ShowSmoothingOpr);
            GISTerrainLoaderSaveLoadPrefs.SavePref("UseTerrainHeightSmoother", (int)Prefs.UseTerrainHeightSmoother);
            GISTerrainLoaderSaveLoadPrefs.SavePref("TerrainHeightSmoothFactor", Prefs.TerrainHeightSmoothFactor);
            GISTerrainLoaderSaveLoadPrefs.SavePref("UseTerrainSurfaceSmoother", (int)Prefs.UseTerrainSurfaceSmoother);
            GISTerrainLoaderSaveLoadPrefs.SavePref("TerrainSurfaceSmoothFactor", Prefs.TerrainSurfaceSmoothFactor);

            ////////////////////////////////////////////////////////////////////////////////

            GISTerrainLoaderSaveLoadPrefs.SavePref("ShowOSMVectorData", ShowVectorDataPanel);
            GISTerrainLoaderSaveLoadPrefs.SavePref("VectorType", (int)Prefs.vectorType);


            GISTerrainLoaderSaveLoadPrefs.SavePref("EnableTreeGeneration", (int)Prefs.EnableTreeGeneration);
            GISTerrainLoaderSaveLoadPrefs.SavePref("TreeScaleFactor", Prefs.TreeScaleFactor);
            GISTerrainLoaderSaveLoadPrefs.SavePref("TreeDistribution", (int)Prefs.TreeDistribution);
            GISTerrainLoaderSaveLoadPrefs.SavePref("TreePrefabs", Prefs.TreePrefabs);
            GISTerrainLoaderSaveLoadPrefs.SavePref("TreeDistance", Prefs.TreeDistance);
            GISTerrainLoaderSaveLoadPrefs.SavePref("BillBoardStartDistance", Prefs.BillBoardStartDistance);

            GISTerrainLoaderSaveLoadPrefs.SavePref("EnableGrassGeneration", (int)Prefs.EnableGrassGeneration);
            GISTerrainLoaderSaveLoadPrefs.SavePref("GrassDistribution", (int)Prefs.GrassDistribution);
            GISTerrainLoaderSaveLoadPrefs.SavePref("GrassScaleFactor", Prefs.GrassScaleFactor);
            GISTerrainLoaderSaveLoadPrefs.SavePref("DetailDistance", Prefs.DetailDistance);
            GISTerrainLoaderSaveLoadPrefs.SavePref("GrassPrefabs", Prefs.GrassPrefabs);

            GISTerrainLoaderSaveLoadPrefs.SavePref("EnableRoadGeneration", (int)Prefs.EnableRoadGeneration);
            GISTerrainLoaderSaveLoadPrefs.SavePref("RoadType", (int)Prefs.RoadGenerator);
            GISTerrainLoaderSaveLoadPrefs.SavePref("RoadRaiseOffset", Prefs.RoadRaiseOffset);
            GISTerrainLoaderSaveLoadPrefs.SavePref("BuildTerrains", (int)Prefs.BuildTerrains);

            GISTerrainLoaderSaveLoadPrefs.SavePref("EnableBuildingGeneration", (int)Prefs.EnableBuildingGeneration);
            GISTerrainLoaderSaveLoadPrefs.SavePref("GenerateBuildingBase", (int)Prefs.GenerateBuildingBase);

            GISTerrainLoaderSaveLoadPrefs.SavePref("EnableWaterGeneration", (int)Prefs.EnableWaterGeneration);
            GISTerrainLoaderSaveLoadPrefs.SavePref("WaterDataSource", (int)Prefs.WaterDataSource);
            GISTerrainLoaderSaveLoadPrefs.SavePref("WaterOffsetY", Prefs.WaterOffsetY);

            GISTerrainLoaderSaveLoadPrefs.SavePref("LandParcelElevationMode", (int)Prefs.LandParcelElevationMode);
            GISTerrainLoaderSaveLoadPrefs.SavePref("LandParcelOffsetY", Prefs.LandParcelOffsetY);
            GISTerrainLoaderSaveLoadPrefs.SavePref("LandParcelPolygonCount", Prefs.LandParcelPolygonCount);
            GISTerrainLoaderSaveLoadPrefs.SavePref("EnableLandParcelGeneration", (int)Prefs.EnableLandParcelGeneration);

            GISTerrainLoaderSaveLoadPrefs.SavePref("EnableGeoPointGeneration", (int)Prefs.EnableGeoPointGeneration);
            GISTerrainLoaderSaveLoadPrefs.SavePref("GeoPointPrefabs", new List<UnityEngine.GameObject>() { Prefs.GeoPointPrefab });
            GISTerrainLoaderSaveLoadPrefs.SavePref("PathPrefabs", new List<GISTerrainLoaderSO_Road>() { Prefs.PathPrefab });

            GISTerrainLoaderSaveLoadPrefs.SavePref("ShowHelpSupport", ShowHelpSupport);

            GISTerrainLoaderSaveLoadPrefs.SavePref("RemovePrvTerrain", (int)Prefs.RemovePrvTerrain);


#if GISTechAdvancedVectorGenerator

            GISTerrainLoaderSaveLoadPrefs.SavePref("ShowAdvancedVectorGenerator", ShowAdvancedVectorGenerator);
            GISTerrainLoaderSaveLoadPrefs.SavePref("EnableAdvancedVectorGenerator", (int)Prefs.EnableAdvancedVectorGenerator);
            GISTerrainLoaderSaveLoadPrefs.SavePref("EnablePointGenerator", (int)Prefs.EnablePointGenerator);
            GISTerrainLoaderSaveLoadPrefs.SavePref("EnableLineGenerator", (int)Prefs.EnableLineGenerator);
            GISTerrainLoaderSaveLoadPrefs.SavePref("EnablePolygonGenerator", (int)Prefs.EnablePolygonGenerator);
            GISTerrainLoaderSaveLoadPrefs.SavePref("PolygonType", (int)Prefs.PolygonType);
            GISTerrainLoaderSaveLoadPrefs.SavePref("PolygonElevationSource", (int)Prefs.PolygonElevationSource);
            GISTerrainLoaderSaveLoadPrefs.SavePref("LineType", (int)Prefs.AVG_LineType);

            GISTerrainLoaderSaveLoadPrefs.SavePref("EnableVectorToAlphaMaps", (int)Prefs.EnableVectorToAlphaMaps);
            GISTerrainLoaderSaveLoadPrefs.SavePref("EnableAlphaMapPoints", (int)Prefs.EnableAlphaMapPoints);
            GISTerrainLoaderSaveLoadPrefs.SavePref("EnableAlphaMapLines", (int)Prefs.EnableAlphaMapLines);
            GISTerrainLoaderSaveLoadPrefs.SavePref("EnableAlphaMapPolygons", (int)Prefs.EnableAlphaMapPolygons);

            GISTerrainLoaderSaveLoadPrefs.SavePref("SymbolsDicPath", AssetDatabase.GetAssetPath(Prefs.SymbolsDic));
#endif
        }
        private void LoadPrefs()
        {
            ShowMainTerrainFile = GISTerrainLoaderSaveLoadPrefs.LoadPref("ShowMainTerrainFile", true);

            Prefs.TerrainFilePath = GISTerrainLoaderSaveLoadPrefs.LoadPref("TerrainFilePath", Prefs.TerrainFilePath);

            if (File.Exists(Prefs.TerrainFilePath))
            {
                DEMFile = (UnityEngine.Object)AssetDatabase.LoadAssetAtPath(GetFileResourceFolder(Prefs.TerrainFilePath), typeof(UnityEngine.Object));
            }

            ShowSetTerrainPref = GISTerrainLoaderSaveLoadPrefs.LoadPref("ShowSetTerrainPref", false);

            Prefs.readingMode = (ReadingMode)GISTerrainLoaderSaveLoadPrefs.LoadPref("readingMode", (int)ReadingMode.Full);
            Prefs.projectionMode = (ProjectionMode)GISTerrainLoaderSaveLoadPrefs.LoadPref("projectionMode", (int)ProjectionMode.Geographic);
            Prefs.tiffElevationSource = (TiffElevationSource)GISTerrainLoaderSaveLoadPrefs.LoadPref("tiffElevationSource", (int)TiffElevationSource.DEM);
            Prefs.EPSGCode = GISTerrainLoaderSaveLoadPrefs.LoadPref("EPSGCode", 0);
            Prefs.BandsIndex = GISTerrainLoaderSaveLoadPrefs.LoadPref("BandsIndex", 0);
            SerializeHeightmap = (OptionEnabDisab)GISTerrainLoaderSaveLoadPrefs.LoadPref("SerializeHeightmap", (int)OptionEnabDisab.Disable);

            ShowCoordinates = GISTerrainLoaderSaveLoadPrefs.LoadPref("ShowCoordinates", false);
            Prefs.SubRegionUpperLeftCoordiante = GISTerrainLoaderSaveLoadPrefs.LoadPref("SubRegionUpperLeftCoordiante", new DVector2(0, 0));
            Prefs.SubRegionDownRightCoordiante = GISTerrainLoaderSaveLoadPrefs.LoadPref("SubRegionDownRightCoordiante", new DVector2(0, 0));

            ////////////////////////////////////////////////////////////////////////////////

            ShowTexturePref = GISTerrainLoaderSaveLoadPrefs.LoadPref("ShowTexturePref", false);
            ShowSplatmapsTerrainLayer = GISTerrainLoaderSaveLoadPrefs.LoadPref("ShowSplatmapsTerrainLayer", false);

            Prefs.TerrainElevation = (TerrainElevation)GISTerrainLoaderSaveLoadPrefs.LoadPref("TerrainElevationMode", (int)TerrainElevation.RealWorldElevation);
            Prefs.TerrainExaggeration = GISTerrainLoaderSaveLoadPrefs.LoadPref("TerrainExaggeration", 0.27f);
            Prefs.terrainDimensionMode = (TerrainDimensionsMode)GISTerrainLoaderSaveLoadPrefs.LoadPref("TerrainDimensionMode", (int)TerrainDimensionsMode.AutoDetection);
            Prefs.TerrainDimensions = GISTerrainLoaderSaveLoadPrefs.LoadPref("TerrainDimensions", new DVector2(10, 10));
            Prefs.ElevationForNullPoints = (EmptyPoints)GISTerrainLoaderSaveLoadPrefs.LoadPref("ElevationForNullPoints", (int)EmptyPoints.Average);
            Prefs.ElevationValueForNullPoints = GISTerrainLoaderSaveLoadPrefs.LoadPref("ElevationValueForNullPoints", 0f);
            Prefs.TerrainFixOption = (FixOption)GISTerrainLoaderSaveLoadPrefs.LoadPref("TerrainFixOption", (int)FixOption.Disable);
            Prefs.TerrainMaxMinElevation = GISTerrainLoaderSaveLoadPrefs.LoadPref("TerrainMaxMinElevation", new Vector2(0, 0));
            Prefs.UnderWater = (OptionEnabDisab)GISTerrainLoaderSaveLoadPrefs.LoadPref("UnderWater", (int)OptionEnabDisab.Disable);
            Prefs.terrainScale = GISTerrainLoaderSaveLoadPrefs.LoadPref("TerrainScale", new Vector3(1, 1, 1));
            Prefs.terrainCount = GISTerrainLoaderSaveLoadPrefs.LoadPref("TerrainCount", Vector2Int.one);


            ////////////////////////////////////////////////////////////////////////////////
            ShowTerrainPref = GISTerrainLoaderSaveLoadPrefs.LoadPref("ShowTerrainPref", false);
            Prefs.heightmapResolution_index = GISTerrainLoaderSaveLoadPrefs.LoadPref("heightmapResolution_index", 2);
            Prefs.heightmapResolution = Prefs.heightmapResolutions[Prefs.heightmapResolution_index];

            Prefs.detailResolution_index = GISTerrainLoaderSaveLoadPrefs.LoadPref("detailResolution_index", 4);
            Prefs.detailResolution = Prefs.availableHeights[Prefs.detailResolution_index];

            Prefs.resolutionPerPatch_index = GISTerrainLoaderSaveLoadPrefs.LoadPref("resolutionPerPatch_index", 1);
            Prefs.resolutionPerPatch = Prefs.availableHeightsResolutionPrePec[Prefs.resolutionPerPatch_index];

            Prefs.baseMapResolution_index = GISTerrainLoaderSaveLoadPrefs.LoadPref("baseMapResolution_index", 4);
            Prefs.baseMapResolution = Prefs.availableHeights[Prefs.baseMapResolution_index];

            Prefs.PixelError = GISTerrainLoaderSaveLoadPrefs.LoadPref("PixelErro", 1.0f);
            Prefs.BaseMapDistance = GISTerrainLoaderSaveLoadPrefs.LoadPref("baseMapDistance", 1000.0f);

            var TerrainsMatPath = GISTerrainLoaderSaveLoadPrefs.LoadPref("terrainMaterialPath", "");
            Prefs.terrainMaterial = (Material)AssetDatabase.LoadAssetAtPath(TerrainsMatPath, typeof(Material));


            var terrainBorderMaterials = GISTerrainLoaderSaveLoadPrefs.LoadPref("terrainBorderMaterial", new List<UnityEngine.Object>());

            if (terrainBorderMaterials.Count > 0)
            {
                if (terrainBorderMaterials[0] != null)
                    Prefs.terrainBorderMaterial = terrainBorderMaterials[0] as Material;
            }

            Prefs.TerrainBackgroundHeightmapResolution_index = GISTerrainLoaderSaveLoadPrefs.LoadPref("TerrainBackgroundHeightmapResolution_index", 2);
            Prefs.TerrainBackgroundHeightmapResolution = Prefs.TerrainBackgroundHeightmapResolutions[Prefs.TerrainBackgroundHeightmapResolution_index];

            Prefs.TerrainBackgroundTextureResolution_index = GISTerrainLoaderSaveLoadPrefs.LoadPref("TerrainBackgroundTextureResolution_index", 2);
            Prefs.TerrainBackgroundTextureResolution = Prefs.TerrainBackgroundTextureResolutions[Prefs.TerrainBackgroundTextureResolution_index];

            ////////////////////////////////////////////////////////////////////////////////

            Prefs.textureMode = (TextureMode)GISTerrainLoaderSaveLoadPrefs.LoadPref("textureMode", (int)TextureMode.WithTexture);
            Prefs.TerrainEmptyColor = GISTerrainLoaderSaveLoadPrefs.LoadPref("textureEmptyColor", Color.white);
            Prefs.UseTerrainEmptyColor = (OptionEnabDisab)GISTerrainLoaderSaveLoadPrefs.LoadPref("useTerrainEmptyColor", (int)OptionEnabDisab.Disable);



            Prefs.Slope = GISTerrainLoaderSaveLoadPrefs.LoadPref("Slope", 0f);
            Prefs.MergeRadius = GISTerrainLoaderSaveLoadPrefs.LoadPref("MergeRadius", 1);
            Prefs.MergingFactor = GISTerrainLoaderSaveLoadPrefs.LoadPref("MergingFactor", 1);
            Prefs.BaseTerrainLayers = GISTerrainLoaderSaveLoadPrefs.LoadPref("BaseTerrainLayers", new GISTerrainLoaderTerrainLayer());
            Prefs.TerrainLayers = GISTerrainLoaderSaveLoadPrefs.LoadPref("TerrainLayers", new List<GISTerrainLoaderTerrainLayer>());
            Prefs.terrainMaterialMode = (TerrainMaterialMode)GISTerrainLoaderSaveLoadPrefs.LoadPref("terrainMaterialMode", (int)TerrainMaterialMode.Standard);
            Prefs.TerrainLayerSet = (OptionEnabDisab)GISTerrainLoaderSaveLoadPrefs.LoadPref("TerrainLayerSet", (int)OptionEnabDisab.Disable);
            Prefs.TerrainLayer_index = GISTerrainLoaderSaveLoadPrefs.LoadPref("TerrainLayer_index", 0);
            Prefs.TerrainLayer = GISTerrainLoaderSaveLoadPrefs.LoadPref("TerrainLayer", 0);
            Prefs.TerrainBaseboards = (OptionEnabDisab)GISTerrainLoaderSaveLoadPrefs.LoadPref("TerrainBaseboards", (int)OptionEnabDisab.Disable);
            Prefs.BorderHigh = GISTerrainLoaderSaveLoadPrefs.LoadPref("BorderHigh", -50);

            Prefs.ContourInterval = GISTerrainLoaderSaveLoadPrefs.LoadPref("ContourInterval", 100f);

            Prefs.AddTextureOffset = (OptionEnabDisab)GISTerrainLoaderSaveLoadPrefs.LoadPref("AddTextureOffset", (int)OptionEnabDisab.Disable);
            Prefs.TextureOffset = GISTerrainLoaderSaveLoadPrefs.LoadPref("TextureOffset", new Vector2(0, 0));

            ////////////////////////////////////////////////////////////////////////////////

            ShowSmoothingOpr = GISTerrainLoaderSaveLoadPrefs.LoadPref("ShowSmoothingOpr", false);

            Prefs.UseTerrainHeightSmoother = (OptionEnabDisab)GISTerrainLoaderSaveLoadPrefs.LoadPref("UseTerrainHeightSmoother", (int)OptionEnabDisab.Disable);
            Prefs.TerrainHeightSmoothFactor = GISTerrainLoaderSaveLoadPrefs.LoadPref("TerrainHeightSmoothFactor", 0.05f);
            Prefs.UseTerrainSurfaceSmoother = (OptionEnabDisab)GISTerrainLoaderSaveLoadPrefs.LoadPref("UseTerrainSurfaceSmoother", (int)OptionEnabDisab.Disable);
            Prefs.TerrainSurfaceSmoothFactor = GISTerrainLoaderSaveLoadPrefs.LoadPref("TerrainSurfaceSmoothFactor", 4);

            ////////////////////////////////////////////////////////////////////////////////
            ShowVectorDataPanel = GISTerrainLoaderSaveLoadPrefs.LoadPref("ShowOSMVectorData", false);
            Prefs.vectorType = (VectorType)GISTerrainLoaderSaveLoadPrefs.LoadPref("VectorType", (int)(VectorType.OpenStreetMap));

            Prefs.EnableGeoPointGeneration = (OptionEnabDisab)GISTerrainLoaderSaveLoadPrefs.LoadPref("EnableGeoPointGeneration", (int)(OptionEnabDisab.Disable));

            Prefs.EnableTreeGeneration = (OptionEnabDisab)GISTerrainLoaderSaveLoadPrefs.LoadPref("EnableTreeGeneration", (int)(OptionEnabDisab.Disable));
            Prefs.TreeScaleFactor = GISTerrainLoaderSaveLoadPrefs.LoadPref("TreeScaleFactor", 2f);
            Prefs.TreeDistribution = (PointDistribution)GISTerrainLoaderSaveLoadPrefs.LoadPref("TreeDistribution", (int)(PointDistribution.Randomly));
            Prefs.TreePrefabs = GISTerrainLoaderSaveLoadPrefs.LoadPref("TreePrefabs", new List<GISTerrainLoaderSO_Tree>());
            Prefs.TreeDistance = GISTerrainLoaderSaveLoadPrefs.LoadPref("TreeDistance", 4000f);
            Prefs.BillBoardStartDistance = GISTerrainLoaderSaveLoadPrefs.LoadPref("BillBoardStartDistance", 300f);

            Prefs.EnableGrassGeneration = (OptionEnabDisab)GISTerrainLoaderSaveLoadPrefs.LoadPref("EnableGrassGeneration", (int)(OptionEnabDisab.Disable));
            Prefs.GrassDistribution = (PointDistribution)GISTerrainLoaderSaveLoadPrefs.LoadPref("GrassDistribution", (int)(PointDistribution.Randomly));
            Prefs.GrassScaleFactor = GISTerrainLoaderSaveLoadPrefs.LoadPref("GrassScaleFactor", 10f);
            Prefs.DetailDistance = GISTerrainLoaderSaveLoadPrefs.LoadPref("DetailDistance", 380f);
            Prefs.GrassPrefabs = GISTerrainLoaderSaveLoadPrefs.LoadPref("GrassPrefabs", new List<GISTerrainLoaderSO_GrassObject>());


            Prefs.EnableRoadGeneration = (OptionEnabDisab)GISTerrainLoaderSaveLoadPrefs.LoadPref("EnableRoadGeneration", (int)(OptionEnabDisab.Disable));
            Prefs.RoadGenerator = (RoadGeneratorType)GISTerrainLoaderSaveLoadPrefs.LoadPref("RoadType", (int)RoadGeneratorType.Line);

            Prefs.BuildTerrains = (OptionEnabDisab)GISTerrainLoaderSaveLoadPrefs.LoadPref("BuildTerrains", (int)(OptionEnabDisab.Disable));
            Prefs.RoadRaiseOffset = GISTerrainLoaderSaveLoadPrefs.LoadPref("RoadRaiseOffset", 0.1f);

            Prefs.EnableBuildingGeneration = (OptionEnabDisab)GISTerrainLoaderSaveLoadPrefs.LoadPref("EnableBuildingGeneration", (int)(OptionEnabDisab.Disable));
            Prefs.GenerateBuildingBase = (OptionEnabDisab)GISTerrainLoaderSaveLoadPrefs.LoadPref("GenerateBuildingBase", (int)(OptionEnabDisab.Disable));

            Prefs.EnableWaterGeneration = (OptionEnabDisab)GISTerrainLoaderSaveLoadPrefs.LoadPref("EnableWaterGeneration", (int)(OptionEnabDisab.Disable));
            Prefs.WaterDataSource = (WaterSource)GISTerrainLoaderSaveLoadPrefs.LoadPref("WaterDataSource", (int)(WaterSource.DefaultPlane));
            Prefs.WaterOffsetY = GISTerrainLoaderSaveLoadPrefs.LoadPref("WaterOffsetY", 2f);

            Prefs.LandParcelElevationMode = (VectorElevationMode)GISTerrainLoaderSaveLoadPrefs.LoadPref("LandParcelElevationMode", (int)(VectorElevationMode.Flat2D));
            Prefs.LandParcelOffsetY = GISTerrainLoaderSaveLoadPrefs.LoadPref("LandParcelOffsetY", 0.1f);
            Prefs.LandParcelPolygonCount = GISTerrainLoaderSaveLoadPrefs.LoadPref("LandParcelPolygonCount", 2);
            Prefs.EnableLandParcelGeneration = (OptionEnabDisab)GISTerrainLoaderSaveLoadPrefs.LoadPref("EnableLandParcelGeneration", (int)(OptionEnabDisab.Disable));



            var GeoPointPrefabs = GISTerrainLoaderSaveLoadPrefs.LoadPref("GeoPointPrefabs", new List<UnityEngine.GameObject>());
            if (GeoPointPrefabs.Count > 0)
            {
                if (GeoPointPrefabs[0] != null)
                    Prefs.GeoPointPrefab = GeoPointPrefabs[0];
            }

            var PathPrefabs = GISTerrainLoaderSaveLoadPrefs.LoadPref("PathPrefabs", new List<GISTerrainLoaderSO_Road>());
            if (PathPrefabs.Count > 0)
            {
                if (PathPrefabs[0] != null)
                    Prefs.PathPrefab = PathPrefabs[0];
            }
            ShowHelpSupport = GISTerrainLoaderSaveLoadPrefs.LoadPref("ShowHelpSupport", false);


            Prefs.RemovePrvTerrain = (OptionEnabDisab)GISTerrainLoaderSaveLoadPrefs.LoadPref("RemovePrvTerrain", (int)(OptionEnabDisab.Enable));

#if GISTechAdvancedVectorGenerator
            ShowAdvancedVectorGenerator = GISTerrainLoaderSaveLoadPrefs.LoadPref("ShowAdvancedVectorGenerator", false);
            Prefs.EnableAdvancedVectorGenerator = (OptionEnabDisab)GISTerrainLoaderSaveLoadPrefs.LoadPref("EnableAdvancedVectorGenerator", (int)(OptionEnabDisab.Disable));
            Prefs.EnablePointGenerator = (OptionEnabDisab)GISTerrainLoaderSaveLoadPrefs.LoadPref("EnablePointGenerator", (int)(OptionEnabDisab.Disable));
            Prefs.EnableLineGenerator = (OptionEnabDisab)GISTerrainLoaderSaveLoadPrefs.LoadPref("EnableLineGenerator", (int)(OptionEnabDisab.Disable));
            Prefs.EnablePolygonGenerator = (OptionEnabDisab)GISTerrainLoaderSaveLoadPrefs.LoadPref("EnablePolygonGenerator", (int)(OptionEnabDisab.Disable));
            Prefs.PolygonType = (PolygonObjectType)GISTerrainLoaderSaveLoadPrefs.LoadPref("PolygonType", (int)(PolygonObjectType.Mesh));
            Prefs.PolygonElevationSource = (VectorElevationMode)GISTerrainLoaderSaveLoadPrefs.LoadPref("PolygonElevationSource", (int)(VectorElevationMode.AdaptedToTerrainElevation3D));
            Prefs.AVG_LineType = (AVG_LineType)GISTerrainLoaderSaveLoadPrefs.LoadPref("LineType", (int)(AVG_LineType.Mesh));

            Prefs.EnableVectorToAlphaMaps = (OptionEnabDisab)GISTerrainLoaderSaveLoadPrefs.LoadPref("EnableVectorToAlphaMaps", (int)(OptionEnabDisab.Disable));
            Prefs.EnableAlphaMapPoints = (OptionEnabDisab)GISTerrainLoaderSaveLoadPrefs.LoadPref("EnableAlphaMapPoints", (int)(OptionEnabDisab.Disable));
            Prefs.EnableAlphaMapLines = (OptionEnabDisab)GISTerrainLoaderSaveLoadPrefs.LoadPref("EnableAlphaMapLines", (int)(OptionEnabDisab.Disable));
            Prefs.EnableAlphaMapPolygons = (OptionEnabDisab)GISTerrainLoaderSaveLoadPrefs.LoadPref("EnableAlphaMapPolygons", (int)(OptionEnabDisab.Disable));

            var SymbolsDicPath = GISTerrainLoaderSaveLoadPrefs.LoadPref("SymbolsDicPath", "");
            Prefs.SymbolsDic = (GISTerrainLoaderCustomDic)AssetDatabase.LoadAssetAtPath(SymbolsDicPath, typeof(GISTerrainLoaderCustomDic));

#endif
        }
        private void ResetPrefs()
        {
            DEMFile = null;
            Prefs.readingMode = ReadingMode.Full;


            ////////////////////////////////////////////////////////////////////////////////

            Prefs.TerrainElevation = TerrainElevation.RealWorldElevation;
            Prefs.TerrainExaggeration = 0.27f;
            Prefs.terrainDimensionMode = TerrainDimensionsMode.AutoDetection;
            Prefs.TerrainDimensions = new DVector2(10, 10);
            Prefs.UnderWater = OptionEnabDisab.Disable;
            Prefs.TerrainFixOption = FixOption.Disable;
            Prefs.TerrainMaxMinElevation = new Vector2(0, 0);
            Prefs.terrainScale = new Vector3(1, 1, 1);

            ////////////////////////////////////////////////////////////////////////////////

            Prefs.heightmapResolution_index = 2;
            Prefs.heightmapResolution = Prefs.heightmapResolutions[Prefs.heightmapResolution_index];

            Prefs.detailResolution_index = 4;
            Prefs.detailResolution = Prefs.availableHeights[Prefs.detailResolution_index];

            Prefs.resolutionPerPatch_index = 1;
            Prefs.resolutionPerPatch = Prefs.availableHeightsResolutionPrePec[Prefs.resolutionPerPatch_index];

            Prefs.baseMapResolution_index = 4;
            Prefs.baseMapResolution = Prefs.availableHeights[Prefs.baseMapResolution_index];

            Prefs.PixelError = 1;
            Prefs.BaseMapDistance = 1000;

            ////////////////////////////////////////////////////////////////////////////////
            Prefs.terrainMaterialMode = TerrainMaterialMode.Standard;
            Prefs.textureMode = TextureMode.WithTexture;
            Prefs.TerrainEmptyColor = Color.white;

            Prefs.TerrainBackgroundHeightmapResolution_index = 2;
            Prefs.TerrainBackgroundHeightmapResolution = Prefs.TerrainBackgroundHeightmapResolutions[Prefs.TerrainBackgroundHeightmapResolution_index];

            ////////////////////////////////////////////////////////////////////////////////

            Prefs.UseTerrainHeightSmoother = OptionEnabDisab.Disable;
            Prefs.TerrainHeightSmoothFactor = 0.05f;
            Prefs.UseTerrainSurfaceSmoother = OptionEnabDisab.Disable;
            Prefs.TerrainSurfaceSmoothFactor = 2;

            ////////////////////////////////////////////////////////////////////////////////

            Prefs.EnableTreeGeneration = OptionEnabDisab.Disable;
            Prefs.TreeDistance = 4000;
            Prefs.BillBoardStartDistance = 300;
            Prefs.TreePrefabs = new List<GISTerrainLoaderSO_Tree>();

            Prefs.EnableGrassGeneration = OptionEnabDisab.Disable;
            Prefs.GrassScaleFactor = 5f;
            Prefs.DetailDistance = 350;
            Prefs.GrassPrefabs = new List<GISTerrainLoaderSO_GrassObject>();

            Prefs.EnableRoadGeneration = OptionEnabDisab.Disable;
            Prefs.RoadGenerator = RoadGeneratorType.Line;

            Prefs.EnableBuildingGeneration = OptionEnabDisab.Disable;

        }

        public static bool IsTerrainFileInsideGISFolder(string TerrainFilePath)
        {
            bool Inside = false;

            DirectoryInfo di = new DirectoryInfo(TerrainFilePath);

            for (int i = 0; i <= 5; i++)
            {
                di = di.Parent;

                if (di.Name == "GIS Terrains")
                {
                    Inside = true; break;
                }

                if (i == 5)
                {
                    Inside = false;
                }

            }
            return Inside;
        }
        public static string GetFileResourceFolder(string TerrainFilePath)
        {
            var ext = Path.GetExtension(TerrainFilePath);
            string TerrainFileName = Path.GetFileNameWithoutExtension(TerrainFilePath);

            string TextureFolder = "";

            DirectoryInfo di = new DirectoryInfo(TerrainFilePath);

            TextureFolder = TerrainFileName;

            for (int i = 0; i <= 8; i++)
            {
                di = di.Parent;
                TextureFolder = di.Name + "/" + TextureFolder;

                if (di.Name == "Assets")
                {
                    break;
                }
            }
            return TextureFolder + ext;

        }
        #endregion
        #region Integration

#if GISTerrainStreamer
        private void GTS_Generate()
        {
            Prefs.TerrainFilePath = Path.Combine(Path.GetDirectoryName(Application.dataPath), AssetDatabase.GetAssetPath(DEMFile));

            if (Prefs.Settings_SO.IsValidTerrainFile(Prefs.TerrainFilePath))
            {

                Prefs.heightmapResolution = Prefs.heightmapResolutions[Prefs.heightmapResolution_index];
                Prefs.TerrainBackgroundHeightmapResolution = Prefs.TerrainBackgroundHeightmapResolutions[Prefs.TerrainBackgroundHeightmapResolution_index];
                Prefs.TerrainBackgroundTextureResolution = Prefs.TerrainBackgroundHeightmapResolutions[Prefs.TerrainBackgroundTextureResolution_index];
                Prefs.TerrainLayer = LayerMask.NameToLayer(Prefs.TerrainLayerSrt[Prefs.TerrainLayer_index]);
                Prefs.detailResolution = Prefs.availableHeights[Prefs.detailResolution_index];
                Prefs.resolutionPerPatch = Prefs.availableHeightsResolutionPrePec[Prefs.resolutionPerPatch_index];
                Prefs.baseMapResolution = Prefs.availableHeights[Prefs.baseMapResolution_index];

                if (!string.IsNullOrEmpty(Prefs.TerrainFilePath))
                {
                    GTS_CheckFileConfig();
                }
                else
                {
                    OnError(" Please set DEM File .. Try againe");
                }
            }
        }
        private async void GTS_CheckFileConfig()
        {
            if (File.Exists(Prefs.TerrainFilePath))
            {
                if (IsTerrainFileInsideGISFolder(Prefs.TerrainFilePath))
                {
                    CheckTerrainTextures();

                    State = GeneratorState.Generating;

                    await Task.Delay(1);

                    await GTS_Phases();
                }
                else
                    Debug.LogError("DEM Not Placed in the Correct Folder : Place your GIS Data into 'GIS Terrain Loader/Recources/GIS Terrains/' Folder");
            }

        }
        public async Task GTS_Phases()
        {
            if (State == GeneratorState.Generating)
            {
                timer.Reset();
                timer.Start();

                await FreeUpMemory();

                GISTerrainStreamerDEMLoader demLoader = new GISTerrainStreamerDEMLoader(Prefs);
                GISTerrainStreamerRasterLoader rasterLoader = new GISTerrainStreamerRasterLoader(Prefs, demLoader);


                await GTS_LoadElevationFile(demLoader, rasterLoader);
                //await demLoader.LoadDem();

                //GISTerrainStreamerRasterLoader rasterLoader = new GISTerrainStreamerRasterLoader(Prefs, demLoader.dem);
                //await demLoader.LoadDem();

                //await LoadElevationFile(Prefs.TerrainFilePath);

                //if (ElevationInfo != null)
                //{
                //    await GenerateContainer();

                //    if (GeneratedContainer)
                //    {
                //        for (int i = 0; i < GeneratedContainer.terrains.Length; i++)
                //        {
                //            if (State != GeneratorState.idle)
                //                await GenerateTerrains(i);
                //        }


                //        for (int i = 0; i < GeneratedContainer.terrains.Length; i++)
                //        {
                //            if (State != GeneratorState.idle)
                //                await GenerateHeightmap(i);
                //        }

                //        if (State != GeneratorState.idle)
                //            RepareTerrains();
                //        if (State != GeneratorState.idle)
                //            await GenerateTextures();
                //        if (State != GeneratorState.idle)
                //            await GenerateBackground();
                //        if (State != GeneratorState.idle)
                //            await GenerateVectorData();
                //        if (State != GeneratorState.idle)
                //            Finish();

                //        timer.Stop();
                //    }
                //}
                //else
                //{
                //    Finish();
                //}

            }
            try
            {


            }
            catch (Exception ex)
            {
                OnError("Couldn't Load Terrain file: " + ex.Message + "  " + Environment.NewLine);
            }
            ;

        }

        public async Task GTS_LoadElevationFile(GISTerrainStreamerDEMLoader dem, GISTerrainStreamerRasterLoader raster)
        {
            bool loaded = await dem.LoadDem();
            ElevationInfo = dem.ElevationInfo;
            await CheckForDimensionAndTiles(true);

            if (loaded)
            {


                raster.BuildOrOpenTextureDatasets();
                int textureResolution = 0;
                int tilePixWBase = dem.ElevationInfo.data.mapSize_col_x / Prefs.terrainCount.x;
                int tilePixHBase = dem.ElevationInfo.data.mapSize_row_y / Prefs.terrainCount.y;
                int hmRes = Mathf.Max(2, Prefs.heightmapResolution);
                int texRes = textureResolution > 0 ? textureResolution : hmRes;
                float demPixelSizeX = (float)dem._demGT[1];
                float demPixelSizeY = (float)Math.Abs(dem._demGT[5]);

                await GenerateContainer();

                int metersPerUnityUnit = 1;
                for (int ty = 0; ty < Prefs.terrainCount.y; ty++)
                {
                    for (int tx = 0; tx < Prefs.terrainCount.x; tx++)
                    {
                        int xOff = tx * tilePixWBase;
                        int yOff = ty * tilePixHBase;
                        int xSize = (tx == Prefs.terrainCount.x - 1) ? (dem.ElevationInfo.data.mapSize_col_x - xOff) : tilePixWBase;
                        int ySize = (ty == Prefs.terrainCount.y - 1) ? (dem.ElevationInfo.data.mapSize_row_y - yOff) : tilePixHBase;

                        float[,] heights = dem.ReadHeightsToUnityNormalized(dem._demDs, xOff, yOff, xSize, ySize, hmRes, hmRes, GeneratedContainer.ContainerSize.y);

                        double minX = dem._demGT[0] + xOff * dem._demGT[1] + yOff * dem._demGT[2];
                        double maxY = dem._demGT[3] + xOff * dem._demGT[4] + yOff * dem._demGT[5];
                        double maxX = dem._demGT[0] + (xOff + xSize) * dem._demGT[1] + yOff * dem._demGT[2];
                        double minY = dem._demGT[3] + xOff * dem._demGT[4] + (yOff + ySize) * dem._demGT[5];

                        // For each texture dataset, read a texture for the same world window
                        List<Texture2D> layerTextures = new List<Texture2D>();

                        for (int i = 0; i < raster._texDsList.Count; i++)
                        {
                            var ds = raster._texDsList[i];
                            var gt = raster._texGTList[i];
                            int w = raster._texWidthList[i];
                            int h = raster._texHeightList[i];
                            var tex = raster.ReadTextureForWorldWindow(ds, gt, w, h, minX, minY, maxX, maxY, texRes, texRes);
                            layerTextures.Add(tex);
                        }

                        float sizeX = (float)(xSize * demPixelSizeX / Math.Max(1e-6f, metersPerUnityUnit)) * 100000;

                        float sizeZ = (float)(ySize * demPixelSizeY / Math.Max(1e-6f, metersPerUnityUnit))*100000;



                        Debug.Log(sizeX + " " + sizeZ + " " + xOff + " " + yOff);
                        var terrain =CreateUnityTerrainTile(dem,metersPerUnityUnit,tx, ty, heights, layerTextures, sizeX, sizeZ, minX, maxY, texRes);
                        terrain.transform.SetParent(GeneratedContainer.transform, false);

                    }
                }

                dem._demDs?.Dispose();
                foreach (var d in raster._texDsList) d?.Dispose();
            }


        }
        private Terrain CreateUnityTerrainTile(GISTerrainStreamerDEMLoader dem,int metersPerUnityUnit, int tileX, int tileY, float[,] heights, List<Texture2D> layerTextures, float sizeX, float sizeZ, double tileMinX, double tileMaxY, int alphamapRes)
        {
            var td = new TerrainData();
            int hmRes = heights.GetLength(0);
            td.heightmapResolution = hmRes;
            td.size = new Vector3(sizeX, GeneratedContainer.ContainerSize.y, sizeZ);
            td.SetHeights(0, 0, heights);

            int layerCount = Math.Max(1, layerTextures.Count);
            TerrainLayer[] layers = new TerrainLayer[layerCount];
            for (int i = 0; i < layerCount; i++)
            {
                var layer = new TerrainLayer();
                // assign diffuse if available
                if (i < layerTextures.Count && layerTextures[i] != null)
                {
                    layer.diffuseTexture = layerTextures[i];
                    // set tile size so that it covers the terrain properly
                    layer.tileSize = new Vector2(sizeX, sizeZ);
                }
                layers[i] = layer;
            }

            td.terrainLayers = layers;

            // Alphamap (splat) resolution and data
            int aRes = Mathf.Max(1, alphamapRes);
            td.alphamapResolution = aRes;

            float[,,] alphaMaps = new float[aRes, aRes, layerCount];

            // If we have at least one texture, build weights per pixel based on brightness/alpha
            if (layerTextures.Count > 0)
            {
                // We'll assume each layer texture corresponds to the same outW/outH resolution used earlier (alphamapRes)
                for (int ly = 0; ly < aRes; ly++)
                {
                    for (int lx = 0; lx < aRes; lx++)
                    {
                        float total = 0f;
                        for (int li = 0; li < layerCount; li++)
                        {
                            float w = 0f;
                            var tex = layerTextures[li];
                            if (tex != null)
                            {
                                int tx = Mathf.Clamp((int)Mathf.Round((float)lx / (aRes - 1) * (tex.width - 1)), 0, tex.width - 1);
                                int ty = Mathf.Clamp((int)Mathf.Round((float)ly / (aRes - 1) * (tex.height - 1)), 0, tex.height - 1);
                                Color c = tex.GetPixel(tx, ty);
                                // Use alpha if present, otherwise use brightness
                                if (c.a > 0.001f) w = c.a;
                                else w = (c.r + c.g + c.b) / 3.0f; // brightness
                            }
                            alphaMaps[ly, lx, li] = w;
                            total += w;
                        }

                        // normalize weights so they sum to 1. If total == 0, give weight 1 to first layer
                        if (total <= 1e-6f)
                        {
                            alphaMaps[ly, lx, 0] = 1f;
                            for (int li = 1; li < layerCount; li++) alphaMaps[ly, lx, li] = 0f;
                        }
                        else
                        {
                            for (int li = 0; li < layerCount; li++) alphaMaps[ly, lx, li] /= total;
                        }
                    }
                }
            }
            else
            {
                // no textures: fill single layer with 1
                for (int y = 0; y < aRes; y++) for (int x = 0; x < aRes; x++) alphaMaps[y, x, 0] = 1f;
            }

            var go = Terrain.CreateTerrainGameObject(td);
            go.name = $"Terrain_{tileX}_{tileY}";
            float posX = (float)((tileMinX - dem._demGT[0]) / Math.Max(1e-6f, metersPerUnityUnit));
            float posZ = (float)((dem._demGT[3] - tileMaxY) / Math.Max(1e-6f, metersPerUnityUnit));
            go.transform.position = new Vector3(posX*100000, 0f, posZ* 100000);

            // apply alphamaps
            try
            {
                go.GetComponent<Terrain>().terrainData.SetAlphamaps(0, 0, alphaMaps);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Failed to set alphamaps: " + ex.Message);
            }

//#if UNITY_EDITOR
//            if (savePngTextures)
//            {
//                try
//                {
//                    var dir = Path.Combine(Path.GetDirectoryName(demPath) ?? Application.dataPath, "_GeneratedTextures");
//                    Directory.CreateDirectory(dir);
//                    for (int i = 0; i < layerTextures.Count; i++)
//                    {
//                        var diffuse = layerTextures[i];
//                        if (diffuse == null) continue;
//                        string path = Path.Combine(dir, $"{savedTexturePrefix}{tileX}_{tileY}_L{i}.png");
//                        File.WriteAllBytes(path, diffuse.EncodeToPNG());
//                    }
//                    AssetDatabase.Refresh();
//                }
//                catch { }
//            }
//#endif
            return go.GetComponent<Terrain>();
        }

#endif

#if GISDataDownloader
        private void OnGDDDownloadComplete(GISDataDownloaderPrefs GDD_prefs)
        {
            if(GDD_prefs.PathType == DownloadPathType.GISTerrainFolder && Prefs.GenerateTerrainFromDownloadedData == OptionEnabDisab.Enable)
            {
                var FolderName = Path.GetFileName(GDD_prefs.DownloadPath.TrimEnd(Path.DirectorySeparatorChar));
                var basePath = Application.dataPath + "\\GIS Tech\\GIS Terrain Loader\\Resources\\GIS Terrains";

                if (Directory.Exists(basePath))
                    Directory.CreateDirectory(basePath);

                var DemFilePath = basePath + FolderName + "\\" + GDD_prefs.ZoneName + "\\" + GDD_prefs.ZoneName + ".tif";

                Prefs.TerrainFilePath = DemFilePath;
                DEMFile = (UnityEngine.Object)AssetDatabase.LoadAssetAtPath(GetFileResourceFolder(Prefs.TerrainFilePath), typeof(UnityEngine.Object));
                if (Prefs.Settings_SO.IsValidTerrainFile(Prefs.TerrainFilePath))
                {

                    Prefs.heightmapResolution = Prefs.heightmapResolutions[Prefs.heightmapResolution_index];
                    Prefs.TerrainBackgroundHeightmapResolution = Prefs.TerrainBackgroundHeightmapResolutions[Prefs.TerrainBackgroundHeightmapResolution_index];
                    Prefs.TerrainBackgroundTextureResolution = Prefs.TerrainBackgroundHeightmapResolutions[Prefs.TerrainBackgroundTextureResolution_index];
                    Prefs.TerrainLayer = LayerMask.NameToLayer(Prefs.TerrainLayerSrt[Prefs.TerrainLayer_index]);
                    Prefs.detailResolution = Prefs.availableHeights[Prefs.detailResolution_index];
                    Prefs.resolutionPerPatch = Prefs.availableHeightsResolutionPrePec[Prefs.resolutionPerPatch_index];
                    Prefs.baseMapResolution = Prefs.availableHeights[Prefs.baseMapResolution_index];

                    if (!string.IsNullOrEmpty(Prefs.TerrainFilePath))
                    {
                        GTLCheckFileConfig();
                    }
                    else
                    {
                        OnError(" Please set DEM File .. Try againe");
                    }
                }
 
            }

            if (GDD_prefs.PathType == DownloadPathType.Custom && Prefs.GenerateTerrainFromDownloadedData == OptionEnabDisab.Enable)
            {
                Debug.Log(" Set 'Download Path Type' to 'GIS Terrain Loader' in order to generate a terrain in Edit mode just after the download is complete");
            }
        }

#endif


#if GISVirtualTexture
        private void AddGISVirtualTexture()
        {

            if (Prefs.terrainMaterialMode == TerrainMaterialMode.GISVirtualTexture)
            {
                if (GeneratedContainer)
                {
                    Prefs.terrainMaterial = (Material)Resources.Load("Materials/GISVirtualTexture", typeof(Material));

                    if (Prefs.terrainMaterial)
                    {
                        if (!GameObject.FindObjectOfType<GISTech.GISVirtualTexture.GISVirtualTextureRuntimePrefs>())
                        {
                            GeneratedContainer.gameObject.AddComponent<GISTech.GISVirtualTexture.GISVirtualTextureRuntimePrefs>();
                        }
                        else
                        {
                            Debug.LogError("Runtime GIS Virtual Texture already exists in your scene");
                        }
                    }
                    else
                        Debug.LogError("GIS Virtual Texture material not found ('GIS Virtual Texture/Resources/Materials/GISVirtualTexture') ");
                }

            }

    }
#endif

        #endregion


#if RDST
        #region RDST

        private void RDST_DrawRDSTPanel()
        {
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("Raster-Driven Splatmap Configuration", EditorStyles.boldLabel);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("RDST Layer Data", EditorStyles.boldLabel);

            Prefs.RDST_layerData = (GISTech.GISTerrainLoader.RasterSplatmap.RDSTLayerData)EditorGUILayout.ObjectField(
                "Layer Data Asset", Prefs.RDST_layerData,
                typeof(GISTech.GISTerrainLoader.RasterSplatmap.RDSTLayerData),
                false);
 
            if (Prefs.RDST_layerData != null)
            {
                EditorGUILayout.HelpBox($"Loaded: {Prefs.RDST_layerData.name}\n{Prefs.RDST_layerData.rasterMasks.Count} mask(s) configured\nAlphamap Resolution: {Prefs.RDST_layerData.alphamapResolution}", MessageType.Info);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Splatmap Quality", EditorStyles.boldLabel);

                string[] resolutionLabels = { "Low (512)", "Medium (1024)", "High (2048)", "Very High (4096)" };
                int[] resolutionValues = { 512, 1024, 2048, 4096 };

                int currentIndex = System.Array.IndexOf(resolutionValues, Prefs.RDST_layerData.alphamapResolution);
                if (currentIndex == -1) currentIndex = 2;

                int newIndex = EditorGUILayout.Popup("Alphamap Resolution", currentIndex, resolutionLabels);
                if (newIndex != currentIndex)
                {
                    Prefs.RDST_layerData.alphamapResolution = resolutionValues[newIndex];
                    EditorUtility.SetDirty(Prefs.RDST_layerData);
                }

                EditorGUILayout.HelpBox($"Current resolution: {Prefs.RDST_layerData.alphamapResolution}×{Prefs.RDST_layerData.alphamapResolution}\nHigher resolution = sharper textures but more memory.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Assign an RDST Layer Data asset.\nCreate one via:\nAssets → Create → GIS Tech → RDST Layer Data", MessageType.Warning);
            }

            EditorGUILayout.Space();

            EditorGUILayout.EndVertical();
        }
 
        private void RDST_ApplyRasterSplatmapsAutomatic()
        {
            if (Prefs.RDST_layerData == null || Prefs.RDST_layerData.rasterMasks.Count == 0)
            {
                Debug.Log("RDST: No raster masks configured - skipping splatmap application");
                return;
            }
            
            if (GeneratedContainer == null || GeneratedContainer.terrains == null || GeneratedContainer.terrains.Length == 0)
            {
                Debug.LogWarning("RDST: GeneratedContainer is null or empty - skipping automatic splatmap application");
                return;
            }
            
            if (GeneratedContainer.terrains[0, 0].container == null)
            {
                Debug.LogWarning("RDST: Terrain container is null - skipping automatic splatmap application");
                return;
            }

            Terrain terrain = GeneratedContainer.terrains[0, 0].container.GetComponent<Terrain>();
            
            if (terrain == null)
            {
                Debug.LogWarning("RDST: Terrain component not found - terrain may not be fully initialized yet. Use 'Update Raster Splatmaps' button after generation.");
                return;
            }

            Debug.Log($"<color=cyan>RDST: Applying splatmaps automatically to {GeneratedContainer.terrains.Length} terrain tiles...</color>");

            bool success = GISTech.GISTerrainLoader.RasterSplatmap.RDSTProcessor.ProcessRasterSplatmaps(
                terrain,
                Prefs.RDST_layerData, 
                Prefs,
                out string errorMessage);

            if (success)
            {
                Debug.Log("<color=green>RDST: Splatmaps applied successfully during terrain generation!</color>");
            }
            else
            {
                Debug.LogError($"RDST: Failed to apply splatmaps automatically: {errorMessage}\nYou can manually apply using 'Update Raster Splatmaps' button.");
            }
        }

        private void RDST_ApplyRasterSplatmapsManual()
        {
            if (Prefs.RDST_layerData == null || Prefs.RDST_layerData.rasterMasks.Count == 0)
            {
                EditorUtility.DisplayDialog("Error", "No raster masks configured. Please add at least one raster mask.", "OK");
                return;
            }
            
            Terrain terrain = null;
            
            if (Selection.activeGameObject != null)
            {
                terrain = Selection.activeGameObject.GetComponent<Terrain>();
            }
            
            if (terrain == null && GeneratedContainer != null && GeneratedContainer.terrains != null && GeneratedContainer.terrains.Length > 0)
            {
                terrain = GeneratedContainer.terrains[0, 0].container.GetComponent<Terrain>();
            }
            
            if (terrain == null)
            {
                EditorUtility.DisplayDialog("Error", "No terrain selected or found. Please select a terrain GameObject in the hierarchy.", "OK");
                return;
            }
            
            Debug.Log($"<color=cyan>RDST: Updating splatmaps on terrain: {terrain.name}</color>");

            bool success = GISTech.GISTerrainLoader.RasterSplatmap.RDSTProcessor.ProcessRasterSplatmaps(
                terrain,
                Prefs.RDST_layerData, 
                Prefs,
                out string errorMessage);

            if (success)
            {
                EditorUtility.DisplayDialog("Success", "Raster splatmaps updated successfully!", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Error", $"Failed to update splatmaps:\n{errorMessage}", "OK");
            }
        }

        #endregion
#endif

    }

}
