using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GISTech.TerrainStreaming
{
    public abstract class TerrainStreamingGeoDataHolder
    {
        public abstract void GetGeoVectorPointsData(TerrainStreamingGeoVectorData GeoDataContainer);
        public abstract void GetGeoVectorRoadsData(TerrainStreamingTileData Tile, ref TerrainStreamingGeoVectorData GeoDataContainer);
        public abstract void GetGeoVectorTreesData(TerrainStreamingTileData Tile, ref TerrainStreamingGeoVectorData GeoDataContainer);
        public abstract void GetGeoVectorGrassData(TerrainStreamingTileData Tile, ref TerrainStreamingGeoVectorData GeoDataContainer);
        public abstract void GetGeoVectorBuildingData(TerrainStreamingGeoVectorData GeoDataContainer);

    }

    public class TerrainStreamingGeoVectorData
    {
        public List<TerrainStreamingPointGeoData> GeoPoints;
        public List<TerrainStreamingLinesGeoData> GeoRoads;
        public List<TerrainStreamingPolygonGeoData> GeoTrees;
        public List<TerrainStreamingPolygonGeoData> GeoGrass;
        public List<TerrainStreamingPolygonGeoData> GeoBuilding;

        public TerrainStreamingGeoVectorData()
        {
            GeoPoints = new List<TerrainStreamingPointGeoData>();
            GeoRoads = new List<TerrainStreamingLinesGeoData>();
            GeoTrees = new List<TerrainStreamingPolygonGeoData>();
            GeoGrass = new List<TerrainStreamingPolygonGeoData>();
            GeoBuilding = new List<TerrainStreamingPolygonGeoData>();

        }

    }

    #region Point
    public class TerrainStreamingPointGeoData
    {
        public string ID;
        public string Name;
        public string Tag;
        public DVector2 GeoPoint;

        public TerrainStreamingPointGeoData()
        {
            ID = "";
            Name = "";
            Tag = "";
            GeoPoint = new DVector2 (0,0);
        }
 
    }
    #endregion
    #region Line
    public class TerrainStreamingLinesGeoData
    {
        public string ID = "";
        public string Name;
        public string Tag_Key;
        public string Tag_Value;
        public List<DVector2> GeoPoints;

        public TerrainStreamingLinesGeoData()
        {
            ID = "";
            Name = "";
            Tag_Key = "";
            Tag_Value = "";
            GeoPoints = new List<DVector2>();
        }

    }
    #endregion
    #region Polygon
    [Serializable]
    public class TerrainStreamingPolygonGeoData
    {
        public string ID="";
        public string Name;
        public string Tag_Key;
        public string Tag_Value;
        public List<DVector2> GeoPoints;

        //Building Data
        public float Height;
        public float MinHeight;
        public int Levels;
        public int MinLevel;

        public TerrainStreamingPolygonGeoData()
        {
            ID = "";
            Name = "";
            Tag_Key = "";
            Tag_Value = "";
            GeoPoints = new List<DVector2>();

            Levels = 0;
            MinLevel = 0;
            Height = 0;
            MinHeight = 0;

        }

    }
    #endregion

}