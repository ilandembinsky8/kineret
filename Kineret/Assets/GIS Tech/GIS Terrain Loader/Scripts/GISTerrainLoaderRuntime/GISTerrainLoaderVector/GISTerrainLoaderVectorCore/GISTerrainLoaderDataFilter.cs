/*     Unity GIS Tech 2020-2023      */

using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Security.Cryptography;
 

namespace GISTech.GISTerrainLoader
{
    public class GISTerrainLoaderDataFilter 
    {
 
        private static HashSet<string> PointsAlreadyAdded = new HashSet<string>();
        private static HashSet<string> LinesAlreadyAdded = new HashSet<string>();
        private static HashSet<string> PolygoneAlreadyAdded = new HashSet<string>();
 
        public static List<GISTerrainLoaderPointGeoData> GetGeoVectorPointsData(GISTerrainLoaderGeoVectorData GeoData, List<string> FiltredAttributes)
        {
            PointsAlreadyAdded = new HashSet<string>();

            List<GISTerrainLoaderPointGeoData> GeoVectorPointsData = new List<GISTerrainLoaderPointGeoData>();

            for (int i = 0; i < GeoData.GeoPoints.Count; i++)
            {
                var Geopoint = GeoData.GeoPoints[i];
    
                IEnumerable<string> PointsIntersection = null;
                var AttributesKeys = Geopoint.GetDataBaseKeys();
                PointsIntersection = FiltredAttributes.Intersect<string>(AttributesKeys);

                if (PointsIntersection != null)
                {
                    var ID = Geopoint.ID;

                    foreach (var attribute in PointsIntersection)
                    {
                        var Value = "";

                        if (Geopoint.TryGetValue(attribute, out Value))
                        {
                            if (!string.IsNullOrEmpty(Value) && !PointsAlreadyAdded.Contains(ID))
                            {
                                Geopoint.Tag = Value.Trim();
                                GeoVectorPointsData.Add(Geopoint);
                                PointsAlreadyAdded.Add(ID);

                            }

                        }
                    }
                }
            }
            return GeoVectorPointsData;
        }
        public static List<GISTerrainLoaderLineGeoData> GetGeoVectorLinesData(GISTerrainLoaderGeoVectorData GeoData, List<string> FiltredAttributes)
        {
            LinesAlreadyAdded = new HashSet<string>();

            List<GISTerrainLoaderLineGeoData> GeoVectorLinesData = new List<GISTerrainLoaderLineGeoData>();

            for (int i = 0; i < GeoData.GeoLines.Count; i++)
            {
                var GeoLine = GeoData.GeoLines[i];

                IEnumerable<string> LinesIntersection = null;
                var AttributesKeys = GeoLine.GetDataBaseKeys();
                LinesIntersection = FiltredAttributes.Intersect<string>(AttributesKeys);

                if (LinesIntersection != null)
                {
                    var ID = GeoLine.ID;

                    foreach (var attribute in LinesIntersection)
                    {
                        var Value = "";

                        if (GeoLine.TryGetValue(attribute, out Value))
                        {
                            if (!string.IsNullOrEmpty(Value) && !LinesAlreadyAdded.Contains(ID))
                            {
                                GeoLine.Tag = Value.Trim();
                                GeoVectorLinesData.Add(GeoLine);
                                LinesAlreadyAdded.Add(ID);

                            }

                        }
                    }


                }
            }
            return GeoVectorLinesData;
        }
        public static List<GISTerrainLoaderPolygonGeoData> GetGeoVectorPolyData(GISTerrainLoaderGeoVectorData GeoData, List<string> FiltredAttributes)
        {
            PolygoneAlreadyAdded = new HashSet<string>();

            List<GISTerrainLoaderPolygonGeoData> GeoVectorPolygoneData = new List<GISTerrainLoaderPolygonGeoData>();

            for (int i = 0; i < GeoData.GeoPolygons.Count; i++)
            {
                var GeoPolygone = GeoData.GeoPolygons[i];

                IEnumerable<string> PolygonsIntersection = null;

                var AttributesKeys = GeoPolygone.GetDataBaseKeys();

                PolygonsIntersection = FiltredAttributes.Intersect<string>(AttributesKeys);

                if (PolygonsIntersection != null)
                {
                    var ID = GeoPolygone.ID;

                    foreach (var attribute in PolygonsIntersection)
                    {
                        var Value = "";

                        if (GeoPolygone.TryGetValue(attribute, out Value))
                        {
                            if (!string.IsNullOrEmpty(Value) && !PolygoneAlreadyAdded.Contains(ID))
                            {
                                GeoPolygone.Tag = Value.Trim();
                                GeoVectorPolygoneData.Add(GeoPolygone);
                                PolygoneAlreadyAdded.Add(ID);
                            }
                        }
                    }

                }
            }
            return GeoVectorPolygoneData;
        }



#if GISTechAdvancedVectorGenerator
        public static List<GISTerrainLoaderPointGeoData> AVG_FilterGeoPointsData(GISTerrainLoaderGeoVectorData SourceGeoData, GISTerrainLoaderPrefs prefs)
        {

#if GISTechS57
            if (prefs.S57SymbolMappingDatabase_SO == null)
                prefs.LoadS57SymbolMappingDatabase();
#endif
            PointsAlreadyAdded = new HashSet<string>();

            List<GISTerrainLoaderPointGeoData> DesGeoVectorPointsData = new List<GISTerrainLoaderPointGeoData>();

            var Tags = new List<string>();

            for (int i = 0; i < prefs.SymbolsDic.PointsFeatures.Count; i++)
            {
                var feature = prefs.SymbolsDic.PointsFeatures[i];

                if (!string.IsNullOrEmpty(feature.Tag) && feature.Enable)
                {
                    Tags.Add(feature.Tag);
                }
            }

            var AddAnyPoly = prefs.SymbolsDic.PointsFeatures.Find(x => x.Tag == "AnyPoint");
 
            for (int i = 0; i < SourceGeoData.GeoPoints.Count; i++)
            {
                var GeoPoint = SourceGeoData.GeoPoints[i];

                IEnumerable<string> PointsIntersection = null;

                var AttributesValues = GeoPoint.GetDataBaseValues();
  
                PointsIntersection = Tags.Intersect<string>(AttributesValues);
 
                //if(PointsIntersection.Count()==0)
                //{
                //    foreach (var attribute in GeoPoint.GetDataBaseKeys())
                //    {
                //        Debug.Log("AVG_FilterGeoPointsData " + GeoPoint.ID + "  " + attribute +"  " +GeoPoint.GetAttributeValue(attribute));
                //    }
                //}

                if (PointsIntersection != null)
                {
                    string ID = "";
 
                    if (string.IsNullOrEmpty(GeoPoint.ID.Trim()))
                        ID = i.ToString();
                    else
                        ID = GeoPoint.ID.Trim();
 
                    foreach (var attribute in PointsIntersection)
                    {

                        if (prefs.VectorParameters_SO.CheckForDuplicatedID)
                        {

                            if (!PointsAlreadyAdded.Contains(ID))
                            {
                                GeoPoint.ID = ID;
                                GeoPoint.Tag = attribute.Trim();

#if GISTechS57
                                if (prefs.VectorParameters_SO.S57ParseRelevantAttributes)
                                {

                                    var symbolKey = GISTerrainLoaderS57SymbolEnumResolver.ResolveSymbol(attribute.Trim(), prefs.S57SymbolMappingDatabase_SO, GISTech.S57Reader.GISTechS57GeometricPrimitive.Point, GeoPoint.DataBase);
                                    //Debug.Log(attribute.Trim() + "  " + symbolKey.GetDescription());
                                    GeoPoint.Tag = symbolKey.GetDescription();
                                    GeoPoint.Name = GeoPoint.GetAttributeValue("OBJNAM");
                                }
#endif
                                DesGeoVectorPointsData.Add(GeoPoint);

                                PointsAlreadyAdded.Add(ID);


                            }
                        }
                        else
                        {
                            GeoPoint.ID = ID;
                            GeoPoint.Tag = attribute.Trim();

#if GISTechS57
                            if (prefs.VectorParameters_SO.S57ParseRelevantAttributes)
                            {
                                var symbolKey = GISTerrainLoaderS57SymbolEnumResolver.ResolveSymbol(attribute.Trim(), prefs.S57SymbolMappingDatabase_SO, GISTech.S57Reader.GISTechS57GeometricPrimitive.Point, GeoPoint.DataBase);
                                //Debug.Log(attribute.Trim() + "  " + symbolKey.GetDescription());

                                GeoPoint.Tag = symbolKey.GetDescription();
                                GeoPoint.Name = GeoPoint.GetAttributeValue("OBJNAM");
                            }
#endif

                            DesGeoVectorPointsData.Add(GeoPoint);
                            PointsAlreadyAdded.Add(ID);
                        }


                    }

                }

                if (PointsIntersection != null && AddAnyPoly != null)
                {
                    if (PointsIntersection.Count() == 0 && AddAnyPoly.Enable)
                    {
                        GeoPoint.Tag = "AnyPoint";
                        DesGeoVectorPointsData.Add(GeoPoint);
                        PointsAlreadyAdded.Add(GeoPoint.ID);
                    }
                }
            }
 
            return DesGeoVectorPointsData;
        }
        public static List<GISTerrainLoaderLineGeoData> AVG_FilterGeoLinesData(GISTerrainLoaderGeoVectorData SourceGeoData, GISTerrainLoaderPrefs prefs)
        {
            LinesAlreadyAdded = new HashSet<string>();

            List<GISTerrainLoaderLineGeoData> DesGeoVectorLineData = new List<GISTerrainLoaderLineGeoData>();

            var Tags = new List<string>();

            for (int i = 0; i < prefs.SymbolsDic.LineFeatures.Count; i++)
            {
                var feature = prefs.SymbolsDic.LineFeatures[i];
                if (!string.IsNullOrEmpty(feature.Tag) && feature.Enable)
                    Tags.Add(feature.Tag);
            }

            var AddAnyPoly = prefs.SymbolsDic.LineFeatures.Find(x => x.Tag == "AnyLine");

            for (int i = 0; i < SourceGeoData.GeoLines.Count; i++)
            {
                var GeoLines = SourceGeoData.GeoLines[i];

                IEnumerable<string> LinesIntersection = null;

                var AttributesValues = GeoLines.GetDataBaseValues();

                LinesIntersection = Tags.Intersect<string>(AttributesValues);

                if (LinesIntersection != null)
                {
                    string ID = "";

                    if (string.IsNullOrEmpty(GeoLines.ID.Trim()))
                        ID = i.ToString();
                    else
                        ID = GeoLines.ID.Trim();

                    foreach (var attribute in LinesIntersection)
                    {
                        if (prefs.VectorParameters_SO.CheckForDuplicatedID)
                        {
                            if (!LinesAlreadyAdded.Contains(ID))
                            {
                                GeoLines.ID = ID;
                                GeoLines.Tag = attribute.Trim();

#if GISTechS57
                                if (prefs.VectorParameters_SO.S57ParseRelevantAttributes)
                                {
                                    var symbolKey = GISTerrainLoaderS57SymbolEnumResolver.ResolveSymbol(attribute.Trim(), prefs.S57SymbolMappingDatabase_SO, GISTech.S57Reader.GISTechS57GeometricPrimitive.Line, GeoLines.DataBase);
                                    GeoLines.Tag = symbolKey.GetDescription();
                                    GeoLines.Name = GeoLines.GetAttributeValue("OBJNAM");
                                    
                                }
#endif

                                DesGeoVectorLineData.Add(GeoLines);
                                LinesAlreadyAdded.Add(ID);

                            }
                        }
                        else
                        {
                            GeoLines.ID = ID;
                            GeoLines.Tag = attribute.Trim();


#if GISTechS57
                            if (prefs.VectorParameters_SO.S57ParseRelevantAttributes)
                            {
                                var symbolKey = GISTerrainLoaderS57SymbolEnumResolver.ResolveSymbol(attribute.Trim(), prefs.S57SymbolMappingDatabase_SO, GISTech.S57Reader.GISTechS57GeometricPrimitive.Line, GeoLines.DataBase);
                                GeoLines.Tag = symbolKey.GetDescription();
                                GeoLines.Name = GeoLines.GetAttributeValue("OBJNAM");
                            }
#endif

                            DesGeoVectorLineData.Add(GeoLines);
                            LinesAlreadyAdded.Add(ID);
                        }
                    }

                }

                if (LinesIntersection != null && AddAnyPoly != null)
                {
                    if (LinesIntersection.Count() == 0 && AddAnyPoly.Enable)
                    {
                        GeoLines.Tag = "AnyLine";
                        DesGeoVectorLineData.Add(GeoLines);
                        LinesAlreadyAdded.Add(GeoLines.ID);
                    }
                }
            }

            return DesGeoVectorLineData;
        }
        public static List<GISTerrainLoaderPolygonGeoData> AVG_FilterGeoPolyData(GISTerrainLoaderGeoVectorData SourceGeoData, GISTerrainLoaderPrefs prefs)
        {
            PolygoneAlreadyAdded = new HashSet<string>();

            List<GISTerrainLoaderPolygonGeoData> DesGeoVectorPolygoneData = new List<GISTerrainLoaderPolygonGeoData>();

            var Tags = new List<string>();

            for (int i = 0; i < prefs.SymbolsDic.PolygonFeatures.Count; i++)
            {
                var feature = prefs.SymbolsDic.PolygonFeatures[i];
                if (!string.IsNullOrEmpty(feature.Tag) && feature.Enable)
                    Tags.Add(feature.Tag);
            }
            var AddAnyPoly = prefs.SymbolsDic.PolygonFeatures.Find(x => x.Tag == "AnyPolygon");

            for (int i = 0; i < SourceGeoData.GeoPolygons.Count; i++)
            {
                var GeoPolygone = SourceGeoData.GeoPolygons[i];

                IEnumerable<string> PolygonsIntersection = null;

                var AttributesValues = GeoPolygone.GetDataBaseValues();

                PolygonsIntersection = Tags.Intersect<string>(AttributesValues);

                if (PolygonsIntersection != null)
                {
                    string ID = "";

                    if (string.IsNullOrEmpty(GeoPolygone.ID.Trim()))
                        ID = i.ToString();
                    else
                        ID = GeoPolygone.ID.Trim();
 
                    foreach (var attribute in PolygonsIntersection)
                    {
                        if (prefs.VectorParameters_SO.CheckForDuplicatedID)
                        {
                            if (!PolygoneAlreadyAdded.Contains(ID))
                            {
                                GeoPolygone.ID = ID;
                                GeoPolygone.Tag = attribute.Trim();

#if GISTechS57
                                if (prefs.VectorParameters_SO.S57ParseRelevantAttributes)
                                {
                                    var symbolKey = GISTerrainLoaderS57SymbolEnumResolver.ResolveSymbol(attribute.Trim(), prefs.S57SymbolMappingDatabase_SO, GISTech.S57Reader.GISTechS57GeometricPrimitive.Area, GeoPolygone.DataBase);
                                    GeoPolygone.Tag = symbolKey.GetDescription();
                                    GeoPolygone.Name = GeoPolygone.GetAttributeValue("OBJNAM");
                                }
#endif


                                DesGeoVectorPolygoneData.Add(GeoPolygone);
                                PolygoneAlreadyAdded.Add(ID);
                            }
                        }
                        else
                        {
                            GeoPolygone.ID = ID;
                            GeoPolygone.Tag = attribute.Trim();
#if GISTechS57
                            if (prefs.VectorParameters_SO.S57ParseRelevantAttributes)
                            {
                                var symbolKey = GISTerrainLoaderS57SymbolEnumResolver.ResolveSymbol(attribute.Trim(), prefs.S57SymbolMappingDatabase_SO, GISTech.S57Reader.GISTechS57GeometricPrimitive.Area, GeoPolygone.DataBase);
                                GeoPolygone.Tag = symbolKey.GetDescription();
                                GeoPolygone.Name = GeoPolygone.GetAttributeValue("OBJNAM");
                            }
#endif
                            DesGeoVectorPolygoneData.Add(GeoPolygone);
                            PolygoneAlreadyAdded.Add(ID);
                        }

                 

                    }

                }

                if (PolygonsIntersection != null && AddAnyPoly != null)
                {
                    if (PolygonsIntersection.Count() == 0 && AddAnyPoly.Enable)
                    {
                        GeoPolygone.Tag = "AnyPolygon";
                        DesGeoVectorPolygoneData.Add(GeoPolygone);

                    }
                }

            }
            return DesGeoVectorPolygoneData;
        }
#endif




    }
}