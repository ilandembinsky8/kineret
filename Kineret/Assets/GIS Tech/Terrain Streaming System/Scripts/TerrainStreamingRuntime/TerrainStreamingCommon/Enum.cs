/*     Unity GIS Tech 2020-2021      */

using System.ComponentModel;

namespace GISTech.TerrainStreaming
{

    public enum MainOperation
    {
        Download,
        DownloadAndGenerate,
        Generate
    }
    public enum DownloaderState
    {
        idle,
        Downloading
    }

    public enum GISDataDownloaderDEMProvider
    {
        SRTM_90m,
        SRTM_30m,
        Mapbox
    }
    public enum GISDataDownloaderDownloadType
    {
        data,
        file,
        www
    }
    public enum IntersectionMode
    {
        FieldOfView,
        Area,
        InCircular
    }
    public enum GISDataDownloaderTotalFileSize
    {
        ByNumber,
        BySize
    }
    public enum GISMapSource
    {
        ArcGIS,
        Mapbox,
        Bingmaps
    }
    public enum GISDataDownloaderArcGISType
    {
        [Description("World_Imagery")]
        Satellite,
        [Description("World_Topo_Map")]
        WorldTopo,
        [Description("World_Street_Map")]
        Streets,
        [Description("World_Shaded_Relief")]
        ShadedRelief,
    }
    public enum GISDataDownloaderBingMapType
    {
        aerial,
        road,
        canvasDark,
        canvasLight,
        ordnanceSurvey

    }
    public enum GISDataDownloaderMapboxType
    {
        [Description("satellite-v9")]
        Satellite,
        [Description("light-v10")]
        Light,
        [Description("streets-v9")]
        Streets,
        [Description("streets-v11")]
        StreetsSimple,
        [Description("outdoors-v11")]
        Outdoors, 
 

    }
 

    public enum GISDataDownloaderVectorProvider
    {
        OpenStreetMap
    }
    public enum TerrainStreamingTerrainGrid
    {
        Auto,
        Standard,
        Custom
    }
    public enum OptionEnabDisab
    {
        Enable,
        Disable
    }
    public enum GenerationMode
    {
        Random,
        Vector
    }


    public enum TerrainMaterialMode
    {
        Standard = 0,
        Custom
    }
    public enum LoadingState
    {
        Loading,
        Loaded,
        Error
    }
    public enum StartMode
    {
        Centre,
        Custom
    }
    public enum Projections
    {
        Geographic_LatLon_Decimale = 0,
        Geographic_LatLon_DegMinSec,
        UTM,
        UTM_MGRUTM,
        Lambert
    }
    public enum RoadGenerator
    {
        SimpleUnityLine
        //EasyRoad3D
    }

    public enum TerrainStreamingCachePathType
    {
        Standard,
        Custom
    }
}