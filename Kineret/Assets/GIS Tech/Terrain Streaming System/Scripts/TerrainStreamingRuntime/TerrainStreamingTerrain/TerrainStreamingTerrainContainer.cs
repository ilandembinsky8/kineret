/*     Unity GIS Tech 2020-2021      */

using UnityEngine;

namespace GISTech.TerrainStreaming
{
    public class TerrainStreamingTerrainContainer : MonoSingleton<TerrainStreamingTerrainContainer>
    {

        public TerrainStreamingTerrainTile [] GetTerrainTiles()
        {
            var TerrainTiles = this.GetComponentsInChildren<TerrainStreamingTerrainTile>();
 
            return TerrainTiles;
        }

    }
}