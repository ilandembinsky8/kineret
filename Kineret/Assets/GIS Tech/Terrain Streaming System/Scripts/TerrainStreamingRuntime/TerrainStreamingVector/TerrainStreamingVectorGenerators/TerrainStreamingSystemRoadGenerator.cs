using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GISTech.TerrainStreaming
{
    public class TerrainStreamingSystemRoadGenerator
    {

        public static void GenerateRoades(TerrainStreamingTerrainTile tile, List<TerrainStreamingLinesGeoData> GeoData)
        {
            if (GeoData.Count > 0)
            {
                foreach (var Road in GeoData)
                    CreateRoad(Road,tile);
            }
        }
        private static GameObject CreateRoad(TerrainStreamingLinesGeoData Road, TerrainStreamingTerrainTile tile)
        {
            Vector3[] linePoints = new Vector3[Road.GeoPoints.Count];

            for (int i = 0; i < Road.GeoPoints.Count; i++)
            {
                var latlon = new DVector2(Road.GeoPoints[i].x, Road.GeoPoints[i].y);

                linePoints[i] = TerrainStreamingGeoConversion.LatLonToUWS(latlon, tile.container,2f);

            }

            var roadtype = GetRoadPrefab(tile.prefs.RoadsPrefab, Road);

            GameObject m_road = null;

            if (roadtype!=null)
            {
                var road = new TerrainStreamingRoad(roadtype, tile);

                road.Points = linePoints;

                switch (tile.prefs.TerrainRoadGenerator)
                {
                    case RoadGenerator.SimpleUnityLine:

                        m_road = CreateLine(road);
                        m_road.transform.parent = tile.transform;
                        m_road.name = roadtype.Tag.Value + "_"+ Road.ID;
                        break;
                }

            }
            return m_road;
        }

        private static TerrainStreamingSO_Road GetRoadPrefab(List<TerrainStreamingSO_Road> Prefabs, TerrainStreamingLinesGeoData line)
        {
            TerrainStreamingSO_Road road = null;

            foreach (var prefab in Prefabs)
            {
                if (prefab != null)
                {
                    if (prefab.Tag.Equal(new TerrainStreamingVectorTag(line.Tag_Key, line.Tag_Value)))
                    {
                        road = prefab;
                    }
                }
            }
            return road;
        }
        public static GameObject CreateLine(TerrainStreamingRoad m_road)
        {
            LineRenderer lineRender = RLine(m_road.Points);

            lineRender.alignment = LineAlignment.TransformZ;

            lineRender.material = m_road.material;

            lineRender.startWidth = m_road.width;
            lineRender.endWidth = m_road.width;

            lineRender.startColor = m_road.color;
            lineRender.endColor = m_road.color;

            return lineRender.gameObject;

        }
        public static LineRenderer RLine(Vector3[] linePoints)
        {
            GameObject result = new GameObject();

            result.transform.Rotate(new Vector3(90, 0, 0));

            LineRenderer lineRender = result.AddComponent<LineRenderer>();
            lineRender.positionCount = linePoints.Length;
            lineRender.SetPositions(linePoints);

            return lineRender;
        }
    }
    public class TerrainStreamingRoad
    {
        public Material material;
        public float width;
        public Color color;
        public Vector3[] Points;

        public TerrainStreamingRoad(TerrainStreamingSO_Road so_road, TerrainStreamingTerrainTile tile)
        {
            width = so_road.RoadWidth * tile.container.Scale.y;
            color = so_road.RoadColor;
            material = so_road.Roadmaterial;
        }

    }
}