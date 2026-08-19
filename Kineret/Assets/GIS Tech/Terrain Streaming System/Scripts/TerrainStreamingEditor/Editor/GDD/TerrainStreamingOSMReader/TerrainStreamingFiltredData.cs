/*     Unity GIS Tech 2020-2021      */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GISTech.TerrainStreaming
{
    public class TerrainStreamingOSMFiltredData
{
    public Dictionary<long, TerrainStreamingOSMNode> Nodes;
    public Dictionary<long, TerrainStreamingOSMWay> Ways;
    public Dictionary<long, TerrainStreamingOSMRelation> Relation;
        public TerrainStreamingOSMFiltredData()
        {
            Nodes = new Dictionary<long, TerrainStreamingOSMNode>();
            Ways = new Dictionary<long, TerrainStreamingOSMWay>();
            Relation = new Dictionary<long, TerrainStreamingOSMRelation>();
        }

    }
}

 