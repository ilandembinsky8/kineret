/*     Unity GIS Tech 2020-2023      */


using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GISTech.GISTerrainLoader
{
public class GISTerrainLoaderLineRendererGenerator
    {
        private static List<GISTerrainLoaderSO_Road> RoadsPrefab;
        private static GameObject highwaysContainer;
        private static GISTerrainContainer container;
        private static GISTerrainLoaderVectorParameters_SO VectorParameters;
        public static void CreateHighways(GISTerrainContainer m_container, List<GISTerrainLoaderLineGeoData> GeoData, GISTerrainLoaderVectorParameters_SO m_VectorParameters, List<GISTerrainLoaderSO_Road> m_RoadsPrefab)
        {
            RoadsPrefab = m_RoadsPrefab;
            highwaysContainer = new GameObject();
            highwaysContainer.name = "Roads";
            highwaysContainer.transform.parent = m_container.transform;
            container = m_container;
            VectorParameters = m_VectorParameters;

            GenerateRoades(GeoData);
        }
        private static void GenerateRoades(List<GISTerrainLoaderLineGeoData> GeoData)
        {
            if (GeoData.Count != 0)
            {
                foreach (var Road in GeoData)
                {
                    TerrainData tdata = container.terrains[0, 0].terrainData;

                    int detailResolution = tdata.detailResolution;

                    List<Vector3[]> Points = new List<Vector3[]>();

                    foreach (var segment in Road.GeoPoints)
                    {
                        Vector3[] linePoints = new Vector3[segment.Count];

                        for (int i = 0; i < segment.Count; i++)
                        {
                            var point = segment[i];

                            var latlon = new DVector2(point.GeoPoint.x, point.GeoPoint.y);

                            linePoints[i] = GISTerrainLoaderGeoConversion.RealWorldCoordinatesToUnityWorldSpace(container, latlon);
                        }

                        Points.Add(linePoints);
                    }

                    var roadtype = Road.Tag;

                    var defaultRoad = RoadsPrefab[0];

                    foreach (var so_road in RoadsPrefab)
                    {
                        if (so_road.name == roadtype)
                        {
                            defaultRoad = so_road;
                        }
                    }
                    foreach (var seg in Points)
                    {
                        var road = new GISTerrainLoaderRoad(defaultRoad, container);

                        road.Points = seg;

                        var m_road = CreateLine(road);

                        m_road.transform.parent = highwaysContainer.transform;

                        m_road.name = roadtype;

                        if (m_road != null)
                        {
                            m_road.name = "GeoLine_" + Road.Name + "_" + Road.Tag;

                            if (GISTerrainLoaderRoadsGenerator.EnableRoadName)
                                GISTerrainLoaderRoadsGenerator.CreateRoadNameLabel(seg, m_road.name, m_road.transform, container);

                            if (VectorParameters.AddVectorDataBaseToGameObjects)
                                GISTerrainLoaderGeoVectorData.AddVectordDataToObject(m_road, VectorObjectType.Line, null, Road);
                        }


                    }
                 }
            }
        }
        public static GameObject CreateLine(GISTerrainLoaderRoad m_road )
        {
            Vector3[] smoothedPoints = GISTerrainLoaderLineTools.SmoothLine(m_road.Points, 4f);
            LineRenderer lineRender = GISTerrainLoaderLineTools.CreateLineRenderer(smoothedPoints, m_road.material,m_road.width,m_road.color,0.02f);

            Material lineMat = new Material(m_road.material); // Clone the original
            lineMat.color = m_road.color; // Apply custom color per road

            lineRender.material = lineMat;
            lineRender.startWidth = m_road.width;
            lineRender.endWidth = m_road.width;
            lineRender.startColor = m_road.color;
            lineRender.endColor = m_road.color;

            return lineRender.gameObject;

        }
    }

public static class GISTerrainLoaderLineTools
{
        /// <summary>
        /// Creates a LineRenderer that follows the contour of the active Terrain at the given X/Z points.
        /// </summary>
        /// <param name="worldXZPoints">
        /// An array of Vector3s where X/Z are the sample positions; Y is ignored.
        /// </param>
        /// <param name="material">The material to use for the line.</param>
        /// <param name="width">Uniform width of the line.</param>
        /// <param name="color">Line color.</param>
        /// <param name="heightOffset">Vertical lift above the terrain to avoid Z-fighting (default 0.02).</param>
        /// <returns>The configured LineRenderer, or null if no active Terrain is found.</returns>
        public static LineRenderer CreateLineRenderer(Vector3[] worldXZPoints,Material material,float width = 1.5f,Color? color = null,float heightOffset = 0.01f)
        {
            Terrain terrain = Terrain.activeTerrain;
            if (terrain == null)
            {
                Debug.LogError("No active terrain found.");
                return null;
            }

            GameObject go = new GameObject("ContourLine");
            LineRenderer lr = go.AddComponent<LineRenderer>();

            // LineRenderer config
            lr.useWorldSpace = true;
            lr.alignment = LineAlignment.View;
            lr.material = material;
            lr.widthMultiplier = width;
            lr.numCapVertices = 6;
            lr.numCornerVertices = 8;
            lr.textureMode = LineTextureMode.Tile;

            Color finalColor = color ?? Color.black;
            lr.startColor = finalColor;
            lr.endColor = finalColor;

            // Position points clamped to terrain
            Vector3 terrainPos = terrain.transform.position;
            for (int i = 0; i < worldXZPoints.Length; i++)
            {
                Vector3 p = worldXZPoints[i];
                float terrainY = terrain.SampleHeight(p) + terrainPos.y;
                p.y = Mathf.Max(terrainY + heightOffset, p.y);
                worldXZPoints[i] = p;
            }

            lr.positionCount = worldXZPoints.Length;
            lr.SetPositions(worldXZPoints);

            return lr;
        }
        public static Vector3[] SmoothLine(Vector3[] points, float maxStep = 5f)
        {
            var smoothed = new List<Vector3>();
            for (int i = 0; i < points.Length - 1; i++)
            {
                Vector3 a = points[i];
                Vector3 b = points[i + 1];
                float dist = Vector3.Distance(a, b);
                int steps = Mathf.CeilToInt(dist / maxStep);
                for (int j = 0; j < steps; j++)
                {
                    float t = j / (float)steps;
                    smoothed.Add(Vector3.Lerp(a, b, t));
                }
            }
            smoothed.Add(points[points.Length - 1]);
            return smoothed.ToArray();
        }
    }

}
