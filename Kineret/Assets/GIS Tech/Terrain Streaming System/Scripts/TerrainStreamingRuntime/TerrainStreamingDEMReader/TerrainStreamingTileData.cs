/*     Unity GIS Tech 2020-2021      */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GISTech.TerrainStreaming
{
    public class TerrainStreamingTileData
    {
        public string Name;
        public Bounds TileBounds;
        public Vector2Int Number;
        public Vector3 Position;
 
        public DVector2 UpperLeftCoordinate = new DVector2(0, 0);
        public DVector2 BottomRightCoordiante = new DVector2(0, 0);

        public DVector2 UpperLeftPointMercator = new DVector2(0, 0);
        public DVector2 BottomRightPointMercator = new DVector2(0, 0);

        public double cellsize = 0;

        public string LocalFilePath;

        public TerrainStreamingTileData(string m_Name)
        {
            Name = m_Name;
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
