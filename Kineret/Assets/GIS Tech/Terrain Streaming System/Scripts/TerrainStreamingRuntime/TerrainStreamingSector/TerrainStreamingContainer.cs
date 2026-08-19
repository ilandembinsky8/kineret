/*     Unity GIS Tech 2020-2021      */

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace GISTech.TerrainStreaming
{
    public class TerrainStreamingContainer : MonoSingleton<TerrainStreamingContainer>
    {
#if UNITY_EDITOR
        public int lastTab = 0;
#endif
        public string ZoneName;
        public DVector2 UpperLeftCoordinate;
        public DVector2 BottomRightCoordiante;
        public Vector3 Scale;
        public Vector3 ContainerSize;
        public Vector3 SubTerrainSize;
        public Vector2Int TilesCount;

        public Vector2 Dimensions;
        public Vector2 MinMaxElevation;

        public DVector2 TLPointMercator;
        public DVector2 DRPointMercator;
 
        public Bounds GlobalTerrainBounds;
 
        public string GeneratedTerrainfolder;

        public TerrainStreamingTerrainContainer TerrainContainer;

        public TerrainStreamingTileData[,] AllSectorsData;

        private TerrainStreamingTileSector[,] _terrains;

        public int heightmapResolution;

        public int zoomLevel;

        public TerrainStreamingTileSector[,] Sectors;

        private OptionEnabDisab m_EnableDrawSectors;
        public OptionEnabDisab EnableDrawSectors
        {
            get { return m_EnableDrawSectors; }
            set
            {
                if (m_EnableDrawSectors != value)
                {
                    m_EnableDrawSectors = value;

                    for (int i = 0; i < Sectors.GetLength(0); i++)
                    {

                        for (int j = 0; j < Sectors.GetLength(1); j++)
                        {
                            Sectors[i, j].EnableDrawSector = m_EnableDrawSectors;
                        }
                    }

                }
            }
        }
        void Start()
        {
            Sectors = new TerrainStreamingTileSector[TilesCount.x, TilesCount.y];
            TerrainStreamingTileSector[] items = GetComponentsInChildren<TerrainStreamingTileSector>();
            foreach (TerrainStreamingTileSector item in items) Sectors[item.Number.x, item.Number.y] = item;

        }
        public void ResetData()
        {
            ZoneName = "";
            UpperLeftCoordinate = new DVector2(0, 0);
            BottomRightCoordiante = new DVector2(0, 0);
            Scale = new Vector3(1, 1,1);
            ContainerSize = new Vector3(1, 1, 1);
            SubTerrainSize = new Vector3(1, 1, 1);
            TilesCount = new Vector2Int(1, 1);
            Dimensions = new Vector2(1, 1);
            MinMaxElevation = new Vector2(1, 1);

            AllSectorsData = new TerrainStreamingTileData[0, 0];
            Sectors = new TerrainStreamingTileSector[0, 0];
            heightmapResolution = 129;
    }


        public bool IncludePoint(DVector2 LatLon)
        {
            bool Include = false;

            var MinLat = BottomRightCoordiante.y;
            var MinLon = UpperLeftCoordinate.x;
            var MaxLat = UpperLeftCoordinate.y;
            var MaxLon = BottomRightCoordiante.x;

            if (LatLon.x > MinLon && LatLon.x < MaxLon && LatLon.y > MinLat && LatLon.y < MaxLat)
                Include = true;

            return Include;
        }
    }
}