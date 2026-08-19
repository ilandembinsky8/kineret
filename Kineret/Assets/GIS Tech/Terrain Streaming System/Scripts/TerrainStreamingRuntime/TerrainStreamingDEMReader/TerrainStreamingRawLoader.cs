/*     Unity GIS Tech 2020-2021      */

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace GISTech.TerrainStreaming
{
    public class TerrainStreamingRawLoader
    {

        public static event ReaderEvents OnReadError;

        public static List<TerrainStreamingFileData> Generated_Tiles_data = new List<TerrainStreamingFileData>();
 
        public TerrainStreamingFileData data;



        public bool LoadComplet;

        private static Depth m_Depth = Depth.Bit16;

        private int m_Width = 1;

        private int m_Height = 1;

        private static ByteOrder m_ByteOrder = ByteOrder.Windows;

        private string errorString;

        public int heightmapResolution;
        public int m_Resolution;


        public void LoadRawGrid(string filepath)
        {
            if (!File.Exists(filepath))
            {
                Debug.LogError("Please select a correct Raw file.");

                if (OnReadError != null)
                {
                    OnReadError();
                }
                return;
            }

            data = new TerrainStreamingFileData();

            PickRawDefaults(filepath);

            if (IsValidFile())
            {
                data.floatheightData = ReadRaw(filepath);
                LoadComplet = true;
            }
            else
                LoadComplet = false;

        }
        private void PickRawDefaults(string path)
        {
            FileStream fileStream = File.Open(path, FileMode.Open, FileAccess.Read);
            int num = (int)fileStream.Length;
            fileStream.Close();

            bool flag = heightmapResolution * heightmapResolution == num;
            if (flag)
            {
                m_Resolution = heightmapResolution;
                m_Depth = Depth.Bit8;
            }
            else
            {
                bool flag2 = heightmapResolution * heightmapResolution * 2 == num;
                if (flag2)
                {
                    this.m_Resolution = heightmapResolution;
                    m_Depth = Depth.Bit16;
                }
                else
                {
                    m_Depth = Depth.Bit16;
                    int num2 = num / (int)m_Depth;
                    int num3 = Mathf.RoundToInt(Mathf.Sqrt((float)num2));
                    bool flag3 = num3 * num3 * (int)m_Depth == num;
                    if (flag3)
                    {
                        this.m_Resolution = num3;
                    }
                    else
                    {
                        m_Depth = Depth.Bit8;
                        num2 = num / (int)m_Depth;
                        num3 = Mathf.RoundToInt(Mathf.Sqrt((float)num2));
                        bool flag4 = num3 * num3 * (int)m_Depth == num;
                        if (flag4)
                        {
                            this.m_Resolution = num3;
                        }
                        else
                        {
                            m_Depth = Depth.Bit16;
                        }
                    }
                }
            }
        }


        private float[,] ReadRaw(string path)
        {
            byte[] array;
            using (BinaryReader binaryReader = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read)))
            {
                array = binaryReader.ReadBytes(this.m_Resolution * this.m_Resolution * (int)m_Depth);
                binaryReader.Close();
            }

            float[,] array2 = new float[heightmapResolution, heightmapResolution];
            bool flag = m_Depth == Depth.Bit16;
            if (flag)
            {
                float num = 1.52587891E-05f;
                for (int i = 0; i < heightmapResolution; i++)
                {
                    for (int j = 0; j < heightmapResolution; j++)
                    {
                        int num2 = Mathf.Clamp(j, 0, this.m_Resolution - 1) + Mathf.Clamp(i, 0, this.m_Resolution - 1) * this.m_Resolution;
                        bool flag2 = m_ByteOrder == ByteOrder.Mac == BitConverter.IsLittleEndian;
                        if (flag2)
                        {
                            byte b = array[num2 * 2];
                            array[num2 * 2] = array[num2 * 2 + 1];
                            array[num2 * 2 + 1] = b;
                        }
                        ushort num3 = BitConverter.ToUInt16(array, num2 * 2);
                        float num4 = (float)num3 * num;
                        //int num5 = m_FlipVertically ? (heightmapResolution - 1 - i) : i;
                        //array2[j,heightmapResolution - i - 1 ] = num4;
                        int num5 = m_FlipVertically ? (heightmapResolution - 1 - i) : i;
                        array2[num5, j] = num4;
                    }
                }
            }
            else
            {
                float num6 = 0.00390625f;
                for (int k = 0; k < heightmapResolution; k++)
                {
                    for (int l = 0; l < heightmapResolution; l++)
                    {
                        int num7 = Mathf.Clamp(l, 0, this.m_Resolution - 1) + Mathf.Clamp(k, 0, this.m_Resolution - 1) * this.m_Resolution;
                        byte b2 = array[num7];
                        float num8 = (float)b2 * num6;
                        int num9 = m_FlipVertically ? (heightmapResolution - 1 - k) : k;
                        array2[num9, l] = num8;
                    }
                }
            }
            return array2;
        }
 

        private bool IsValidFile()
        {
            bool valid = false;

            if (this.m_Width > 4097 || this.m_Height > 4097)
            {
                valid = false;
                errorString = "Heightmaps above 4097x4097 in resolution are not supported";
                Debug.LogError(errorString);
            }
            else
                valid = true;

            return valid;

        }


        public static bool m_FlipVertically = false;
        public static bool isDone;



        public static async Task WriteRaw(string path, float[,] floatheightData, CancellationTokenSource taskSource)
        {
            isDone = false;

            int m_Depth = 16;
            Depth depth = Depth.Bit16;
            ByteOrder order = ByteOrder.Windows;
            if (depth == Depth.Bit16)
                m_Depth = 16;

            int heightmapResolution = floatheightData.GetLength(0);

            int cx = 1;
            int cy = 1;

            FileStream stream = new FileStream(path, FileMode.Create);

            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                int textureWidth = cx * heightmapResolution;
                int coof = m_Depth == 8 ? 1 : 2;

                for (int y = 0; y < cy; y++)
                {
                    for (int x = 0; x < cx; x++)
                    {
                        float[,] heightmap = floatheightData;

                        for (int dy = 0; dy < heightmapResolution; dy++)
                        {

                            int row = cy * heightmapResolution - (y * heightmapResolution + dy) - 1;
                            int seek = (row * textureWidth + x * heightmapResolution) * coof;

                            stream.Seek(seek, SeekOrigin.Begin);

                            for (int dx = 0; dx < heightmapResolution; dx++)
                            {
                                if (m_Depth == 8) writer.Write((byte)Mathf.RoundToInt(heightmap[dy, dx] * 255));
                                else
                                {
                                    short v = (short)Mathf.RoundToInt(heightmap[dx, dy] * 65536);
                                    if (order == ByteOrder.Windows) writer.Write(v);
                                    else
                                    {
                                        writer.Write((byte)(v / 256));
                                        writer.Write((byte)(v % 256));
                                    }
                                }
                            }
                        }
                    }
                }

                isDone = true;

                stream.Close();

                await Task.Delay(TimeSpan.FromSeconds(0.01)).CancelWith(taskSource.Token);

                
            }


        }


    }




    public enum Depth
    {
        Bit8 = 1,
        Bit16
    }

    public enum ByteOrder
    {
        Mac = 1,
        Windows
    }


}