/*     Unity GIS Tech 2020-2021      */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace GISTech.TerrainStreaming
{
    public class TerrainStreamingOSMFileLoader : TerrainStreamingGeoDataHolder
    {
        private static object osmDataLock;
        public TerrainStreamingOSMData osmData;

        public TerrainStreamingOSMFiltredData FiltredPointsData;
        public TerrainStreamingOSMFiltredData FiltredGrassData;
        public TerrainStreamingOSMFiltredData FiltredRoadsData;
        public TerrainStreamingOSMFiltredData FiltredBuildingsData;
        public TerrainStreamingOSMFiltredData FiltredTreesData;


        private static TerrainStreamingAttributes_SO Attribute_Roads;

        public TerrainStreamingOSMFileLoader(string FilePath, TerrainStreamingTileData tile)
        {

            var parser = new TerrainStreamingOSMParser();
            osmData = parser.ParseFromFile(FilePath);
            osmData.FillNodes(tile);
            FilterData();
        }

        private void FilterData()
        {
            FiltredPointsData = new TerrainStreamingOSMFiltredData();

            FiltredRoadsData = new TerrainStreamingOSMFiltredData();
            FiltredTreesData = new TerrainStreamingOSMFiltredData();

            FiltredBuildingsData = new TerrainStreamingOSMFiltredData();
            FiltredGrassData = new TerrainStreamingOSMFiltredData();


            var Attribute_Points = Resources.Load("VectorAttributes/Attribute_Points") as TerrainStreamingAttributes_SO;

            if (!Attribute_Points)
            {
                Attribute_Points = new TerrainStreamingAttributes_SO();
                Attribute_Points.Content = new List<TerrainStreamingVectorTag>();
                Debug.LogError("Roads Attribute File Not found .. Restore 'Attribute_Roads' ScriptableObject ");
            }

            var Attribute_Roads = Resources.Load("VectorAttributes/Attribute_Roads") as TerrainStreamingAttributes_SO;
            if (!Attribute_Roads)
            {
                Attribute_Roads = new TerrainStreamingAttributes_SO();
                Attribute_Roads.Content = new List<TerrainStreamingVectorTag>();
                Debug.LogError("Roads Attribute File Not found .. Restore 'Attribute_Roads' ScriptableObject ");
            }

            var Attribute_Trees = Resources.Load("VectorAttributes/Attribute_Trees") as TerrainStreamingAttributes_SO;
            if (!Attribute_Trees)
            {
                Attribute_Trees = new TerrainStreamingAttributes_SO();
                Attribute_Trees.Content = new List<TerrainStreamingVectorTag>();
                Debug.LogError("Trees Attribute File Not found .. Restore 'Attribute_Trees' ScriptableObject ");
            }

            var Attribute_Grass = Resources.Load("VectorAttributes/Attribute_Grass") as TerrainStreamingAttributes_SO;
            if (!Attribute_Grass)
            {
                Attribute_Grass = new TerrainStreamingAttributes_SO();
                Attribute_Grass.Content = new List<TerrainStreamingVectorTag>();
                Debug.LogError("Grass Attribute File Not found .. Restore 'Attribute_Grass' ScriptableObject ");
            }
            var Attribute_Buildings = Resources.Load("VectorAttributes/Attribute_Buildings") as TerrainStreamingAttributes_SO;
            if (!Attribute_Buildings)
            {
                Attribute_Buildings = new TerrainStreamingAttributes_SO();
                Attribute_Buildings.Content = new List<TerrainStreamingVectorTag>();
                Debug.LogError("Buildings Attribute File Not found .. Restore 'Attribute_Buildings' ScriptableObject ");
            }




            foreach (var wayDic in osmData.Ways)
            {
                long wayID = long.Parse(wayDic.Value.Id);

                var WayDicAttributes = wayDic.Value.Tags;

                foreach (var Value in WayDicAttributes)
                {
                    //Roads
                    if (Attribute_Roads.Contains(new TerrainStreamingVectorTag(Value.Attribute, Value.Value)))
                    {
                        wayDic.Value.MainTag.Attribute = Value.Attribute;
                        wayDic.Value.MainTag.Value = Value.Value;

                        if (!FiltredRoadsData.Ways.ContainsKey(wayID))
                        {
                            FiltredRoadsData.Ways.Add(wayID, wayDic.Value);

                            break;
                        }

                    }
                    
                    //Trees
                    if (Attribute_Trees.ContainsAndTagEnabled(new TerrainStreamingVectorTag(Value.Attribute, Value.Value)))
                    {
                        wayDic.Value.MainTag.Attribute = Value.Attribute;
                        wayDic.Value.MainTag.Value = Value.Value;
                        if (!FiltredTreesData.Ways.ContainsKey(wayID))
                        {
                        FiltredTreesData.Ways.Add(wayID, wayDic.Value);
                        }

                    }

                    //Grass
                    if (Attribute_Grass.ContainsAndTagEnabled(new TerrainStreamingVectorTag(Value.Attribute, Value.Value)))
                    {
                        wayDic.Value.MainTag.Attribute = Value.Attribute;
                        wayDic.Value.MainTag.Value = Value.Value;

                        if (!FiltredGrassData.Ways.ContainsKey(wayID))
                        {
                            FiltredGrassData.Ways.Add(wayID, wayDic.Value);
                            break;
                        }

                    }
                    //Buildings
                    if (Attribute_Buildings.Contains(new TerrainStreamingVectorTag(Value.Attribute, Value.Value)))
                    {
                        wayDic.Value.MainTag.Attribute = Value.Attribute;
                        wayDic.Value.MainTag.Value = Value.Value;

                        if (!FiltredBuildingsData.Ways.ContainsKey(wayID))
                        {
                            FiltredBuildingsData.Ways.Add(wayID, wayDic.Value);
                            break;
                        }

                    }
                }

            }


            foreach (var node in osmData.Nodes)
            {
                long wayID = node.Key;

                var nodeAttributes = node.Value.Tags;

                foreach (var Value in nodeAttributes)
                {
                    //Points
                    if (Attribute_Points.Contains(new TerrainStreamingVectorTag(Value.Attribute, Value.Value)))
                    {
                        node.Value.MainTag.Attribute = Value.Attribute;
                        node.Value.MainTag.Value = Value.Value;

                        if (!FiltredPointsData.Nodes.ContainsKey(wayID))
                        {
                            FiltredPointsData.Nodes.Add(wayID, node.Value);
                            break;
                        }
                    }
                    //Grass
                    if (Attribute_Grass.Contains(new TerrainStreamingVectorTag(Value.Attribute, Value.Value)))
                    {
                        if (!FiltredGrassData.Nodes.ContainsKey(wayID))
                        {
                            node.Value.MainTag.Attribute = Value.Attribute;
                            node.Value.MainTag.Value = Value.Value;
                            FiltredGrassData.Nodes.Add(wayID, node.Value);
                        }
                    }

                }

            }




            //Add Points to road


            foreach (var wayDic in FiltredRoadsData.Ways)
            {
                var nodes = GeneratePoints(wayDic.Value.Nodes, 0.01);

                wayDic.Value.Nodes = nodes;
            }


        }

   
        public override void GetGeoVectorRoadsData(TerrainStreamingTileData Tile, ref TerrainStreamingGeoVectorData GeoDataContainer)
        {
            if (osmData.Ways.Count != 0)
            {

                foreach (var wayDic in FiltredRoadsData.Ways)
                {

                    TerrainStreamingLinesGeoData RoadGeoData = new TerrainStreamingLinesGeoData();

                    var Line = wayDic;

                    List<DVector2> LinePoints = new List<DVector2>();

                    RoadGeoData.Tag_Key = Line.Value.MainTag.Attribute;
                    RoadGeoData.Tag_Value = Line.Value.MainTag.Value;
                    RoadGeoData.ID = wayDic.Value.Id;

                    for (int i = 0; i < Line.Value.Nodes.Count; i++)
                    {
                        var latlon = new DVector2(Line.Value.Nodes[i].Lon, Line.Value.Nodes[i].Lat);
                        LinePoints.Add(latlon);
                    }

                    foreach (var attribute in Line.Value.Tags)
                    {
                        if (attribute.Attribute == "name")
                            RoadGeoData.Name = attribute.Value;
                    }


                    TerrainStreamingPolygonGeoData tilePoly = new TerrainStreamingPolygonGeoData();
                    tilePoly.GeoPoints.Add(Tile.UpperLeftCoordinate);
                    tilePoly.GeoPoints.Add(new DVector2(Tile.BottomRightCoordiante.x, Tile.UpperLeftCoordinate.y));
                    tilePoly.GeoPoints.Add(Tile.BottomRightCoordiante);
                    tilePoly.GeoPoints.Add(new DVector2(Tile.UpperLeftCoordinate.x, Tile.BottomRightCoordiante.y));

                    List<DVector2> IntersectedPoints = new List<DVector2>();

                    foreach (var point in LinePoints)
                    {
                        if (TerrainStreamingExtensions.IsPointInPolygon4(tilePoly.GeoPoints, point))
                            IntersectedPoints.Add(point);

                    }

                    if (IntersectedPoints.Count >= 2)
                    {
                        RoadGeoData.GeoPoints = new List<DVector2>();
                        RoadGeoData.GeoPoints.AddRange(IntersectedPoints);
                        GeoDataContainer.GeoRoads.Add(RoadGeoData);
                    }

                }
            }

        }






        public List<TerrainStreamingOSMNode> GeneratePoints(List<TerrainStreamingOSMNode> nodes, double pointDistance)
        {
            List<TerrainStreamingOSMNode> Newnodes = new List<TerrainStreamingOSMNode>();
 

            if (nodes.Count <= 1)
            {
                return null;
            }


            for (int i = 0; i < nodes.Count-1; i++)
            {
                DVector2 start = new DVector2(nodes[i].Lon, nodes[i].Lat);
                DVector2 next = new DVector2(nodes[i+1].Lon, nodes[i+1].Lat);

                double contextDistance = TerrainStreamingGeoConversion.CalDistance(start, next);

                if (contextDistance > pointDistance)
                {
                    int segments = Mathf.RoundToInt((float)(contextDistance / pointDistance));

                    var X_Dist =Math.Abs((next.x - start.x) / segments);
                    var Y_Dist = Math.Abs((next.y - start.y) / segments);

                    if(segments>=2)
                    {
                        List<TerrainStreamingOSMNode> Subnodes = new List<TerrainStreamingOSMNode>();

                        for (int s = 0; s <= segments; s++)
                        {
                            var Seg_start = CalculatePoint(start, next, X_Dist, Y_Dist);

                            var node_Start = new TerrainStreamingOSMNode();
                            node_Start.Lon = start.x;
                            node_Start.Lat = start.y;
                            Subnodes.Add(node_Start);

                            var node_next = new TerrainStreamingOSMNode();
                            node_next.Lon = Seg_start.x;
                            node_next.Lat = Seg_start.y;
                            Subnodes.Add(node_next);

                            start = Seg_start;


                        }
                        Newnodes.AddRange(Subnodes);
                    }

                }
                else
                {
                    var node_Start = new TerrainStreamingOSMNode();
                    node_Start.Lon = start.x;
                    node_Start.Lat = start.y;

                    var node_next = new TerrainStreamingOSMNode();
                    node_next.Lon = next.x;
                    node_next.Lat = next.y;

                    Newnodes.Add(node_Start);
                    Newnodes.Add(node_next);
                }
            }

            return Newnodes;
        }
        private static DVector2 CalculatePoint(DVector2 a, DVector2 b, double distance_x, double distance_y)
        {
            var dir = (b - a).Normalized();
            // d. calculate and Draw the new vector,
            return new DVector2((a.x) + dir.x * distance_x, (a.y) + dir.y * distance_y);
        }
        public static List<TerrainStreamingOSMNode> SplitLine(DVector2 a,DVector2 b,int count)
        {
            count = count + 1;

            Double d = Math.Sqrt((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y)) / count;
            Double fi = Math.Atan2(b.y - a.y, b.x - a.x);

            List<TerrainStreamingOSMNode> points = new List<TerrainStreamingOSMNode>(count + 1);

            for (int i = 0; i <= count; ++i)
            {
                var node = new TerrainStreamingOSMNode();
                node.Lon = a.x + i * d * Math.Cos(fi);
                node.Lat = a.y + i * d * Math.Sin(fi);

                points.Add(node);

            }

            return points;
        }
        public override void GetGeoVectorTreesData(TerrainStreamingTileData Tile, ref TerrainStreamingGeoVectorData GeoDataContainer)
        {
            if (osmData.Ways.Count != 0)
            {
                foreach (var wayDic in FiltredTreesData.Ways)
                {
                    TerrainStreamingPolygonGeoData TreeGeoData = new TerrainStreamingPolygonGeoData();

                    var Poly = wayDic;

                    List<DVector2> PolyPoints = new List<DVector2>();

                    TreeGeoData.Tag_Key = Poly.Value.MainTag.Attribute;
                    TreeGeoData.Tag_Value = Poly.Value.MainTag.Value;
                    TreeGeoData.ID = wayDic.Value.Id;

                    for (int i = 0; i < Poly.Value.Nodes.Count; i++)
                    {
                        var latlon = new DVector2(Poly.Value.Nodes[i].Lon, Poly.Value.Nodes[i].Lat);
                        PolyPoints.Add(latlon);
                    }

                    foreach (var attribute in Poly.Value.Tags)
                    {
                        if (attribute.Attribute == "name")
                            TreeGeoData.Name = attribute.Value;
                     }

                    TerrainStreamingPolygonGeoData tilePoly = new TerrainStreamingPolygonGeoData();
                    tilePoly.GeoPoints.Add(Tile.UpperLeftCoordinate);
                    tilePoly.GeoPoints.Add(new DVector2(Tile.BottomRightCoordiante.x, Tile.UpperLeftCoordinate.y));
                    tilePoly.GeoPoints.Add(Tile.BottomRightCoordiante);
                    tilePoly.GeoPoints.Add(new DVector2(Tile.UpperLeftCoordinate.x, Tile.BottomRightCoordiante.y));


                    List<DVector2> TilePoints = new List<DVector2>();

                    if (TerrainStreamingExtensions.IsPointInPolygon4(PolyPoints, Tile.UpperLeftCoordinate))
                        TilePoints.Add(Tile.UpperLeftCoordinate);
                    if (TerrainStreamingExtensions.IsPointInPolygon4(PolyPoints, new DVector2(Tile.BottomRightCoordiante.x, Tile.UpperLeftCoordinate.y)))
                        TilePoints.Add(new DVector2(Tile.BottomRightCoordiante.x, Tile.UpperLeftCoordinate.y));
                    if (TerrainStreamingExtensions.IsPointInPolygon4(PolyPoints, Tile.BottomRightCoordiante))
                        TilePoints.Add(Tile.BottomRightCoordiante);
                    if (TerrainStreamingExtensions.IsPointInPolygon4(PolyPoints, new DVector2(Tile.UpperLeftCoordinate.x, Tile.BottomRightCoordiante.y)))
                        TilePoints.Add(new DVector2(Tile.UpperLeftCoordinate.x, Tile.BottomRightCoordiante.y));

                    if (TilePoints.Count == 4)
                    {
                        TreeGeoData.GeoPoints = new List<DVector2>();
                        TreeGeoData.GeoPoints.AddRange(TilePoints);
                        GeoDataContainer.GeoTrees.Add(TreeGeoData);
                    }
                    else
                    if (PolyPoints.Count >= 3)
                    {
                        var PolyInter = TerrainStreamingPolygonMathUtility.ClipPolygons(PolyPoints, tilePoly.GeoPoints, BooleanOperation.Intersection);

                        foreach (var Points in PolyInter)
                        {
                            if (Points.Count >= 3)
                            {
                                TreeGeoData.GeoPoints = new List<DVector2>();
                                TreeGeoData.GeoPoints.AddRange(Points);
                                GeoDataContainer.GeoTrees.Add(TreeGeoData);
                            }
                        }
                    }
                }
            }

        }
        public override void GetGeoVectorGrassData(TerrainStreamingTileData Tile, ref TerrainStreamingGeoVectorData GeoDataContainer)
        {
            if (osmData.Ways.Count != 0)
            {
                foreach (var wayDic in FiltredGrassData.Ways)
                {
                    TerrainStreamingPolygonGeoData GrassGeoData = new TerrainStreamingPolygonGeoData();

                    var Poly = wayDic;

                    List<DVector2> PolyPoints = new List<DVector2>();

                    GrassGeoData.Tag_Key = Poly.Value.MainTag.Attribute;
                    GrassGeoData.Tag_Value = Poly.Value.MainTag.Value;
                    GrassGeoData.ID = wayDic.Value.Id;

                    for (int i = 0; i < Poly.Value.Nodes.Count; i++)
                    {
                        var latlon = new DVector2(Poly.Value.Nodes[i].Lon, Poly.Value.Nodes[i].Lat);
                        PolyPoints.Add(latlon);
                    }

                    foreach (var attribute in Poly.Value.Tags)
                    {
                        if (attribute.Attribute == "name")
                            GrassGeoData.Name = attribute.Value;
                    }

                    TerrainStreamingPolygonGeoData tilePoly = new TerrainStreamingPolygonGeoData();
                    tilePoly.GeoPoints.Add(Tile.UpperLeftCoordinate);
                    tilePoly.GeoPoints.Add(new DVector2(Tile.BottomRightCoordiante.x, Tile.UpperLeftCoordinate.y));
                    tilePoly.GeoPoints.Add(Tile.BottomRightCoordiante);
                    tilePoly.GeoPoints.Add(new DVector2(Tile.UpperLeftCoordinate.x, Tile.BottomRightCoordiante.y));


                    List<DVector2> TilePoints = new List<DVector2>();

                    if (TerrainStreamingExtensions.IsPointInPolygon4(PolyPoints, Tile.UpperLeftCoordinate))
                        TilePoints.Add(Tile.UpperLeftCoordinate);
                    if (TerrainStreamingExtensions.IsPointInPolygon4(PolyPoints, new DVector2(Tile.BottomRightCoordiante.x, Tile.UpperLeftCoordinate.y)))
                        TilePoints.Add(new DVector2(Tile.BottomRightCoordiante.x, Tile.UpperLeftCoordinate.y));
                    if (TerrainStreamingExtensions.IsPointInPolygon4(PolyPoints, Tile.BottomRightCoordiante))
                        TilePoints.Add(Tile.BottomRightCoordiante);
                    if (TerrainStreamingExtensions.IsPointInPolygon4(PolyPoints, new DVector2(Tile.UpperLeftCoordinate.x, Tile.BottomRightCoordiante.y)))
                        TilePoints.Add(new DVector2(Tile.UpperLeftCoordinate.x, Tile.BottomRightCoordiante.y));

                    if (TilePoints.Count == 4)
                    {
                        GrassGeoData.GeoPoints = new List<DVector2>();
                        GrassGeoData.GeoPoints.AddRange(TilePoints);
                        GeoDataContainer.GeoGrass.Add(GrassGeoData);
                    }
                    else
                    if (PolyPoints.Count >= 3)
                    {
                        var PolyInter = TerrainStreamingPolygonMathUtility.ClipPolygons(PolyPoints, tilePoly.GeoPoints, BooleanOperation.Intersection);

                        foreach (var Points in PolyInter)
                        {
                            if (Points.Count >= 3)
                            {
                                GrassGeoData.GeoPoints = new List<DVector2>();
                                GrassGeoData.GeoPoints.AddRange(Points);
                                GeoDataContainer.GeoGrass.Add(GrassGeoData);
                            }
                        }
                    }
                }
            }

        }




        public override void GetGeoVectorBuildingData(TerrainStreamingGeoVectorData GeoDataContainer)
        {
            if (osmData.Ways.Count != 0)
            {
                foreach (var wayDic in FiltredBuildingsData.Ways)
                {
                    TerrainStreamingPolygonGeoData BuildingGeoData = new TerrainStreamingPolygonGeoData();

                    BuildingGeoData.Tag_Value = wayDic.Value.MainTag.Value;

                    for (int i = 0; i < wayDic.Value.Nodes.Count; i++)
                    {
                        var latlon = new DVector2(wayDic.Value.Nodes[i].Lon, wayDic.Value.Nodes[i].Lat);

                        BuildingGeoData.GeoPoints.Add(latlon);
                    }

                    foreach (var attribute in wayDic.Value.Tags)
                    {
                        if (attribute.Attribute == "name")
                            BuildingGeoData.Name = attribute.Value;

                        if (attribute.Attribute == "building:levels")
                            BuildingGeoData.Levels = int.Parse(attribute.Value, CultureInfo.InvariantCulture);

                        if (attribute.Attribute == "building:min_level")
                            BuildingGeoData.MinLevel = int.Parse(attribute.Value, CultureInfo.InvariantCulture);

                        if (attribute.Attribute == "building:height")
                            BuildingGeoData.Height = float.Parse(attribute.Value.Replace(" ", "").Replace("m", ""), CultureInfo.InvariantCulture); ;

                        if (attribute.Attribute == "building:min_height")
                            BuildingGeoData.MinHeight = float.Parse(attribute.Value, CultureInfo.InvariantCulture);


                    }

                    GeoDataContainer.GeoBuilding.Add(BuildingGeoData);

                }
            }
        }
        public override void GetGeoVectorPointsData(TerrainStreamingGeoVectorData GeoDataContainer)
        {

            if (osmData.Nodes.Count != 0)
            {
                foreach (var node in FiltredPointsData.Nodes)
                {
                    TerrainStreamingPointGeoData PointGeoData = new TerrainStreamingPointGeoData();
                    PointGeoData.Tag = node.Value.MainTag.Value;
                    PointGeoData.GeoPoint = new DVector2(node.Value.Lon, node.Value.Lat);

                    foreach (var attribute in node.Value.Tags)
                    {
                        if (attribute.Attribute == "name")
                            PointGeoData.Name = attribute.Value;
                    }

                    GeoDataContainer.GeoPoints.Add(PointGeoData);

                }
            }
        }




    }

}

