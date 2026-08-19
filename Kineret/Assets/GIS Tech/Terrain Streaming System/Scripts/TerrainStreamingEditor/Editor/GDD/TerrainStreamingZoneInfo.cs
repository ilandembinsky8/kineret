using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace GISTech.TerrainStreaming
{
    public class TerrainStreamingZoneInfo
    {
        public static event DownloadingProgress OnDownloadProgressChanged;
        public static event DownloadEvent OnError;

        public static DVector2 UpperRightCoordiante;
        public static DVector2 DownLeftCoordiante;

        public static Vector2 UL_From;
        public static Vector2 DR_To;



        public string MainPath = "";
        private string FileExtension;
        private string urlDEM = "";
        private Vector2Int urlStep = new Vector2Int(0, 0);

        public static bool GenerateWorldPreview;

        private static List<TerrainStreamingFileData> LoadedFiles = new List<TerrainStreamingFileData>();


        private int TileCout = 0;
        private float TotalProc = 0;

        public Vector2Int textureSize = new Vector2Int(1024, 1024);
        public Vector2Int terrainCount = new Vector2Int(1, 1);

        public static float[,] totaldataHeightmap;
        public static Vector2 TotalZoneMinMax = Vector2.zero;


        public bool Extracting = false;
        private static TerrainStreamingDownloader Prefs;


        CancellationTokenSource taskSource;

        TerrainStreamingWebDownloader WebDownloader;

        TerrainStreamingMultiWebDownloader RasterDownloaders;
        List<Task> tasks = new List<Task>();
        List<CancellationTokenSource> taskSources = new List<CancellationTokenSource>();

        public float TotalWidth = 0;
        public float TotalHeight = 0;

        private int NumberRequestedZip = 0;
 
        #region Public
        public TerrainStreamingZoneInfo(TerrainStreamingDownloader m_terrainStreamingDownloader)
        {
            Prefs = m_terrainStreamingDownloader;

            DownLeftCoordiante = new DVector2(Prefs.UpperLeftCoordiante.x, Prefs.DownRightCoordiante.y);
            UpperRightCoordiante = new DVector2(Prefs.DownRightCoordiante.x, Prefs.UpperLeftCoordiante.y);

            if (Prefs.WorldPreview == OptionEnabDisab.Enable)
                GenerateWorldPreview = true;
            else GenerateWorldPreview = false;

            var mainTypePath = TerrainStreamingProvidersData.GetMainDEMPath(Prefs.DEMSource, out urlDEM, out FileExtension, Prefs.UpperLeftCoordiante, Prefs.DownRightCoordiante, out UL_From, out DR_To, out urlStep);

            if (!string.IsNullOrEmpty(mainTypePath))
            {
                MainPath = mainTypePath;
                NumberRequestedZip = 0;
            }

            TerrainStreamingDownloader.OnDownloadStarted += OnDownloadStarted;
            TerrainStreamingDownloader.OnDownloadCancelled += OnDownloadCancelled;

        }
        private static void GetOptimizedInputs(int TotalWidth, int TotalHeight, bool CheckError)
        {
            var TileWidth = (int)(TotalWidth / Prefs.StreamingGridCount.x);
            var TileHeight = (int)(TotalHeight / Prefs.StreamingGridCount.y);

            if (TileWidth > 2000 || TileHeight > 1500)
            {
                if (CheckError)
                {
                    Debug.LogError("Tile Width/Height > the maximum values, click on the get optimized values button to obtain the best input for your terrain");

                    if (OnError != null)
                        OnError();
                }

            }

        }
        private void OnDownloadStarted()
        {


        }
        private void OnDownloadCancelled()
        {
            if (WebDownloader != null)
                WebDownloader.OnDownloadCancelled();

            foreach (var task in taskSources)
            {
                task.Cancel();
            }

            if (RasterDownloaders != null)
            {
                if (RasterDownloaders.WebDownloaders.Count > 0)
                {
                    foreach (var d in RasterDownloaders.WebDownloaders)
                    {
                        d.OnDownloadCancelled();
                    }
                }
            }

            try
            {

            }catch(OperationCanceledException)
            {
                if (taskSource != null)
                {
                    //if(!taskSource.IsCancellationRequested)
                    taskSource.Cancel();

                }
            }


        }

        #endregion
        #region DEM
        public List<RequestedFileData> GetDEMURLs_MapBox()
        {
             var ZipFolder = MainPath ;

            if (!Directory.Exists(ZipFolder))
                Directory.CreateDirectory(ZipFolder);
 
            var BaseUrl = TerrainStreamingProvidersData.MapBox_PngRaw_url;

            var urls = new List<RequestedFileData>();

            int zoom = TerrainStreamingPngRawLoader.GetZoomLevel(Prefs.UpperLeftCoordiante, Prefs.DownRightCoordiante, Prefs.StreamingGridCount, Prefs.HeightmapResolution);
 
            var UpperLeftTilePos = TerrainStreamingGeoConversion.GetLatLongToTile(Prefs.UpperLeftCoordiante.x, Prefs.UpperLeftCoordiante.y, zoom).ToIntVector();
            var DownRightTilePos = TerrainStreamingGeoConversion.GetLatLongToTile(Prefs.DownRightCoordiante.x, Prefs.DownRightCoordiante.y, zoom).ToIntVector(); ;
 
            DVector2 P1 = TerrainStreamingGeoConversion.GetLatLongToTile(UpperLeftTilePos.x, UpperLeftTilePos.y, zoom);
            DVector2 P2 = TerrainStreamingGeoConversion.GetLatLongToTile(DownRightTilePos.x + 1, DownRightTilePos.y + 1, zoom);

            TerrainStreamingGeoConversion.LatLongToMercat(ref P1.x, ref P1.y);
            TerrainStreamingGeoConversion.LatLongToMercat(ref P2.x, ref P2.y);
 
            int TotalMapWidth = (DownRightTilePos.x - UpperLeftTilePos.x + 1) * 256;
            int TotalMapHeight = (DownRightTilePos.y - UpperLeftTilePos.y + 1) * 256;

            NumberRequestedZip = 0;

            for (int tx = UpperLeftTilePos.x; tx <= DownRightTilePos.x; tx++)
            {
                for (int ty = UpperLeftTilePos.y; ty <= DownRightTilePos.y; ty++)
                {
                    string TileName = zoom + "x" + tx + "x" + ty + ".pngraw";
 
                    var TilePath = Path.Combine(ZipFolder, TileName);

                     string TileUrl = new StringBuilder(BaseUrl).Append(zoom).Append("/").Append(tx).Append("/").Append(ty).Append(".pngraw?access_token=").Append(Prefs.MapBoxKey).ToString();

                    var filedata = new RequestedFileData(TileUrl, TilePath, GISDataDownloaderDownloadType.data, "DEM", 200000000);

                    urls.Add(filedata);

                    NumberRequestedZip++;
                }
            }
  
            return urls;
        }
        public List<RequestedFileData> GetDEMURLs_SRTM90()
        {
            var urls = new List<RequestedFileData>();

            Vector2Int step = new Vector2Int(0, 0);

            var ZipFolder = MainPath + "/" + Prefs.DEMSource.ToString() + "_Zip";

            if (!Directory.Exists(ZipFolder))
                Directory.CreateDirectory(ZipFolder);

            step = TerrainStreamingProvidersData.srtm_90_tiff_LatLon_Step;

            for (var lat = UL_From.x; lat <= DR_To.x; lat += step.y)
            {
                for (var lon = UL_From.y; lon <= DR_To.y; lon += step.x)
                {
                    int Posx = Mathf.FloorToInt((lat) / step.x + 1);
                    int Posy = Mathf.FloorToInt((lon) / step.y - 6);

                    var TileNumber = $"srtm_{Posx:00}_{Posy:00}";

                    var ZipedTilePath = Path.Combine(ZipFolder, TileNumber + ".zip");

                    var ZipedTileUrl = urlDEM + TileNumber + ".zip";

                    var filedata = new RequestedFileData(ZipedTileUrl, ZipedTilePath, GISDataDownloaderDownloadType.data, "DEM", 200000000);

                    urls.Add(filedata);
                }
            }

            return urls;
        }
        public List<RequestedFileData> GetDEMURLs_SRTM30()
        {
            var urls = new List<RequestedFileData>();

            var ZipFolder = MainPath + "/" + Prefs.DEMSource.ToString() + "_Zip";

            if (!Directory.Exists(ZipFolder))
                Directory.CreateDirectory(ZipFolder);

            int sx = (int)Math.Floor(Prefs.UpperLeftCoordiante.x + 180) - 180;
            int sy = (int)Math.Floor(Prefs.UpperLeftCoordiante.y + 90) - 90;
            int ex = (int)Math.Floor(Prefs.DownRightCoordiante.x + 180) - 180;
            int ey = (int)Math.Floor(Prefs.DownRightCoordiante.y + 90) - 90;

            for (int x = sx; x <= ex; x++)
            {
                for (int y = sy; y >= ey; y--)
                {
                    var x1 = x;
                    var x2 = x + 1;

                    var y1 = y + 1;
                    var y2 = y;

                    var Mercat_P1 = TerrainStreamingGeoConversion.LatLongToMercat(x1, y1);
                    var Mercat_P2 = TerrainStreamingGeoConversion.LatLongToMercat(x2, y2);


                    string ax = x1 < 0 ? "W" : "E";
                    int absX = Mathf.Abs(x1);
                    if (absX < 100) ax += "0";
                    if (absX < 10) ax += "0";
                    ax += absX;

                    string ay = y2 < 0 ? "S" : "N";
                    int absY = Mathf.Abs(y2);
                    if (absY < 10) ay += "0";
                    ay += absY;

                    var TileNumber = String.Format("{1}{0}", ax, ay);

                    var ZipedTilePath = Path.Combine(ZipFolder, TileNumber + ".zip");

                    string ZipedTileUrl = urlDEM + ay.ToUpper() + ax.ToUpper() + ".SRTMGL1.hgt.zip";

                    var filedata = new RequestedFileData(ZipedTileUrl, ZipedTilePath, GISDataDownloaderDownloadType.data, "DEM", 200000000);

                    urls.Add(filedata);

                }
            }

            return urls;
        }
        public async Task StartDEMDownloading(List<RequestedFileData> filesData, CancellationTokenSource m_taskSource)
        {
            taskSource = m_taskSource;
            WebDownloader = new TerrainStreamingWebDownloader("Download DEMs ZIP", true, true, GISDataDownloaderTotalFileSize.BySize);
            WebDownloader.ReplaceExisitingFiles = OptionEnabDisab.Disable;

            if (Prefs.DEMSource == GISDataDownloaderDEMProvider.Mapbox)
                WebDownloader.useFileSize = false;

            if (Prefs.DEMSource == GISDataDownloaderDEMProvider.SRTM_30m)
            {
                WebDownloader.NeedAutho = true;
                WebDownloader.User = Prefs.User;
                WebDownloader.Pass = Prefs.Pass;
            }

            WebDownloader.AddRange(filesData);

            try
            {
                await WebDownloader.StartDownloading(taskSource).CancelWith(taskSource.Token);
                GC.Collect();
            }
            catch (OperationCanceledException)
            {
                taskSource.Cancel();
                throw;
            }
        }
        public async Task ExtractDEMsZip(CancellationTokenSource taskSource)
        {

            taskSource = new CancellationTokenSource();

            try
            {
                var ZipFolder = MainPath + "/" + Prefs.DEMSource.ToString() + "_Zip";
                var UnzipedFolder = MainPath + "/" + Prefs.DEMSource.ToString() + "_UnZiped";

                Vector2Int step = new Vector2Int(0, 0);
 
                var TotalProc = (((DR_To.x - UL_From.x) / step.x + 1) * ((DR_To.y - UL_From.y) / step.y + 1));

                var activeId = 0;

                if (Prefs.DEMSource == GISDataDownloaderDEMProvider.SRTM_90m)
                {
                    step = TerrainStreamingProvidersData.srtm_90_tiff_LatLon_Step;
                    TotalProc = (((DR_To.x - UL_From.x) / step.x + 1) * ((DR_To.y - UL_From.y) / step.y + 1));

                    if (!Directory.Exists(UnzipedFolder))
                        Directory.CreateDirectory(UnzipedFolder);

                    for (var lat = UL_From.x; lat <= DR_To.x; lat += step.y)
                    {
                        for (var lon = UL_From.y; lon <= DR_To.y; lon += step.x)
                        {
                            activeId++;

                            var prog = activeId * 100 / TotalProc;

                            int Posx = Mathf.FloorToInt((lat) / step.x + 1);
                            int Posy = Mathf.FloorToInt((lon) / step.y - 6);

                            var TileNumber = $"srtm_{Posx:00}_{Posy:00}";
                            NumberRequestedZip++;
                            var ZipedTilePath = Path.Combine(ZipFolder, TileNumber + ".zip");
                            var UnzipedTilePath = Path.Combine(UnzipedFolder, TileNumber + FileExtension);

                            if (!File.Exists(UnzipedTilePath))
                            {
                                await Task.Run(() =>
                                {
                                    if (OnDownloadProgressChanged != null)
                                        OnDownloadProgressChanged("Extracting Zip ", (int)(prog));

                                    TerrainStreamingZipUtil.Unzip(ZipedTilePath, UnzipedFolder);
                                }, taskSource.Token).CancelWith(taskSource.Token);

                            }

                        }
                    }
                }

                if (Prefs.DEMSource == GISDataDownloaderDEMProvider.SRTM_30m)
                {
                    step = TerrainStreamingProvidersData.srtm_30_hgt_LatLon_Step;

                    TotalProc = (((DR_To.x - UL_From.x) / step.x + 1) * ((DR_To.y - UL_From.y) / step.y + 1));

                    if (!Directory.Exists(UnzipedFolder))
                        Directory.CreateDirectory(UnzipedFolder);

                    int sx = (int)Math.Floor(Prefs.UpperLeftCoordiante.x + 180) - 180;
                    int sy = (int)Math.Floor(Prefs.UpperLeftCoordiante.y + 90) - 90;
                    int ex = (int)Math.Floor(Prefs.DownRightCoordiante.x + 180) - 180;
                    int ey = (int)Math.Floor(Prefs.DownRightCoordiante.y + 90) - 90;

                    for (int x = sx; x <= ex; x++)
                    {
                        for (int y = sy; y >= ey; y--)
                        {
                            activeId++;

                            var prog = activeId * 100 / TotalProc;

                            var x1 = x;
                            var x2 = x + 1;

                            var y1 = y + 1;
                            var y2 = y;

                            var Mercat_P1 = TerrainStreamingGeoConversion.LatLongToMercat(x1, y1);
                            var Mercat_P2 = TerrainStreamingGeoConversion.LatLongToMercat(x2, y2);


                            string ax = x1 < 0 ? "W" : "E";
                            int absX = Mathf.Abs(x1);
                            if (absX < 100) ax += "0";
                            if (absX < 10) ax += "0";
                            ax += absX;

                            string ay = y2 < 0 ? "S" : "N";
                            int absY = Mathf.Abs(y2);
                            if (absY < 10) ay += "0";
                            ay += absY;

                            var TileNumber = String.Format("{1}{0}", ax, ay);

                            var ZipedTilePath = Path.Combine(ZipFolder, TileNumber + ".zip");

                            var UnzipedTilePath = Path.Combine(UnzipedFolder, TileNumber + FileExtension);

                            NumberRequestedZip++;

                            if (!File.Exists(UnzipedTilePath))
                            {
                                await Task.Run(() =>
                                {
                                    if (OnDownloadProgressChanged != null)
                                        OnDownloadProgressChanged("Extracting Zip ", (int)(prog));

                                    TerrainStreamingZipUtil.Unzip(ZipedTilePath, UnzipedFolder);
                                }, taskSource.Token).CancelWith(taskSource.Token);

                            }

                        }
                    }


                }
                GC.Collect();
            }
            catch (OperationCanceledException)
            {
                taskSource.Cancel();
                throw;
            }
        }
        public async Task GenerateDEMTilesTasks(CancellationTokenSource m_taskSource)
        {
            if (taskSource == null)
                taskSource = m_taskSource;

            try
            {
                switch (Prefs.DEMSource)
                {
                    case GISDataDownloaderDEMProvider.SRTM_90m:

                        try
                        {
                                await ReadHeightMapSRTM90Tiff(urlStep, false, taskSource).CancelWith(taskSource.Token);

                        }
                        catch
                        {

                            if (OnError != null)
                                OnError();
                        }
                        break;

                    case GISDataDownloaderDEMProvider.SRTM_30m:
                        try
                        {
                            await ReadHeightMapSRTM30HGT(urlStep, taskSource).CancelWith(taskSource.Token);
                        }
                        catch
                        {

                            if (OnError != null)
                                OnError();
                        }
                        break;
                    case GISDataDownloaderDEMProvider.Mapbox:
                        try
                        {
                            await ReadHeightMapMapBoxPNGRAW(urlStep, taskSource).CancelWith(taskSource.Token);

                        }
                        catch
                        {

                            if (OnError != null)
                                OnError();
                        }
                        break;
                }
                GC.Collect();
            }
            catch (OperationCanceledException)
            {
                taskSource.Cancel();
                taskSource.Token.ThrowIfCancellationRequested();
                throw;
            }

        }
        private async Task ReadHeightMapSRTM90Tiff(Vector2Int step,bool Horizon, CancellationTokenSource m_taskSource)
        {
            try
            {
                if (taskSource == null)
                    taskSource = m_taskSource;

                var UnzipedFolder = TerrainStreamingParameters.srtm_90_tiff_temp_path + "/" + Prefs.DEMSource.ToString() + "_UnZiped"; ;

                TotalProc = (((DR_To.x - UL_From.x) / step.x + 1) * ((DR_To.y - UL_From.y) / step.y + 1));

                var count = 0;

                for (var lat = UL_From.x; lat <= DR_To.x; lat += step.y)
                {
                    for (var lon = UL_From.y; lon <= DR_To.y; lon += step.x)
                    {
                        count++;

                        var prog = count * 100 / TotalProc;

                        int Posx = Mathf.FloorToInt((lat) / step.x + 1);
                        int Posy = Mathf.FloorToInt((lon) / step.y - 6);

                        var TileNumber = $"srtm_{Posx:00}_{Posy:00}";

                        var UnzipedTilePath = Path.Combine(UnzipedFolder, TileNumber + FileExtension);

                        if (File.Exists(UnzipedTilePath))
                        {
                            TileCout++;

                            var TiffReader = new TerrainStreamingTiffILoader();

                            if (Prefs.State != DownloaderState.idle)
                                await TiffReader.LoadTiff(UnzipedTilePath, TileCout, NumberRequestedZip, taskSource).CancelWith(taskSource.Token);

                            if (TiffReader.LoadComplet)
                            {
                                TiffReader.data.GetDetails();
                                LoadedFiles.Add(TiffReader.data);
                                TiffReader.LoadComplet = false;
                            }
                        }
                    }
                }
                TileCout = 0;
 
                if (Prefs.State != DownloaderState.idle)
                {
                    if (Horizon)
                        await GenerateHeightHorizonMapTiles(taskSource).CancelWith(taskSource.Token);
                    else
                        await GenerateHeightMapTiles(taskSource).CancelWith(taskSource.Token);
                }

            }
            catch (OperationCanceledException)
            {
                taskSource.Cancel();
                throw;
            }
        }
        private static async Task GenerateHeightHorizonMapTiles(CancellationTokenSource taskSource)
        {
            try
            {
                var Hr_Maindata = new TerrainStreamingFileData();

                Hr_Maindata.UpperRightCoordinate = UpperRightCoordiante;
                Hr_Maindata.UpperLeftCoordinate = Prefs.UpperLeftCoordiante;
                Hr_Maindata.BottomRightCoordiante = Prefs.DownRightCoordiante;
                Hr_Maindata.BottomLeftCoordinate = DownLeftCoordiante;

                Vector2 Hr_TotalZoneMinMax = new Vector2(0, 0);

                if (LoadedFiles.Count > 0)
                {
                    var Total_size_col = 0;
                    var Total_size_row = 0;


                    var cellsize_x = Math.Abs(LoadedFiles[0].cellsize_x);
                    var cellsize_y = Math.Abs(LoadedFiles[0].cellsize_y);

                    Total_size_col = Mathf.FloorToInt((float)((Prefs.DownRightCoordiante.x - DownLeftCoordiante.x) / cellsize_x));
                    Total_size_row = Mathf.FloorToInt((float)((UpperRightCoordiante.y - DownLeftCoordiante.y) / cellsize_y));

                    totaldataHeightmap = new float[Total_size_row, Total_size_col];

                    Hr_Maindata.mapSize_row = Total_size_row;
                    Hr_Maindata.mapSize_col = Total_size_col;

                    int x_pos = 0;
                    int y_pos = -1;

                    Hr_TotalZoneMinMax = GetMinMaxForTotalZone(LoadedFiles);

                    int c = 0;

                    for (double y = Prefs.UpperLeftCoordiante.y; y >= Prefs.DownRightCoordiante.y; y = y - cellsize_y)
                    {
                        y_pos++;

                        c++;

                        for (double x = Prefs.UpperLeftCoordiante.x; x <= UpperRightCoordiante.x; x = x + cellsize_x)
                        {
                            var pos = new DVector2(x, y);

                            foreach (var file in LoadedFiles)
                            {

                                if (file.Contains(x, y))
                                {
                                    var el = file.GetElevation(pos);

                                    if (y_pos >= totaldataHeightmap.GetLength(0))
                                        y_pos = Total_size_row - 1;

                                    if (x_pos >= totaldataHeightmap.GetLength(1))
                                        x_pos = Total_size_col - 1;

                                    totaldataHeightmap[y_pos, x_pos] = el;

                                    if (el < Hr_Maindata.MinElevation)
                                        Hr_Maindata.MinElevation = el;

                                    if (el > Hr_Maindata.MaxElevation)
                                        Hr_Maindata.MaxElevation = el;
                                }
                            }
                            x_pos++;
                        }
                        x_pos = 0;

                    }

                    Hr_Maindata.cellsize_x = cellsize_x;
                    Hr_Maindata.cellsize_y = cellsize_y;
                }


                Hr_Maindata.MinElevation = Hr_TotalZoneMinMax.x;
                Hr_Maindata.MaxElevation = Hr_TotalZoneMinMax.y;

                var p1 = new DVector2(Hr_Maindata.UpperRightCoordinate.x, Hr_Maindata.BottomLeftCoordinate.y);
                var p2 = new DVector2(Hr_Maindata.BottomLeftCoordinate.x, Hr_Maindata.UpperRightCoordinate.y);

                Hr_Maindata.Terrain_Dimension.x = TerrainStreamingGeoConversion.Getdistance(Hr_Maindata.BottomLeftCoordinate.y, Hr_Maindata.BottomLeftCoordinate.x, p1.y, p1.x);
                Hr_Maindata.Terrain_Dimension.y = TerrainStreamingGeoConversion.Getdistance(Hr_Maindata.BottomLeftCoordinate.y, Hr_Maindata.BottomLeftCoordinate.x, p2.y, p2.x);


                Hr_Maindata.floatheightData = totaldataHeightmap;

                var SaveFolder = Prefs.DownloadPath + "/HorizonDEMData/";

                if (!Directory.Exists(SaveFolder))
                    Directory.CreateDirectory(SaveFolder);

                await TerrainStreamingWorldGenerator.SaveDEMTiles(taskSource, Prefs, Prefs.Hr_StreamingGridCount, SaveFolder, Hr_Maindata, TotalZoneMinMax, true, false).CancelWith(taskSource.Token); ;

            }
            catch (OperationCanceledException)
            {
                taskSource.Cancel();
                throw;
            }
}

        private async Task ReadHeightMapSRTM30HGT(Vector2Int step, CancellationTokenSource taskSource)
        {
            var UnzipedFolder = TerrainStreamingParameters.srtm_30_hgt_temp_path + "/" + Prefs.DEMSource.ToString() + "_UnZiped"; ;

            TotalProc = (((DR_To.x - UL_From.x) / step.x + 1) * ((DR_To.y - UL_From.y) / step.y + 1));

            var count = 0;

            int sx = (int)Math.Floor(Prefs.UpperLeftCoordiante.x + 180) - 180;
            int sy = (int)Math.Floor(Prefs.UpperLeftCoordiante.y + 90) - 90;
            int ex = (int)Math.Floor(Prefs.DownRightCoordiante.x + 180) - 180;
            int ey = (int)Math.Floor(Prefs.DownRightCoordiante.y + 90) - 90;

            for (int x = sx; x <= ex; x++)
            {
                for (int y = sy; y >= ey; y--)
                {
                    count++;

                    var prog = count * 100 / TotalProc;

                    var x1 = x;
                    var x2 = x + 1;

                    var y1 = y + 1;
                    var y2 = y;

                    var Mercat_P1 = TerrainStreamingGeoConversion.LatLongToMercat(x1, y1);
                    var Mercat_P2 = TerrainStreamingGeoConversion.LatLongToMercat(x2, y2);


                    string ax = x1 < 0 ? "W" : "E";
                    int absX = Mathf.Abs(x1);
                    if (absX < 100) ax += "0";
                    if (absX < 10) ax += "0";
                    ax += absX;

                    string ay = y2 < 0 ? "S" : "N";
                    int absY = Mathf.Abs(y2);
                    if (absY < 10) ay += "0";
                    ay += absY;

                    var TileNumber = String.Format("{1}{0}", ax, ay);

                    var UnzipedTilePath = Path.Combine(UnzipedFolder, TileNumber + FileExtension);

                    if (File.Exists(UnzipedTilePath))
                    {
                        TileCout++;

                        var hgtReader = new TerrainStreamingHGTLoader();

                        if (Prefs.State != DownloaderState.idle)
                            await hgtReader.LoadFloatGrid(UnzipedTilePath, TileCout, NumberRequestedZip, taskSource).CancelWith(taskSource.Token);

                        if (hgtReader.LoadComplet)
                        {
                            hgtReader.data.GetDetails();
                            LoadedFiles.Add(hgtReader.data);
                        }
                    }

                }
            }

            TileCout = 0;
            if (Prefs.State != DownloaderState.idle)
                await GenerateHeightMapTiles(taskSource).CancelWith(taskSource.Token); ;
        }
        private async Task ReadHeightMapMapBoxPNGRAW(Vector2Int step, CancellationTokenSource m_taskSource)
        {
            var UnzipedFolder = TerrainStreamingParameters.Mapbox_pngraw_temp_path ;

            try
            {
                if (taskSource == null)
                    taskSource = m_taskSource;
 
                TotalProc = (((DR_To.x - UL_From.x) / step.x + 1) * ((DR_To.y - UL_From.y) / step.y + 1));

                int zoom = TerrainStreamingPngRawLoader.GetZoomLevel(Prefs.UpperLeftCoordiante, Prefs.DownRightCoordiante, Prefs.StreamingGridCount, Prefs.HeightmapResolution);

                var UpperLeftTilePos = TerrainStreamingGeoConversion.GetLatLongToTile(Prefs.UpperLeftCoordiante.x, Prefs.UpperLeftCoordiante.y, zoom).ToIntVector();
                var DownRightTilePos = TerrainStreamingGeoConversion.GetLatLongToTile(Prefs.DownRightCoordiante.x, Prefs.DownRightCoordiante.y, zoom).ToIntVector(); ;

                DVector2 P1 = TerrainStreamingGeoConversion.GetLatLongToTile(UpperLeftTilePos.x, UpperLeftTilePos.y, zoom);
                DVector2 P2 = TerrainStreamingGeoConversion.GetLatLongToTile(DownRightTilePos.x + 1, DownRightTilePos.y + 1, zoom);

                TerrainStreamingGeoConversion.LatLongToMercat(ref P1.x, ref P1.y);
                TerrainStreamingGeoConversion.LatLongToMercat(ref P2.x, ref P2.y);

                int TotalMapWidth = (DownRightTilePos.x - UpperLeftTilePos.x + 1) * 256;
                int TotalMapHeight = (DownRightTilePos.y - UpperLeftTilePos.y + 1) * 256;

                for (int tx = UpperLeftTilePos.x; tx <= DownRightTilePos.x; tx++)
                {
                    for (int ty = UpperLeftTilePos.y; ty <= DownRightTilePos.y; ty++)
                    {
                        string TileName = zoom + "x" + tx + "x" + ty + ".pngraw";

                        var TilePath = Path.Combine(UnzipedFolder, TileName);

                        if (File.Exists(TilePath))
                        {
                            TileCout++;

                            var PngRawReader = new TerrainStreamingPngRawLoader();
                            
                            if (Prefs.State != DownloaderState.idle)
                                await PngRawReader.LoadFile(TilePath, TileCout, NumberRequestedZip, taskSource).CancelWith(taskSource.Token);

                            if (PngRawReader.LoadComplet)
                            {
                                PngRawReader.data.GetDetails();
                                LoadedFiles.Add(PngRawReader.data);
                                PngRawReader.LoadComplet = false;
                            }
                        }
                       

                    }
                }
            }
            catch (OperationCanceledException)
            {
                taskSource.Cancel();
                throw;
            }

            TileCout = 0;
            if (Prefs.State != DownloaderState.idle)
                await GenerateHeightMapTiles(taskSource).CancelWith(taskSource.Token); 
        }

        private async Task GenerateHeightMapTiles(CancellationTokenSource m_taskSource)
        {
            try
            {
                taskSource = m_taskSource;

                var Maindata = new TerrainStreamingFileData();

                Maindata.UpperRightCoordinate = UpperRightCoordiante;
                Maindata.UpperLeftCoordinate = Prefs.UpperLeftCoordiante;
                Maindata.BottomRightCoordiante = Prefs.DownRightCoordiante;
                Maindata.BottomLeftCoordinate = DownLeftCoordiante;

                if (LoadedFiles.Count > 0)
                {
                    var Total_size_col = 0;
                    var Total_size_row = 0;


                    var cellsize_x = Math.Abs(LoadedFiles[0].cellsize_x);
                    var cellsize_y = Math.Abs(LoadedFiles[0].cellsize_y);

                    Total_size_col = Mathf.FloorToInt((float)((Prefs.DownRightCoordiante.x - DownLeftCoordiante.x) / cellsize_x));
                    Total_size_row = Mathf.FloorToInt((float)((UpperRightCoordiante.y - DownLeftCoordiante.y) / cellsize_y));

                    totaldataHeightmap = new float[Total_size_row, Total_size_col];


                    Maindata.mapSize_row = Total_size_row;
                    Maindata.mapSize_col = Total_size_col;

                    int x_pos = 0;
                    int y_pos = -1;

                    TotalZoneMinMax = GetMinMaxForTotalZone(LoadedFiles);

                    int c = 0;

                    for (double y = Prefs.UpperLeftCoordiante.y; y >= Prefs.DownRightCoordiante.y; y = y - cellsize_y)
                    {

                        y_pos++;

                        c++;

                        for (double x = Prefs.UpperLeftCoordiante.x; x <= UpperRightCoordiante.x; x = x + cellsize_x)
                        {
                            var pos = new DVector2(x, y);

                            foreach (var file in LoadedFiles)
                            {
                                if (file.Contains(x, y))
                                {
                                    var el = file.GetElevation(pos);


                                    if (y_pos >= totaldataHeightmap.GetLength(0))
                                        y_pos = Total_size_row - 1;

                                    if (x_pos >= totaldataHeightmap.GetLength(1))
                                        x_pos = Total_size_col - 1;

                                    totaldataHeightmap[y_pos, x_pos] = el;

                                    if (el < Maindata.MinElevation)
                                        Maindata.MinElevation = el;

                                    if (el > Maindata.MaxElevation)
                                        Maindata.MaxElevation = el;
                                }
                            }
                            x_pos++;
                        }
                        x_pos = 0;

                    }

                    Maindata.cellsize_x = cellsize_x;
                    Maindata.cellsize_y = cellsize_y;
                }

                Maindata.MinElevation = TotalZoneMinMax.x;
                Maindata.MaxElevation = TotalZoneMinMax.y;

                var p1 = new DVector2(Maindata.UpperRightCoordinate.x, Maindata.BottomLeftCoordinate.y);
                var p2 = new DVector2(Maindata.BottomLeftCoordinate.x, Maindata.UpperRightCoordinate.y);

                Maindata.Terrain_Dimension.x = TerrainStreamingGeoConversion.Getdistance(Maindata.BottomLeftCoordinate.y, Maindata.BottomLeftCoordinate.x, p1.y, p1.x);
                Maindata.Terrain_Dimension.y = TerrainStreamingGeoConversion.Getdistance(Maindata.BottomLeftCoordinate.y, Maindata.BottomLeftCoordinate.x, p2.y, p2.x);


                Maindata.floatheightData = totaldataHeightmap;

                var SaveFolder = Prefs.DownloadPath + "/DEMData/";

                if (!Directory.Exists(SaveFolder))
                    Directory.CreateDirectory(SaveFolder);

                if(Prefs.State != DownloaderState.idle)
                await TerrainStreamingWorldGenerator.SaveDEMTiles(taskSource,Prefs, Prefs.StreamingGridCount, SaveFolder, Maindata, TotalZoneMinMax,false, GenerateWorldPreview).CancelWith(taskSource.Token);

            }
            catch (OperationCanceledException)
            {
                taskSource.Cancel();
                throw;
            }
        }
        private static Vector2 GetMinMaxForTotalZone(List<TerrainStreamingFileData> files)
        {
            Vector2 MinMax = new Vector2(9000, -9000);

            foreach (var file in files)
            {
                var min = file.MinElevation;
                if (min < MinMax.x) MinMax.x = min;

                var max = file.MaxElevation;
                if (max > MinMax.y) MinMax.y = max;

            }
            return MinMax;
        }
        #endregion
        #region Horizon
 
        public async Task GenerateHorizonTiles(CancellationTokenSource m_taskSource)
        {
            taskSource = m_taskSource;

            await ReadHeightMapSRTM90Tiff(TerrainStreamingProvidersData.srtm_90_tiff_LatLon_Step, true, taskSource).CancelWith(taskSource.Token);


        }
        #endregion
        #region Raster
        public async Task StartRasterDownloading(List<List<RequestedFileData>> totalLists, CancellationTokenSource m_taskSource)
        {
            taskSources = new List<CancellationTokenSource>();
            taskSource = m_taskSource;

            RasterDownloaders = new TerrainStreamingMultiWebDownloader("Download Raster ");

            var Downloaders = new List<TerrainStreamingWebDownloader>();

            foreach (var subList in totalLists)
            {
                var RasterDownloader = new TerrainStreamingWebDownloader("Download Raster ", false, false, GISDataDownloaderTotalFileSize.ByNumber);
                RasterDownloader.AddRange(subList);
                RasterDownloader.parentDownloader = RasterDownloaders;
                Downloaders.Add(RasterDownloader);
                taskSources.Add(new CancellationTokenSource());
            }

            RasterDownloaders.WebDownloaders = Downloaders;

            try
            {
                await Task.WhenAll(RasterDownloaders.WebDownloaders.Select(i => i.StartDownloading(taskSource).CancelWith(taskSources[RasterDownloaders.WebDownloaders.IndexOf(i)].Token))).CancelWith(taskSource.Token);
                GC.Collect();
            }
            catch (OperationCanceledException)
            {
                taskSource.Cancel();
                throw;
            }
        }
        public static int Tab_index = 0;
        public async Task<List<RequestedFileData>> GetRasterURLs(List<TerrainStreamingTileData> tiles, int Zoom, string m_Path, CancellationTokenSource m_taskSource)
        {
            if (taskSource == null)
                taskSource = m_taskSource;

                var urls = new List<RequestedFileData>();

            var Raster_DownloadPath = Prefs.DownloadPath + m_Path;

            if (!Directory.Exists(Raster_DownloadPath))
                Directory.CreateDirectory(Raster_DownloadPath);

            switch (Prefs.MapSource)
            {
                case GISMapSource.ArcGIS:

                    string BaseUrl = "https://services.arcgisonline.com/arcgis/rest/services/";
                    string MapType = Prefs.ArcGISType.GetDescription();

                    TotalWidth = 0; TotalHeight = 0;

                    int c = 0;

                    foreach (var tile in tiles)
                    {

                        string TileName = string.Format("Tile__{0}__{1}", tile.Number.x, tile.Number.y);
                        string localFilename = Path.Combine(Raster_DownloadPath, TileName + ".jpg");

                        var TL = tile.UpperLeftCoordinate;
                        var DR = tile.BottomRightCoordiante;

                        int minpixelX, minpixelY, maxpixelX, maxpixelY;

                        TerrainStreamingGeoConversion.LatLongToPixelXY(TL.y, TL.x, Zoom, out minpixelX, out minpixelY);
                        TerrainStreamingGeoConversion.LatLongToPixelXY(DR.y, DR.x, Zoom, out maxpixelX, out maxpixelY);

                        float width = maxpixelX - minpixelX; ;
                        float hight = maxpixelY - minpixelY;

                        if (c < Prefs.StreamingGridCount.x)
                            TotalWidth += width;
                        if (c < Prefs.StreamingGridCount.y)
                            TotalHeight += hight;


                        string fullUrl = BaseUrl +MapType+ "/MapServer/export?bbox=" + TL.x.ToString().Replace(",", ".") + "," + TL.y.ToString().Replace(",", ".") + "," + DR.x.ToString().Replace(",", ".") + "," + DR.y.ToString().Replace(",", ".") + "&bboxSR=4326&size=" + width + ',' + hight + "&f=image";


                        var filedata = new RequestedFileData(fullUrl, localFilename, GISDataDownloaderDownloadType.data, "RASTER", 200000000);

                        urls.Add(filedata);
 
                        if (Prefs.GenerateTabFiles == OptionEnabDisab.Enable)
                        {
                            var tProg = c * 100 / (tiles.Count);

                            if (OnDownloadProgressChanged != null)
                                OnDownloadProgressChanged("Generating Tab Files ", tProg);

                            await Task.Delay(TimeSpan.FromSeconds(0.001)).CancelWith(taskSource.Token);

                            if (Prefs.State != DownloaderState.idle)
                                await GenerateTabFile(tile, Raster_DownloadPath, width, hight, taskSource).CancelWith(taskSource.Token);

                        }

                        c++;
                    }

                    break;
                case GISMapSource.Mapbox:

                     BaseUrl = "https://api.mapbox.com/styles/v1/mapbox/";
                     MapType = Prefs.MapBoxType.GetDescription();
                    string Key = Prefs.MapBoxKey;

                    bool showlogo = false; bool showAttribution = false;
                    if (Prefs.ShowLogo == OptionEnabDisab.Enable)
                        showlogo = true;
                    if (Prefs.ShowAttribution == OptionEnabDisab.Enable)
                        showAttribution = true;

                    TotalWidth = 0; TotalHeight = 0;

                     c = 0;

                    foreach (var tile in tiles)
                    {

                        string TileName = string.Format("Tile__{0}__{1}", tile.Number.x, tile.Number.y);
                        string localFilename = Path.Combine(Raster_DownloadPath, TileName + ".jpg");

                        var TL = tile.UpperLeftCoordinate;
                        var DR = tile.BottomRightCoordiante;


                        int minpixelX, minpixelY, maxpixelX, maxpixelY;

                        TerrainStreamingGeoConversion.LatLongToPixelXY(TL.y, TL.x, Zoom, out minpixelX, out minpixelY);
                        TerrainStreamingGeoConversion.LatLongToPixelXY(DR.y, DR.x, Zoom, out maxpixelX, out maxpixelY);

                        float width = maxpixelX - minpixelX; ;
                        float hight = maxpixelY - minpixelY;

                        if (c < Prefs.StreamingGridCount.x)
                            TotalWidth += width;
                        if (c < Prefs.StreamingGridCount.y)
                            TotalHeight += hight;

                        int buffer = 512;

                        DVector2 center = new DVector2(0, 0);
                        center.y = (DR.y + TL.y) / 2;
                        center.x = (DR.x + TL.x) / 2;

                        double zoom1 = 0, zoom2 = 0;

                        if (DR.x != TL.x && DR.y != TL.y)
                        {
                            zoom1 = Math.Log(360.0 / 256.0 * (width - 2 * buffer) / (DR.x - TL.x)) / Math.Log(2);
                            zoom2 = Math.Log(180.0 / 256.0 * (hight - 2 * buffer) / (DR.y - TL.y)) / Math.Log(2);
                        }
                        var zoomLevel = (zoom1 < zoom2) ? zoom1 : zoom2;

                        var FullUrl = BaseUrl + MapType + "/static/" + center.x.ToString().Replace(',', '.') + "," + center.y.ToString().Replace(',', '.') + "," + (Zoom - 1) + "/" + width + "x" + hight + "?access_token=" + Key + "&logo=" + showlogo.ToString().ToLower() + "&attribution=" + showAttribution.ToString().ToLower();

                        var filedata = new RequestedFileData(FullUrl, localFilename, GISDataDownloaderDownloadType.data, "RASTER", 200000000);

                        urls.Add(filedata);

                        if (Prefs.GenerateTabFiles == OptionEnabDisab.Enable)
                        {
                            var tProg = c * 100 / (tiles.Count);

                            if (OnDownloadProgressChanged != null)
                                OnDownloadProgressChanged("Generating Tab Files ", tProg);

                            await Task.Delay(TimeSpan.FromSeconds(0.001)).CancelWith(taskSource.Token);

                            if (Prefs.State != DownloaderState.idle)
                                await GenerateTabFile(tile, Raster_DownloadPath, width, hight, taskSource).CancelWith(taskSource.Token);

                        }
                        c++;
                    }
                    break;
                case GISMapSource.Bingmaps:

                    Key = Prefs.BingKey;
                    BaseUrl = "http://dev.virtualearth.net/REST/v1/Imagery/Map/";
                    MapType = Prefs.BingmapType.ToString();
                    TotalWidth = 0; TotalHeight = 0;

                    c = 0;

                    foreach (var tile in tiles)
                    {

                        string TileName = string.Format("Tile__{0}__{1}", tile.Number.x, tile.Number.y);
                        string localFilename = Path.Combine(Raster_DownloadPath, TileName + ".jpg");

                        var TL = tile.UpperLeftCoordinate;
                        var DR = tile.BottomRightCoordiante;


                        int minpixelX, minpixelY, maxpixelX, maxpixelY;

                        TerrainStreamingGeoConversion.LatLongToPixelXY(TL.y, TL.x, Zoom, out minpixelX, out minpixelY);
                        TerrainStreamingGeoConversion.LatLongToPixelXY(DR.y, DR.x, Zoom, out maxpixelX, out maxpixelY);

                        float width = maxpixelX - minpixelX; ;
                        float hight = maxpixelY - minpixelY;

                        if (c < Prefs.StreamingGridCount.x)
                            TotalWidth += width;
                        if (c < Prefs.StreamingGridCount.y)
                            TotalHeight += hight;

                        int buffer = 512;

                        DVector2 center = new DVector2(0, 0);
                        center.y = (DR.y + TL.y) / 2;
                        center.x = (DR.x + TL.x) / 2;

                        double zoom1 = 0, zoom2 = 0;

                        if (DR.x != TL.x && DR.y != TL.y)
                        {
                            zoom1 = Math.Log(360.0 / 256.0 * (width - 2 * buffer) / (DR.x - TL.x)) / Math.Log(2);
                            zoom2 = Math.Log(180.0 / 256.0 * (hight - 2 * buffer) / (DR.y - TL.y)) / Math.Log(2);
                        }
                        var zoomLevel = (zoom1 < zoom2) ? zoom1 : zoom2;

                        var FullUrl = BaseUrl + MapType + "/" + center.y.ToString().Replace(',', '.') + "," + center.x.ToString().Replace(',', '.') + "/" + Zoom + "?&mapSize=" + width + "," + hight + "&key=" + Key;

                        var filedata = new RequestedFileData(FullUrl, localFilename, GISDataDownloaderDownloadType.data, "RASTER", 200000000);

                        urls.Add(filedata);

                        if (Prefs.GenerateTabFiles == OptionEnabDisab.Enable)
                        {
                            var tProg = c * 100 / (tiles.Count);

                            if (OnDownloadProgressChanged != null)
                                OnDownloadProgressChanged("Generating Tab Files ", tProg);

                            await Task.Delay(TimeSpan.FromSeconds(0.001)).CancelWith(taskSource.Token);

                            if (Prefs.State != DownloaderState.idle)
                                await GenerateTabFile(tile, Raster_DownloadPath, width, hight, taskSource).CancelWith(taskSource.Token);

                        }
                        c++;
                    }
                    break;
            }


            GetOptimizedInputs((int)TotalWidth, (int)TotalHeight, true);

            return urls;
        }
        public async Task DownloadRasterData(List<RequestedFileData> filesData, OptionEnabDisab ReplaceExisitingRaster, CancellationTokenSource m_taskSource)
        {
            taskSource = m_taskSource;
            WebDownloader = new TerrainStreamingWebDownloader("Download Raster Data", true, false, GISDataDownloaderTotalFileSize.ByNumber);
            WebDownloader.AddRange(filesData);
            WebDownloader.ReplaceExisitingFiles = ReplaceExisitingRaster;

            try
            {
                await WebDownloader.StartDownloading(taskSource).CancelWith(taskSource.Token);
                GC.Collect();
            }
            catch (OperationCanceledException)
            {
                taskSource.Cancel();
                throw;
            }
        }
        public async Task GeneratePreviewZone(CancellationTokenSource m_taskSource)
        {
            taskSource = m_taskSource;

            var PreviewZone_DownloadPath = Prefs.DownloadPath + "/PreviewZone";

            if (!Directory.Exists(PreviewZone_DownloadPath))
                Directory.CreateDirectory(PreviewZone_DownloadPath);

            string BaseUrl = "https://services.arcgisonline.com/arcgis/rest/services/World_Imagery/MapServer/export?bbox=";

            var TL = Prefs.UpperLeftCoordiante;
            var DR = Prefs.DownRightCoordiante;

            var DL = new DVector2(TL.x, DR.y);
            var TR = new DVector2(DR.x, TL.y);

            int minpixelX, minpixelY, maxpixelX, maxpixelY;

            TerrainStreamingGeoConversion.LatLongToPixelXY(TL.y, TL.x, 12, out minpixelX, out minpixelY);
            TerrainStreamingGeoConversion.LatLongToPixelXY(DR.y, DR.x, 12, out maxpixelX, out maxpixelY);

            float Totalwidth = maxpixelX - minpixelX;
            float TotalHeight = maxpixelY - minpixelY;



            string fullUrl = BaseUrl + TL.x.ToString().Replace(",", ".") + "," + TL.y.ToString().Replace(",", ".") + "," + DR.x.ToString().Replace(",", ".") + "," + DR.y.ToString().Replace(",", ".") + "&bboxSR=4326&size=" + Totalwidth + ',' + TotalHeight + "&f=image";
            string localFilename = PreviewZone_DownloadPath + "/Zone.jpg";

            var filedata = new RequestedFileData(fullUrl, localFilename, GISDataDownloaderDownloadType.data, "RASTER", 200000000);
            var RasterDownloader = new TerrainStreamingWebDownloader("Download Raster Data", true, false, GISDataDownloaderTotalFileSize.ByNumber);
            RasterDownloader.Add(filedata);
            RasterDownloader.ReplaceExisitingFiles = Prefs.ReplaceExisitingRaster;

            byte[] data = new byte[0];

            try
            {
                await RasterDownloader.StartDownloading(taskSource).CancelWith(taskSource.Token);

                await Task.Delay(3).CancelWith(taskSource.Token);

                data = await TerrainStreamingFileAsync.ReadAllBytes(localFilename).CancelWith(taskSource.Token);

                if (File.Exists(localFilename))
                {
                    Texture2D tex = new Texture2D(2, 2);

                    tex.wrapMode = TextureWrapMode.Repeat;
                    tex.LoadImage(data);
                    Totalwidth = tex.width;
                    TotalHeight = tex.height;
                }

                var TabSavePath = PreviewZone_DownloadPath + "/Zone.tab";
                if (File.Exists(TabSavePath))
                    File.Delete(TabSavePath);

                var ZoneData = "";
                ZoneData += "!table" + "\n";
                ZoneData += "!version 300" + "\n";
                ZoneData += "!charset WindowsLatin1" + "\n";
                ZoneData += " " + "\n";
                ZoneData += "Definition Table" + "\n";

                ZoneData += "File " + "\"" + "Zone.jpg" + "\"" + "\n";
                ZoneData += "Type " + "\"" + "RASTER" + "\"" + "\n";

                ZoneData += "  (" + Prefs.UpperLeftCoordiante.x.ToString().Replace(",", ".") + ", " + Prefs.UpperLeftCoordiante.y.ToString().Replace(",", ".") + ")  (" + 0 + "," + 0 + ") Label " + "\"NW\"," + "\n";
                ZoneData += "  (" + Prefs.UpperLeftCoordiante.x.ToString().Replace(",", ".") + ", " + Prefs.DownRightCoordiante.y.ToString().Replace(",", ".") + ")  (" + 0 + "," + TotalHeight + ") Label " + "\"SW\"," + "\n";
                ZoneData += "  (" + Prefs.DownRightCoordiante.x.ToString().Replace(",", ".") + ", " + Prefs.UpperLeftCoordiante.y.ToString().Replace(",", ".") + ")  (" + Totalwidth + "," + 0 + ") Label " + "\"NE\"," + "\n";
                ZoneData += "  (" + Prefs.DownRightCoordiante.x.ToString().Replace(",", ".") + ", " + Prefs.DownRightCoordiante.y.ToString().Replace(",", ".") + ")  (" + Totalwidth + "," + TotalHeight + ") Label " + "\"SE\"," + "\n";

                ZoneData += "CoordSys Earth Projection 1" + "\n";
                ZoneData += "degree";



                using (StreamWriter file = new StreamWriter(TabSavePath))
                {
                    await file.WriteAsync(ZoneData);
                }


                GC.Collect();
            }
            catch (OperationCanceledException)
            {
                taskSource.Cancel();
                throw;
            }


        }
        private static void GenerateTabFile(TerrainStreamingTileData Tile, string Path, float width, float hight)
        {
            string TileName = string.Format("Tile__{0}__{1}", Tile.Number.x, Tile.Number.y);

            var ZoneData = "";
            ZoneData += "!table" + "\n";
            ZoneData += "!version 300" + "\n";
            ZoneData += "!charset WindowsLatin1" + "\n";
            ZoneData += " " + "\n";
            ZoneData += "Definition Table" + "\n";

            ZoneData += "File " + "\"" + TileName + ".jpg" + "\"" + "\n";
            ZoneData += "Type " + "\"" + "RASTER" + "\"" + "\n";

            ZoneData += "  (" + Tile.UpperLeftCoordinate.x.ToString().Replace(",", ".") + ", " + Tile.UpperLeftCoordinate.y.ToString().Replace(",", ".") + ")  (" + 0 + "," + 0 + ") Label " + "\"NW\"," + "\n";
            ZoneData += "  (" + Tile.UpperLeftCoordinate.x.ToString().Replace(",", ".") + ", " + Tile.BottomRightCoordiante.y.ToString().Replace(",", ".") + ")  (" + 0 + "," + hight + ") Label " + "\"SW\"," + "\n";
            ZoneData += "  (" + Tile.BottomRightCoordiante.x.ToString().Replace(",", ".") + ", " + Tile.UpperLeftCoordinate.y.ToString().Replace(",", ".") + ")  (" + width + "," + 0 + ") Label " + "\"NE\"," + "\n";
            ZoneData += "  (" + Tile.BottomRightCoordiante.x.ToString().Replace(",", ".") + ", " + Tile.BottomRightCoordiante.y.ToString().Replace(",", ".") + ")  (" + width + "," + hight + ") Label " + "\"SE\"," + "\n";

            ZoneData += "CoordSys Earth Projection 1" + "\n";
            ZoneData += "degree";

            var TabSavePath = Path + "/" + TileName + ".tab";

            using (StreamWriter file = new StreamWriter(TabSavePath))
            {
                file.Write(ZoneData);
                file.Close();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }


        private async Task GenerateTabFile(TerrainStreamingTileData Tile, string Path, float width, float hight, CancellationTokenSource m_taskSource)
        {
            string TileName = string.Format("Tile__{0}__{1}", Tile.Number.x, Tile.Number.y);

            var ZoneData = "";
            ZoneData += "!table" + "\n";
            ZoneData += "!version 300" + "\n";
            ZoneData += "!charset WindowsLatin1" + "\n";
            ZoneData += " " + "\n";
            ZoneData += "Definition Table" + "\n";

            ZoneData += "File " + "\"" + TileName + ".jpg" + "\"" + "\n";
            ZoneData += "Type " + "\"" + "RASTER" + "\"" + "\n";

            ZoneData += "  (" + Tile.UpperLeftCoordinate.x.ToString().Replace(",", ".") + ", " + Tile.UpperLeftCoordinate.y.ToString().Replace(",", ".") + ")  (" + 0 + "," + 0 + ") Label " + "\"NW\"," + "\n";
            ZoneData += "  (" + Tile.UpperLeftCoordinate.x.ToString().Replace(",", ".") + ", " + Tile.BottomRightCoordiante.y.ToString().Replace(",", ".") + ")  (" + 0 + "," + hight + ") Label " + "\"SW\"," + "\n";
            ZoneData += "  (" + Tile.BottomRightCoordiante.x.ToString().Replace(",", ".") + ", " + Tile.UpperLeftCoordinate.y.ToString().Replace(",", ".") + ")  (" + width + "," + 0 + ") Label " + "\"NE\"," + "\n";
            ZoneData += "  (" + Tile.BottomRightCoordiante.x.ToString().Replace(",", ".") + ", " + Tile.BottomRightCoordiante.y.ToString().Replace(",", ".") + ")  (" + width + "," + hight + ") Label " + "\"SE\"," + "\n";

            ZoneData += "CoordSys Earth Projection 1" + "\n";
            ZoneData += "degree";

            var TabSavePath = Path + "/" + TileName + ".tab";

            using (StreamWriter file = new StreamWriter(TabSavePath))
            {
                await file.WriteAsync(ZoneData).CancelWith(taskSource.Token);
                file.Close();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
 
        }

        #endregion
        #region Vector
        public List<RequestedFileData> GetVectorURLs(List<TerrainStreamingTileData> tiles)
        {
            var urls = new List<RequestedFileData>();

            int Zoom = Prefs.ZoomLevel;

            switch (Prefs.VectorSource)
            {
                case GISDataDownloaderVectorProvider.OpenStreetMap:
 
                    string Base_url = "https://www.overpass-api.de/api/interpreter?data=[out:xml];(";

                    var Vector_DownloadPath = Prefs.DownloadPath + "/OSMData";

                    var Trees_Content = Resources.Load("VectorAttributes/Attribute_Trees") as TerrainStreamingAttributes_SO;
                    if (!Trees_Content)
                    {
                        Trees_Content = new TerrainStreamingAttributes_SO();
                        Trees_Content.Content = new List<TerrainStreamingVectorTag>();
                        Debug.LogError("Trees Downloadable Content File Not found .. Restore 'Attribute_Trees' ScriptableObject ");
                    }

                    var Roads_Content = Resources.Load("VectorAttributes/Attribute_Roads") as TerrainStreamingAttributes_SO;
                    if (!Roads_Content)
                    {
                        Roads_Content = new TerrainStreamingAttributes_SO();
                        Roads_Content.Content = new List<TerrainStreamingVectorTag>();
                        Debug.LogError("Roads Downloadable Content File Not found .. Restore 'Attribute_Roads' ScriptableObject ");
                    }


                    foreach (var tile in tiles)
                    {
                        string URL = "";
                        string bbox = tile.BottomRightCoordiante.y.ToString().Replace(",", ".") + "," + tile.UpperLeftCoordinate.x.ToString().Replace(",", ".") + "," + tile.UpperLeftCoordinate.y.ToString().Replace(",", ".") + "," + tile.BottomRightCoordiante.x.ToString().Replace(",", ".");
                        string ContentToDownload = "";

                        if (Prefs.DownloadTree == OptionEnabDisab.Enable)
                        {
                            foreach (var element in Trees_Content.Content)
                            {
                                if(element.EnableTag)
                                ContentToDownload += "way[" + element.Attribute + "=" + element.Value + "](" + bbox + ");>;";
                            }

                            URL = Base_url + ContentToDownload + ");out;>;out skel qt;";

                            var VectorTilePath = Vector_DownloadPath + "/" + Prefs.ZoneName  + "__Tree.osm";                           
                            var filedata = new RequestedFileData(URL, VectorTilePath, GISDataDownloaderDownloadType.data, "Vector", 200000000);
                            urls.Add(filedata);
                        }

                        if (Prefs.DownloadRoads == OptionEnabDisab.Enable)
                        {
                            foreach (var element in Roads_Content.Content)
                            {
                                if (element.EnableTag)
                                    ContentToDownload += "way[" + element.Attribute + "=" + element.Value + "](" + bbox + ");>;";
                            }

                            URL = Base_url + ContentToDownload + ");out geom("+ bbox+");";
                            var VectorTilePath = Vector_DownloadPath + "/" + Prefs.ZoneName + "__Road.osm";
                            var filedata = new RequestedFileData(URL, VectorTilePath, GISDataDownloaderDownloadType.data, "Vector", 200000000);
                            urls.Add(filedata);
                        }

                    }
                     break;
            }
 
            return urls;
        }
        public async Task DownloadVectorData(List<RequestedFileData> filesData, CancellationTokenSource m_taskSource)
        {
            var Vector_DownloadPath = Prefs.DownloadPath + "/OSMData";

            if (!Directory.Exists(Vector_DownloadPath))
                Directory.CreateDirectory(Vector_DownloadPath);

            taskSource = m_taskSource;
            WebDownloader = new TerrainStreamingWebDownloader("Download Vector Data", true, false, GISDataDownloaderTotalFileSize.ByNumber);
            WebDownloader.AddRange(filesData);
            WebDownloader.ReplaceExisitingFiles = Prefs.ReplaceDownloadedVector;

            try
            {
                await WebDownloader.StartDownloading(taskSource).CancelWith(taskSource.Token);
                GC.Collect();
            }
            catch (OperationCanceledException)
            {
                taskSource.Cancel();
                throw;
            }
        }
        public async Task GenerateVectorTiles(TerrainStreamingTileData MainZone ,List<TerrainStreamingTileData> tiles, CancellationTokenSource m_taskSource)
        {
            var OSMVector_DownloadPath = Prefs.DownloadPath + "/OSMData";

            if (!Directory.Exists(OSMVector_DownloadPath))
                Directory.CreateDirectory(OSMVector_DownloadPath);

            var Vector_DownloadPath = Prefs.DownloadPath + "/VectorData";

            if (!Directory.Exists(Vector_DownloadPath))
                Directory.CreateDirectory(Vector_DownloadPath);


            taskSource = m_taskSource;

           if (Prefs.DownloadTree == OptionEnabDisab.Enable)
            {

                if (!Directory.Exists(Vector_DownloadPath + "/Trees"))
                    Directory.CreateDirectory(Vector_DownloadPath + "/Trees");

                var Tree_OSMPath = OSMVector_DownloadPath + "/" + Prefs.ZoneName + "__Tree.osm";

                if (File.Exists(Tree_OSMPath))
                {
                    TerrainStreamingOSMFileLoader osmloader = new TerrainStreamingOSMFileLoader(Tree_OSMPath, MainZone);

                    foreach (var tile in tiles)
                    {
                        TerrainStreamingGeoVectorData TileGeoData = new TerrainStreamingGeoVectorData();

                        osmloader.GetGeoVectorTreesData(tile,ref TileGeoData);

                        try
                        {
                            if (OnDownloadProgressChanged != null)
                                OnDownloadProgressChanged("Generate Tree Vector Tile..   ", (tiles.IndexOf(tile) * 100 / tiles.Count));

                            await TerrainStreamingVectorTileGenerator.GeneratePolygonGeoDataVectorTile("Tree", TileGeoData.GeoTrees, tile, Vector_DownloadPath + "/Trees", Prefs.ReplaceGeneratedVectorTiles, taskSource).CancelWith(taskSource.Token);

                            GC.Collect();
                        }
                        catch (OperationCanceledException)
                        {
                            taskSource.Cancel();
                            throw;
                        }
                    }


                }


            }

            if (Prefs.DownloadGrass == OptionEnabDisab.Enable)
            {

                if (!Directory.Exists(Vector_DownloadPath + "/Grass"))
                    Directory.CreateDirectory(Vector_DownloadPath + "/Grass");

                var Grass_OSMPath = OSMVector_DownloadPath + "/" + Prefs.ZoneName + "__Grass.osm";

                if (File.Exists(Grass_OSMPath))
                {
                    TerrainStreamingOSMFileLoader osmloader = new TerrainStreamingOSMFileLoader(Grass_OSMPath, MainZone);

                    foreach (var tile in tiles)
                    {
                        TerrainStreamingGeoVectorData TileGeoData = new TerrainStreamingGeoVectorData();

                        osmloader.GetGeoVectorGrassData(tile, ref TileGeoData);

                        try
                        {
                            if (OnDownloadProgressChanged != null)
                                OnDownloadProgressChanged("Generate Grass Vector Tile..   ", (tiles.IndexOf(tile) * 100 / tiles.Count));

                            await TerrainStreamingVectorTileGenerator.GeneratePolygonGeoDataVectorTile("Grass", TileGeoData.GeoGrass, tile, Vector_DownloadPath + "/Grass", Prefs.ReplaceGeneratedVectorTiles, taskSource).CancelWith(taskSource.Token);

                            GC.Collect();
                        }
                        catch (OperationCanceledException)
                        {
                            taskSource.Cancel();
                            throw;
                        }
                    }


                }


            }
            if (Prefs.DownloadRoads == OptionEnabDisab.Enable)
            {

                if (!Directory.Exists(Vector_DownloadPath + "/Roads"))
                    Directory.CreateDirectory(Vector_DownloadPath + "/Roads");

                var Road_OSMPath = OSMVector_DownloadPath + "/" + Prefs.ZoneName + "__Road.osm";

                if (File.Exists(Road_OSMPath))
                {
                    TerrainStreamingOSMFileLoader osmloader = new TerrainStreamingOSMFileLoader(Road_OSMPath, MainZone);

                    foreach (var tile in tiles)
                    {
                        TerrainStreamingGeoVectorData TileGeoData = new TerrainStreamingGeoVectorData();

                        osmloader.GetGeoVectorRoadsData(tile, ref TileGeoData);

                        try
                        {
                            if (OnDownloadProgressChanged != null)
                                OnDownloadProgressChanged("Generate Road Vector Tile..   ", (tiles.IndexOf(tile) * 100 / tiles.Count));

                            await TerrainStreamingVectorTileGenerator.GenerateLineGeoDataVectorTile("Road", TileGeoData.GeoRoads, tile, Vector_DownloadPath + "/Roads", Prefs.ReplaceGeneratedVectorTiles, taskSource).CancelWith(taskSource.Token);

                            GC.Collect();
                        }
                        catch (OperationCanceledException)
                        {
                            taskSource.Cancel();
                            throw;
                        }
                    }


                }


            }
        }
        public void Clear()
        {
            LoadedFiles.Clear();
            WebDownloader.ClearTemp();

        }
        public static CultureInfo cultureInfo
        {
            get { return CultureInfo.InvariantCulture; }
        }
        public static NumberFormatInfo numberFormat
        {
            get { return cultureInfo.NumberFormat; }
        }
        public static string EscapeURL(string url)
        {
#if UNITY_2018_3_OR_NEWER
            return UnityWebRequest.EscapeURL(url);
#else
            return WWW.EscapeURL(url);
#endif
        }

        #endregion

        //////////////////////////////////////////////////////////
 
    };
 
}
