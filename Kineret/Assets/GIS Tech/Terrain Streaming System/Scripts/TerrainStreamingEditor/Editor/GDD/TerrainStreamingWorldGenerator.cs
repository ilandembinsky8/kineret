/*     Unity GIS Tech 2020-2021      */

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace GISTech.TerrainStreaming
{
    public class TerrainStreamingWorldGenerator
    {
        private static TerrainStreamingFileData mainData;

        private static Vector2Int terrainCount;
        private static int HeightmapResolution;
        private static Vector3 WorldScale;


        private static string SaveFolder;
        private static string DownloadPath;

        private static float ScaleFactor = 1000;
        private static float ElevationScaleValue = 1112.0f;
        private static TerrainStreamingTileData[,] AllSectors;

        private static int lastX;

        private static TerrainStreamingContainer terrainContainer;

        public static event TerrainProgression OnProgress;

        public static event DEMEvent OnTilesGenerated;

        public static List<TerrainStreamingTileData> Generated_Tiles_data = new List<TerrainStreamingTileData>();
        public static int index = 0;
        static CancellationTokenSource taskSource;
        public static async Task SaveDEMTiles(CancellationTokenSource m_taskSource, TerrainStreamingDownloader Prefs, Vector2Int m_StreamingGridCount, string m_SaveFolder, TerrainStreamingFileData m_mainData , Vector2 MinMaxZone,bool Horizon = false, bool m_generateworld=false)
        {
            taskSource = m_taskSource;

            try {

                if (GameObject.Find("m_SectorsContainer"))
                    GameObject.DestroyImmediate(GameObject.Find("m_SectorsContainer"));


                mainData = m_mainData;
                terrainCount = m_StreamingGridCount;

                HeightmapResolution = Prefs.HeightmapResolution;
                WorldScale = Prefs.WorldScale;
                DownloadPath = Prefs.DownloadPath;
                SaveFolder = m_SaveFolder;

                GameObject SectorsContainer = null;
                GameObject m_SectorsContainer = null;

                if (m_generateworld)
                {
                    if (GameObject.Find("SectorsContainer"))
                        GameObject.DestroyImmediate(GameObject.Find("SectorsContainer"));

                    SectorsContainer = new GameObject("SectorsContainer");

                    SectorsContainer.transform.position = new Vector3(0, 0, 0);
                }

                float maxElevation = mainData.MaxElevation;
                float minElevation = mainData.MinElevation;

                float ElevationRange = maxElevation - minElevation;

                var sizeX = Mathf.Floor((float)mainData.Terrain_Dimension.x * Prefs.WorldScale.x * ScaleFactor) / terrainCount.x;
                var sizeZ = Mathf.Floor((float)mainData.Terrain_Dimension.y * Prefs.WorldScale.z * ScaleFactor) / terrainCount.y;
                float sizeY = sizeY = (ElevationRange / ElevationScaleValue) * ScaleFactor * Prefs.WorldScale.y;

                Vector3 Tilesize = new Vector3(sizeX, sizeY, sizeZ);

                if (m_generateworld)
                    terrainContainer = SectorsContainer.AddComponent<TerrainStreamingContainer>();
                else
                {
                    m_SectorsContainer = new GameObject("m_SectorsContainer");
                    terrainContainer = m_SectorsContainer.AddComponent<TerrainStreamingContainer>();
                }

                terrainContainer.TilesCount = terrainCount;

                terrainContainer.Scale = Prefs.WorldScale;

                terrainContainer.SubTerrainSize = Tilesize;

                terrainContainer.ContainerSize = new Vector3(Tilesize.x * terrainCount.x, Tilesize.y, Tilesize.z * terrainCount.y);

                terrainContainer.heightmapResolution = HeightmapResolution;

                terrainContainer.zoomLevel = Prefs.ZoomLevel;

                terrainContainer.ZoneName = Prefs.ZoneName;

                AllSectors = new TerrainStreamingTileData[terrainCount.x, terrainCount.y];

                for (int x = 0; x < terrainCount.x; x++)
                {
                    for (int y = 0; y < terrainCount.y; y++)
                    {
                        var TerrainTileSector = new TerrainStreamingTileData(string.Format("Tile_{0}__{1}", x, y));
                        TerrainTileSector.Number = new Vector2Int(x, y);
                        TerrainTileSector.Position = new Vector3(Tilesize.x * x, 0, terrainContainer.ContainerSize.z / 2 - Tilesize.z * y);
                        TerrainTileSector.TileBounds = new Bounds(TerrainTileSector.Position, Tilesize);
                        AllSectors[x, y] = TerrainTileSector;

                        if (m_generateworld)
                        {
                            GameObject TileSector = new GameObject(TerrainTileSector.Name);
                            var Tile = TileSector.AddComponent<TerrainStreamingTileSector>();
                            Tile.tileBounds = TerrainTileSector.TileBounds;
                            Tile.Number = TerrainTileSector.Number;
                            TileSector.transform.position = TerrainTileSector.Position;
                            TileSector.transform.parent = SectorsContainer.transform;
                        }

                    }
                }

                index = 0;

                var LonStep = (mainData.BottomRightCoordiante.x - mainData.BottomLeftCoordinate.x) / terrainCount.x;
                var LatStep = (mainData.UpperLeftCoordinate.y - mainData.BottomRightCoordiante.y) / terrainCount.y;


                for (int x = 0; x < terrainCount.x; x++)
                {
                    for (int y = 0; y < terrainCount.y; y++)
                    {
                        var tile = AllSectors[x, y];

                        if(Prefs.State != DownloaderState.idle)
                        await GenerateHeightMap(tile, terrainCount, HeightmapResolution, taskSource).CancelWith(taskSource.Token);

                        tile.UpperLeftCoordinate = new DVector2(mainData.UpperLeftCoordinate.x + x * LonStep, mainData.UpperLeftCoordinate.y - y * LatStep);
                        tile.BottomRightCoordiante = new DVector2(tile.UpperLeftCoordinate.x + LonStep, tile.UpperLeftCoordinate.y - LatStep);

                        Generated_Tiles_data.Add(tile);
                    }
                }

                if (Prefs.State != DownloaderState.idle)
                {
                    

                    if (Horizon)
                        await Hr_WriteTerrainData(taskSource).CancelWith(taskSource.Token);
                    else
                    await WriteTerrainData(taskSource).CancelWith(taskSource.Token);
                }


                if (OnTilesGenerated != null)
                    OnTilesGenerated(Generated_Tiles_data);

                if (m_SectorsContainer)
                    GameObject.DestroyImmediate(m_SectorsContainer);
            }
            catch (OperationCanceledException)
            {
                taskSource.Cancel();
                throw;
            }
}
        public static async Task GenerateHeightMap(TerrainStreamingTileData item, Vector2Int m_terraincount, int m_heightmapResolution, CancellationTokenSource taskSource)
        {
           try {
                var tdataHeightmap = new float[m_heightmapResolution, m_heightmapResolution];

                float elevationRange = mainData.MaxElevation - mainData.MinElevation;

                float thx = m_heightmapResolution - 1;
                float thy = m_heightmapResolution - 1;


                var y_Terrain_Row_num = (mainData.mapSize_row / m_terraincount.y);
                var x_Terrain_Col_num = (mainData.mapSize_col / m_terraincount.x);

                int tw = m_heightmapResolution;
                int th = m_heightmapResolution;

                for (int x = lastX; x < tw; x++)
                {
                    for (int y = 0; y < th; y++)
                    {
                        var x_from = item.Number.x * x_Terrain_Col_num;
                        var x_To = (item.Number.x * x_Terrain_Col_num + x_Terrain_Col_num);

                        var y_from = (item.Number.y * y_Terrain_Row_num);
                        var y_To = (item.Number.y * y_Terrain_Row_num + y_Terrain_Row_num);

                        float fpx = Mathf.Lerp(x_from, x_To, x / thx);
                        float fpy = Mathf.Lerp(y_from, y_To, y / thy);

                        int px = Mathf.FloorToInt(fpx);
                        int py = Mathf.FloorToInt(fpy);

                        var Rel = mainData.GetElevation(fpx, fpy);

                        var el = (((Rel - mainData.MinElevation)) / elevationRange);


                        tdataHeightmap[x, y] = el;
                    }

                    lastX = x;
                }

                lastX = 0;

                index++;
                string TileName = string.Format("Tile__{0}__{1}", item.Number.x, item.Number.y);

                await TerrainStreamingRawLoader.WriteRaw(SaveFolder + TileName + ".raw", tdataHeightmap, taskSource).CancelWith(taskSource.Token); ;

                while (!TerrainStreamingRawLoader.isDone)
                    await Task.Delay(TimeSpan.FromSeconds(0.001)).CancelWith(taskSource.Token);

                var tProg = index * 100 / (terrainCount.x * terrainCount.y);

                if (OnProgress != null)
                    OnProgress("Generating DEM Tiles ", tProg);

                await Task.Delay(TimeSpan.FromSeconds(0.001)).CancelWith(taskSource.Token);
            }
            catch (OperationCanceledException)
            {
                taskSource.Cancel();
                throw;
            }
}
        public static async Task WriteTerrainData(CancellationTokenSource taskSource)
        {
            var TerrainExportData = "";

            TerrainExportData += "Main Zone  = " + terrainContainer.ZoneName + "\n";
            TerrainExportData += "UpperLeftCoordinate_x  = " +mainData.UpperLeftCoordinate.x.ToString() + "\n";
            TerrainExportData += "UpperLeftCoordinate_y  = " + mainData.UpperLeftCoordinate.y.ToString() + "\n";

            TerrainExportData += "BottomRightCoordiante_x  = " + mainData.BottomRightCoordiante.x.ToString() + "\n";
            TerrainExportData += "BottomRightCoordiante_y  = " + mainData.BottomRightCoordiante.y.ToString() + "\n";
 
            TerrainExportData += "MinElevation  = " + mainData.MinElevation.ToString() + "\n";
            TerrainExportData += "MaxElevation  = " + mainData.MaxElevation.ToString() + "\n";

            TerrainExportData += "Dimension_x  = " + mainData.Terrain_Dimension.x.ToString() + "\n";
            TerrainExportData += "Dimension_y  = " + mainData.Terrain_Dimension.y.ToString() + "\n";

            TerrainExportData += "TilesCount_x  = " + terrainCount.x.ToString() + "\n";
            TerrainExportData += "TilesCount_y  = " + terrainCount.y.ToString() + "\n";

            TerrainExportData += "sizeY  = " + terrainContainer.SubTerrainSize.y + "\n";

            TerrainExportData += "HeightmapResolution  = " + terrainContainer.heightmapResolution + "\n";

            TerrainExportData += "ZoomLevel  = " + terrainContainer.zoomLevel + "\n";

            using (StreamWriter file = new StreamWriter(DownloadPath + "/TerrainData.dat"))
            {
                await file.WriteAsync(TerrainExportData).CancelWith(taskSource.Token);
            }
 
        }
        public static async Task Hr_WriteTerrainData(CancellationTokenSource taskSource)
        {
         
            var TerrainExportData = "";

            TerrainExportData += "UpperLeftCoordinate_x  = " + mainData.UpperLeftCoordinate.x.ToString() + "\n";
            TerrainExportData += "UpperLeftCoordinate_y  = " + mainData.UpperLeftCoordinate.y.ToString() + "\n";

            TerrainExportData += "BottomRightCoordiante_x  = " + mainData.BottomRightCoordiante.x.ToString() + "\n";
            TerrainExportData += "BottomRightCoordiante_y  = " + mainData.BottomRightCoordiante.y.ToString() + "\n";

            TerrainExportData += "MinElevation  = " + mainData.MinElevation.ToString() + "\n";
            TerrainExportData += "MaxElevation  = " + mainData.MaxElevation.ToString() + "\n";

            TerrainExportData += "Dimension_x  = " + mainData.Terrain_Dimension.x.ToString() + "\n";
            TerrainExportData += "Dimension_y  = " + mainData.Terrain_Dimension.y.ToString() + "\n";

            TerrainExportData += "TilesCount_x  = " + terrainCount.x.ToString() + "\n";
            TerrainExportData += "TilesCount_y  = " + terrainCount.y.ToString() + "\n";

            TerrainExportData += "sizeY  = " + terrainContainer.SubTerrainSize.y + "\n";

            TerrainExportData += "HeightmapResolution  = " + terrainContainer.heightmapResolution + "\n";

            TerrainExportData += "ZoomLevel  = " + terrainContainer.zoomLevel + "\n";

            using (StreamWriter file = new StreamWriter(DownloadPath + "/TerrainData_Hr.hor"))
            {
                await file.WriteAsync(TerrainExportData).CancelWith(taskSource.Token); ;
            }

        }
    }
}
