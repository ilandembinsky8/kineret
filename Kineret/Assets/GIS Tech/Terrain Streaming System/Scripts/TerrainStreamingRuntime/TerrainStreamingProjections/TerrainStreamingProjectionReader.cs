/*     Unity GIS Tech 2020-2021      */

using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

#if GISTerrainLoaderPdal
using pdal;
#endif

namespace GISTech.TerrainStreaming
{
    public class TerrainStreamingProjectionReader
    {

        public string Projection;
        public int ESPGCode;
        public int Zone;
        public string Datum;
        public string Ellps;
        public string Unite;
        public string ZoneLettre;

        public DVector2 TP_LatLon;
        public DVector2 DR_LatLon;

#if GISTerrainLoaderPdal
         public GISTerrainLoaderProjectionReader(string RasterPath, PdalRader type)
        {
            SourceFile = RasterPath;
            GenerateJson(type);
            LoadJsonData();
        }
        public GISTerrainLoaderProjectionReader()
        {
        }
        public void GenerateJson(PdalRader type)
        {
            string Pdalpipeline = "";
            if (type == PdalRader.Raster)
            {
                Pdalpipeline =
                   "{"
                       + "\n"
                       + "\"" + "pipeline" + "\"" + ":["
                       + "\n"
                    + "{"

                    + "\n"
                          + "\"filename\"" + ":" + "\"" + SourceFile.Replace(@"\", "/") + "\"" + ","

                          + "\n"

                          + "\"type\"" + ":" + "\"" + "readers.gdal" + "\""


                         + "\n"

                         + "}"

                         + "\n"

                         + "]"

                         + "\n"

                        + "}";
            }
            else
                if (type == PdalRader.LAS)
            {
                Pdalpipeline =
                   "{"
                       + "\n"
                       + "\"" + "pipeline" + "\"" + ":["
                       + "\n"
                    + "{"

                    + "\n"
                          + "\"filename\"" + ":" + "\"" + SourceFile.Replace(@"\", "/") + "\"" + ","

                          + "\n"

                          + "\"type\"" + ":" + "\"" + "readers.las" + "\""


                         + "\n"

                         + "}"

                         + "\n"

                         + "]"

                         + "\n"

                        + "}";
            }

            //Save Json
            var Jsonpath = Path.Combine(Application.persistentDataPath, JsonName);
            File.WriteAllText(Jsonpath, Pdalpipeline);

        }
        private void LoadJsonData()
        {
            try
            {
                //Load Json
                var Jsonpath = Path.Combine(Application.persistentDataPath, JsonName);
                string jsonFile = File.ReadAllText(Jsonpath);

                if (!string.IsNullOrEmpty(jsonFile))
                {
                    pdal.Config config = new pdal.Config();

                    if (Application.isPlaying && !Application.isEditor)
                    {
                        var PluginsPath = Application.dataPath + "/Plugins";
                        config.GdalData = PluginsPath + "/Lidar/gdal/Data";
                        config.Proj4Data = PluginsPath + "/Lidar/proj4/Data";
                    }
                    else
                    {
                        config.GdalData = Path.Combine(Application.dataPath, "GIS Tech/GIS Terrain Loader/Plugins/Lidar/gdal/Data");
                        config.Proj4Data = Path.Combine(Application.dataPath, "GIS Tech/GIS Terrain Loader/Plugins/Lidar/proj4/Data");

                    }

                    pdal.Pipeline pipeline = new pdal.Pipeline(jsonFile);

                    long TotalPipelinepointCount = pipeline.Execute();

                    //Read Header  + MetaData 

                    ReadLasMetaData(pipeline);

                    pipeline.Dispose();

                    if (TotalPipelinepointCount == 0)
                    {
                        Debug.LogError("File Not Valid ... ");

                        OnReadError();
                    }

                }


            }
            catch (Exception ex)
            {
                Debug.Log("Couldn't Load Terrain file: " + ex.Message + "  " + Environment.NewLine);

                OnReadError();
            };



        }
        private void ReadLasMetaData(pdal.Pipeline pipeline)
        {
            PointViewIterator views = pipeline.Views;
            PointView view = views != null ? views.Next : null;

            while (view != null)
            {
                var viewId = "View " + view.Id;
                var tproj4 = "\tproj4: " + view.Proj4;
                var tWKT = "\tWKT: " + view.Wkt;

                var ProjData = view.Proj4.Split('+');

                foreach (var line in ProjData)
                {
                    var linedata = line.Split('=');
                    if (linedata.Count() > 1)
                    {
                        var key = linedata[0];
                        var value = linedata[1].Trim();

                        if (key == "proj")
                            Projection = value.Trim();
                        if (key == "datum")
                            Datum = value;
                        if (key == "ellps")
                            Ellps = value;
                        if (key == "zone")
                            Zone = int.Parse(value);
                        if (key == "units")
                            Unite = value;


                    }

                }
                view.Dispose();
                view = views.Next;
                //Debug.Log(" Projection " + Projection + " Datum " + Datum + " Ellps " + Ellps + " Zone " + Zone + " Unite " + Unite);

            }

            if (views != null)
            {
                views.Dispose();
            }
        }
#endif

        /// <summary>
        /// Read *.Prj file
        /// </summary>
        /// <param name="prjFile">Full path for the prj file</param>
        public static TSSGeographicCoordinateSystem ReadProjectionFile(string PathFile)
        {
           TSSGeographicCoordinateSystem CoordinateReferenceSystem = null;

            string prjFile = PathFile.Replace(Path.GetExtension(PathFile), ".prj");

            if (string.IsNullOrEmpty(prjFile))
                throw new ArgumentNullException("prjFile");

            // Prj file is optional so ignore if it doesn't exist
            if (!File.Exists(prjFile))
            {
                return null;
            }
            TSSGeographicCoordinateSystem gcs = new TSSGeographicCoordinateSystem();
            GTLProjection prj = new GTLProjection();

            System.IO.TextReader tr = new StreamReader(prjFile);
            string prjContent = tr.ReadLine();

            Regex PROJCS = new Regex("(?:PROJCS\\[\")(?<PRJName>.*)(?:\",GEOGCS\\[\")(?<CRSName>.*" +
      ")(?:\",DATUM\\[\")(?<DatumName>.*)(?:\",SPHEROID\\[\")(?<Sph" +
      "eroidName>.*)(?:\",)(?<InverseFlatteningRatio>.*)(?:,)(?<Ax" +
      "is>.*)(?:\\]\\])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);


            Regex GEOGCS = new Regex("(?:GEOGCS\\[\")(?<CRSName>.*" +
      ")(?:\",DATUM\\[\")(?<DatumName>.*)(?:\",SPHEROID\\[\")(?<Sph" +
      "eroidName>.*)(?:\",)(?<InverseFlatteningRatio>.*)(?:,)(?<Ax" +
      "is>.*)(?:\\]\\])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

            if (PROJCS.IsMatch(prjContent))
            {
                Match m = PROJCS.Match(prjContent);
                gcs.Name = m.Groups["PRJName"].Value;
                gcs.GEOGCSProjection = m.Groups["CRSName"].Value;
                GTLRefDatum rd = new GTLRefDatum();
                rd.Name = m.Groups["DatumName"].Value;
                gcs.Datum = rd;
                String[] prjParamsArray = m.Groups["Parameters"].Value.Split(new Char[] { ',' });
                List<GTLProjectionParameter> prjParams = new List<GTLProjectionParameter>();
                for (int i = 0; i < prjParamsArray.Length - 1; i++)
                {
                    prjParams.Add(new GTLProjectionParameter(prjParamsArray[i].Replace("PARAMETER[", "").Replace("\"", ""), double.Parse(prjParamsArray[i + 1].Replace("]", ""), CultureInfo.InvariantCulture)));
                    i++;
                }
                prj.Parameters = prjParams;

                gcs.projectionData = prj;
            }
            else if (GEOGCS.IsMatch(prjContent))
            {
                Match m = GEOGCS.Match(prjContent);
                gcs.GEOGCSProjection = m.Groups["CRSName"].Value;
                GTLRefDatum rd = new GTLRefDatum();
                rd.Name = m.Groups["DatumName"].Value;
                gcs.Datum = rd;
                List<GTLProjectionParameter> prjParams = new List<GTLProjectionParameter>();
                prj.Parameters = prjParams;
                gcs.projectionData = prj;
            }

            tr.Close();

            CoordinateReferenceSystem = gcs;

            return CoordinateReferenceSystem;
        }
    }
}