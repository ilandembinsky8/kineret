/*     Unity GIS Tech 2020-2021      */

using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using System.IO;
using System.Text;

namespace GISTech.TerrainStreaming
{
    public static class TerrainStreamingGeoConversion
    {
        /// <summary>
        /// Convert Unity World space (X,Y,Z) coordinates to (Lat, Lon) coordinates
        /// </summary>
        /// <returns>
        /// Returns DVector2 containing Latitude and Longitude
        /// </returns>
        /// <param name='position'>
        /// (X,Y,Z) Position Parameter
        /// </param>
        public static DVector2 UWSToLatLog(Vector3 position, float scale)
        {
            FindMetersPerLat(_LatOrigin);
            DVector2 geoLocation = new DVector2(0, 0);
            geoLocation.y = (_LatOrigin + (position.z * scale) / metersPerLat); 
            geoLocation.x = (_LonOrigin + (position.x * scale) / metersPerLon); 
            return geoLocation;
        }
        /// <summary>
                 /// Convert (Lat, Lon) coordinates to Unity World space (X,Y,Z) coordinates
                 /// by Using TerrainContainerObject Real World Elevation In (m)
                 /// </summary>
                 /// <param name="latlon"></param>
                 /// <param name="container"></param>
                 /// <param name="RealWorldElevation in (m) "></param>
                 /// <returns></returns>
        public static Vector3 LatLonToUWS(DVector2 latlon, TerrainStreamingContainer container, float RealWorldElevation)
        {
            if (container)
            {
                var TLPMercator_X = container.TLPointMercator.x;
                var TLPMercator_Y = container.TLPointMercator.y;

                var DRPMercator_X = container.DRPointMercator.x;
                var DRPMercator_Y = container.DRPointMercator.y;

                var NodeP_Merc = LatLongToMercat(latlon.x, latlon.y);

                double Offest_x = (NodeP_Merc.x - TLPMercator_X) / (DRPMercator_X - TLPMercator_X);

                double Offest_y = 1 - (NodeP_Merc.y - TLPMercator_Y) / (DRPMercator_Y - TLPMercator_Y);

                Vector3 HightWSPos = new Vector3((float)(container.transform.position.x + container.ContainerSize.x * Offest_x), 50000, (float)(container.transform.position.z + container.ContainerSize.z * Offest_y));

                var elevation = GetHeight(HightWSPos) + RealWorldElevation / 10 * container.Scale.y;

                var UnityWorldSpacePosition = new Vector3((float)(container.transform.position.x + container.ContainerSize.x * Offest_x), elevation, (float)(container.transform.position.z + container.ContainerSize.z * Offest_y));

                return UnityWorldSpacePosition;

            }
            else
            {
                Debug.LogError("No Terrain Existing");

                return Vector3.zero;
            }

        }

        /// <summary>
        /// Convert (Lat, Lon) coordinates to Unity World space (X,Y,Z) coordinates
        /// </summary>
        /// <returns>
        /// Returns a Vector3 containing (X, Y, Z)
        /// </returns>
        /// <param name='latlon'>
        /// (Lat, Lon) as Vector2
        /// </param>
        public static Vector3 LatLonToUnityWorldSpace(DVector2 latlon, TerrainStreamingContainer container, bool GetElevation = true)
        {
            if (container)
            {
                float elevation = 0;

                Vector3 UnityWorldSpacePosition = Vector3.zero;

                var TLPMercator_X = container.TLPointMercator.x;
                var TLPMercator_Y = container.TLPointMercator.y;

                var DRPMercator_X = container.DRPointMercator.x;
                var DRPMercator_Y = container.DRPointMercator.y;

                var NodeP_Merc = LatLongToMercat(latlon.x, latlon.y);

                double Offest_x = (NodeP_Merc.x - TLPMercator_X) / (DRPMercator_X - TLPMercator_X);

                double Offest_y = 1 - (NodeP_Merc.y - TLPMercator_Y) / (DRPMercator_Y - TLPMercator_Y);

                if (GetElevation)
                {
                    Vector3 HightWSPos = new Vector3((float)(container.transform.position.x + container.ContainerSize.x * Offest_x), 50000, (float)(container.ContainerSize.z * Offest_y));
                    elevation = GetHeight(HightWSPos) + 0.7f;
                    UnityWorldSpacePosition = new Vector3((float)(container.transform.position.x + container.ContainerSize.x * Offest_x), elevation, (float)(container.transform.position.z + container.ContainerSize.z * Offest_y));

                }
                else
                {
                    UnityWorldSpacePosition = new Vector3((float)(container.transform.position.x + container.ContainerSize.x * Offest_x), 0, (float)(container.transform.position.z + container.ContainerSize.z * Offest_y));
                }

                return UnityWorldSpacePosition;
            }
            else
            {
                Debug.LogError("No Terrain Existing");

                return Vector3.zero;
            }

        }
        /// <summary>
        /// Convert Unity World space (X,Y,Z) coordinates to (Lat, Lon) coordinates
        /// </summary>
        /// <returns>
        /// Returns DVector2 containing Latitude and Longitude
        /// </returns>
        /// <param name='position'>
        /// (X,Y,Z) Position Parameter
        /// </param>
        public static DVector2 UnityWorldSpaceToLatLog(Vector3 position, TerrainStreamingContainer container)
        {
            var m_Origin = new DVector2(container.UpperLeftCoordinate.x, container.BottomRightCoordiante.y);
            SetLocalOrigin(m_Origin);
            FindMetersPerLat(container.BottomRightCoordiante.y);
            DVector2 geoLocation = new DVector2(0, 0);
            geoLocation.y = (_LatOrigin + (position.z / container.Scale.z) / metersPerLat);
            geoLocation.x = (_LonOrigin + (position.x / container.Scale.x) / metersPerLon);
            return geoLocation;
        }
        //public static DVector2 UnityWorldSpaceToLatLog(Vector3 position, TerrainStreamingContainer container)
        //{
        //    var m_Origin = new DVector2(container.UpperLeftCoordinate.x, container.BottomRightCoordiante.y);
        //    SetLocalOrigin(m_Origin);
        //    var Scale = container.Scale.y;
        //    FindMetersPerLat(container.BottomRightCoordiante.y);
        //    DVector2 geoLocation = new DVector2(0, 0);
        //    geoLocation.y = (_LatOrigin + (position.z * container.Scale.x) / metersPerLat);
        //    geoLocation.x = (_LonOrigin + (position.x * container.Scale.x) / metersPerLon);
        //    return geoLocation;
        //}
        public static float GetRealWorldHeight(TerrainStreamingContainer container, Vector3 SpacePosition)
        {
            var PostionOnTerrain = GetHeight(SpacePosition);

            var Diff = SpacePosition.y - PostionOnTerrain;

            var elevation = (Diff / container.Scale.y) * 10;

            return elevation;


        }
        public static float GetHeight(Vector3 WSposition)
        {
            float height = 0;

            terrain = GetTerrain(WSposition);

            if (terrain != null)
            {
                TerrainData t = terrain.terrainData;
                height = terrain.SampleHeight(WSposition)
                + terrain.GetPosition().y;
            }


            return height;
        }
        public static Terrain GetTerrain(Vector3 WSposition)
        {
            var downDirection = Vector3.down;

            RaycastHit hitInfo;

            var ray = new Ray(WSposition, (downDirection));

            if (Physics.Raycast(ray, out hitInfo, 100000))
            {
                if (terrain == null)
                {
                    var t = hitInfo.collider.transform.gameObject.GetComponent<Terrain>();

                    if (t)
                        terrain = t;
                }

                if (terrain != null)
                {
                    if (!string.Equals(hitInfo.collider.transform.name, terrain.name))
                    {
                        if (hitInfo.collider.transform.gameObject.GetComponent<Terrain>())
                            terrain = hitInfo.collider.transform.gameObject.GetComponent<Terrain>();
                    }
                }

            }

            return terrain;
        }
        /// <summary>
        /// Convert (Lat, Lon) coordinates to Unity World space (X,Y,Z) coordinates
        /// </summary>
        /// <returns>
        /// Returns a Vector3 containing (X, Y, Z)
        /// </returns>
        /// <param name='latlon'>
        /// (Lat, Lon) as Vector2
        /// </param>
        public static Vector3 LatLogToUWS(DVector2 latlon, int scale)
        {
            FindMetersPerLat(_LatOrigin);
            double zPosition = metersPerLat * (latlon.y - _LatOrigin);
            double xPosition = metersPerLon * (latlon.x - _LonOrigin);
            return new Vector3((float)zPosition / scale, 0, (float)xPosition / scale);
        }
        /// <summary>
        /// Convert (Lat, Lon) coordinates to Unity World space (X,Y,Z) coordinates
        /// </summary>
        /// <returns>
        /// Returns a Vector3 containing (X, Y, Z)
        /// </returns>
        /// <param name='latlon'>
        /// (Lat, Lon) as Vector2
        /// </param>
        /// 
        /// <summary>
        /// Convert Lat/Lon to Different Projection
        /// </summary>
        /// <param name="projReader"></param>
        /// <param name="point"></param>
        /// <param name="Szone"></param>
        /// <returns></returns>
        public static string ConvertLatLonTO(DVector2 LatLon, Projections proj)
        {
            string pos = " ";

            switch (proj)
            {
                case Projections.Geographic_LatLon_Decimale:
                    pos = LatLon.x + " , " + LatLon.y;
                    break;
                case Projections.Geographic_LatLon_DegMinSec:
                    pos = TerrainStreamingGeographic.DecimalToDegMinSec(LatLon);
                    break;
                case Projections.UTM:
                    TerrainStreamingUTM utm = new TerrainStreamingUTM();
                    pos = utm.LatLonToUTM(LatLon);
                    break;

                case Projections.UTM_MGRUTM:
                    //UTM Zone 19 + Latitude Band T + MGRS column + DMGRS row J + MGRS Easting 38588 + MGRS Northing 97366
                    TerrainStreamingUTM MGRUTM = new TerrainStreamingUTM();
                    pos = MGRUTM.LatLonToMGRUTM(LatLon);
                    break;

                case Projections.Lambert:
                    DVector3 Lambert = TerrainStreamingLambert.LatLonToLambert(LatLon, LambertZone.Lambert93);
                    var p = new DVector2(Lambert.x, Lambert.y);
                    pos = p.x + " , " + p.y;
                    break;

            }

            return pos;


        }
        public static Vector3 LatLogToUWS(DVector2 latlon, int scale, DVector2 origin)
        {
            SetLocalOrigin(origin);
               FindMetersPerLat(_LatOrigin);
            double zPosition = metersPerLat * (latlon.y - _LatOrigin);
            double xPosition = metersPerLon * (latlon.x - _LonOrigin);
            return new Vector3((float)zPosition / scale, 0, (float)xPosition / scale);
        }
        //private static double EarthRadius = 6378137;
        private static double MinLatitude = -85.05112878;
        private static double MaxLatitude = 85.05112878;
        private static double MinLongitude = -180;
        private static double MaxLongitude = 180;
        private static double Clip(double n, double minValue, double maxValue)
        {
            return Math.Min(Math.Max(n, minValue), maxValue);
        }
 
        // Determines the map width and height (in pixels) at a specified level of detail.
        public static uint MapSize(int levelOfDetail)
        {
            return (uint)256 << levelOfDetail;
        }
        public static void LatLongToPixelXY(double latitude, double longitude, int levelOfDetail, out int pixelX, out int pixelY)
        {
            latitude = Clip(latitude, MinLatitude, MaxLatitude);
            longitude = Clip(longitude, MinLongitude, MaxLongitude);

            double x = (longitude + 180) / 360;
            double sinLatitude = Math.Sin(latitude * Math.PI / 180);
            double y = 0.5 - Math.Log((1 + sinLatitude) / (1 - sinLatitude)) / (4 * Math.PI);

            uint mapSize = MapSize(levelOfDetail);
            pixelX = (int)Clip(x * mapSize + 0.5, 0, mapSize - 1);
            pixelY = (int)Clip(y * mapSize + 0.5, 0, mapSize - 1);
        }

        /// <summary>
        /// Change the relative origin offset (Lat, Lon), the Default is (0,0), 
        /// used to bring a local area to (0,0,0) in UCS coordinate system
        /// </summary>
        /// <param name='localOrigin'>
        /// Referance point.
        /// </param>
        public static void SetLocalOrigin(DVector2 origine)
        {
            Origine.x = origine.x;

            Origine.y = origine.y;
        }
 
 
        private static DVector2 Origine = new DVector2(0, 0);

        private static double _LatOrigin { get { return Origine.y; } }
        private static double _LonOrigin { get { return Origine.x; } }

        private static float metersPerLat;
        private static float metersPerLon;
 
        private static void FindMetersPerLat(double lat)
        {
            // Compute lengths of degrees
            // Set up "Constants"
            float m1 = 111132.92f;     
            float m2 = -559.82f;        
            float m3 = 1.175f;       
            float m4 = -0.0023f;         

            float p1 = 111412.84f;     
            float p2 = -93.5f;      
            float p3 = 0.118f;       

            lat = lat * Mathf.Deg2Rad;

            // Calculate the length of a degree of latitude and longitude in meters
            metersPerLat = m1 + (m2 * Mathf.Cos(2 * (float)lat)) + (m3 * Mathf.Cos(4 * (float)lat)) + (m4 * Mathf.Cos(6 * (float)lat));

            metersPerLon = (p1 * Mathf.Cos((float)lat)) + (p2 * Mathf.Cos(3 * (float)lat)) + (p3 * Mathf.Cos(5 * (float)lat));
        }
 
        /// <summary>
        /// Calculate the distance between two Lat/Log Points.
        /// </summary>
        /// <param name="lon1"></param>
        /// <param name="lat1"></param>
        /// <param name="lon2"></param>
        /// <param name="lat2"></param>
        /// <returns></returns>
        public static double Getdistance(double lat1, double lon1, double lat2, double lon2, char unit = 'K')
        {
            if ((lat1 == lat2) && (lon1 == lon2))
            {
                return 0;
            }
            else
            {
                var radlat1 = Math.PI * lat1 / 180;
                var radlat2 = Math.PI * lat2 / 180;
                var theta = lon1 - lon2;
                var radtheta = Math.PI * theta / 180;
                var dist = Math.Sin(radlat1) * Math.Sin(radlat2) + Math.Cos(radlat1) * Math.Cos(radlat2) * Math.Cos(radtheta);
                if (dist > 1)
                {
                    dist = 1;
                }
                dist = Math.Acos(dist);
                dist = dist * 180 / Math.PI;
                dist = dist * 60 * 1.1515;
                if (unit == 'K') { dist = dist * 1.609344; }
                if (unit == 'N') { dist = dist * 0.8684; }
                return dist;
            }
        }
        public static double Getdistance(DVector2 P1, DVector2 P2, char ax, char unit = 'K')
        {
            double distance = 0;

            if (ax == 'X')
            {
                if (P1.x < 0 && P2.x > 0)
                {
                    var p0 = new DVector2(0, 0);
                    var p4 = new DVector2(P2.x, 0);

                    var d1 = CalDistance(new DVector2(P1.x, 0), new DVector2(0, 0));
                    var d2 = CalDistance(new DVector2(0, 0), p4);

                    distance = d1 + d2;

                }
                else
                    distance = CalDistance(P1, P2);
            }

            if (ax == 'Y')
            {
                if (P1.y < 0 && P2.y > 0)
                {


                    var p0 = new DVector2(0, 0);
                    var p4 = new DVector2(0, P2.y);

                    var d1 = CalDistance(new DVector2(0, P1.y), new DVector2(0, 0));
                    var d2 = CalDistance(new DVector2(0, 0), p4);

                    distance = d1 + d2;

                }
                else
                    distance = CalDistance(P1, P2);
            }
            return distance;

        }
        public static double CalDistance(DVector2 P1, DVector2 P2, char unit = 'K')
        {
            if ((P1.y == P2.y) && (P1.x == P2.x))
            {
                return 0;
            }
            else
            {
                var radlat1 = Math.PI * P1.y / 180;
                var radlat2 = Math.PI * P2.y / 180;
                var theta = P2.x - P1.x;
                var radtheta = Math.PI * theta / 180;
                var dist = Math.Sin(radlat1) * Math.Sin(radlat2) + Math.Cos(radlat1) * Math.Cos(radlat2) * Math.Cos(radtheta);

                if (dist > 1)
                {
                    dist = 1;
                }
                //if (dist > 0) dist = dist * -1;
                dist = Math.Acos(dist);
                dist = dist * 180 / Math.PI;
                dist = dist * 60 * 1.1515;
                if (unit == 'K') { dist = dist * 1.609344; }
                if (unit == 'N') { dist = dist * 0.8684; }

                return dist;
            }
        }
        public static double GetDistance(DVector2 P1, DVector2 P2, char ax, char unit = 'K')
        {
            double distance = 0;

            if (ax == 'X')
            {
                if (P1.x < 0 && P2.x > 0)
                {
                    var p0 = new DVector2(0, 0);
                    var p4 = new DVector2(P2.x, 0);

                    var d1 = CalDistance(new DVector2(P1.x, 0), new DVector2(0, 0));
                    var d2 = CalDistance(new DVector2(0, 0), p4);

                    distance = d1 + d2;

                }
                else
                    distance = CalDistance(P1, P2);
            }

            if (ax == 'Y')
            {
                if (P1.y < 0 && P2.y > 0)
                {


                    var p0 = new DVector2(0, 0);
                    var p4 = new DVector2(0, P2.y);

                    var d1 = CalDistance(new DVector2(0, P1.y), new DVector2(0, 0));
                    var d2 = CalDistance(new DVector2(0, 0), p4);

                    distance = d1 + d2;

                }
                else
                    distance = CalDistance(P1, P2);
            }
            return distance;

        }

        public const double DEG2RAD = Math.PI / 180;
        public static DVector2 LatLongToMercat(double x, double y)
        {
            double sy = Math.Sin(y * DEG2RAD);
            var mx = (x + 180) / 360;
            var my = 0.5 - Math.Log((1 + sy) / (1 - sy)) / (Math.PI * 4);

            return new DVector2(mx, my);
        }


        /// <summary>
        /// Convert Lat/Lon to EPSG:3857 -- WGS84 Web Mercator (Auxiliary Sphere)
        /// </summary>
        /// <param name="lat"></param>
        /// <param name="lng"></param>
        /// <returns></returns>
        private static double[] LatLonTo_WGS84WebMercator(double lat, double lng)
        {
            double x = lng * 20037508.34 / 180; double y = Math.Log(Math.Tan((90 + lat) * Math.PI / 360)) / (Math.PI / 180); y = y * 20037508.34 / 180; return new double[] { x, y };
        }


        /// <summary>
        /// Size of tile.
        /// </summary>
        public const short TILE_SIZE = 256;
        public static void MercatToLatLong(double mx, double my, out double x, out double y)
        {
            uint mapSize = (uint)TILE_SIZE << 20;
            double px = Clamp(mx * mapSize + 0.5, 0, mapSize - 1);
            double py = Clamp(my * mapSize + 0.5, 0, mapSize - 1);
            mx = px / TILE_SIZE;
            my = py / TILE_SIZE;
            TileToLatLong(mx, my, 20, out x, out y);
        }
        public static void TileToLatLong(double tx, double ty, int zoom, out double lx, out double ly)
        {
            double mapSize = TILE_SIZE << zoom;
            lx = 360 * (Repeat(tx * TILE_SIZE, 0, mapSize - 1) / mapSize - 0.5);
            ly = 90 - 360 * Math.Atan(Math.Exp(-(0.5 - Clamp(ty * TILE_SIZE, 0, mapSize - 1) / mapSize) * 2 * Math.PI)) / Math.PI;
        }
        public static void LatLongToMercat(ref double x, ref double y)
        {
            double sy = Math.Sin(y * DEG2RAD);
            x = (x + 180) / 360;
            y = 0.5 - Math.Log((1 + sy) / (1 - sy)) / (Math.PI * 4);
        }
        public static DVector2 GetLatLongToTile(double dx, double dy, int zoom)
        {
            LatLongToMercat(ref dx, ref dy);
            uint mapSize = (uint)TILE_SIZE << zoom;
            double px = Clamp(dx * mapSize + 0.5, 0, mapSize - 1);
            double py = Clamp(dy * mapSize + 0.5, 0, mapSize - 1);
            double tx = px / TILE_SIZE;
            double ty = py / TILE_SIZE;
            return new DVector2(tx, ty);
        }
        public static void LatLongToTile(double dx, double dy, int zoom, out double tx, out double ty)
        {
            LatLongToMercat(ref dx, ref dy);
            uint mapSize = (uint)TILE_SIZE << zoom;
            double px = Clamp(dx * mapSize + 0.5, 0, mapSize - 1);
            double py = Clamp(dy * mapSize + 0.5, 0, mapSize - 1);
            tx = px / TILE_SIZE;
            ty = py / TILE_SIZE;
        }
        public static void LatLongToMercat(double x, double y, out double mx, out double my)
        {
            double sy = Math.Sin(y * DEG2RAD);
            mx = (x + 180) / 360;
            my = 0.5 - Math.Log((1 + sy) / (1 - sy)) / (Math.PI * 4);
        }
        public static string TileToQuadKey(int x, int y, int zoom)
        {
            StringBuilder quadKey = new StringBuilder();
            for (int i = zoom; i > 0; i--)
            {
                char digit = '0';
                int mask = 1 << (i - 1);
                if ((x & mask) != 0) digit++;
                if ((y & mask) != 0)
                {
                    digit++;
                    digit++;
                }
                quadKey.Append(digit);
            }
            return quadKey.ToString();
        }
        public static double Repeat(double n, double minValue, double maxValue)
        {
            if (double.IsInfinity(n) || double.IsInfinity(minValue) || double.IsInfinity(maxValue) || double.IsNaN(n) || double.IsNaN(minValue) || double.IsNaN(maxValue)) return n;

            double range = maxValue - minValue;
            while (n < minValue || n > maxValue)
            {
                if (n < minValue) n += range;
                else if (n > maxValue) n -= range;
            }
            return n;
        }
        public static double Clamp(double n, double minValue, double maxValue)
        {
            if (n < minValue) return minValue;
            if (n > maxValue) return maxValue;
            return n;
        }

        public const short tileSize = 256;
        public static Vector2 LatLongToMercat(float x, float y)
        {
            float sy = Mathf.Sin(y * Mathf.Deg2Rad);
            return new Vector2((x + 180) / 360, 0.5f - Mathf.Log((1 + sy) / (1 - sy)) / (4 * Mathf.PI));
        }

        public static Vector2Int LatLongToTile (double x, double y, int zoom)
        {
            
            DVector2 mPos = LatLongToMercat2(x, y);
     
            uint mapSize = (uint)tileSize << zoom;
            int px = (int)Clip(mPos.x * mapSize + 0.5, 0, mapSize - 1);
            int py = (int)Clip(mPos.y * mapSize + 0.5, 0, mapSize - 1);
            int ix = px / tileSize;
            int iy = py / tileSize;
            return new Vector2Int(ix, iy);
        }
 

        public static DVector2 LatLongToMercat2(double x, double y)
        {
            double sy = Mathf.Sin((float)(y * Mathf.Deg2Rad));
            return new DVector2((x + 180) / 360, 0.5f - Mathf.Log((float)((1 + sy) / (1 - sy))) / (4 * Mathf.PI));
        }
        public static Vector2Int LatLongToTile(DVector2 p, int zoom)
        {
            return LatLongToTile(p.x, p.y, zoom);
        }
        public static DVector2 TileToLatLong(int x, int y, int zoom)
        {
            double mapSize = tileSize << zoom;
            double lx = 360 * ((Clip(x * tileSize, 0, mapSize - 1) / mapSize) - 0.5);
            double ly = 90 - 360 * Math.Atan(Math.Exp(-(0.5 - (Clip(y * tileSize, 0, mapSize - 1) / mapSize)) * 2 * Math.PI)) / Math.PI;

            return new DVector2(lx,ly);
        }
        public static DVector2 TileToLatLong2(double tx, double ty, int zoom)
        {
            double mapSize = TILE_SIZE << zoom;
            var lx = 360 * (Repeat(tx * TILE_SIZE, 0, mapSize - 1) / mapSize - 0.5);

            var ly = 90 - 360 * Math.Atan(Math.Exp(-(0.5 - Clamp(ty * TILE_SIZE, 0, mapSize - 1) / mapSize) * 2 * Math.PI)) / Math.PI;

            return new DVector2(lx, ly);
        }
 

        //Get Terrain Height

        private static Terrain m_terrain;

        public static Terrain terrain
        {
            get { return m_terrain; }
            set
            {
                if (m_terrain != value)
                {
                    m_terrain = value;

                }
            }
        }

        private static Ray ray;
        public static float GetHeight(TerrainStreamingContainer container, Vector3 WSposition)
        {
            float height = 0;

            terrain = GetTerrain(container, WSposition);

            if (terrain!=null)
            {
                TerrainData t = terrain.terrainData;
                height = terrain.SampleHeight(WSposition);
            }     
            return height;
        }

        private static Terrain GetTerrain(TerrainStreamingContainer container, Vector3 WSposition)
        {
            var downDirection = Vector3.down;

            RaycastHit hitInfo;

            var ray = new Ray(WSposition, (downDirection));

            if (Physics.Raycast(ray, out hitInfo, 100000))
            {
                if (terrain == null)
                {

                    if(hitInfo.collider.transform.gameObject.GetComponent<Terrain>())
                        terrain = hitInfo.collider.transform.gameObject.GetComponent<Terrain>();
                }

                if (terrain != null)
                {
                    if (!string.Equals(hitInfo.collider.transform.name, terrain.name))
                    {
                        if (hitInfo.collider.transform.gameObject.GetComponent<Terrain>())
                            terrain = hitInfo.collider.transform.gameObject.GetComponent<Terrain>();
                    }
                }

            }

            return terrain;
        }



    }




}

