/*     Unity GIS Tech 2020-2021      */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GISTech.TerrainStreaming
{
    public class TerrainStreamingSO_Tree : ScriptableObject
    {
        public TerrainStreamingVectorTag Tag = new TerrainStreamingVectorTag("", "");

        [Range(1, 100)]
        public float TreeDensity = 70f;
        [Range(0.1f, 50)]
        public float TreeScaleFactor = 1.5f;
        [Range(0, 1)]
        public float TreeRandomScaleFactor = 0.5f;
        public List<Object> TreePrefab = new List<Object>();


    }
}