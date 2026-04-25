/*     Unity GIS Tech 2020-2023      */

using BitMiracle.LibTiff.Classic;
using System;
using System.Collections.Generic;
using UnityEngine;
 
namespace GISTech.GISTerrainLoader
{
    public class GISTerrainLoaderTiffExporter
    {
        private GISTerrainContainer container;
        private string path;
        private TiffElevation tiffElevation;
        private Vector2 minMaxElevation;
        private static int EPSGCode = 4326;
        public GISTerrainLoaderTiffExporter(string m_path, GISTerrainContainer m_container, TiffElevation m_tiffElevation, Vector2 m_minMaxElevation)
        {
            path = m_path;
            container = m_container;
            tiffElevation = m_tiffElevation;
            minMaxElevation = m_minMaxElevation;
            EPSGCode = m_container.data.EPSG;
            if(EPSGCode==0) EPSGCode = 4326;
         }
        public GISTerrainLoaderTiffExporter(string m_path, GISTerrainContainer m_container)
        {
            path = m_path;
            container = m_container;
            tiffElevation = TiffElevation.Auto;
            EPSGCode = m_container.data.EPSG;
            if (EPSGCode == 0) EPSGCode = 4326;
        }
        public void ExportToTiff()
        {
            int heightmapResolution = -1;

            int cx = container != null ? container.TerrainCount.x : 1;
            int cy = container != null ? container.TerrainCount.y : 1;

            foreach (var terrain in container.terrains)
            {
                if (heightmapResolution == -1) heightmapResolution = terrain.terrainData.heightmapResolution;
                else if (heightmapResolution != terrain.terrainData.heightmapResolution)
                {
                    Debug.LogError("Error Terrains have different heightmap resolution.");
                    return;
                }
            }

            float RWMinElevation = 99999;
            float RWMaxElevation = -99999;


            if (tiffElevation == TiffElevation.Custom)
            {
                container.data.MinMaxElevation = minMaxElevation;
            }

            float RW_Range = container.data.MinMaxElevation.y - container.data.MinMaxElevation.x;
 
            int totalWidth = heightmapResolution * cx;
            int totalHeight = heightmapResolution * cy;

            float[,] RWElevations = new float[totalHeight, totalWidth];

            for (int ty = 0; ty < cy; ty++)
            {
                for (int tx = 0; tx < cx; tx++)
                {
                    float[,] raw = container.terrains[tx, ty].terrainData.GetHeights(
                        0, 0, heightmapResolution, heightmapResolution);

                    for (int dy = 0; dy < heightmapResolution; dy++)
                    {
                        for (int dx = 0; dx < heightmapResolution; dx++)
                        {
                            float h = raw[dy, dx]; // Unity = [row, col]
                            float RWE = (h * RW_Range) + container.data.MinMaxElevation.x;

                            if (RWE < RWMinElevation)
                                RWMinElevation = RWE;
                            if (RWE > RWMaxElevation)
                                RWMaxElevation = RWE;

                            // Correct placement
                            int col = tx * heightmapResolution + dx;
                            int row = ty * heightmapResolution + dy;

                            // Flip Y for TIFF (top-left origin)
                            int flippedRow = (totalHeight - 1) - row;

                            RWElevations[flippedRow, col] = RWE;
                        }
                    }
                }
            }


            container.data.floatheightData = RWElevations;
            container.data.MinMaxElevation = new Vector2(RWMinElevation, RWMaxElevation);
            WriteTiff(path, container.data);

        }


        //private const TiffTag ProjLinearUnitsGeoKey = (TiffTag)ExtraTiffTag.GeoKeyDirectoryTag;
 
        private const TiffTag GeoKeyDirectoryTag = (TiffTag)ExtraTiffTag.GeoKeyDirectoryTag;
        private const TiffTag GeoDoubleParamsTag = (TiffTag)34736;
        private const TiffTag GeoAsciiParamsTag = (TiffTag)34737;
        private const TiffTag GDAL_METADATA = (TiffTag)42112;
        private const TiffTag GDAL_NODATA = (TiffTag)42113;

        private Tiff.TiffExtendProc m_parentExtender;
        private void TagExtender(Tiff tiff)
        {
 
            TiffFieldInfo[] tiffFieldInfo =
            {
        new TiffFieldInfo((TiffTag)ExtraTiffTag.ProjLinearUnitsGeoKey, 1, 1, TiffType.SHORT, FieldBit.Custom, false, true, "LinearUnits"),
        new TiffFieldInfo(TiffTag.GEOTIFF_MODELTIEPOINTTAG, 6, 6, TiffType.DOUBLE, FieldBit.Custom, false, true, "MODELTILEPOINTTAG"),
        new TiffFieldInfo(TiffTag.GEOTIFF_MODELPIXELSCALETAG, 3, 3, TiffType.DOUBLE, FieldBit.Custom, false, true, "MODELPIXELSCALETAG"),
        new TiffFieldInfo(TiffTag.GEOTIFF_MODELTRANSFORMATIONTAG, 16, 16, TiffType.DOUBLE, FieldBit.Custom, true, false, "GEOTIFF_MODELTRANSFORMATIONTAG"),
        new TiffFieldInfo(GeoKeyDirectoryTag, (short)GeoTags.Length, (short)GeoTags.Length, TiffType.SHORT, FieldBit.Custom, true, false, "GeoKeyDirectoryTag")
        };
            tiff.MergeFieldInfo(tiffFieldInfo, tiffFieldInfo.Length);
            if (m_parentExtender != null)
                m_parentExtender(tiff);
        }

        public void WriteTiff(string fileName, GISTerrainLoaderFileData item)
        {
            float[,] data = item.floatheightData;
            int Col = data.GetLength(1);
            int Row = data.GetLength(0);

            double PixelScaleX = Math.Abs((item.DROriginal_Coor.x - item.TLOriginal_Coor.x)) / Col;
            double PixelScaleY = Math.Abs((item.TLOriginal_Coor.y - item.DROriginal_Coor.y)) / Row;

            Tiff.TiffExtendProc extender = TagExtender;
            m_parentExtender = Tiff.SetTagExtender(extender);

            using (Tiff tiff = Tiff.Open(fileName, "w"))
            {
                if (tiff == null)
                    return;

                tiff.SetField(TiffTag.IMAGEWIDTH, Col);
                tiff.SetField(TiffTag.IMAGELENGTH, Row);
                tiff.SetField(TiffTag.SAMPLESPERPIXEL, 1);
                tiff.SetField(TiffTag.BITSPERSAMPLE, 32);
                tiff.SetField(TiffTag.SAMPLEFORMAT, SampleFormat.IEEEFP);

                tiff.SetField(TiffTag.ORIENTATION, BitMiracle.LibTiff.Classic.Orientation.TOPLEFT);
                tiff.SetField(TiffTag.ROWSPERSTRIP, Col);
                tiff.SetField(TiffTag.XRESOLUTION, 1.0);
                tiff.SetField(TiffTag.YRESOLUTION, 1.0);
                tiff.SetField(TiffTag.RESOLUTIONUNIT, ResUnit.CENTIMETER);
                tiff.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);
                tiff.SetField(TiffTag.PHOTOMETRIC, Photometric.MINISBLACK);
                tiff.SetField(TiffTag.COMPRESSION, Compression.NONE);
                tiff.SetField(TiffTag.FILLORDER, FillOrder.MSB2LSB);
                //tiff.SetField(TiffTag.MINSAMPLEVALUE, item.MinMaxElevation.x);
                tiff.SetField(TiffTag.MAXSAMPLEVALUE, item.MinMaxElevation.y);
                tiff.SetField(TiffTag.ROWSPERSTRIP, 1);
                tiff.SetField(TiffTag.COPYRIGHT, "Created By GIS Terrain Downloader Pro From Unity GISTech");

                tiff.SetField((TiffTag)ExtraTiffTag.ProjLinearUnitsGeoKey, 1, (int)container.data.Unite);

                double[] geotiff_modeltiepointtag = new double[] { 0, 0, 0, item.TLOriginal_Coor.x, item.TLOriginal_Coor.y, 0 };
                tiff.SetField(TiffTag.GEOTIFF_MODELTIEPOINTTAG, 6, (object)geotiff_modeltiepointtag);

                double[] modelpixelscaletag = new double[] { PixelScaleX, PixelScaleY, 0.5 };
                
                tiff.SetField(TiffTag.GEOTIFF_MODELPIXELSCALETAG, 3, modelpixelscaletag);
               
                tiff.SetField(GeoKeyDirectoryTag, GeoTags);

                float[] source = new float[Col];

                for (int i = 0; i < Row; i++)
                {
                    for (int j = 0; j < Col; j++)
                        source[j] = data[i, j];

                    byte[] dest = new byte[source.Length * sizeof(float)];
                    Buffer.BlockCopy(source, 0, dest, 0, dest.Length);
                    tiff.WriteScanline(dest, i);
                }

                tiff.Dispose();
            }

        }





        public enum ModelTypeEnum
        {
            ModelTypeProjected = 1,
            ModelTypeGeographic = 2
        }
        public enum RasterTypeEnum
        {
            RasterPixelIsArea = 1,
            RasterPixelIsPoint = 2
        }
        private static RasterTypeEnum RasterType { get; set; } = RasterTypeEnum.RasterPixelIsPoint;
        private static ModelTypeEnum ModelType { get; set; } = ModelTypeEnum.ModelTypeProjected;


        private static UInt16[] GeoTags
        {
            get
            {
                List<UInt16> tags = new List<UInt16>();

                // KeyDirectory header
                // KeyDirectoryVersion, KeyRevision, MinorRevision, NumberOfKeys
                ushort numberOfKeys = 3;
                if (EPSGCode > 0) numberOfKeys++;

                tags.Add(1); // KeyDirectoryVersion
                tags.Add(1); // KeyRevision
                tags.Add(0); // MinorRevision
                tags.Add(numberOfKeys);

                // GTModelTypeGeoKey (Projected)
                tags.Add(1024); // keyId
                tags.Add(0);    // TIFFTagLocation
                tags.Add(1);    // Count
                tags.Add((ushort)ModelTypeEnum.ModelTypeProjected);

                // GTRasterTypeGeoKey
                tags.Add(1025);
                tags.Add(0);
                tags.Add(1);
                tags.Add((ushort)RasterType);

                if (EPSGCode > 0)
                {
                    // ProjectedCSTypeGeoKey
                    tags.Add(3072);
                    tags.Add(0);
                    tags.Add(1);
                    tags.Add((ushort)EPSGCode);
                }

                return tags.ToArray();
            }
        }
 
    }

}