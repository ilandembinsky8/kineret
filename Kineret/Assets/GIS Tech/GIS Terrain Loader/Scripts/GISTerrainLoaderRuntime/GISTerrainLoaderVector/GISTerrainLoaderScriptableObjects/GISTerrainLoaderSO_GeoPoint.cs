/*     Unity GIS Tech 2020-2025      */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GISTech.GISTerrainLoader
{
    public class GISTerrainLoaderSO_GeoPoint : ScriptableObject
    {
        public string GeoPointType;
        public List<GameObject> Prefabs;
        public InstantiatePlacment PrefabsInstantiatePlacment;

        [Header("Randomize Scale for a group of prefabs")]
        /** scale randomizer
 */
        public bool randScale = true;
        public Vector3 minScale = new Vector3(0, 0, 0);
        public Vector3 maxScale = new Vector3(1, 1, 1);

        [Header("Randomize Position for a group of prefabs")]
        /** position randomizer
         */
        public WorldSpace posSpace = WorldSpace.World;
        public bool randPos = false;
        public Vector3 minPos = new Vector3(-10, 0, -10);
        public Vector3 maxPos = new Vector3(10, 0, 10);

        [Header("Randomize Rotation for a group of prefabs")]
        /** rotation randomizer
 */
        public WorldSpace rotSpace = WorldSpace.World;
        public bool randRot = false;
        public Vector3 minRot = new Vector3(0, 0, 0);
        public Vector3 maxRot = new Vector3(0, 0, 360);

    }


    /// <summary>
    /// Instantiate one Random Prefab from prefabs List/ Instantiate All Prefabs exit in prefabs list
    /// </summary>
    public enum InstantiatePlacment
    {
        OneRandomPrefab,
        AllPrefabs
    }
    /// <summary>
    /// This Option works only with (InstantiatePlacment : AllPrefabs),
    /// Use Keep Prefabs to avoid any transform randoms inside the prefabs list
    /// </summary>
    public enum InstantiateTransformRandom
    {
        WithoutTransformRandom,
        RandomTransforms
    }

    public enum WorldSpace
    {
        Local,
        World,
    };
}