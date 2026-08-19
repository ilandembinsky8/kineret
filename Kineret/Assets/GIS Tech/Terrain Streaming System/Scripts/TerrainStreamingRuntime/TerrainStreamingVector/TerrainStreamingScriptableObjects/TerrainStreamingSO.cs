/*     Unity GIS Tech 2020-2021      */

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GISTech.TerrainStreaming
{
#if UNITY_EDITOR
    public class TerrainStreamingSO : MonoBehaviour
    {
        [MenuItem("Tools/GIS Tech/Terrain Streaming/Create Vector Prefab/Tree")]
        public static void CreateTreeSO()
        {
            TerrainStreamingSO_Tree asset = ScriptableObject.CreateInstance<TerrainStreamingSO_Tree>();

            AssetDatabase.CreateAsset(asset, "Assets/GIS Tech/Terrain Streaming System/Resources/Prefabs/Environment/Trees/NewTree.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = asset;
        }
        [MenuItem("Tools/GIS Tech/Terrain Streaming/Create Vector Prefab/Road")]
        public static void CreateRoadSO()
        {
            TerrainStreamingSO_Road asset = ScriptableObject.CreateInstance<TerrainStreamingSO_Road>();

            AssetDatabase.CreateAsset(asset, "Assets/GIS Tech/Terrain Streaming System/Resources/Prefabs/Environment/Roads/NewRoad.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = asset;
        }
        [MenuItem("Tools/GIS Tech/Terrain Streaming/Create Vector Prefab/Grass/Grass Model")]
        public static void CreateGrassSO_Model()
        {
            TerrainStreamingSO_Grass asset = ScriptableObject.CreateInstance<TerrainStreamingSO_Grass>();

            AssetDatabase.CreateAsset(asset, "Assets/GIS Tech/Terrain Streaming System/Resources/Prefabs/Environment/Grass/Models/NewGrassModel.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = asset;
        }
        [MenuItem("Tools/GIS Tech/Terrain Streaming/Create Vector Prefab/Grass/Grass Prefab")]
        public static void CreateGrassSO_Prefab()
        {
            TerrainStreamingSO_GrassObject asset = ScriptableObject.CreateInstance<TerrainStreamingSO_GrassObject>();

            AssetDatabase.CreateAsset(asset, "Assets/GIS Tech/Terrain Streaming System/Resources/Prefabs/Environment/Grass/NewGrass.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = asset;
        }
    }

#endif
}