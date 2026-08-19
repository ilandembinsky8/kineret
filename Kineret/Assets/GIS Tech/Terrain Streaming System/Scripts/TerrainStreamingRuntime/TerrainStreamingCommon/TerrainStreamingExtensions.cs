/*     Unity GIS Tech 2020-2021      */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;

namespace GISTech.TerrainStreaming
{
    public class TerrainStreamingExtensions
    {
        public static string EditorCheckForOSMFile(string TerrainFilePath, string TerrainFileName, out bool exist)
        {
            exist = false;
            string osmfile = "";

            DirectoryInfo di = new DirectoryInfo(TerrainFilePath);

            var VectorFolderPath = TerrainFileName + "_VectorData";

            for (int i = 0; i <= 5; i++)
            {
                di = di.Parent;

                VectorFolderPath = di.Name + "/" + VectorFolderPath;

                //If Directory GIS Terrains Exist
                if (di.Name == "GIS Terrains")
                {
                    var MainfolderPath = Path.GetDirectoryName(TerrainFilePath);
                    var VectorDataFolder = Path.Combine(MainfolderPath, TerrainFileName + "_VectorData");

                    osmfile = VectorDataFolder + "/" + TerrainFileName + ".osm";

                    if (File.Exists(osmfile))
                    {
                        exist = true;
                    }
                    else
                        Debug.LogError("Osm File Not Found : Please put your terrain in GIS Terrain Loader/Recources/GIS Terrains/TerrainFileName_VectorData/TerrainFileName.osm  " + osmfile);

                    break;
                }


                if (i == 5)
                {
                    exist = false;
                    Debug.LogError("Vector folder not found! : Please put your terrain in GIS Terrain Loader/Recources/GIS Terrains/");
                }

            }
            return osmfile;
        }
        public static string RuntimeCheckForOSMFile(string TerrainFilePath, string TerrainFileName, out bool exist)
        {
            exist = false;
            string osmfile = "";

            DirectoryInfo di = new DirectoryInfo(TerrainFilePath);



            var MainfolderPath = Path.GetDirectoryName(TerrainFilePath);

            var VectorFolderPath = MainfolderPath + "/" + TerrainFileName + "_VectorData";


            if (Directory.Exists(VectorFolderPath))
            {
                osmfile = VectorFolderPath + "/" + TerrainFileName + ".osm";

                if (File.Exists(osmfile))
                {
                    exist = true;
                }
                else
                    Debug.LogError("Osm File Not Found : Please put your terrain in Path../TerrainFolder/TerrainFileName_VectorData/TerrainFileName.osm  " + osmfile);

            }
            else
            {
                exist = false;
                Debug.LogError("Vector folder not found!");
            }

            return osmfile;
        }
        public static double ConvertToDouble(string s)
        {
            char systemSeparator = Thread.CurrentThread.CurrentCulture.NumberFormat.CurrencyDecimalSeparator[0];
            double result = 0;
            try
            {
                if (s != null)
                    if (!s.Contains(","))
                        result = double.Parse(s, CultureInfo.InvariantCulture);
                    else
                        result = Convert.ToDouble(s.Replace(".", systemSeparator.ToString()).Replace(",", systemSeparator.ToString()));
            }
            catch (Exception e)
            {
                try
                {
                    result = Convert.ToDouble(s);
                }
                catch
                {
                    try
                    {
                        result = Convert.ToDouble(s.Replace(",", ";").Replace(".", ",").Replace(";", "."));
                    }
                    catch
                    {
                        throw new Exception("Wrong string-to-double format  :" + e.Message);
                    }
                }
            }
            return result;
        }
        public static bool IsPointInPolygon3D(Vector3[] poly, float x, float y)
        {
            int i, j;
            bool c = false;
            for (i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                if ((poly[i].z <= y && y < poly[j].z ||
                     poly[j].z <= y && y < poly[i].z) &&
                    x < (poly[j].x - poly[i].x) * (y - poly[i].z) / (poly[j].z - poly[i].z) + poly[i].x)
                {
                    c = !c;
                }
            }
            return c;
        }
        public static bool IsPointInPolygon2D(List<DVector2> poly, DVector2 point)
        {
            int i, j;
            bool c = false;
            for (i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
            {
                if ((poly[i].y <= point.y && point.y < poly[j].y ||
                     poly[j].y <= point.y && point.y < poly[i].y) &&
                    point.x < (poly[j].x - poly[i].x) * (point.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                {
                    c = !c;
                }
            }
            return c;
        }
        public static bool IsPointInPolygon4(List<DVector2> polygon, DVector2 testPoint)
        {
            bool result = false;
            int j = polygon.Count() - 1;
            for (int i = 0; i < polygon.Count(); i++)
            {
                if (polygon[i].y < testPoint.y && polygon[j].y >= testPoint.y || polygon[j].y < testPoint.y && polygon[i].y >= testPoint.y)
                {
                    if (polygon[i].x + (testPoint.y - polygon[i].y) / (polygon[j].y - polygon[i].y) * (polygon[j].x - polygon[i].x) < testPoint.x)
                    {
                        result = !result;
                    }
                }
                j = i;
            }
            return result;
        }
        public static Rect GetRectFromPoints(List<Vector3> points)
        {

            return new Rect
            {
                x = points.Min(p => p.x),
                y = points.Min(p => p.z),
                xMax = points.Max(p => p.x),
                yMax = points.Max(p => p.z)
            };
        }
        public static string[] GetTextureTiles(string terrainPath)
        {
            var folderPath = Path.GetDirectoryName(terrainPath);
            var TerrainFilename = Path.GetFileNameWithoutExtension(terrainPath);
            var TextureFolder = Path.Combine(folderPath, TerrainFilename + "_Textures");
            string[] tiles = null;

            if (Directory.Exists(TextureFolder))
            {
                var supportedExtensions = new HashSet<string> { ".png", ".jpg" };
                tiles = Directory.GetFiles(TextureFolder, "*.*", SearchOption.AllDirectories).Where(f => supportedExtensions.Contains(new FileInfo(f).Extension, StringComparer.OrdinalIgnoreCase)).ToArray();
            }
            return tiles;
        }

        public static bool IsGeoFile(string extension)
        {
            bool ValidFile = false;

            if (extension == ".ter" || extension == ".png" || extension == ".raw")
                ValidFile = false;
            else
                ValidFile = true;

            return ValidFile;
        }


        public static bool IsSubRegionIncluded(DVector2 FileUpperLeftCoordiante, DVector2 FileDownRightCoordiante, DVector2 SubRegionUpperLeftCoordiante, DVector2 SubRegionDownRightCoordiante)
        {
            bool Included = true;

            if (SubRegionUpperLeftCoordiante.x >= SubRegionDownRightCoordiante.x)
            {
                Debug.LogError("Down-Right Longitude must be greater than Top-Left Longitude");
                Included = false;
            }
            if (SubRegionUpperLeftCoordiante.y <= SubRegionDownRightCoordiante.y)
            {
                Debug.LogError("Top-Left Latitude must be greater than Bottom-Right Latitude");
                Included = false;
            }
            //-------
            if (SubRegionUpperLeftCoordiante.x < FileUpperLeftCoordiante.x)
            {
                Debug.LogError("Sub region Top-Left Longitude must be greater or equal than file Top-Left Longitude");
                Included = false;
            }

            if (SubRegionUpperLeftCoordiante.y > FileUpperLeftCoordiante.y)
            {
                Debug.LogError("Sub region Top-Left Latitude must be smaller or equal than file Top-Left Latitude");
                Included = false;
            }
            //-------
            if (SubRegionDownRightCoordiante.x > FileDownRightCoordiante.x)
            {
                Debug.LogError("Sub region Top-Left Longitude must be smaller or equal than file Top-Left Longitude");
                Included = false;
            }

            if (SubRegionDownRightCoordiante.y < FileDownRightCoordiante.y)
            {
                Debug.Log(SubRegionDownRightCoordiante.y + "  " + FileDownRightCoordiante.y);
                Debug.LogError("Sub region Down-Right Latitude must be greater or equal than file Top-Left Latitude");
                Included = false;
            }

            return Included;
        }
        public static void GetZoneCoordinates(DVector2 FileUpperLeftCoordiante, DVector2 FileDownRightCoordiante, DVector2 SubRegionUpperLeftCoordiante, DVector2 SubRegionDownRightCoordiante, out DVector2 TakedSubRegionUpperLeftCoordiante,out DVector2 TakedSubRegionDownRightCoordiante)
        {
            TakedSubRegionUpperLeftCoordiante = new DVector2(0, 0);
            TakedSubRegionDownRightCoordiante = new DVector2(0, 0);

            //Case 1 :
            if (FileContainsCoordinates(SubRegionUpperLeftCoordiante, FileUpperLeftCoordiante, FileDownRightCoordiante))
            {
                if (FileContainsCoordinates(SubRegionDownRightCoordiante, FileUpperLeftCoordiante, FileDownRightCoordiante))
                {
                    TakedSubRegionUpperLeftCoordiante = SubRegionUpperLeftCoordiante;
                    TakedSubRegionDownRightCoordiante = SubRegionDownRightCoordiante;
                    return;
                }else
                {
                    if(SubRegionDownRightCoordiante.x>FileDownRightCoordiante.x)
                    {
                        TakedSubRegionUpperLeftCoordiante = SubRegionUpperLeftCoordiante;
                        TakedSubRegionDownRightCoordiante = FileDownRightCoordiante;
                        return;
                    }
                }

            }else
            {
                //Case 4 :
                if (FileContainsCoordinates(SubRegionDownRightCoordiante, FileUpperLeftCoordiante, FileDownRightCoordiante))
                {
                    TakedSubRegionUpperLeftCoordiante = new DVector2(FileUpperLeftCoordiante.x, FileUpperLeftCoordiante.y);
                    TakedSubRegionDownRightCoordiante = new DVector2(SubRegionDownRightCoordiante.x, SubRegionDownRightCoordiante.y);
                    return;

                }
                else
                {    //Case 2 :
                    if (SubRegionUpperLeftCoordiante.x > FileUpperLeftCoordiante.x && SubRegionUpperLeftCoordiante.x < FileDownRightCoordiante.y)
                    {
                        if (!FileContainsCoordinates(SubRegionDownRightCoordiante, FileUpperLeftCoordiante, FileDownRightCoordiante))
                        {
                            if (SubRegionDownRightCoordiante.y < FileUpperLeftCoordiante.y && SubRegionDownRightCoordiante.y > FileDownRightCoordiante.y)
                            {
                                TakedSubRegionUpperLeftCoordiante = new DVector2(SubRegionUpperLeftCoordiante.x, FileUpperLeftCoordiante.y);
                                TakedSubRegionDownRightCoordiante = new DVector2(FileDownRightCoordiante.x, SubRegionDownRightCoordiante.y);
                                return;
                            }
                            else
                            {
                                TakedSubRegionUpperLeftCoordiante = new DVector2(SubRegionUpperLeftCoordiante.x, FileUpperLeftCoordiante.y);
                                TakedSubRegionDownRightCoordiante = new DVector2(FileDownRightCoordiante.x, FileDownRightCoordiante.y);
                                return;
                            }

                        }

                        //Case 3 :
                        if (!FileContainsCoordinates(SubRegionDownRightCoordiante, FileUpperLeftCoordiante, FileDownRightCoordiante))
                        {
                            if (SubRegionUpperLeftCoordiante.x < FileUpperLeftCoordiante.x && (SubRegionUpperLeftCoordiante.y < FileUpperLeftCoordiante.y && SubRegionUpperLeftCoordiante.y > FileDownRightCoordiante.y))
                            {

                                if (SubRegionDownRightCoordiante.x < FileDownRightCoordiante.x && SubRegionDownRightCoordiante.x > FileUpperLeftCoordiante.x)
                                {
                                    TakedSubRegionUpperLeftCoordiante = new DVector2(FileUpperLeftCoordiante.x, SubRegionUpperLeftCoordiante.y);
                                    TakedSubRegionDownRightCoordiante = new DVector2(SubRegionDownRightCoordiante.x, FileDownRightCoordiante.y);
                                }

                            }
                        }


                    }
                }
   
            }

            if (SubRegionUpperLeftCoordiante.x > FileUpperLeftCoordiante.x || SubRegionUpperLeftCoordiante.x < FileDownRightCoordiante.x)
            {
                if(SubRegionUpperLeftCoordiante.y < FileDownRightCoordiante.x || SubRegionUpperLeftCoordiante.x < FileDownRightCoordiante.x)
                TakedSubRegionUpperLeftCoordiante = SubRegionUpperLeftCoordiante;
            }else
            {

            }
            if (SubRegionUpperLeftCoordiante.x > FileUpperLeftCoordiante.x || SubRegionUpperLeftCoordiante.x < FileDownRightCoordiante.x)
            {
                TakedSubRegionUpperLeftCoordiante = SubRegionUpperLeftCoordiante;
            }



            if (SubRegionUpperLeftCoordiante.y < FileUpperLeftCoordiante.x || SubRegionUpperLeftCoordiante.x > FileDownRightCoordiante.x)
                Debug.LogError("Zone not set corrcetly");



            if (SubRegionUpperLeftCoordiante.x >= FileUpperLeftCoordiante.x && SubRegionUpperLeftCoordiante.x<= FileDownRightCoordiante.x)
            {
                Debug.LogError("Down-Right Longitude must be greater than Top-Left Longitude");
            }
            if (SubRegionUpperLeftCoordiante.y <= SubRegionDownRightCoordiante.y)
            {
                Debug.LogError("Top-Left Latitude must be greater than Bottom-Right Latitude");
            }
            //-------
            if (SubRegionUpperLeftCoordiante.x < FileUpperLeftCoordiante.x)
            {
                Debug.LogError("Sub region Top-Left Longitude must be greater or equal than file Top-Left Longitude");
            }

            if (SubRegionUpperLeftCoordiante.y > FileUpperLeftCoordiante.y)
            {
                Debug.LogError("Sub region Top-Left Latitude must be smaller or equal than file Top-Left Latitude");
            }
            //-------
            if (SubRegionDownRightCoordiante.x > FileDownRightCoordiante.x)
            {
                Debug.LogError("Sub region Top-Left Longitude must be smaller or equal than file Top-Left Longitude");
            }

            if (SubRegionDownRightCoordiante.y < FileDownRightCoordiante.y)
            {
                Debug.LogError("Sub region Down-Right Latitude must be greater or equal than file Top-Left Latitude");

            }

          
        }
        private static bool FileContainsCoordinates(DVector2 point,DVector2 FileUpper,DVector2 FileBottom)
        {
            if ((point.x > FileUpper.x && point.x < FileBottom.x) && (point.y > FileBottom.y && point.y < FileUpper.y))
                return true;
            else
                return false;
        }
         public static void SafeDeleteFile(string filename, int tryCount = 10)
        {
            while (tryCount-- > 0)
            {
                try
                {
                    File.Delete(filename);
                    break;
                }
                catch (Exception)
                {
                    Thread.Sleep(10);
                }
            }
        }
        public static List<List<T>> Chunk<T>(List<T> theList, int chunkSize)
        {
            List<List<T>> result = new List<List<T>>();
            try
            {
                result = theList
                .Select((x, i) => new {
                    data = x,
                    indexgroup = i / chunkSize
                })
                .GroupBy(x => x.indexgroup, x => x.data)
                .Select(g => new List<T>(g))
                .ToList();
            }catch(Exception ex)
            {
                Debug.LogError("Raster can not be downloaded, Reduce the number of threads .. " + ex);
            }

            return result;
        }
    }
    public static class TerrainStreamingEnumExtension
    {
        public static string GetDescription(this Enum e)
        {
            var attribute =
                e.GetType()
                    .GetTypeInfo()
                    .GetMember(e.ToString())
                    .FirstOrDefault(member => member.MemberType == MemberTypes.Field)
                    .GetCustomAttributes(typeof(DescriptionAttribute), false)
                    .SingleOrDefault()
                    as DescriptionAttribute;

            return attribute?.Description ?? e.ToString();
        }
    }
    public static class TransformExtensions
    {
        /// <summary>
        /// Updates the local eulerAngles to a new vector3, if a value is omitted then the old value will be used.
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public static void SetLocalEulerAngles(this Transform transform, float? x = null, float? y = null, float? z = null)
        {
            var vector = new Vector3();
            if (x != null) { vector.x = x.Value; } else { vector.x = transform.localEulerAngles.x; }
            if (y != null) { vector.y = y.Value; } else { vector.y = transform.localEulerAngles.y; }
            if (z != null) { vector.z = z.Value; } else { vector.z = transform.localEulerAngles.z; }
            transform.localEulerAngles = vector;
        }

        /// <summary>
        /// Updates the position to a new vector3, if a value is omitted then the old value will be used.
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public static void SetPosition(this Transform transform, float? x = null, float? y = null, float? z = null)
        {
            var vector = new Vector3();
            if (x != null) { vector.x = x.Value; } else { vector.x = transform.position.x; }
            if (y != null) { vector.y = y.Value; } else { vector.y = transform.position.y; }
            if (z != null) { vector.z = z.Value; } else { vector.z = transform.position.z; }
            transform.position = vector;
        }

        public static void DestroyChildren(this Transform t)
        {
            bool isPlaying = Application.isPlaying;

            while (t.childCount != 0)
            {
                Transform child = t.GetChild(0);

                if (isPlaying)
                {
                    child.parent = null;
                    UnityEngine.Object.Destroy(child.gameObject);
                }
                else UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
        }
    }

    [Serializable]
    public class DVector3
    {
        public double x;
        public double y;
        public double z;

        private const double radianTodegree = 180.0 / Math.PI;

        public DVector3(double x, double y, double z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }




        public void translate(double x, double y, double z)
        {

            this.x += x;
            this.y += y;
            this.z += z;
        }
        private void Scale(double scale)
        {
            this.x *= scale;
            this.y *= scale;
            this.z *= scale;
        }

        public void toDegree()
        {
            Scale(radianTodegree);
        }
        public string toString()
        {
            return this.x + " " + this.y + " " + this.z;
 
        }
        public DVector2 ToDVector2()
        {
            return new DVector2(this.x,this.y);
        }

    }


    [Serializable]
    public class DVector2
    {
        public static DVector2 Zero => new DVector2(0, 0);

        public double x;
        public double y;

        private static System.Random _random = new System.Random();

        public DVector2(double x, double y)
        {
            this.x = x;
            this.y = y;
        }

        public void Reset()
        {
            x = 0;
            y = 0;
        }

        public void Normalize()
        {
            double length = Length();

            x /= length;
            y /= length;
        }

        public DVector2 Normalized()
        {
            return Clone() / Length();
        }
        public static DVector2 Normalize(DVector2 v)
        {
            double v_magnitude = Magnitude(v);

            DVector2 v_normalized = new DVector2(v.x / v_magnitude, v.y / v_magnitude);

            return v_normalized;
        }
        public static double Magnitude(DVector2 a)
        {
            double magnitude = Mathf.Sqrt((float)SqrMagnitude(a));

            return magnitude;
        }
        public static double SqrMagnitude(DVector2 a)
        {
            double sqrMagnitude = (a.x * a.x) + (a.y * a.y);

            return sqrMagnitude;
        }
        public static double SqrDistance(DVector2 a, DVector2 b)
        {
            double distance = SqrMagnitude(a - b);

            return distance;
        }
 
        public void Negate()
        {
            x = -x;
            y = -y;
        }

        public DVector2 Clone()
        {
            return new DVector2(x, y);
        }

        public static DVector2 operator +(DVector2 a, DVector2 b)
        {
            return new DVector2(a.x + b.x, a.y + b.y);
        }

        public static DVector2 operator -(DVector2 a, DVector2 b)
        {
            return new DVector2(a.x - b.x, a.y - b.y);
        }
        public static DVector2 operator -(DVector2 a)
        {
            return a * -1f;
        }
        public static DVector2 operator *(DVector2 a, double b)
        {
            return new DVector2(a.x * b, a.y * b);
        }
        public static DVector2 operator *(DVector2 a, float b)
        {
            return new DVector2(a.x * b, a.y * b);
        }
        public static DVector2 operator *(double b, DVector2 a)
        {
            return new DVector2(a.x * b, a.y * b);
        }
        public static DVector2 operator *(float b, DVector2 a)
        {
            return new DVector2(a.x * b, a.y * b);
        }
        public static DVector2 operator /(DVector2 a, DVector2 b)
        {
            return new DVector2(a.x / b.x, a.y / b.y);
        }

        public static DVector2 operator /(DVector2 a, double b)
        {
            return new DVector2(a.x / b, a.y / b);
        }
 
        public void Accumulate(DVector2 other)
        {
            x += other.x;
            y += other.y;
        }

        public DVector2 Divide(float scalar)
        {
            return new DVector2(x / scalar, y / scalar);
        }

        public DVector2 Divide(double scalar)
        {
            return new DVector2(x / scalar, y / scalar);
        }

        public double Dot(DVector2 v)
        {
            return x * v.x + y * v.y;
        }
        public static double Dot(DVector2 a, DVector2 b)
        {
            double dotProduct = (a.x * b.x) + (a.y * b.y);

            return dotProduct;
        }

        public double Cross(DVector2 v)
        {
            return x * v.y - y * v.x;
        }

        public double Length()
        {
            return Math.Sqrt(x * x + y * y);
        }

        public double LengthSquared()
        {
            return x * x + y * y;
        }

        public double Angle()
        {
            return Math.Atan2(y, x);
        }

        public static DVector2 Lerp(DVector2 from, DVector2 to, double t)
        {
            return new DVector2(from.x + t * (to.x - from.x),
                               from.y + t * (to.y - from.y));
        }

        public static DVector2 FromAngle(double angle)
        {
            return new DVector2(Math.Cos(angle), Math.Sin(angle));
        }

        public static double Distance(DVector2 v1, DVector2 v2)
        {
            return (v2 - v1).Length();
        }

        public static DVector2 RandomUnitVector()
        {
            double angle = _random.NextDouble() * Math.PI * 2;

            return new DVector2(Math.Cos(angle), Math.Sin(angle));
        }

        public override string ToString()
        {
            return "{" + Math.Round(x, 5) + "," + Math.Round(y, 5) + "}";
        }
        public float sqrMagnitude
        {
            get
            {
                return (float)(this.x * (double)this.x + this.y * (double)this.y);
            }
        }
        public Vector2Int ToIntVector()
        {
            return new Vector2Int((int)x,(int)y);
        }
    }

    public class IntVector2
    {
        public static IntVector2 Zero = new IntVector2(0, 0);

        public int x;
        public int y;

        private static System.Random _random = new System.Random();

        public IntVector2(int x, int y)
        {
            this.x = x;
            this.y = y;
        }


        public void reset()
        {
            x = 0;
            y = 0;
        }
    }

}