using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace GISTech.TerrainStreaming
{
    public static class TerrainStreamingProvidersData
    {
        private static string srtm_90_tiff_url = "https://srtm.csi.cgiar.org/wp-content/uploads/files/srtm_5x5/TIFF/";
        public static Vector2Int srtm_90_tiff_LatLon_Step = new Vector2Int(5, 5);
 
        public static string srtm_30_mainUrs = "https://urs.earthdata.nasa.gov";
        private static string srtm_30_hgt_url = "https://e4ftl01.cr.usgs.gov/MEASURES/SRTMGL1.003/2000.02.11/";
        public static Vector2Int srtm_30_hgt_LatLon_Step = new Vector2Int(5, 5);


        public static string MapBox_PngRaw_url = "https://api.mapbox.com/v4/mapbox.terrain-rgb/";

        #region RasterData

        public static string ArcGISserver = "http://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{0}/{1}/{2}";
        public static string RasterArcGIS = "https://services.arcgisonline.com/arcgis/rest/services/";
        public static string RasterMapBox = "https://api.mapbox.com/styles/v1/mapbox/";
        public static string RasterBingmaps = "http://dev.virtualearth.net/REST/v1/Imagery/Map/";
        #endregion
 
        public static string GetMainDEMPath(GISDataDownloaderDEMProvider DEMSource, out string DEMUrl, out string extension, DVector2 m_UpperLeftCoordiante, DVector2 m_DownRightCoordiante, out Vector2 UL_From, out Vector2 DR_To, out Vector2Int step)
        {
            string mainpath = "";
            DEMUrl = "";
            extension = "";
            UL_From = new Vector2(0, 0);
            DR_To = new Vector2(0, 0);
            step = new Vector2Int(0, 0);
            switch (DEMSource)
            {
                case GISDataDownloaderDEMProvider.SRTM_90m:

                    DEMUrl = srtm_90_tiff_url;
                    mainpath = TerrainStreamingParameters.srtm_90_tiff_temp_path;
                    extension = ".tif";
                    step = srtm_90_tiff_LatLon_Step;


                    UL_From = new Vector2(Mathf.FloorToInt((float)m_UpperLeftCoordiante.x / 5.0f) * 5 + 180, 90 - Mathf.FloorToInt((float)m_UpperLeftCoordiante.y / 5.0f) * 5);
                    DR_To = new Vector2(Mathf.FloorToInt((float)m_DownRightCoordiante.x / 5.0f) * 5 + 180, 90 - Mathf.FloorToInt((float)m_DownRightCoordiante.y / 5.0f) * 5);


                    if (!Directory.Exists(mainpath))
                        Directory.CreateDirectory(mainpath);

                    break;

                case GISDataDownloaderDEMProvider.SRTM_30m:

                    DEMUrl = srtm_30_hgt_url;
                    mainpath = TerrainStreamingParameters.srtm_30_hgt_temp_path;
                    extension = ".hgt";
                    step = srtm_30_hgt_LatLon_Step;

                    if (!Directory.Exists(mainpath))
                        Directory.CreateDirectory(mainpath);

                    break;

                case GISDataDownloaderDEMProvider.Mapbox:

                    DEMUrl = MapBox_PngRaw_url;
                    mainpath = TerrainStreamingParameters.Mapbox_pngraw_temp_path;
                    extension = ".pngraw";

                    if (!Directory.Exists(mainpath))
                        Directory.CreateDirectory(mainpath);

                    break;
            }

            return mainpath;
        }
    }
}