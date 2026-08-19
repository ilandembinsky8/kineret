/*     Unity GIS Tech 2020-2021      */

using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;

namespace GISTech.TerrainStreaming
{
    public class TerrainStreamingVectorTileGenerator
    {
        public static async Task GeneratePolygonGeoDataVectorTile(string fileName , List<TerrainStreamingPolygonGeoData> PolyGeoPoints,TerrainStreamingTileData tile, string Vector_DownloadPath, OptionEnabDisab ReplaceGeneratedVectorTiles, CancellationTokenSource m_taskSource)
        {
            string TileName = string.Format("Tile__{0}__{1}", tile.Number.x, tile.Number.y);
            string localFilename = Path.Combine(Vector_DownloadPath, TileName + "__"+ fileName + ".tsv");

            if (PolyGeoPoints.Count > 0)
            {
                byte[] bytes = new byte[0];

                if (ReplaceGeneratedVectorTiles == OptionEnabDisab.Enable)
                {
                    TerrainStreamingVectorSerializer.Serialize(PolyGeoPoints.ToArray(), ref bytes);
                    await TerrainStreamingFileAsync.WriteAllBytes(localFilename, bytes, m_taskSource).CancelWith(m_taskSource.Token);

                }
                else
                {
                    if (!File.Exists(localFilename))
                    {
                        TerrainStreamingVectorSerializer.Serialize(PolyGeoPoints.ToArray(), ref bytes);
                        await TerrainStreamingFileAsync.WriteAllBytes(localFilename, bytes, m_taskSource).CancelWith(m_taskSource.Token);
                    }
                }
            }
        }
        public static async Task GenerateLineGeoDataVectorTile(string fileName, List<TerrainStreamingLinesGeoData> LineGeoPoints, TerrainStreamingTileData tile, string Vector_DownloadPath, OptionEnabDisab ReplaceGeneratedVectorTiles, CancellationTokenSource m_taskSource)
        {
            string TileName = string.Format("Tile__{0}__{1}", tile.Number.x, tile.Number.y);
            string localFilename = Path.Combine(Vector_DownloadPath, TileName + "__" + fileName + ".tsv");

            if (LineGeoPoints.Count > 0)
            {
                byte[] bytes = new byte[0];

                if (ReplaceGeneratedVectorTiles == OptionEnabDisab.Enable)
                {
                    TerrainStreamingVectorSerializer.Serialize(LineGeoPoints.ToArray(), ref bytes);
                    await TerrainStreamingFileAsync.WriteAllBytes(localFilename, bytes, m_taskSource).CancelWith(m_taskSource.Token);

                }
                else
                {
                    if (!File.Exists(localFilename))
                    {
                        TerrainStreamingVectorSerializer.Serialize(LineGeoPoints.ToArray(), ref bytes);
                        await TerrainStreamingFileAsync.WriteAllBytes(localFilename, bytes, m_taskSource).CancelWith(m_taskSource.Token);
                    }
                }
            }
        }
    }
}
