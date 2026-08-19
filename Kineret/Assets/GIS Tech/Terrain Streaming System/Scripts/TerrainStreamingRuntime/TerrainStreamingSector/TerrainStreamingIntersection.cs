/*     Unity GIS Tech 2020-2021      */


using System.Collections.Generic;
using UnityEngine;

namespace GISTech.TerrainStreaming
{
    public class TerrainStreamingIntersection
    {
        private static TerrainStreamingContainer SectorContainer;
        private static TerrainStreamingSystemPrefs prefs;
        private static TerrainStreamingTileSector[,] AllSectors;
        private static TerrainStreamingPlayer player;
        public static int Samples { get; private set; } = -1;
        public static void SetParamerters(TerrainStreamingPlayer m_player,TerrainStreamingContainer m_SectorContainer , TerrainStreamingSystemPrefs m_prefs, TerrainStreamingTileSector[,]  m_AllSectors)
        {
            player = m_player;
            SectorContainer = m_SectorContainer;
            prefs = m_prefs;
            AllSectors = m_AllSectors;

        }
        public static List<TerrainStreamingTileSector> GetAllTilesInFOV(TerrainStreamingPlayer player, Vector3 worldPosOffset, float TileDistance = 100, int spacing = 1)
        {
            TerrainStreamingTileSector[,] tempRowsAndColumns = new TerrainStreamingTileSector[prefs.TilesCount.x, prefs.TilesCount.y];
            List<TerrainStreamingTileSector> tempTiles = new List<TerrainStreamingTileSector>();
            Samples = 0;
            spacing = 1;
            var Camposition = new Vector3(player.playerCam.transform.position.x - worldPosOffset.x, player.playerCam.transform.position.y, player.playerCam.transform.position.z - worldPosOffset.y);
            TerrainStreamingTileSector tempTile = GetNearestTile(Camposition);

            if (tempTile != null)
            { AddTileAndDiscardIfDuplicate(tempRowsAndColumns, tempTiles, tempTile); }

            Vector3[] clipPoints = new Vector3[5];

            float halfFOV = (player.playerCam.fieldOfView / 2) * Mathf.Deg2Rad;
            float aspect = player.playerCam.aspect;
            float distance = (player.ClippingFarDistance * TileDistance) / 100;
            float height = distance * Mathf.Tan(halfFOV);
            float width = height * aspect;



            // lower right.
            clipPoints[0] = Camposition + player.playerCam.transform.right * width;
            clipPoints[0] -= player.playerCam.transform.up * height;
            clipPoints[0] += player.playerCam.transform.forward * distance;

            // lower left.
            clipPoints[1] = Camposition - player.playerCam.transform.right * width;
            clipPoints[1] -= player.playerCam.transform.up * height;
            clipPoints[1] += player.playerCam.transform.forward * distance;

            // upper right.
            clipPoints[2] = Camposition + player.playerCam.transform.right * width;
            clipPoints[2] += player.playerCam.transform.up * height;
            clipPoints[2] += player.playerCam.transform.forward * distance;

            // upper left.
            clipPoints[3] = Camposition - player.playerCam.transform.right * width;
            clipPoints[3] += player.playerCam.transform.up * height;
            clipPoints[3] += player.playerCam.transform.forward * distance;

            // middle.
            clipPoints[4] = Camposition + player.playerCam.transform.forward * -player.playerCam.nearClipPlane;

            clipPoints[0].y = Camposition.y;
            clipPoints[1].y = Camposition.y;

            float s = Vector3.Distance(clipPoints[0], Camposition);
            float numPointsVertical = s / spacing;

            Vector3 dir1 = (clipPoints[0] - Camposition);
            dir1.Normalize();

            Vector3 dir2 = (clipPoints[1] - Camposition);
            dir2.Normalize();

            for (int i = 1; i <= numPointsVertical; ++i)
            {
                Samples++;

                Vector3 p1 = Camposition + (dir1 * (i * spacing));
                tempTile = GetNearestTile(p1);
                if (tempTile != null)
                { AddTileAndDiscardIfDuplicate(tempRowsAndColumns, tempTiles, tempTile); }

                Vector3 p2 = Camposition + (dir2 * (i * spacing));
                tempTile = GetNearestTile(p2);
                if (tempTile != null)
                { AddTileAndDiscardIfDuplicate(tempRowsAndColumns, tempTiles, tempTile); }

                var spacing_horizontal = SectorContainer.SubTerrainSize.x / 5;

                s = Vector3.Distance(p1, p2);
                var numPointsHorizontal = s / spacing_horizontal;

                Vector3 dir3 = p1 - p2;
                dir3.Normalize();

                Gizmos.color = Color.blue;
                for (int j = 1; j <= numPointsHorizontal - 1; j++)
                {
                    Samples++;

                    Vector3 p3 = p2 + (dir3 * (j * spacing_horizontal));

                    tempTile = GetNearestTile(p3);

                    if (tempTile != null)
                    { AddTileAndDiscardIfDuplicate(tempRowsAndColumns, tempTiles, tempTile); }
                }
            }

            return tempTiles;
        }
        public static List<TerrainStreamingTileSector> GetTilesWithinRectangle(TerrainStreamingPlayer m_player, Vector3 worldPosOffset)
        {
            var Camposition = new Vector3(m_player.transform.position.x - worldPosOffset.x, m_player.transform.position.y, m_player.transform.position.z - worldPosOffset.y);

            TerrainStreamingTileSector tempTile = GetNearestTile(Camposition);
            TerrainStreamingTileSector[,] tempRowsAndColumns = new TerrainStreamingTileSector[prefs.TilesCount.x, prefs.TilesCount.y];
            List<TerrainStreamingTileSector> tempTiles = new List<TerrainStreamingTileSector>();

            if (tempTile)
            {
                Vector3[] rectangle = GetPointsForRectangle(m_player);

                var p0 = (rectangle[0] + rectangle[1]) / 2;
                var p1 = (rectangle[2] + rectangle[3]) / 2;

                var p3 = (rectangle[0] + rectangle[2]) / 2;
                var p4 = (rectangle[1] + rectangle[3]) / 2;

                var distance_z = Vector3.Distance(p0, p1);
                var distance_x = Vector3.Distance(p3, p4);

                var dir_z = p4 - p3; dir_z.Normalize();
                var dir_x = p0 - p1; dir_x.Normalize();

                var size_x = tempTile.size.x;
                var size_z = tempTile.size.z;

                var numPoints_vertical = Mathf.RoundToInt(distance_z / size_z);
 
                for (int i = 0; i <= numPoints_vertical; i++)
                {
                    var Dir = rectangle[2] + (dir_x * (i * size_z));
                    Camposition = new Vector3(Dir.x - worldPosOffset.x, Dir.y, Dir.z - worldPosOffset.y);
                    Vector3 item_1 = Camposition;
                    tempTile = GetNearestTile(item_1);

                    if (tempTile != null)
                        AddTileAndDiscardIfDuplicate(tempRowsAndColumns, tempTiles, tempTile);

                    var Dir_2 = rectangle[3] + (dir_x * (i * size_z));
                    Camposition = new Vector3(Dir_2.x - worldPosOffset.x, Dir_2.y, Dir_2.z - worldPosOffset.y);
                    Vector3 item_2 = Camposition;
                    tempTile = GetNearestTile(item_2);
                    if (tempTile != null)
                    { AddTileAndDiscardIfDuplicate(tempRowsAndColumns, tempTiles, tempTile); }

                    distance_z = Vector3.Distance(item_1, item_2);
                    var dir_2 = item_2 - item_1;
                    dir_2.Normalize();

                    var numPoints_horizontal = Mathf.RoundToInt(distance_x / size_x);
 
                    for (int j = 1; j <= numPoints_horizontal; j++)
                    {
                        var Dir_3 = item_1 + (dir_2 * (j * size_x));
                        Camposition = new Vector3(Dir_3.x - worldPosOffset.x, Dir_3.y, Dir_3.z - worldPosOffset.y);
                        Vector3 item_3 = Dir_3;
                        tempTile = GetNearestTile(item_3);

                        if (tempTile != null)
                            AddTileAndDiscardIfDuplicate(tempRowsAndColumns, tempTiles, tempTile);
                    }
                }
            }
            return tempTiles;
        }
        public static List<TerrainStreamingTileSector> GetTilesInRadius(Vector3 pos, Vector3 worldPosOffset, float radius)
        {
            var tileSize_X = SectorContainer.SubTerrainSize.x;
            var tileSize_Y = SectorContainer.SubTerrainSize.y;

            List<TerrainStreamingTileSector> tempTiles = new List<TerrainStreamingTileSector>();
            int level = 0;

            if (radius > tileSize_X)
                level = (int)Mathf.Floor(radius / tileSize_X);

            else if (tileSize_X > radius)
                level = 0;

            tempTiles = GetNearestTilePlusNeighbours(pos, worldPosOffset, level);

            return tempTiles;
        }
        public static List<TerrainStreamingTileSector> GetNearestTilePlusNeighbours(Vector3 position, Vector3 worldPosOffset, int level = 2)
        {
            var tileSize_X = SectorContainer.SubTerrainSize.x;
            var tileSize_Y = SectorContainer.SubTerrainSize.y;

            var ContainerSize_X = SectorContainer.ContainerSize.x;
            var ContainerSize_Y = SectorContainer.ContainerSize.y;

            TerrainStreamingTileSector[,] gridRowsAndColumns = new TerrainStreamingTileSector[prefs.TilesCount.x, prefs.TilesCount.y];
            List<TerrainStreamingTileSector> tempTiles = new List<TerrainStreamingTileSector>();

            var Camposition = new Vector3(position.x - worldPosOffset.x, position.y, position.z - worldPosOffset.y);

            var tempTile = GetNearestTile(Camposition);

            if (tempTile == null)
                return tempTiles;

            tempTiles.Add(tempTile);

            int numTiles = ((int)(ContainerSize_X / tileSize_X)) - 1;

            TerrainStreamingTileSector tile = null;
            // Row Y Col X
            int ColPos = tempTile.Number.y;
            int RowPos = tempTile.Number.x;
            // left neighbours.
            int i = 0;
            while (i < level && RowPos - i > 0)
            {
                i++;
                tile = AllSectors[RowPos - i, ColPos];
                if (tile != null)
                    tempTiles.Add(tile);
            }

            // right neighbours.
            tile = null;
            i = 0;
            while (i < level && (RowPos + i) < numTiles)
            {
                i++;
                tile = AllSectors[RowPos + i, ColPos];
                if (tile != null)
                    tempTiles.Add(tile);
            }

            // front neighbours.
            tile = null;
            i = 0;
            while (i < level && (ColPos + i) < numTiles)
            {
                i++;
                if (RowPos < AllSectors.GetLength(0) && (ColPos + i) < AllSectors.GetLength(1))
                    tempTiles.Add(AllSectors[RowPos, ColPos + i]);
            }

            // front right neighbours.
            i = 1;
            while (i <= level)
            {
                for (int j = 1; j < level + 1; j++)
                {
                    if (RowPos + i > numTiles ||
                        ColPos + j > numTiles)
                        break;
                    if (RowPos + i < AllSectors.GetLength(0) && (ColPos + j) < AllSectors.GetLength(1))
                        tempTiles.Add(AllSectors[RowPos + i, ColPos + j]);
                }
                i++;
            }

            // front left neighbours.
            i = 1;
            while (i <= level)
            {
                for (int j = 1; j < level + 1; j++)
                {
                    if (
                        ColPos + j > numTiles ||
                        RowPos - i < 0)
                        break;
                    if (RowPos - i < AllSectors.GetLength(0) && (ColPos + j) < AllSectors.GetLength(1))
                        tempTiles.Add(AllSectors[RowPos - i, ColPos + j]);
                }
                i++;
            }

            // back neighbours.
            i = 0;
            while (i < level && (ColPos - i > 0))
            {
                i++;
                tempTiles.Add(AllSectors[RowPos, ColPos - i]);
            }

            // back right neighbours.
            i = 1;
            while (i <= level)
            {
                for (int j = 1; j < level + 1; j++)
                {
                    if (RowPos + i > numTiles ||
                        ColPos - j < 0)
                        break;

                    tempTiles.Add(AllSectors[RowPos + i, ColPos - j]);
                }
                i++;
            }

            // back left neighbours.
            i = 1;
            while (i <= level)
            {
                for (int j = 1; j < level + 1; j++)
                {
                    if (
                        RowPos - i < 0 ||
                        ColPos - j < 0)
                        break;

                    tempTiles.Add(AllSectors[RowPos - i, ColPos - j]);
                }
                i++;
            }

            return tempTiles;
        }
        private static TerrainStreamingTileSector GetNearestTile(Vector3 pos)
        {

            int col = (int)(pos.x / SectorContainer.SubTerrainSize.x);
            int row = (int)(pos.z / SectorContainer.SubTerrainSize.z);

            if (col > prefs.TilesCount.x - 1 || row > prefs.TilesCount.y - 1 || row < 0 || col < 0)
                return null;
            else
                return AllSectors[col, prefs.TilesCount.y - row - 1];
        }
        private static void AddTileAndDiscardIfDuplicate(TerrainStreamingTileSector[,] tempRowsAndColumns, List<TerrainStreamingTileSector> tempTiles, TerrainStreamingTileSector tile)
        {
            if (tempRowsAndColumns[tile.Number.x, tile.Number.y] == null)
            {
                tempTiles.Add(tile);
                tempRowsAndColumns[tile.Number.x, tile.Number.y] = tile;
            }
        }
        private static Vector3[] GetPointsForRectangle(TerrainStreamingPlayer m_player)
        {
            Vector3[] rectanglePoints = new Vector3[4];

            // top left
            var x = m_player.transform.position.x - (m_player.m_TerrainLoadSize.x / 2);
            var y= m_player.transform.position.z + (m_player.m_TerrainLoadSize.z / 2);
            rectanglePoints[0] = new Vector3(x,0,y);

            // top right
            x = m_player.transform.position.x + (m_player.m_TerrainLoadSize.x / 2);
            y = m_player.transform.position.z + (m_player.m_TerrainLoadSize.z / 2);
            rectanglePoints[1] = new Vector3(x, 0, y);

            // bottom left
            x = m_player.transform.position.x - (m_player.m_TerrainLoadSize.x / 2);
            y = m_player.transform.position.z - (m_player.m_TerrainLoadSize.z / 2);
            rectanglePoints[2] = new Vector3(x, 0, y);

            // bottom right
            x = m_player.transform.position.x + (m_player.m_TerrainLoadSize.x / 2);
            y = m_player.transform.position.z - (m_player.m_TerrainLoadSize.z / 2);
            rectanglePoints[3] = new Vector3(x, 0, y);

            return rectanglePoints;
        }
    }
}