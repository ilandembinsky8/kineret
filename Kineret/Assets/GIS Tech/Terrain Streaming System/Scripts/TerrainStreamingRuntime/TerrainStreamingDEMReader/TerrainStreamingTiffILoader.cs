using BitMiracle.LibTiff.Classic;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace GISTech.TerrainStreaming
{
    public class TerrainStreamingTiffILoader
    {
        public static event ReaderEvents OnReadError;

        public static event TerrainProgression OnProgress;
 
        public TerrainStreamingFileData data;

        public bool LoadComplet;

        int currentIndex;
        int totalRequest;
        int Row = 0;

        static int prog = 0;
        static int Totaltiles = 0;
        static int TotalProg = 0;

        private const TiffTag GeoKeyDirectoryTag = (TiffTag)34735;
        private const TiffTag GeoDoubleParamsTag = (TiffTag)34736;
        private const TiffTag GeoAsciiParamsTag = (TiffTag)34737;
        private const TiffTag TIFFTAG_ASCIITAG = (TiffTag)666;
        private const TiffTag GDAL_METADATA = (TiffTag)42112;
        private const TiffTag GDAL_NODATA = (TiffTag)42113;


        public async Task LoadTiff(string filepath, int m_currentIndex, int m_totalRequest, CancellationTokenSource taskSource)
        {
            TotalProg = 0;
            LoadComplet = false;

            if (File.Exists(filepath))
            {
                try
                {
                    currentIndex = m_currentIndex;
                    totalRequest = m_totalRequest;

                    Row = 0;

                    data = new TerrainStreamingFileData();

                    await ReadTiff(filepath, taskSource).CancelWith(taskSource.Token);

                    var p1 = new DVector2(data.UpperRightCoordinate.x, data.BottomLeftCoordinate.y);
                    var p2 = new DVector2(data.BottomLeftCoordinate.x, data.UpperRightCoordinate.y);

                    data.Terrain_Dimension.x = TerrainStreamingGeoConversion.Getdistance(data.BottomLeftCoordinate.y, data.BottomLeftCoordinate.x, p1.y, p1.x) * 10;
                    data.Terrain_Dimension.y = TerrainStreamingGeoConversion.Getdistance(data.BottomLeftCoordinate.y, data.BottomLeftCoordinate.x, p2.y, p2.x) * 10;

                    LoadComplet = true;

                }
                catch (Exception e)
                {
                    Debug.LogError("Error occured while reading Tiff file! "+ e.ToString());

                    if (OnReadError != null)
                    {
                        OnReadError();
                    }
                    return;
                }


            }


        }
        private async Task ReadTiff(string filepath, CancellationTokenSource taskSource)
        {
            try
            {
                using (Tiff tiff = Tiff.Open(filepath, "r"))
                {

                    int width = tiff.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
                    int lenght = tiff.GetField(TiffTag.IMAGELENGTH)[0].ToInt();

                    int BITSPERSAMPLE = tiff.GetField(TiffTag.BITSPERSAMPLE)[0].ToInt();

                    data.mapSize_row = lenght;
                    data.mapSize_col = width;

                    data.floatheightData = new float[width, lenght];

                    byte[] scanline16 = new byte[tiff.ScanlineSize()];

                    for (int row = 0; row < width; row++)
                    {
                        tiff.ReadScanline(scanline16, row);

                        for (int col = 0; col < lenght; col++)
                        {
                            var el = (short)((scanline16[col * 2 + 1] << 8) + scanline16[col * 2]);

                            var el1 = Convert.ToSingle(el);
 
                            if (el1 < -9999 || el1.Equals(-32768))
                                el1 = 0;
                 
                            data.floatheightData[row, col] = el1;

                            if (el1 < data.MinElevation)
                                data.MinElevation = el1;
                            if (el1 > data.MaxElevation)
                                data.MaxElevation = el1;

                        }

                        Row++;

                        var prog = (Row * 100 / data.mapSize_col);

                        if (prog != TotalProg)
                        {
                            TotalProg = prog;
                            if (OnProgress != null)
                                OnProgress("Load Elevation..   " + currentIndex + "/" + totalRequest, TotalProg);

                            await Task.Delay(1).CancelWith(taskSource.Token);
                        }

                    }

                    FieldValue[] modelPixelScaleTag = tiff.GetField((TiffTag)33550);
                    FieldValue[] modelTiepointTag = tiff.GetField((TiffTag)33922);

                    byte[] modelPixelScale = modelPixelScaleTag[1].GetBytes();

                    double pixelSizeX = BitConverter.ToDouble(modelPixelScale, 0);
                    double pixelSizeY = BitConverter.ToDouble(modelPixelScale, 8) * -1;

                    data.cellsize_x = pixelSizeX;
                    data.cellsize_y = pixelSizeY;

                    byte[] modelTransformation = modelTiepointTag[1].GetBytes();

                    double originLon = BitConverter.ToDouble(modelTransformation, 24);
                    double originLat = BitConverter.ToDouble(modelTransformation, 32);


                    double startLat = originLat + (pixelSizeY / 2.0);
                    double startLon = originLon + (pixelSizeX / 2.0);

                    double currentLat = startLat;
                    double currentLon = startLon;

                    data.BottomLeftCoordinate = new DVector2(originLon, startLat + (pixelSizeY * lenght));
                    data.UpperLeftCoordinate = new DVector2(originLon, originLat);
                    data.BottomRightCoordiante = new DVector2(startLon + (pixelSizeX * width), startLat + (pixelSizeY * lenght));
                    data.UpperRightCoordinate = new DVector2(data.BottomRightCoordiante.x, data.UpperLeftCoordinate.y);
 
                }
            }

            catch (Exception)
            {
 
                if (OnReadError != null)
                {
                    OnReadError();
                }
            };
        }
        public static void WriteTiffTiles(string SaveFolder, TerrainStreamingFileData mainData, Vector2Int tileCount)
        {
            double Lon_Step = (mainData.BottomRightCoordiante.x - mainData.BottomLeftCoordinate.x) / tileCount.x;
            double Lat_Step = (mainData.UpperLeftCoordinate.y - mainData.BottomLeftCoordinate.y) / tileCount.y;

            int Tile_Col = mainData.mapSize_col / tileCount.x + 1;
            int Tile_Row = mainData.mapSize_row / tileCount.y + 1;

            prog = -1;

            Totaltiles = tileCount.x * tileCount.y;
            TotalProg = 0;

            for (int Tile_x = 0; Tile_x < tileCount.x; Tile_x++)
            {
                for (int Tile_y = 0; Tile_y < tileCount.y; Tile_y++)
                {

                    string TileName = string.Format("Tile__{0}__{1}", Tile_x, Tile_y);
                    double offest_Lon = 0;
                    double offest_Lat = 0;
                    var Tile_data = new TerrainStreamingFileData();

                    Tile_data.X_Tile = Tile_x;
                    Tile_data.Y_Tile = Tile_y;

                    var Tile_Col_from = Tile_x * Tile_Col;
                    var Tile_Col_To = (Tile_Col_from + Tile_Col);

                    var Tile_Row_from = Tile_y * Tile_Row;
                    var Tile_Row_To = (Tile_Row_from + Tile_Row);


                    if (Tile_Col_from != 0)
                        offest_Lon = (mainData.cellsize_x) * Tile_x;

                    offest_Lat = (mainData.cellsize_y / 2) * (Tile_y);

                    Tile_data.BottomLeftCoordinate = new DVector2(mainData.UpperLeftCoordinate.x + Lon_Step * Tile_x + offest_Lon, mainData.UpperLeftCoordinate.y - Lat_Step * (Tile_y + 1) - offest_Lat);
                    Tile_data.UpperLeftCoordinate = new DVector2(mainData.UpperLeftCoordinate.x, mainData.UpperLeftCoordinate.y - Lat_Step * (Tile_y) - offest_Lat);
                    Tile_data.BottomRightCoordiante = new DVector2(mainData.UpperLeftCoordinate.x + Lon_Step * (Tile_x + 1) + offest_Lon, mainData.UpperLeftCoordinate.y - Lat_Step * (Tile_y + 1) - offest_Lat);

                    Tile_data.mapSize_row = (Tile_Row_To - Tile_Row_from);
                    Tile_data.mapSize_col = (Tile_Col_To - Tile_Col_from);


                    Tile_data.floatheightData = new float[Tile_data.mapSize_row, Tile_data.mapSize_col];

                    Tile_data.cellsize_x = mainData.cellsize_x;
                    Tile_data.cellsize_y = mainData.cellsize_y;

                    Tile_data.GetDetails();

                    for (int r = Tile_Row_from; r < Tile_Row_To; r++)
                    {
                        for (int c = Tile_Col_from; c < Tile_Col_To; c++)
                        {

                            var R = r - Tile_Row_from;
                            var C = c - Tile_Col_from;

                            var Main_C = c;
                            var Main_R = r;

                            if (Main_C >= mainData.floatheightData.GetLength(1))
                                Main_C = mainData.floatheightData.GetLength(1) - 1;

                            if (Main_R >= mainData.floatheightData.GetLength(0))
                                Main_R = mainData.floatheightData.GetLength(0) - 1;

                            var el = mainData.floatheightData[Main_R, Main_C];

                            if (el > -999999)
                            {
                                if (el < Tile_data.MinElevation)
                                    Tile_data.MinElevation = el;


                                if (el > Tile_data.MaxElevation)
                                    Tile_data.MaxElevation = el;

                            }
                            Tile_data.floatheightData[R, C] = el;

                        }

                    }
                    prog++;

                    WriteTiff(SaveFolder, Tile_data);
                }

            }
        }
        public static void WriteTiff(string fileName, TerrainStreamingFileData item)
        {
            float[,] data = item.floatheightData;
            int Col = data.GetLength(1);
            int Row = data.GetLength(0);

            double PixelScaleX = Math.Abs((item.BottomRightCoordiante.x - item.UpperLeftCoordinate.x)) / Col;
            double PixelScaleY = Math.Abs((item.UpperLeftCoordinate.y - item.BottomRightCoordiante.y)) / Row;
            Tiff.SetTagExtender(TagExtender);
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
                tiff.SetField(TiffTag.MAXSAMPLEVALUE, item.MaxElevation);
                tiff.SetField(TiffTag.MINSAMPLEVALUE, 0);
                tiff.SetField(TiffTag.ROWSPERSTRIP, 1);
                tiff.SetField(TiffTag.COPYRIGHT, "Created By GIS Data Downloader (Unity GISTech)");
                tiff.SetField(GeoKeyDirectoryTag, GeoTags);

                double[] tiePoints = new double[] { 0, 0, 0, item.UpperLeftCoordinate.x, item.UpperLeftCoordinate.y, 0 };
                tiff.SetField(TiffTag.GEOTIFF_MODELTIEPOINTTAG, 6, (object)tiePoints);

                double[] pixelScale = new double[] { PixelScaleX, PixelScaleY, 0.5 };
                tiff.SetField(TiffTag.GEOTIFF_MODELPIXELSCALETAG, 3, pixelScale);

                float[] source = new float[Col];

                for (int i = 0; i < Row; i++)
                {
                    for (int j = 0; j < Col; j++)
                        source[j] = data[i, j];

                    byte[] dest = new byte[source.Length * sizeof(float)];
                    Buffer.BlockCopy(source, 0, dest, 0, dest.Length);
                    tiff.WriteScanline(dest, i);
                }

            }
        }

        public static void TagExtender(Tiff tiff)
        {
            TiffFieldInfo[] tiffFieldInfo =
            {
        new TiffFieldInfo(TiffTag.GEOTIFF_MODELTIEPOINTTAG, 6, 6, TiffType.DOUBLE, FieldBit.Custom, false, true, "MODELTILEPOINTTAG"),
        new TiffFieldInfo(TiffTag.GEOTIFF_MODELPIXELSCALETAG, 3, 3, TiffType.DOUBLE, FieldBit.Custom, false, true, "MODELPIXELSCALETAG"),
        new TiffFieldInfo(TiffTag.GEOTIFF_MODELTRANSFORMATIONTAG, 16, 16, TiffType.DOUBLE, FieldBit.Custom, true, false, "GEOTIFF_MODELTRANSFORMATIONTAG"),
        new TiffFieldInfo(GeoKeyDirectoryTag, (short)GeoTags.Length, (short)GeoTags.Length, TiffType.SHORT, FieldBit.Custom, true, false, "GeoKeyDirectoryTag"),
        };
            tiff.MergeFieldInfo(tiffFieldInfo, tiffFieldInfo.Length);
        }

        static int CodeNumber = 4326;
        static UInt16[] GeoTags
        {
            get
            {
                var index = 0;
                UInt16 count = 2;

                count += (CodeNumber > 0) ? (UInt16)1 : (UInt16)0;
                count += (CodeNumber > 0) ? (UInt16)1 : (UInt16)0;

                var geotags = new UInt16[(count + 1) * 4];

                geotags.SetValue((UInt16)1, index++); geotags.SetValue((UInt16)1, index++); geotags.SetValue((UInt16)1, index++); geotags.SetValue((UInt16)count, index++);
                geotags.SetValue((UInt16)1024, index++); geotags.SetValue((UInt16)0, index++); geotags.SetValue((UInt16)1, index++); geotags.SetValue((UInt16)2, index++);
                geotags.SetValue((UInt16)1025, index++); geotags.SetValue((UInt16)0, index++); geotags.SetValue((UInt16)1, index++); geotags.SetValue((UInt16)2, index++);

                if (CodeNumber > 0)
                {
                    geotags.SetValue((UInt16)2048, index++); geotags.SetValue((UInt16)0, index++); geotags.SetValue((UInt16)1, index++); geotags.SetValue((UInt16)CodeNumber, index++);
                }

                return geotags;
            }
        }


    }


}
