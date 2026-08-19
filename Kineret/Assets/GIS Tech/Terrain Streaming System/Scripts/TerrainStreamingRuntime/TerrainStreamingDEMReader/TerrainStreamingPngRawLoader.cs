/*     Unity GIS Tech 2020-2022      */

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace GISTech.TerrainStreaming
{
    public class TerrainStreamingPngRawLoader
    {
        public static event ReaderEvents OnReadError;
        public TerrainStreamingFileData data;
        public static event TerrainProgression OnProgress;

        public static List<TerrainStreamingFileData> Generated_Tiles_data = new List<TerrainStreamingFileData>();
        public bool LoadComplet;

        private int currentIndex;
        private int totalRequest;

        private int Row = 0;
        public async Task LoadFile(string filepath, int m_currentIndex, int m_totalRequest, CancellationTokenSource taskSource)
        {
            try
            {
                currentIndex = m_currentIndex;
                totalRequest = m_totalRequest;
                Row = 0;
                data = new TerrainStreamingFileData();
         
                await ReadFile(filepath, taskSource).CancelWith(taskSource.Token);

                var TileName = Path.GetFileNameWithoutExtension(filepath).Split('x');

                int Zoom = int.Parse(TileName[0]);
                int X_Pos = int.Parse(TileName[1]);
                int Y_Pos = int.Parse(TileName[2]);

                data.UpperLeftCoordinate = TerrainStreamingGeoConversion.TileToLatLong(X_Pos, Y_Pos, Zoom);
                data.BottomRightCoordiante = TerrainStreamingGeoConversion.TileToLatLong(X_Pos+1, Y_Pos+1, Zoom);
                data.UpperRightCoordinate = TerrainStreamingGeoConversion.TileToLatLong(X_Pos +1, Y_Pos, Zoom);
                data.BottomLeftCoordinate = TerrainStreamingGeoConversion.TileToLatLong(X_Pos, Y_Pos + 1, Zoom);

                data.cellsize_x = Math.Abs(data.BottomRightCoordiante.x - data.BottomLeftCoordinate.x) / data.mapSize_col;
                data.cellsize_y = Math.Abs(data.UpperLeftCoordinate.y - data.BottomLeftCoordinate.y) / data.mapSize_row;

                LoadComplet = true;

            }
            catch (Exception e)
            {
                Debug.LogError("Error occured while reading file! " + e.ToString());

                if (OnReadError != null)
                {
                    OnReadError();
                }
                return;
            }
        }
        private async Task ReadFile(string filepath, CancellationTokenSource taskSource)
        {
            const int res = 256;

            data.mapSize_row = res;
            data.mapSize_col = res;

            data.floatheightData = new float[res, res];

            byte[] Filedata = new byte[0];

            Filedata = await TerrainStreamingFileAsync.ReadAllBytes(filepath).CancelWith(taskSource.Token);

            Texture2D texture = new Texture2D(res, res);

            texture.LoadImage(Filedata);

            Color[] colors = texture.GetPixels();

            UnityEngine.Object.DestroyImmediate(texture);

            GC.Collect();

            for (int y = 0; y < res; y++)
            {
                int py = (255 - y) * res;

                for (int x = 0; x < res; x++)
                {
                    int px = (255 - x) * res;

                    Color c = colors[py + x];

                    double el = -10000 + (c.r * 255 * 256 * 256 + c.g * 255 * 256 + c.b * 255) * 0.1;

                    data.floatheightData[y , x ] = (float)el;

                    if (el < data.MinElevation)
                        data.MinElevation = (float)el;
                    if (el > data.MaxElevation)
                        data.MaxElevation = (float)el;
                }

                Row++;

                if (Row > 256 - 1)
                {
                    Row++;
                    var Localprog = (((Row * 100) / 256) / totalRequest);
                    var TotalProgress = (((currentIndex - 1) * 100) / totalRequest) + Localprog;

                    if (OnProgress != null)
                        OnProgress("Load Elevation Data  ", TotalProgress);

                    await Task.Delay(1, taskSource.Token).CancelWith(taskSource.Token);

                    Row = 0;
                }

                //var prog = (Row * 100 / data.mapSize_col);

                //if (prog != TotalProg)
                //{
                //    TotalProg = prog;

                //    if (OnProgress != null)
                //        OnProgress("Load Elevation..   " + currentIndex + "/" + totalRequest, TotalProg);

                //    await Task.Delay(TimeSpan.FromMilliseconds(1)).CancelWith(taskSource.Token);
                //}
            }
         
        }

        public static int GetZoomLevel(DVector2 UpperLeftCoordiante , DVector2 DownRightCoordiante,Vector2Int StreamingGridCount , int HeightmapResolution)
        {
           int BestZoomeLevel = 0; int MaxZoomeLevel = 14;

            double R_x = UpperLeftCoordiante.x - DownRightCoordiante.x;
            double R_y = UpperLeftCoordiante.y - DownRightCoordiante.y;

            double Max_R = Mathf.Max(HeightmapResolution * StreamingGridCount.x, HeightmapResolution * StreamingGridCount.y) / Math.Max(R_x, R_y);
 
            for (int i = 5; i < MaxZoomeLevel; i++)
            {
                float Best_R = 256 * (1 << i) / 360f;

                if (Best_R > Max_R)
                {
                    BestZoomeLevel = i;
                    break;
                }
            }

            if (BestZoomeLevel == 0) BestZoomeLevel = MaxZoomeLevel;

            return BestZoomeLevel;
        }
    }
}