/*     Unity GIS Tech 2020-2021      */

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace GISTech.TerrainStreaming
{

    public class TerrainStreamingHGTLoader 
    {
        public TerrainStreamingFileData data;

        public static List<TerrainStreamingFileData> Generated_Tiles_data = new List<TerrainStreamingFileData>();

        int currentIndex;
        int totalRequest;

        public static event ReaderEvents OnReadError;

        public static event TerrainProgression OnProgress;
 
        public bool LoadComplet;

        public async Task LoadFloatGrid(string filepath, int m_currentIndex, int m_totalRequest, CancellationTokenSource taskSource)
        {
            if (File.Exists(filepath))
            {
                currentIndex = m_currentIndex;
                totalRequest = m_totalRequest;

                Generated_Tiles_data = new List<TerrainStreamingFileData>();
                data = new TerrainStreamingFileData();
 
                LoadComplet = false;
                string filename = Path.GetFileNameWithoutExtension(filepath).ToLower();
                string[] fileCoordinate = filename.Split(new[] { 'e', 'w' });
                if (fileCoordinate.Length != 2)
                    throw new ArgumentException("Invalid filename.", filepath);

                fileCoordinate[0] = fileCoordinate[0].TrimStart(new[] { 'n', 's' });
                var Latitude = int.Parse(fileCoordinate[0]);
                data.BottomLeftCoordinate.y = Latitude;
                if (filename.Contains("s"))
                    data.BottomLeftCoordinate.y *= -1;

                var Longitude = int.Parse(fileCoordinate[1]);
                data.BottomLeftCoordinate.x = Longitude;
                if (filename.Contains("w"))
                    data.BottomLeftCoordinate.x *= -1;

                var HgtData = File.ReadAllBytes(filepath);

                switch (HgtData.Length)
                {
                    case 1201 * 1201 * 2:
                        data.mapSize_col = data.mapSize_row = 1201;
                        break;
                    case 3601 * 3601 * 2:
                        data.mapSize_col = data.mapSize_row = 3601;
                        break;
                    default:
                        throw new ArgumentException("Invalid file size.", filepath);
                }


                data.UpperRightCoordinate = new DVector2(data.BottomLeftCoordinate.x + 1, data.BottomLeftCoordinate.y + 1);
                data.UpperLeftCoordinate = new DVector2(data.BottomLeftCoordinate.x, data.UpperRightCoordinate.y);
                data.BottomRightCoordiante = new DVector2(data.UpperRightCoordinate.x, data.BottomLeftCoordinate.y);
                data.BottomLeftCoordinate = new DVector2(data.UpperLeftCoordinate.x, data.BottomRightCoordiante.y);




                data.cellsize_x = Math.Abs(data.BottomRightCoordiante.x - data.BottomLeftCoordinate.x) / data.mapSize_col;
                data.cellsize_y = Math.Abs(data.UpperLeftCoordinate.y - data.BottomLeftCoordinate.y) / data.mapSize_row;

                data.Terrain_Dimension.x = TerrainStreamingGeoConversion.Getdistance(data.BottomLeftCoordinate, data.BottomRightCoordiante, 'X');
                data.Terrain_Dimension.y = TerrainStreamingGeoConversion.Getdistance(data.BottomLeftCoordinate, data.UpperLeftCoordinate, 'Y');

                await ReadFile(filepath, taskSource).CancelWith(taskSource.Token);

               
                if (!File.Exists(filepath))
                {
                    if (OnReadError != null)
                    {
                        OnReadError();
                    }

                    return;
                }

                LoadComplet = true;
            }


        }
        private async Task ReadFile(string filepath, CancellationTokenSource taskSource)
        {
            data.floatheightData = new float[data.mapSize_col, data.mapSize_row];

            short[,] heightMap = new short[data.mapSize_col + 1, data.mapSize_row + 1];

            using (FileStream fs = File.OpenRead(filepath))
            {
                const int size = 1000000;

                int c = 0;

                do
                {
                    byte[] buffer = new byte[size];
                    int count = await fs.ReadAsync(buffer, 0, size).CancelWith(taskSource.Token); ;

                    for (int i = 0; i < count; i += 2)
                    {
                        var buf = buffer[i] * 256 + buffer[i + 1];

                        short value = (short)(buf);

                        heightMap[c % data.mapSize_col, c / data.mapSize_row] = value;

                        float el = value;

                        var x = c % data.mapSize_col;
                        var y = c / data.mapSize_row;

                        if (el < data.MinElevation)
                            data.MinElevation = el;
                        if (el > data.MaxElevation)
                            data.MaxElevation = el;

                        data.floatheightData[y, x] = el;

                        c++;

                        if (OnProgress != null)
                            OnProgress("Load Elevation..   " + currentIndex + "/" + totalRequest, (c * 100 / data.mapSize_row));

                    }

                }
                while (fs.Position != fs.Length);

                fs.Close();
                GC.Collect();

                LoadComplet = true;
            }
        }

     }

}