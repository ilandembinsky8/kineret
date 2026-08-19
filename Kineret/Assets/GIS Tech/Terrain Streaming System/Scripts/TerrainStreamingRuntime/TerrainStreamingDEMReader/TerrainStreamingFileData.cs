/*     Unity GIS Tech 2020-2021      */

using System;
using UnityEngine;

namespace GISTech.TerrainStreaming
{
    public class TerrainStreamingFileData
    {
        public bool AlreadyLoaded =false;

        public float[,] floatheightData;

        public float MaxElevation = -9000;
        public float MinElevation = 9000;

        public int mapSize_row;
        public int mapSize_col;

        public Vector2 Tiles = new Vector2(0, 0);
 
        public DVector2 BottomLeftCoordinate = new DVector2(0, 0);
        public DVector2 UpperRightCoordinate = new DVector2(0, 0);

        public DVector2 UpperLeftCoordinate = new DVector2(0, 0);
        public DVector2 BottomRightCoordiante = new DVector2(0, 0);

        public DVector2 dim = new DVector2(0, 0);

        public DVector2 Terrain_Dimension = new DVector2(0, 0);

        public double cellsize_x = 0;
        public double cellsize_y = 0;

        private double rang_x;
        private double rang_y;

        public int X_Tile;
        public int Y_Tile;
 
 
        public TerrainStreamingFileData()
        {
   
            MaxElevation = -5000;
            MinElevation = 5000;

            mapSize_row = 0;
            mapSize_col = 0;

            Vector2 Tiles = new Vector2(0, 0);

            DVector2 Origin = new DVector2(0, 0);
            DVector2 TopRightPoint = new DVector2(0, 0);

            DVector2 TopLeftPoint = new DVector2(0, 0);
            DVector2 DownRightPoint = new DVector2(0, 0);

            DVector2 dim = new DVector2(0, 0);

            DVector2 Terrain_Dimension = new DVector2(0, 0);

        }
        public void GetDetails()
        {
            var p1 = new DVector2(UpperRightCoordinate.x, BottomLeftCoordinate.y);
            var p2 = new DVector2(BottomLeftCoordinate.x, UpperRightCoordinate.y);

            UpperLeftCoordinate = new DVector2(BottomLeftCoordinate.x, UpperRightCoordinate.y);
            BottomRightCoordiante = new DVector2(UpperRightCoordinate.x, BottomLeftCoordinate.y);

            Terrain_Dimension.x = TerrainStreamingGeoConversion.Getdistance(BottomLeftCoordinate.y, BottomLeftCoordinate.x, p1.y, p1.x) * 10;
            Terrain_Dimension.y = TerrainStreamingGeoConversion.Getdistance(BottomLeftCoordinate.y, BottomLeftCoordinate.x, p2.y, p2.x) * 10;

            rang_x = Math.Abs(Math.Abs(BottomRightCoordiante.x) - Math.Abs(UpperLeftCoordinate.x));
            rang_y = Math.Abs(Math.Abs(UpperLeftCoordinate.y) - Math.Abs(BottomRightCoordiante.y));
 
        }
        public float GetElevation(DVector2 point)
        {
            var rang_px = Math.Abs(Math.Abs(point.x) - Math.Abs(BottomLeftCoordinate.x));
            var rang_py = Math.Abs(Math.Abs(UpperLeftCoordinate.y) - Math.Abs(point.y));

            int localLon = Mathf.FloorToInt((float)(rang_px * mapSize_col / rang_x));
            int localLat = Mathf.FloorToInt((float)(rang_py * mapSize_row / rang_y));

            if (localLon > mapSize_col - 1) localLon = mapSize_col - 1;
            if (localLat > mapSize_row - 1) localLat = mapSize_row - 1;

            var elevation = floatheightData[localLat,localLon];

            return elevation;
        }
        public bool Contains(double x, double y)
        {
            return x >= BottomLeftCoordinate.x && x <= BottomRightCoordiante.x && y >= BottomRightCoordiante.y && y <= UpperLeftCoordinate.y;
        }

        public float[,] GetNormlizedHeightmap(int heightmapResolution, TerrainStreamingFileData item)
        {
            var tdataHeightmap = new float[heightmapResolution, heightmapResolution];

            float elevationRange = item.MaxElevation - item.MinElevation;

            float thx = heightmapResolution - 1;
            float thy = heightmapResolution - 1;
 
            int tw = heightmapResolution;
            int th = heightmapResolution;
 

            for (int x = 0; x < tw; x++)
            {
                for (int y = 0; y < th; y++)
                {
 
                    float fpc = Mathf.Lerp(0, item.mapSize_col, x / thx);
                    float fpr = Mathf.Lerp(0, item.mapSize_row, y / thy);

                    int pr = Mathf.FloorToInt(fpr);
                    int pc = Mathf.FloorToInt(fpc);



                    if (pr > item.floatheightData.GetLength(0) - 1)
                        pr = item.floatheightData.GetLength(0) - 1;

                    if (pc > item.floatheightData.GetLength(1) - 1)
                        pc = item.floatheightData.GetLength(1) - 1;


                    var Rel = item.GetElevation(fpr, fpc);
                    
                    tdataHeightmap[x, y] = (Rel - item.MinElevation) / elevationRange;

                }

            }
            return tdataHeightmap;
        }
 

        public float GetElevation(float fpx, float fpy)
        {
            int px = (int)fpx;
            int py = (int)fpy;

            float Rx = fpx - px;
            float Ry = fpy - py;

            return GetAverageElevation(Rx, Ry, px, py);
        }
        public float GetAverageElevation(float Rx, float Ry, int px, int py)
        {

            float C_25 = 0.25f;
            float C_12 = 12.0f;
            float C_36 = 36.0f;

            var Rsx_1 = Rx - 1;
            var Rsx_2 = Rx - 2;
            var RsxP_1 = Rx + 1;

            var Rsy_1 = Ry - 1;
            var Rsy_2 = Ry - 2;
            var RsyP_1 = Ry + 1;

            var PsxP_1 = px + 1;
            var PsyP_1 = py + 1;

            var PxyM = Rx * Ry;

            var Psx_1 = px - 1;
            var Psy_1 = py - 1;

            var PsxP_2 = px + 2;
            var PsyP_2 = py + 2;

            float el = Rsx_1 * Rsx_2 * RsxP_1 * Rsy_1 * Rsy_2 * RsyP_1 * C_25 * ReadValue(px, py);

            el -= Rx * RsxP_1 * Rsx_2 * Rsy_1 * Rsy_2 * RsyP_1 * C_25 * ReadValue(PsxP_1, py);
            el -= Ry * Rsx_1 * Rsx_2 * RsxP_1 * RsyP_1 * Rsy_2 * C_25 * ReadValue(px, PsyP_1);
            el += PxyM * RsxP_1 * Rsx_2 * RsyP_1 * Rsy_2 * C_25 * ReadValue(PsxP_1, PsyP_1);
            el -= Rx * Rsx_1 * Rsx_2 * Rsy_1 * Rsy_2 * RsyP_1 / C_12 * ReadValue(Psx_1, py);
            el -= Ry * Rsx_1 * Rsx_2 * RsxP_1 * Rsy_1 * Rsy_2 / C_12 * ReadValue(px, Psy_1);
            el += PxyM * Rsx_1 * Rsx_2 * RsyP_1 * Rsy_2 / C_12 * ReadValue(Psx_1, PsyP_1);
            el += PxyM * RsxP_1 * Rsx_2 * Rsy_1 * Rsy_2 / C_12 * ReadValue(PsxP_1, Psy_1);
            el += Rx * Rsx_1 * RsxP_1 * Rsy_1 * Rsy_2 * RsyP_1 / C_12 * ReadValue(PsxP_2, py);
            el += Ry * Rsx_1 * Rsx_2 * RsxP_1 * Rsy_1 * RsyP_1 / C_12 * ReadValue(px, PsyP_2);
            el += PxyM * Rsx_1 * Rsx_2 * Rsy_1 * Rsy_2 / C_36 * ReadValue(Psx_1, Psy_1);
            el -= PxyM * Rsx_1 * RsxP_1 * RsyP_1 * Rsy_2 / C_12 * ReadValue(PsxP_2, PsyP_1);
            el -= PxyM * RsxP_1 * Rsx_2 * Rsy_1 * RsyP_1 / C_12 * ReadValue(PsxP_1, PsyP_2);
            el -= PxyM * Rsx_1 * RsxP_1 * Rsy_1 * Rsy_2 / C_36 * ReadValue(PsxP_2, Psy_1);
            el -= PxyM * Rsx_1 * Rsx_2 * Rsy_1 * RsyP_1 / C_36 * ReadValue(Psx_1, PsyP_2);
            el += PxyM * Rsx_1 * RsxP_1 * Rsy_1 * RsyP_1 / C_36 * ReadValue(PsxP_2, PsyP_2);

            return el;
        }
        public float ReadValue(int PX, int PY)
        {          
            try
            {
      PX = Mathf.Clamp(PX, 0, mapSize_col - 1);
                PY = Mathf.Clamp(PY, 0, mapSize_row - 1);

            var el = floatheightData[PY,PX];

                return el;
            }
            catch (Exception e)
            {
                var s = e;
                return 0;
            }
        }
    }
}