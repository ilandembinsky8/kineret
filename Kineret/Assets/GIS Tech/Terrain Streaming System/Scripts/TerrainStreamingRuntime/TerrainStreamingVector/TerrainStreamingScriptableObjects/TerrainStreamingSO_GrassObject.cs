/*     Unity GIS Tech 2020-2021      */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GISTech.TerrainStreaming
{
    public class TerrainStreamingSO_GrassObject : ScriptableObject
    {
        public TerrainStreamingVectorTag Tag = new TerrainStreamingVectorTag("", "");
        [Range(1, 100)]
        public float GrassDensity;
        public List<TerrainStreamingSO_Grass> GrassPrefab = new List<TerrainStreamingSO_Grass>();
    }
}