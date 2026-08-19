/*     Unity GIS Tech 2020-2021      */

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace GISTech.TerrainStreaming
{

    public class TerrainStreamingTextureLoader
    {

        public static string CheckForTexture(string RasterFolderPath, TerrainStreamingTerrainTile terrain, out bool exist)
        {
            string TextureFilePath = "";
            exist = false;

            var TexturePath = RasterFolderPath + "/" + "Tile__" + terrain.Number.x.ToString() + "__" + terrain.Number.y.ToString();

            if (File.Exists(TexturePath + ".jpg"))
            {
                TextureFilePath = TexturePath + ".jpg";
                exist = true;
            }
            else
            {
                if (File.Exists(TexturePath + ".png"))
                {
                    TextureFilePath = TexturePath + ".png";
                    exist = true;
                }

            }

            return TextureFilePath;
        }

    }
}
