using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GISTech.TerrainStreaming
{
    public class TerrainStreamingTileSector : MonoBehaviour
    {
        [HideInInspector]
        public TerrainStreamingTileData TileData;
        [HideInInspector]
        public TerrainStreamingTerrainTile TerrainTile;
        [HideInInspector]
        public Bounds tileBounds;
        [HideInInspector]
        public Color SelectedColor = Color.red;
        [HideInInspector]
        public Vector3 size;

        public Vector2Int Number;
        [HideInInspector]
        public OptionEnabDisab EnableDrawSector = OptionEnabDisab.Enable;

        void OnDrawGizmos()
        {
            if(EnableDrawSector== OptionEnabDisab.Enable)
            {
                Gizmos.color = SelectedColor;
                Gizmos.DrawWireCube(tileBounds.center, tileBounds.size);
            }

        }

    }
}