using System;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace GISTech.TerrainStreaming
{
    public class TerrainStreamingVectorSerializer
    {
        public static void RegisterCustomType<T>(byte code)
        {
            Type type = typeof(T);

            MethodInfo[] methodInfos = type.GetMethods();

            Func<object, byte[]> serializeMethod = null;
            Func<byte[], object> deserializeMethod = null;

            foreach (MethodInfo methodInfo in methodInfos)
            {
                if (methodInfo.Name == "Serialize")
                {
                    if (!methodInfo.IsStatic)
                    {
                        Debug.LogError(string.Format("Serialize method must be static! Registering custom type \"{0}\" failed.", type.ToString()));
                        return;
                    }

                    serializeMethod = (Func<object, byte[]>)methodInfo.CreateDelegate(typeof(Func<object, byte[]>));
                }
                if (methodInfo.Name == "Deserialize")
                {
                    if (!methodInfo.IsStatic)
                    {
                        Debug.LogError(string.Format("Deserialize method must be static! Registering custom type \"{0}\" failed.", type.ToString()));
                        return;
                    }

                    deserializeMethod = (Func<byte[], object>)methodInfo.CreateDelegate(typeof(Func<byte[], object>));
                }
            };

            if (serializeMethod == null)
            {
                Debug.LogError(string.Format("There is no serialize method! Registering custom type \"{0}\" failed.", type.ToString()));
                return;
            }

            if (deserializeMethod == null)
            {
                Debug.LogError(string.Format("There is no serialize method! Registering custom type \"{0}\" failed.", type.ToString()));
                return;
            }


        }

        public static byte[] JoinBytes(params byte[][] bytes)
        {
            byte[] rv = new byte[bytes.Sum(x => x.Length)];
            int offset = 0;

            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i].CopyTo(rv, offset);
                offset += bytes[i].Length;
            }

            return rv;
        }

        public static void JoinBytes(ref byte[] bytesArray, byte[] joinBytes)
        {
            bytesArray = bytesArray.Concat(joinBytes).ToArray();
        }

        public static void JoinBytes(ref byte[] bytesArray, params byte[][] joinBytes)
        {
            for (int i = 0; i < joinBytes.Length; i++)
            {
                bytesArray = bytesArray.Concat(joinBytes[i]).ToArray();
            }
        }

        #region Serialize
        public static void Serialize(int value, ref byte[] bytes)
        {
            byte[] _bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(_bytes);

            JoinBytes(ref bytes, _bytes);
        }

        public static void Serialize(float value, ref byte[] bytes)
        {
            byte[] _bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(_bytes);

            JoinBytes(ref bytes, _bytes);
        }

        public static void Serialize(bool value, ref byte[] bytes)
        {
            byte[] _bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(_bytes);

            JoinBytes(ref bytes, _bytes);
        }

        public static void Serialize(Vector3 value, ref byte[] bytes)
        {
            byte[] x = BitConverter.GetBytes(value.x);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(x);

            byte[] y = BitConverter.GetBytes(value.y);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(y);

            byte[] z = BitConverter.GetBytes(value.z);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(z);

            JoinBytes(ref bytes, x, y, z);
        }

        public static void Serialize(Vector2 value, ref byte[] bytes)
        {
            byte[] x = BitConverter.GetBytes(value.x);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(x);

            byte[] y = BitConverter.GetBytes(value.y);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(y);

            JoinBytes(ref bytes, x, y);
        }

        public static void Serialize(DVector2 value, ref byte[] bytes)
        {
            byte[] x = BitConverter.GetBytes(value.x);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(x);

            byte[] y = BitConverter.GetBytes(value.y);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(y);

            JoinBytes(ref bytes, x, y);
        }

        public static void Serialize(Quaternion value, ref byte[] bytes)
        {
            byte[] x = BitConverter.GetBytes(value.x);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(x);

            byte[] y = BitConverter.GetBytes(value.y);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(y);

            byte[] z = BitConverter.GetBytes(value.z);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(z);

            byte[] w = BitConverter.GetBytes(value.w);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(w);

            JoinBytes(ref bytes, x, y, z, w);
        }


        public static void Serialize(string value, ref byte[] bytes)
        {
            if (string.IsNullOrEmpty(value))
            {
                Serialize(0, ref bytes);
                return;
            }

            byte[] stringBytes = Encoding.UTF8.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(stringBytes);

            byte[] lengthBytes = BitConverter.GetBytes(stringBytes.Length);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(lengthBytes);

            JoinBytes(ref bytes, lengthBytes, stringBytes);
        }

        public static void Serialize(int[] values, ref byte[] bytes)
        {
            int length = values == null ? 0 : values.Length;
            if (length > 0)
            {
                Serialize(length, ref bytes);
                for (int i = 0; i < values.Length; i++)
                {
                    Serialize(values[i], ref bytes);
                }
            }
            else
            {
                Serialize(0, ref bytes);
            }
        }

        public static void Serialize(float[] values, ref byte[] bytes)
        {
            int length = values == null ? 0 : values.Length;
            if (length > 0)
            {
                Serialize(length, ref bytes);
                for (int i = 0; i < values.Length; i++)
                {
                    Serialize(values[i], ref bytes);
                }
            }
            else
            {
                Serialize(0, ref bytes);
            }
        }

        public static void Serialize(bool[] values, ref byte[] bytes)
        {
            int length = values == null ? 0 : values.Length;
            if (length > 0)
            {
                Serialize(length, ref bytes);
                for (int i = 0; i < values.Length; i++)
                {
                    Serialize(values[i], ref bytes);
                }
            }
            else
            {
                Serialize(0, ref bytes);
            }
        }
        public static void Serialize(Vector2[] values, ref byte[] bytes)
        {
            int length = values == null ? 0 : values.Length;
            if (length > 0)
            {
                Serialize(length, ref bytes);
                for (int i = 0; i < values.Length; i++)
                {
                    Serialize(values[i], ref bytes);
                }
            }
            else
            {
                Serialize(0, ref bytes);
            }
        }
        public static void Serialize(DVector2[] values, ref byte[] bytes)
        {
            int length = values == null ? 0 : values.Length;
            if (length > 0)
            {
                Serialize(length, ref bytes);
                for (int i = 0; i < values.Length; i++)
                {
                    Serialize(values[i], ref bytes);
                }
            }
            else
            {
                Serialize(0, ref bytes);
            }
        }
        public static void Serialize(Vector3[] values, ref byte[] bytes)
        {
            int length = values == null ? 0 : values.Length;
            if (length > 0)
            {
                Serialize(length, ref bytes);
                for (int i = 0; i < values.Length; i++)
                {
                    Serialize(values[i], ref bytes);
                }
            }
            else
            {
                Serialize(0, ref bytes);
            }
        }
        public static void Serialize(Quaternion[] values, ref byte[] bytes)
        {
            int length = values == null ? 0 : values.Length;
            if (length > 0)
            {
                Serialize(length, ref bytes);
                for (int i = 0; i < values.Length; i++)
                {
                    Serialize(values[i], ref bytes);
                }
            }
            else
            {
                Serialize(0, ref bytes);
            }
        }
        public static void Serialize(string[] values, ref byte[] bytes)
        {
            int length = values == null ? 0 : values.Length;
            if (length > 0)
            {
                Serialize(length, ref bytes);
                for (int i = 0; i < values.Length; i++)
                {
                    Serialize(values[i], ref bytes);
                }
            }
            else
            {
                Serialize(0, ref bytes);
            }
        }

        public static void Serialize(TerrainStreamingPolygonGeoData customObject, ref byte[] bytes)
        {
            TerrainStreamingPolygonGeoData o = (TerrainStreamingPolygonGeoData)customObject;

            TerrainStreamingVectorSerializer.Serialize(o.ID, ref bytes);
            TerrainStreamingVectorSerializer.Serialize(o.Name, ref bytes);
            TerrainStreamingVectorSerializer.Serialize(o.Tag_Key, ref bytes);
            TerrainStreamingVectorSerializer.Serialize(o.Tag_Value, ref bytes);
            TerrainStreamingVectorSerializer.Serialize(o.GeoPoints.ToArray(), ref bytes);
            TerrainStreamingVectorSerializer.Serialize(o.Height, ref bytes);
            TerrainStreamingVectorSerializer.Serialize(o.MinHeight, ref bytes);
            TerrainStreamingVectorSerializer.Serialize(o.Levels, ref bytes);
            TerrainStreamingVectorSerializer.Serialize(o.MinLevel, ref bytes);

            JoinBytes(ref bytes);
        }
        public static void Serialize(TerrainStreamingLinesGeoData customObject, ref byte[] bytes)
        {
            TerrainStreamingLinesGeoData o = (TerrainStreamingLinesGeoData)customObject;

            TerrainStreamingVectorSerializer.Serialize(o.ID, ref bytes);
            TerrainStreamingVectorSerializer.Serialize(o.Name, ref bytes);
            TerrainStreamingVectorSerializer.Serialize(o.Tag_Key, ref bytes);
            TerrainStreamingVectorSerializer.Serialize(o.Tag_Value, ref bytes);
            TerrainStreamingVectorSerializer.Serialize(o.GeoPoints.ToArray(), ref bytes);

            JoinBytes(ref bytes);
        }
        public static void Serialize(TerrainStreamingPolygonGeoData[] values, ref byte[] bytes)
        {
            int length = values == null ? 0 : values.Length;
            if (length > 0)
            {
                Serialize(length, ref bytes);
                for (int i = 0; i < values.Length; i++)
                {
                    Serialize(values[i], ref bytes);
                }
            }
            else
            {
                Serialize(0, ref bytes);
            }
        }
        public static void Serialize(TerrainStreamingLinesGeoData[] values, ref byte[] bytes)
        {
            int length = values == null ? 0 : values.Length;
            if (length > 0)
            {
                Serialize(length, ref bytes);
                for (int i = 0; i < values.Length; i++)
                {
                    Serialize(values[i], ref bytes);
                }
            }
            else
            {
                Serialize(0, ref bytes);
            }
        }
        #endregion

        #region Deserialize
        public static int DeserializeInt(byte[] bytes, ref int offset)
        {
            byte[] _bytes = new byte[4];
            Array.Copy(bytes, offset, _bytes, 0, 4);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(_bytes);

            offset += 4;
            return BitConverter.ToInt32(_bytes, 0);

            // int, float, bool vecot3, ve2 ,qua, string
        }

        public static float DeserializeFloat(byte[] bytes, ref int offset)
        {
            byte[] _bytes = new byte[4];
            Array.Copy(bytes, offset, _bytes, 0, 4);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(_bytes);

            offset += 4;
            return BitConverter.ToSingle(_bytes, 0);
        }

        public static bool DeserializeBool(byte[] bytes, ref int offset)
        {
            byte[] _bytes = new byte[1];
            Array.Copy(bytes, offset, _bytes, 0, 1);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(_bytes);

            offset += 1;
            return BitConverter.ToBoolean(_bytes, 0);
        }

        public static Vector3 DeserializeVector3(byte[] bytes, ref int offset)
        {
            byte[] _xBytes = new byte[4];
            byte[] _yBytes = new byte[4];
            byte[] _zBytes = new byte[4];

            Array.Copy(bytes, offset, _xBytes, 0, 4);
            Array.Copy(bytes, offset + 4, _yBytes, 0, 4);
            Array.Copy(bytes, offset + 8, _zBytes, 0, 4);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(_xBytes);
                Array.Reverse(_yBytes);
                Array.Reverse(_zBytes);
            }

            Vector3 o = new Vector3();
            o.x = BitConverter.ToSingle(_xBytes, 0);
            o.y = BitConverter.ToSingle(_yBytes, 0);
            o.z = BitConverter.ToSingle(_zBytes, 0);

            offset += 12;
            return o;
        }

        public static Vector2 DeserializeVector2(byte[] bytes, ref int offset)
        {
            byte[] _xBytes = new byte[4];
            byte[] _yBytes = new byte[4];

            Array.Copy(bytes, offset, _xBytes, 0, 4);
            Array.Copy(bytes, offset + 4, _yBytes, 0, 4);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(_xBytes);
                Array.Reverse(_yBytes);
            }

            Vector2 o = new Vector2();
            o.x = BitConverter.ToSingle(_xBytes, 0);
            o.y = BitConverter.ToSingle(_yBytes, 0);

            offset += 8;
            return o;
        }

        public static DVector2 DeserializeDVector2D(byte[] bytes, ref int offset)
        {
            byte[] _xBytes = new byte[8];
            byte[] _yBytes = new byte[8];

            Array.Copy(bytes, offset, _xBytes, 0, 8);
            Array.Copy(bytes, offset + 8, _yBytes, 0, 8);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(_xBytes);
                Array.Reverse(_yBytes);
            }

            DVector2 o = new DVector2(0, 0);
            o.x = BitConverter.ToDouble(_xBytes, 0);
            o.y = BitConverter.ToDouble(_yBytes, 0);

            offset += 16;
            return o;
        }

        public static Quaternion DeserializeQuaternion(byte[] bytes, ref int offset)
        {
            byte[] _xBytes = new byte[4];
            byte[] _yBytes = new byte[4];
            byte[] _zBytes = new byte[4];
            byte[] _wBytes = new byte[4];

            Array.Copy(bytes, offset, _xBytes, 0, 4);
            Array.Copy(bytes, offset + 4, _yBytes, 0, 4);
            Array.Copy(bytes, offset + 8, _zBytes, 0, 4);
            Array.Copy(bytes, offset + 12, _wBytes, 0, 4);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(_xBytes);
                Array.Reverse(_yBytes);
                Array.Reverse(_zBytes);
                Array.Reverse(_wBytes);
            }

            Quaternion o = new Quaternion();
            o.x = BitConverter.ToSingle(_xBytes, 0);
            o.y = BitConverter.ToSingle(_yBytes, 0);
            o.z = BitConverter.ToSingle(_zBytes, 0);
            o.w = BitConverter.ToSingle(_wBytes, 0);

            offset += 16;
            return o;
        }

        public static string DeserializeString(byte[] bytes, ref int offset)
        {
            int length = DeserializeInt(bytes, ref offset);
            if (length > 0)
            {
                byte[] _bytes = new byte[length];
                Array.Copy(bytes, offset, _bytes, 0, length);

                if (BitConverter.IsLittleEndian)
                    Array.Reverse(_bytes);

                offset += length;
                return Encoding.UTF8.GetString(_bytes);
            }

            return "";
        }

        public static int[] DeserializeIntArray(byte[] bytes, ref int offset)
        {
            int length = DeserializeInt(bytes, ref offset);

            if (length > 0)
            {
                int[] array = new int[length];
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = DeserializeInt(bytes, ref offset);
                }

                return array;
            }

            return new int[0];
        }

        public static float[] DeserializeFloatArray(byte[] bytes, ref int offset)
        {
            int length = DeserializeInt(bytes, ref offset);
            if (length > 0)
            {
                float[] array = new float[length];
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = DeserializeFloat(bytes, ref offset);
                }

                return array;
            }

            return new float[0];
        }

        public static bool[] DeserializeBoolArray(byte[] bytes, ref int offset)
        {
            int length = DeserializeInt(bytes, ref offset);
            if (length > 0)
            {
                bool[] array = new bool[length];
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = DeserializeBool(bytes, ref offset);
                }

                return array;
            }

            return new bool[0];
        }

        public static Vector3[] DeserializeVector3Array(byte[] bytes, ref int offset)
        {
            int length = DeserializeInt(bytes, ref offset);
            if (length > 0)
            {
                Vector3[] array = new Vector3[length];
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = DeserializeVector3(bytes, ref offset);
                }

                return array;
            }

            return new Vector3[0];
        }

        public static Vector2[] DeserializeVector2Array(byte[] bytes, ref int offset)
        {
            int length = DeserializeInt(bytes, ref offset);

            if (length > 0)
            {
                Vector2[] array = new Vector2[length];
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = DeserializeVector2(bytes, ref offset);
                }

                return array;
            }

            return new Vector2[0];
        }
        public static DVector2[] DeserializeDVector2Array(byte[] bytes, ref int offset)
        {
            int length = DeserializeInt(bytes, ref offset);
            if (length > 0)
            {
                DVector2[] array = new DVector2[length];

                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = DeserializeDVector2D(bytes, ref offset);
                }

                return array;
            }

            return new DVector2[0];
        }
        public static Quaternion[] DeserializeQuaternionArray(byte[] bytes, ref int offset)
        {
            int length = DeserializeInt(bytes, ref offset);
            if (length > 0)
            {
                Quaternion[] array = new Quaternion[length];
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = DeserializeQuaternion(bytes, ref offset);
                }

                return array;
            }

            return new Quaternion[0];
        }

        public static string[] DeserializeStringArray(byte[] bytes, ref int offset)
        {
            int length = DeserializeInt(bytes, ref offset);
            if (length > 0)
            {
                string[] array = new string[length];
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = DeserializeString(bytes, ref offset);
                }

                return array;
            }

            return new string[0];
        }


        public static TerrainStreamingPolygonGeoData DeserializePolygonGeoData(byte[] bytes, ref int offset)
        {
            TerrainStreamingPolygonGeoData o = new TerrainStreamingPolygonGeoData();
            o.ID = TerrainStreamingVectorSerializer.DeserializeString(bytes, ref offset);
            o.Name = TerrainStreamingVectorSerializer.DeserializeString(bytes, ref offset);
            o.Tag_Key = TerrainStreamingVectorSerializer.DeserializeString(bytes, ref offset);
            o.Tag_Value = TerrainStreamingVectorSerializer.DeserializeString(bytes, ref offset);
            o.GeoPoints = TerrainStreamingVectorSerializer.DeserializeDVector2Array(bytes, ref offset).ToList();
            o.Height = TerrainStreamingVectorSerializer.DeserializeFloat(bytes, ref offset);
            o.MinHeight = TerrainStreamingVectorSerializer.DeserializeFloat(bytes, ref offset);
            o.Levels = TerrainStreamingVectorSerializer.DeserializeInt(bytes, ref offset);
            o.MinLevel = TerrainStreamingVectorSerializer.DeserializeInt(bytes, ref offset);
            return o;
        }
        public static TerrainStreamingLinesGeoData DeserializeLineGeoData(byte[] bytes, ref int offset)
        {
            TerrainStreamingLinesGeoData o = new TerrainStreamingLinesGeoData();
            o.ID = TerrainStreamingVectorSerializer.DeserializeString(bytes, ref offset);
            o.Name = TerrainStreamingVectorSerializer.DeserializeString(bytes, ref offset);
            o.Tag_Key = TerrainStreamingVectorSerializer.DeserializeString(bytes, ref offset);
            o.Tag_Value = TerrainStreamingVectorSerializer.DeserializeString(bytes, ref offset);
            o.GeoPoints = TerrainStreamingVectorSerializer.DeserializeDVector2Array(bytes, ref offset).ToList();
            return o;
        }
        public static TerrainStreamingPolygonGeoData[] DeserializePolygonGeoDataArray(byte[] bytes, ref int offset)
        {
            int length = DeserializeInt(bytes, ref offset);
            if (length > 0)
            {
                TerrainStreamingPolygonGeoData[] array = new TerrainStreamingPolygonGeoData[length];

                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = DeserializePolygonGeoData(bytes, ref offset);
                }

                return array;
            }

            return new TerrainStreamingPolygonGeoData[0];
        }
        public static TerrainStreamingLinesGeoData[] TerrainStreamingLinesGeoData(byte[] bytes, ref int offset)
        {
            int length = DeserializeInt(bytes, ref offset);
            if (length > 0)
            {
                TerrainStreamingLinesGeoData[] array = new TerrainStreamingLinesGeoData[length];

                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = DeserializeLineGeoData(bytes, ref offset);
                }

                return array;
            }

            return new TerrainStreamingLinesGeoData[0];
        }
        #endregion
    }
}