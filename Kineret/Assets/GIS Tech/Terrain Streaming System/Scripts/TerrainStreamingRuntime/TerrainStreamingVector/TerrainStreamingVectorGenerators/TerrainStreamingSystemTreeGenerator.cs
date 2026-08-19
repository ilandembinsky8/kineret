/*     Unity GIS Tech 2020-2021      */

using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace GISTech.TerrainStreaming
{
    public class TerrainStreamingTreeGenerator
    {
        private static List<PrototypeTagIndex> Treeprototypes;
        private static Dictionary<TerrainStreamingVectorTag, int> Treeprototypes_Ind = new Dictionary<TerrainStreamingVectorTag, int>();

        private static TerrainStreamingTerrainTile tile;
        private static float TreeDistance;
        private static float BillBoardStartDistance;
        private static List<TerrainStreamingSO_Tree> treesPrefabs = new List<TerrainStreamingSO_Tree>();
        private static GenerationMode TreesGenerationMode;

        public static void GenerateTrees(TerrainStreamingTerrainTile tile, List<TerrainStreamingPolygonGeoData> GeoData)
        {

            var treesPrefabs_str = new List<TerrainStreamingVectorTag>();

            foreach (var p in treesPrefabs)
            {
                if (p != null)
                    treesPrefabs_str.Add(p.Tag);
            }

            if (GeoData.Count == 0)
                return;
 
            List<Vector3> Points = new List<Vector3>();

            var TLPMercator_X = tile.container.TLPointMercator.x;
            var TLPMercator_Y = tile.container.TLPointMercator.y;

            var DRPMercator_X = tile.container.DRPointMercator.x;
            var DRPMercator_Y = tile.container.DRPointMercator.y;

            TerrainStreamingSO_Tree tree_SO = null;
 
            for (int i = 0; i < GeoData.Count; i++)
            {
                var Poly = GeoData[i];

                tree_SO = GetTreePrefab(Poly);

                List<Vector3> points = GetGlobalSpacePoints(Poly,tile);

                if (tree_SO != null)
                {
                    float m_TreeDensity = 100 - tree_SO.TreeDensity;

                    Rect rect = TerrainStreamingExtensions.GetRectFromPoints(points);
                    int lx = Mathf.RoundToInt(rect.width / m_TreeDensity);
                    int ly = Mathf.RoundToInt(rect.height / m_TreeDensity);

                    if (lx > 0 && ly > 0)
                        GenerateTerrainsTrees(tree_SO, tile, lx, ly, rect, points);
                }

            }
        }
        private static void GenerateTerrainsTrees(TerrainStreamingSO_Tree tree, TerrainStreamingTerrainTile m_tile, int factorX, int factorY, Rect rect, List<Vector3> points)
        {
            float TreeScaleFactor = tree.TreeScaleFactor * m_tile.container.Scale.x;
            float TreeRandomScaleFactor = tree.TreeRandomScaleFactor * m_tile.container.Scale.x;
            var m_TreeDensity = 100 - tree.TreeDensity;
            float treeDensity = m_TreeDensity * m_tile.container.Scale.x;


            Bounds bounds = m_tile.TileSector.tileBounds;

            Vector3 Bmin = bounds.min;
            Vector3 Bmax = bounds.max;

            float TreeValue = 10 / treeDensity;

            float rectx = (rect.xMax - rect.xMin) / factorX;
            float recty = (rect.yMax - rect.yMin) / factorY;

            int counter = 0;

            Vector3[] ps = points.ToArray();

            int Max_S_x = Mathf.Max(Mathf.FloorToInt((Bmin.x - rect.xMin) / rectx + 1), 0);
            int Min_E_x = Mathf.Min(Mathf.FloorToInt((Bmax.x - rect.xMin) / rectx), factorX);

            int Max_S_y = Mathf.Max(Mathf.FloorToInt((Bmin.z - rect.yMin) / recty + 1), 0);
            int Min_E_y = Mathf.Min(Mathf.FloorToInt((Bmax.z - rect.yMin) / recty), factorY);

            for (int x = Max_S_x; x < Min_E_x; x++)
            {

                float rx = x * rectx + rect.xMin;

                for (int y = Max_S_y; y < Min_E_y; y++)
                {
                    float ry = y * recty + rect.yMin;

                    float px = rx + UnityEngine.Random.Range(-TreeValue, TreeValue);
                    float pz = ry + UnityEngine.Random.Range(-TreeValue, TreeValue);

                    if (TerrainStreamingExtensions.IsPointInPolygon3D(ps, px, pz))
                    {
                        CreateTree(tree, m_tile, new Vector3(px, 0, pz));
                        counter++;
                    }
                }
            }

        }
        private static void CreateTree(TerrainStreamingSO_Tree tree, TerrainStreamingTerrainTile TerrainContainer, Vector3 pos)
        {

            float TreeScaleFactor = tree.TreeScaleFactor * tile.container.Scale.x;
            float RandomScaleFactor = tree.TreeRandomScaleFactor * tile.container.Scale.x;


            var m_prototypeIndex = GetTreePrototype(tree.Tag, Treeprototypes);

            Terrain terrain = tile.terrain;
            terrain.treeBillboardDistance = BillBoardStartDistance;
            terrain.treeDistance = TreeDistance;
            TerrainData tData = terrain.terrainData;
            Vector3 terPos = terrain.transform.position;
            Vector3 localPos = pos - terPos;
            float heightmapWidth = (tData.heightmapResolution - 1) * tData.heightmapScale.x;
            float heightmapHeight = (tData.heightmapResolution - 1) * tData.heightmapScale.z;

            if (localPos.x > 0 && localPos.z > 0 && localPos.x < heightmapWidth && localPos.z < heightmapHeight)
            {
                terrain.AddTreeInstance(new TreeInstance
                {
                    color = Color.white,
                    heightScale = TreeScaleFactor + UnityEngine.Random.Range(-RandomScaleFactor, RandomScaleFactor),
                    lightmapColor = Color.white,
                    position = new Vector3(localPos.x / heightmapWidth, 0, localPos.z / heightmapHeight),
                    prototypeIndex = UnityEngine.Random.Range(m_prototypeIndex.x, m_prototypeIndex.y),
                    widthScale = TreeScaleFactor + UnityEngine.Random.Range(-RandomScaleFactor, RandomScaleFactor)
                });

            }
        }
        public static void AddTreePrefabsToTerrains(TerrainStreamingTerrainTile m_tile, GenerationMode m_TreesGenerationMode, List<TerrainStreamingSO_Tree> m_treesPrefabs, float m_TreeDistance, float m_BillBoardStartDistance)
        {
            TreeDistance = m_TreeDistance;
            BillBoardStartDistance = m_BillBoardStartDistance;
            treesPrefabs = m_treesPrefabs;
            tile = m_tile;
            TreesGenerationMode = m_TreesGenerationMode;

            int c = 0;
            List<object> objects = new List<object>();
            List<TerrainStreamingVectorTag> objects_type = new List<TerrainStreamingVectorTag>();
            Treeprototypes_Ind = new Dictionary<TerrainStreamingVectorTag, int>();

            foreach (var prefab in m_treesPrefabs)
            {
                if (prefab != null)
                {
                    foreach (var t in prefab.TreePrefab)
                    {
                        if (t != null)
                        {
                            objects.Add(t);
                            objects_type.Add(prefab.Tag);
                            c++;
                        }
                    }
                    if (!Treeprototypes_Ind.ContainsKey(prefab.Tag))
                        Treeprototypes_Ind.Add(prefab.Tag, c);
                    c = 0;
                }
            }

            TreePrototype[] prototypes = new TreePrototype[objects.Count];

            Treeprototypes = new List<PrototypeTagIndex>();

            for (int i = 0; i < prototypes.Length; i++)
            {
                prototypes[i] = new TreePrototype
                {
                    prefab = (GameObject)objects[i] as GameObject
                };

                Treeprototypes.Add(new PrototypeTagIndex(prototypes[i], objects_type[i]));

            }

            m_tile.terrainData.treePrototypes = prototypes;
            m_tile.terrainData.treeInstances = new TreeInstance[0];

        }
        public static List<Vector3> GetGlobalSpacePoints(TerrainStreamingPolygonGeoData Poly,TerrainStreamingTerrainTile tile)
        {
            List<Vector3> points = new List<Vector3>();

            if (Poly.GeoPoints.Count == 0) return points;

            foreach (var point in Poly.GeoPoints)
            {
                var spaceP = TerrainStreamingGeoConversion.LatLonToUWS(point, tile.container, 0);

                points.Add(spaceP);
            }

            return points;
        }
        private static Vector2Int GetTreePrototype(TerrainStreamingVectorTag treetype, List<PrototypeTagIndex> Treeprototypes)
        {
            Vector2Int Index = new Vector2Int(0, 0);

            var prototypes = Treeprototypes_Ind.ToList();
            int t_value = 0;

            foreach (var prototype in prototypes)
            {
                if (prototype.Key.Equal(treetype))
                {
                    Index = new Vector2Int(t_value, (t_value + prototype.Value));
                }
                t_value += prototype.Value;
            }
            return Index;
        }
        private static TerrainStreamingSO_Tree GetTreePrefab(TerrainStreamingPolygonGeoData treePoly)
        {
            TerrainStreamingSO_Tree tree = null;

            foreach (var prefab in treesPrefabs)
            {
                if (prefab != null)
                {
                    if (prefab.Tag.Equal(new TerrainStreamingVectorTag(treePoly.Tag_Key, treePoly.Tag_Value))) 
                    {
                        tree = prefab;
                    }
                  

                }
            }
            return tree;
        }
    }
    
}
