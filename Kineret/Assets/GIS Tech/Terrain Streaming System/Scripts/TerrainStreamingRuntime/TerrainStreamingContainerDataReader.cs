/*     Unity GIS Tech 2020-2021      */

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace GISTech.TerrainStreaming
{
    public class TerrainStreamingContainerDataReader 
    {
        public TerrainStreamingContainer Container;
        public TerrainStreamingContainerDataReader(string dataPath)
        {
            try
            {
                if (File.Exists(dataPath))
                {
                    LoadFloatGrid(dataPath);
                }
                else
                    Debug.LogError("File not found .. !");
            }
            catch (Exception ex)
            {
                Debug.LogError("Can not Read Data file " + ex.Message);
            }

        }
        private void LoadFloatGrid(string filepath)
        {
            GameObject s = new GameObject();
            Container = s.AddComponent<TerrainStreamingContainer>();
            Container.ResetData();

            StreamReader DataReader = new StreamReader(filepath);

            string hdrTemp = null;

            hdrTemp = DataReader.ReadLine();

            while (hdrTemp != null)
            {
                hdrTemp.Replace(" ", "");
                string[] lineTemp = hdrTemp.Split('=');

                switch (lineTemp[0].Trim())
                {
                    case "MainZone":
                        Container.ZoneName = lineTemp[1];
                        break;
                    case "UpperLeftCoordinate_x":
                        Container.
                            UpperLeftCoordinate.x = TerrainStreamingExtensions.ConvertToDouble(lineTemp[1]);
                        break;
                    case "UpperLeftCoordinate_y":
                        Container.UpperLeftCoordinate.y = TerrainStreamingExtensions.ConvertToDouble(lineTemp[1]);
                        break;
                    case "BottomRightCoordiante_x":
                        Container.BottomRightCoordiante.x = TerrainStreamingExtensions.ConvertToDouble(lineTemp[1]);
                        break;
                    case "BottomRightCoordiante_y":
                        Container.BottomRightCoordiante.y = TerrainStreamingExtensions.ConvertToDouble(lineTemp[1]);
                        break;
                    case "MinElevation":
                        Container.MinMaxElevation.x = (float)TerrainStreamingExtensions.ConvertToDouble(lineTemp[1]);
                        break;
                    case "MaxElevation":
                        Container.MinMaxElevation.y = (float)TerrainStreamingExtensions.ConvertToDouble(lineTemp[1]);
                        break;
                    case "Dimension_x":
                        Container.Dimensions.x = (float)TerrainStreamingExtensions.ConvertToDouble(lineTemp[1]);
                        break;
                    case "Dimension_y":
                        Container.Dimensions.y = (float)TerrainStreamingExtensions.ConvertToDouble(lineTemp[1]);
                        break;
                    case "TilesCount_x":
                        Container.TilesCount.x = Int32.Parse(lineTemp[1]);
                        break;
                    case "TilesCount_y":
                        Container.TilesCount.y = Int32.Parse(lineTemp[1]);
                        break;
                    case "sizeY":
                        Container.SubTerrainSize.y = (float)TerrainStreamingExtensions.ConvertToDouble(lineTemp[1]);
                        break;
                    case "HeightmapResolution":
                        Container.heightmapResolution = Int32.Parse(lineTemp[1]);
                        break;
                    case "ZoomLevel":
                        Container.zoomLevel = Int32.Parse(lineTemp[1]);
                        break;
                }
                hdrTemp = DataReader.ReadLine();
            }

            DataReader.Close();

            GameObject.Destroy(s);
        }

    }
}