/*     Unity GIS Tech 2020-2021      */

using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace GISTech.TerrainStreaming
{
    public class TerrainStreamingGrassGenerator
    {
        private static GenerationMode GrassGenerationMode;
        private static TerrainStreamingTerrainTile tile;
        private static List<TerrainStreamingSO_GrassObject> GrassPrefabs;
        private static float DetailDistance;
        private static float GrassScaleFactor;
        private static Dictionary<TerrainStreamingVectorTag, int> Grassprototypes_Ind = new Dictionary<TerrainStreamingVectorTag, int>();
        private static int totalGrassCount = 0;
        private static int detailResolution;
        private static List<DetailPrototype> DetailPrototypes;
        private static IndexedDetails indexedDetails;
        
        public static void AddGrassPrefabsToTerrains(TerrainStreamingTerrainTile m_tile,GenerationMode m_GrassGenerationMode, List<TerrainStreamingSO_GrassObject> m_GrassPrefabs, float m_DetailDistance, float m_GrassScaleFactor)
        {
            GrassPrefabs = m_GrassPrefabs;
            tile = m_tile;
            DetailDistance = m_DetailDistance;
            GrassScaleFactor = m_GrassScaleFactor * tile.container.Scale.x;
            GrassGenerationMode = m_GrassGenerationMode;

            int c = 0;
            List<TerrainStreamingSO_Grass> objects = new List<TerrainStreamingSO_Grass>();
            List<TerrainStreamingVectorTag> objects_type = new List<TerrainStreamingVectorTag>();

            Grassprototypes_Ind = new Dictionary<TerrainStreamingVectorTag, int>();
            totalGrassCount = 0;

            foreach (var element in m_GrassPrefabs)
            {
                if (element != null)
                {
                    foreach (var prefab in element.GrassPrefab)
                    {
                        if (prefab != null)
                        {
                            objects.Add(prefab);
                            objects_type.Add(element.Tag);
                            c++;
                        }

                    }
                    if (!Grassprototypes_Ind.ContainsKey(element.Tag))
                        Grassprototypes_Ind.Add(element.Tag, c);
                    c = 0;

                }

            }

            DetailPrototypes = new List<DetailPrototype>(objects.Count);

            for (int i = 0; i < objects.Count; i++)
            {
                var prefab = objects[i];
                DetailPrototypes.Add((CopyDetailPrototype(m_tile, prefab)));

            }

            foreach (var SO_prefab in GrassPrefabs)
            {
                if (SO_prefab != null)
                {
                    foreach (var prefab in SO_prefab.GrassPrefab)
                    {
                        totalGrassCount++;
                    }
                }
                else
                    Debug.LogError("Grass Prefab is null ");

            }

            TerrainData tdata = tile.terrain.terrainData;
            detailResolution = tdata.detailResolution;

            indexedDetails = new IndexedDetails(tile.terrain, new Vector2Int(detailResolution, detailResolution), totalGrassCount);
 
            tile.terrain.terrainData.detailPrototypes = DetailPrototypes.ToArray();
            tile.terrain.detailObjectDistance = DetailDistance;



        }
        private static DetailPrototype CopyDetailPrototype(TerrainStreamingTerrainTile m_tile, TerrainStreamingSO_Grass Source_item)
        {
            var detailPrototype = new DetailPrototype();

            detailPrototype.renderMode = DetailRenderMode.GrassBillboard;
            detailPrototype.prototypeTexture = Source_item.DetailTexture;
            detailPrototype.minWidth = Source_item.MinWidth;
            detailPrototype.maxWidth = Source_item.MaxWidth * GrassScaleFactor * m_tile.container.Scale.x;
            detailPrototype.minHeight = Source_item.MinHeight;
            detailPrototype.maxHeight = Source_item.MaxHeight * GrassScaleFactor * m_tile.container.Scale.x;
            detailPrototype.noiseSpread = Source_item.Noise;
            detailPrototype.healthyColor = Source_item.HealthyColor;
            detailPrototype.dryColor = Source_item.DryColor;


            if (Source_item.BillBoard)
                detailPrototype.renderMode = DetailRenderMode.GrassBillboard;
            else detailPrototype.renderMode = DetailRenderMode.Grass;

            return detailPrototype;
        }
        public static void GenerateGrass(TerrainStreamingTerrainTile tile, List<TerrainStreamingPolygonGeoData> GeoData)
        {
            var GrassPrefabs_str = new List<TerrainStreamingVectorTag>();

            foreach (var p in GrassPrefabs)
            {
                if (p != null)
                    GrassPrefabs_str.Add(p.Tag);
            }

            if (GeoData.Count == 0)
                return;

            List<Vector3> Points = new List<Vector3>();

            var TLPMercator_X = tile.container.TLPointMercator.x;
            var TLPMercator_Y = tile.container.TLPointMercator.y;

            var DRPMercator_X = tile.container.DRPointMercator.x;
            var DRPMercator_Y = tile.container.DRPointMercator.y;

            TerrainStreamingSO_GrassObject Grass_SO = null;

            for (int i = 0; i < GeoData.Count; i++)
            {
                var Poly = GeoData[i];

                Grass_SO = GetGrassPrefab(Poly);

                DVector3 TL_point = new DVector3(0, 0, 0);
                DVector3 DR_point = new DVector3(0, 0, 0);

                List<DVector3> points = GetGlobalPointsFromWay(Poly.GeoPoints,ref TL_point, ref DR_point);

                if (Grass_SO != null)
                {
                    double Step = 0.00001 * (100 - (Grass_SO.GrassDensity - 0.01f));
                    if (Step == 0)
                        Step = 0.00001;

                    List<DVector3> Newpoints = new List<DVector3>();

                    for (var lon = TL_point.x; lon <= DR_point.x; lon += Step)
                    {
                        for (var lat = DR_point.z; lat <= TL_point.z; lat += Step)
                        {
                            DVector3 p = new DVector3(lon, 0, lat);
                            Newpoints.Add(p);
                        }
                    }

                    foreach (var p in Newpoints)
                    {
                        var space = TerrainStreamingGeoConversion.LatLonToUWS(new DVector2(p.x, p.z), tile.container, 2f);

                        SetGrass(Grass_SO, tile.Number, space, 1f);
                    }

                }

            }
            indexedDetails.SetTerraindetails();

        }
        private static TerrainStreamingSO_GrassObject GetGrassPrefab(TerrainStreamingPolygonGeoData GrassPoly)
        {
            TerrainStreamingSO_GrassObject Grass = null;

            foreach (var prefab in GrassPrefabs)
            {
                if (prefab != null)
                {
                    if (prefab.Tag.Equal(new TerrainStreamingVectorTag(GrassPoly.Tag_Key, GrassPoly.Tag_Value)))
                    {
                        Grass = prefab;
                    }


                }
            }
            return Grass;
        }
        public static List<DVector3> GetGlobalPointsFromWay(List<DVector2> GeoPoints, ref DVector3 TL_Point, ref DVector3 DR_Point)
        {
            TL_Point = new DVector3(180, 0, -90);
            DR_Point = new DVector3(-180, 0, 90);

            List<DVector3> points = new List<DVector3>();

            if (GeoPoints.Count == 0) return points;

            foreach (var p in GeoPoints)
            {
                if (p != null)
                {
                    var ps = new DVector3(p.x, 0, p.y);

                    if (ps.x < TL_Point.x)
                        TL_Point.x = ps.x;
                    if (ps.z > TL_Point.z)
                        TL_Point.z = ps.z;

                    if (ps.x > DR_Point.x)
                        DR_Point.x = ps.x;
                    if (ps.z < DR_Point.z)
                        DR_Point.z = ps.z;

                    points.Add(ps);
                }

            }

            return points;
        }
        private static void SetGrass(TerrainStreamingSO_GrassObject grass_SO, Vector2Int t_index, Vector3 position, float radius)
        {

            int Prefab_index = UnityEngine.Random.Range(0, grass_SO.GrassPrefab.Count);
            var grassModel = grass_SO.GrassPrefab[Prefab_index];
            int m_prototypeIndex = GetGrassPrototypeIndex(grassModel);
 
            var map = indexedDetails.details[m_prototypeIndex];

            int TerrainDetailMapSize = indexedDetails.terrain.terrainData.detailResolution;

            float PrPxSize = TerrainDetailMapSize / indexedDetails.terrain.terrainData.size.x;

            Vector3 TexturePoint3D = position - indexedDetails.terrain.transform.position;
            TexturePoint3D = TexturePoint3D * PrPxSize;

            float[] xymaxmin = new float[4];
            xymaxmin[0] = TexturePoint3D.z + radius;
            xymaxmin[1] = TexturePoint3D.z - radius;
            xymaxmin[2] = TexturePoint3D.x + radius;
            xymaxmin[3] = TexturePoint3D.x - radius;

            for (int y = 0; y < indexedDetails.terrain.terrainData.detailHeight; y++)
            {
                if (xymaxmin[2] > y && xymaxmin[3] < y)
                {
                    for (int x = 0; x < indexedDetails.terrain.terrainData.detailWidth; x++)
                    {
                        if (xymaxmin[0] > x && xymaxmin[1] < x)
                            map[x, y] = 1;
                    }
                }
            }

        }
        private static int GetGrassPrototypeIndex(TerrainStreamingSO_Grass SO_Grass)
        {
            int Index = 0;

            for (int j = 0; j < DetailPrototypes.Count; j++)
            {
                var Details = DetailPrototypes[j];

                if (SO_Grass.DetailTexture.name == Details.prototypeTexture.name)
                {
                    Index = DetailPrototypes.IndexOf(Details);
                    continue;
                }


            }

            return Index;
        }
    }
    public class PrototypeTagIndex
    {
        public TreePrototype protoType;
        public TerrainStreamingVectorTag treeType;

        public PrototypeTagIndex(TreePrototype m_protoType, TerrainStreamingVectorTag m_treeType)
        {
            protoType = m_protoType;
            treeType = m_treeType;
        }

    }
    public class IndexedDetails
    {
        public List<int[,]> details = new List<int[,]>();
        public Terrain terrain;
        public IndexedDetails(Terrain m_terrain, Vector2Int dim, int totalGrassCount)
        {
            terrain = m_terrain;

            for (int i = 0; i < totalGrassCount; i++)
            {
                details.Add(new int[dim.x, dim.y]);
            }
        }

        public void SetTerraindetails()
        {
            for (var x = 0; x < details.Count; x++)
            {
                terrain.terrainData.SetDetailLayer(0, 0, x, details[x]);
            }

        }
    }
}
