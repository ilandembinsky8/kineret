/*     Unity GIS Tech 2020-2023      */

using System.Collections.Generic;
using UnityEngine;

namespace GISTech.GISTerrainLoader
{
    public class GISTerrainLoaderVectorParameters_SO : ScriptableObject
    {
        [Header("Filter Vector Data (Attributes)")]
        public List<string> Attributes_Points = new List<string>();
        public List<string> Attributes_Grass = new List<string>();
        public List<string> Attributes_Trees = new List<string>();
        public List<string> Attributes_Buildings = new List<string>();
        public List<string> Attributes_Roads = new List<string>();
        public List<string> Attributes_Water = new List<string>();
        public List<string> Attributes_LandParcel = new List<string>();

#if GISTerrainLoaderFenceGenerator
        public List<string> Attributes_Fences = new List<string>();
#endif
        [Space(10)]
        [Header("Customized Attributes")]
        public string ID_Tag = "id";
        public string Layer_Tag = "LAYER";
        public string Name_Tag = "name";
        public string Null_Tag = "None";

        [Space(10)]
        [Header("Buildings Tags")]
        public string building_Levels_Tga = "building:levels";
        public string building_MinLevel_Tga = "building:min_level";
        public string building_Height_Tga = "building:height";
        public string building_MinHeight_Tga = "building:min_height";
        public string building_MaxHeight_Tga = "building:max_height";
        [Space(10)]
        [Header("Default Building Values")]
        public float DefaultHeightScale = 5f;
        [Tooltip("Enable This Option to add the ability to generate building as 'Default Building' if Tag not exists in Building Prefabs ")]
        public bool GenerateDefaultBuilding =true;

        [Space(10)]
        [Header("Vector Options")]
        [Tooltip("Enable This Option to add  VectorDataBase Component to gameobjects generated from vector data, this component will include all geo data (Tag, Layer, Coordinates, Database ... etc)")]
        public bool AddVectorDataBaseToGameObjects = false;
        [Tooltip("Enable This Option to Unload All Vector Data with coordinates out of the container bounds")]
        public bool LoadDataOutOfContainerBounds = false;
        [Tooltip("Enable This Option to filter features ID and remove duplicated objects by using thier ID")]
        public bool CheckForDuplicatedID = false;
 
        [Space(5)]
        [Header("GPX")]
        [Tooltip("Enable This Option to add a Random ID for each GPX Vector Object")]
        public bool AddRandom_ID_Vector_GPX = true;
        public string GPX_GeoPoint_Attribute = "GPX_Point";
        public string GPX_GeoPoint_Value = "GPX_GeoPoint_Model";
        public string GPX_GeoLine_Attribute = "GPX_Line";
        public string GPX_GeoLine_Value = "GPX_GeoLine_Model";
        [Space(5)]
        [Header("KML")]
        [Tooltip("Enable This Option to add a Random ID for each KML Vector Object")]
        public bool AddRandom_ID_Vector_KML = true;
        public string KML_GeoPoint_Attribute = "KML_Point";
        public string KML_GeoPoint_Value = "KML_GeoPoint_Model";
        public string KML_GeoLine_Attribute = "KML_Line";
        public string KML_GeoLine_Value = "KML_GeoLine_Model";
        [Space(5)]
        [Header("GEOJSON")]
        [Tooltip("Enable This Option to Attribute and value to the Geojeson database eg : 'GEOJSON_Point', 'GEOJSON_GeoPoint_Model' ")]
        public bool Add_Attribute_Value_To_GEOJSON;
        public string GEOJSON_GeoPoint_Attribute = "GEOJSON_Point";
        public string GEOJSON_GeoPoint_Value = "GEOJSON_GeoPoint_Model";
        public string GEOJSON_GeoLine_Attribute = "GEOJSON_Line";
        public string GEOJSON_GeoLine_Value = "GEOJSON_GeoLine_Model";
        public string GEOJSON_GeoPolygon_Attribute = "GEOJSON_Polygon";
        public string GEOJSON_GeoPolygon_Value = "GEOJSON_GeoPolygon_Model";
        //[Space(5)]
        //[Header("SHP")]
        //[Tooltip("Enable This Option to add a Random ID for each SHP Vector Object")]



#if GISTechS57        
        [Space(5)]
        [Header("Add-On Options")]
        [Tooltip("Enable This Option to Read and parse the full attribute list for each feature, this fix reading and rendering ENC57 features in Unity, as some features have the same description (like \"awash rock\") appear with different symbols in GlobalMapper, whereas in Unity they appear identical,")]
        public bool S57ParseRelevantAttributes = false;
#endif

        public static GISTerrainLoaderVectorParameters_SO LoadParameters()
        {
            GISTerrainLoaderVectorParameters_SO VectorParameters = Resources.Load("Settings/GISTerrainLoaderVectorParameters") as GISTerrainLoaderVectorParameters_SO;

            if (!VectorParameters)
            {
                Debug.LogError("Vector Parameters ScriptableObject File Not found ...");
                return null;
            }

            return VectorParameters;
        }
    }
}
