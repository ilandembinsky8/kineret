using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace GISTech.TerrainStreaming
{
    public delegate void DownloadingEvent();
    public delegate void DownloadingProgress(string phase,int progress);

    public class TerrainStreamingMultiWebDownloader
    {
        public static event DownloadingProgress OnDownloadProgressChanged;
#pragma warning disable
        public static event DownloadEvent OnError;

        public List<TerrainStreamingWebDownloader> WebDownloaders = new List<TerrainStreamingWebDownloader>();
        public string title;
        public bool downloading;
        public TerrainStreamingMultiWebDownloader(string m_title,List<TerrainStreamingWebDownloader> m_WebDownloaders)
        {
            title = m_title;
            WebDownloaders = m_WebDownloaders;
        }
        public TerrainStreamingMultiWebDownloader(string m_title)
        {
            title = m_title;
        }
 
        public void SetTotalProgress(string from)
        {
             if (WebDownloaders.Count > 0)
            {
                var s_Totalprogress = (WebDownloaders.Sum(i => i.totalProgress)) / WebDownloaders.Count;
 
                if (Totalprogress != (int)s_Totalprogress)
                {
                    if((int)s_Totalprogress>Totalprogress)
                    {
                        Totalprogress = s_Totalprogress;
                    }
                       
                }

            }
        }
        private int m_Totalprogress=0;
        public int Totalprogress
        {
            get { return m_Totalprogress; }
            set
            {
                if (m_Totalprogress != value)
                {
                    m_Totalprogress = value;

                    if (OnDownloadProgressChanged != null)
                    {
                        OnDownloadProgressChanged(title, Totalprogress);
                    }
                }
            }

        }
    }
    public class TerrainStreamingWebDownloader
    {
        public bool NeedAutho = false;
        public string User = "";
        public string Pass = "";

        public static event DownloadingProgress OnDownloadProgressChanged;
        public static event DownloadEvent OnError;
        public TerrainStreamingMultiWebDownloader parentDownloader;

        public string title;

        private int activeItemID = -1;
        private WebClient client;
        public List<TerrainStreamingRequestedElement> items;
        public List<TerrainStreamingRequestedElement> itemsToDownload;
        private float totalSize;

        public bool useFileSize;
        public float defaultSize = 20000;
        private bool EnableProgress;
        private TerrainStreamingRequestedElement activeItem;
        private GISDataDownloaderTotalFileSize TotalFileSize;
        public OptionEnabDisab ReplaceExisitingFiles = OptionEnabDisab.Enable;
        private bool CanDownload = false;

        public TerrainStreamingWebDownloader(string m_title,bool m_EnableProgress = true,bool m_UseFileSize=false, GISDataDownloaderTotalFileSize m_TotalFileSize=GISDataDownloaderTotalFileSize.BySize)
        {
            items = new List<TerrainStreamingRequestedElement>();
            itemsToDownload = new List<TerrainStreamingRequestedElement>();
            totalSize = 0;
            title = m_title;
            useFileSize =  m_UseFileSize;
            EnableProgress = m_EnableProgress;
            TotalFileSize = m_TotalFileSize;
            CanDownload = true;
        }
        public void AddRange(List<RequestedFileData> m_requests)
        {
            for (int i = 0; i < m_requests.Count; i++)
                Add(m_requests[i]);

        }
        public void Add(RequestedFileData m_request)
        {
            float FileSize = defaultSize;

            if (useFileSize)
            {
                FileSize = GetFileSize(m_request.url);

                if (FileSize != -9999)
                    m_request.FileSize = FileSize;
            }


            TerrainStreamingRequestedElement item = new TerrainStreamingRequestedElement(m_request);
            totalSize += FileSize;
            items.Add(item);
        }

        CancellationTokenSource MainTaskSource = null;
        CancellationTokenSource SubSource = null;
        List<CancellationTokenSource> taskSources = new List<CancellationTokenSource>();
        public async Task StartDownloading(CancellationTokenSource taskSource)
        {
            activeItemID = -1;
            progress = 0;
            totalProgress = 0;
 

            MainTaskSource = new CancellationTokenSource();
            taskSources = new List<CancellationTokenSource>();

            if (items == null || items.Count == 0)
                return;

            foreach (var file in items)
            {
                if (File.Exists(file.localFilename) && useFileSize)
                {
                    var downloadedFileSize = GetLocalFileSizeByts(file.localFilename);

                    if (downloadedFileSize != file.avarageSize)
                        TerrainStreamingExtensions.SafeDeleteFile(file.localFilename);

                }
 
                if (!File.Exists(file.localFilename) || ReplaceExisitingFiles == OptionEnabDisab.Enable)
                {
                    itemsToDownload.Add(file);
                    taskSources.Add(new CancellationTokenSource());

                }                 

            }

            if (itemsToDownload.Count > 0)
            {
                foreach (var file in itemsToDownload)
                {
                    try
                    {
                        if (CanDownload)
                        {
                            CancellationToken token = taskSources[0].Token;

                            activeItemID++;
                            progress = 0;

                            if (activeItemID < taskSources.Count)
                                token = taskSources[activeItemID].Token;

                            await DownloadFilesAsync(file, MainTaskSource).CancelWith(token);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        MainTaskSource.Cancel();
                        MainTaskSource.Token.ThrowIfCancellationRequested();
                        throw;
                    }
                }
            }

        }

        private async Task DownloadFilesAsync(TerrainStreamingRequestedElement Requestfile, CancellationTokenSource taskSource)
        {
            SubSource = taskSource;

            client = new WebClient();

            if(NeedAutho)
            {
                try
                {
                    using (FileStream fileStream = new System.IO.FileStream(Requestfile.localFilename, System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.Write))
                    {
                        CookieContainer myContainer = new CookieContainer();

                        CredentialCache cache = new CredentialCache();
                        cache.Add(new Uri(TerrainStreamingProvidersData.srtm_30_mainUrs), "Basic", new NetworkCredential(User, Pass));

                        var request = (HttpWebRequest)WebRequest.Create(Requestfile.weburl);
                        request.Method = "GET";
                        request.Credentials = cache;
                        request.CookieContainer = myContainer;
                        request.PreAuthenticate = false;
                        request.AllowAutoRedirect = true;

                        const int BUFFER_SIZE = 16 * 1024;
                        using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                        {
                            int maxValue = (int)response.ContentLength;
                            long totalDownloadedByte = 0;
                            using (var responseStream = response.GetResponseStream())
                            {
                                var buffer = new byte[BUFFER_SIZE];
                                int bytesRead = 0;
                                do
                                {
                                    totalDownloadedByte = bytesRead + totalDownloadedByte;
                                    OnProgressChanged(totalDownloadedByte, maxValue);
                                    bytesRead = await responseStream.ReadAsync(buffer, 0, BUFFER_SIZE).CancelWith(SubSource.Token);
                                    await fileStream.WriteAsync(buffer, 0, bytesRead).CancelWith(SubSource.Token); ;
                                } while (bytesRead > 0);
                            }
                        }
                        fileStream.Close();

                    }
                }
                catch(Exception ex)
                {
                    Debug.LogError("Connexion not Authorized to EarthData : Check Your Username + Login or Update Download Url "+ ex);

                    if (OnError != null)
                        OnError();

                }
 
            }
            else
            {
                try
                {
                    activeItem = Requestfile;

                    string filepath = Requestfile.localFilename;
                    client.DownloadProgressChanged += OnProgressChanged;
                    client.DownloadFileCompleted += OnDownloadComplete;
                    await client.DownloadFileTaskAsync(Requestfile.url, filepath).CancelWith(SubSource.Token);


                }
                catch(Exception)
                {
                    //Debug.LogError("Error : occurred while downloading / Check your connexion or Inputs  " + ex);

                    if (OnError != null)
                        OnError();

                }

            }

        }

        private void OnDownloadComplete(object sender, AsyncCompletedEventArgs e)
        {
            if(TotalFileSize == GISDataDownloaderTotalFileSize.ByNumber)
            {
                if (activeItemID == 0)
                    activeItemID++;

                totalProgress = (activeItemID * 100) / itemsToDownload.Count;
            }

            if (e.Error != null)
                TerrainStreamingExtensions.SafeDeleteFile(activeItem.localFilename);

        }
        private void OnProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            if (itemsToDownload.Count > 0)
            {
                var localProgress = (e.ProgressPercentage);

                if (progress != (int)localProgress)
                {
                    if ((int)localProgress > progress)
                    {
                        progress = (int)localProgress;
                    }

                    //totalProgress = (100 / itemsToDownload.Count) * activeItemID + (progress / itemsToDownload.Count);

                }

            }

        }
        private void OnProgressChanged(float currentValue,float maxValue)
        {
            if (itemsToDownload.Count > 0)
            {
                var localProgress = (currentValue * 100)/ maxValue;

                if (progress != (int)localProgress)
                {
                    if ((int)localProgress > progress)
                    {
                        progress = (int)localProgress;
                    }

                }

            }

        }

        public int m_totalProgress;
        public int totalProgress
        {
            get { return m_totalProgress; }
            set
            {
                if (m_totalProgress != value)
                {
                    m_totalProgress = value;

                    if (parentDownloader != null)
                        parentDownloader.SetTotalProgress(parentDownloader.WebDownloaders.IndexOf(this) + " - " + totalProgress);


                }
            }

        }

        public int m_progress;
        public int progress
        {
            get { return m_progress; }
            set
            {
                if (m_progress != value)
                {
                    m_progress = value;

                    if (EnableProgress)
                    {

                        if (TotalFileSize == GISDataDownloaderTotalFileSize.BySize)
                            totalProgress = ((activeItemID * 100) / itemsToDownload.Count) + (progress / itemsToDownload.Count);


                        if (OnDownloadProgressChanged != null)
                        {
                            OnDownloadProgressChanged(title, totalProgress);
                        }
                    }


                }
            }

        }
        public void ClearTemp()
        {
            items = null;
            GC.Collect();
        }
        public float TotalSize
        {
            get
            {
                return totalSize;
            }
        }
        private float GetFileSize(string URL)
        {
            float fileSize = -9999;
            try
            {
                System.Net.WebRequest req = System.Net.HttpWebRequest.Create(URL);
                req.Method = "HEAD";
                System.Net.WebResponse resp = req.GetResponse();
                long ContentLength = 0;
                //long result;

                if (long.TryParse(resp.Headers.Get("Content-Length"), out ContentLength))
                {
                    var s = (ContentLength / 1024).ToString();
                    fileSize = float.Parse(s);
                }
            }
            catch(Exception ex)
            {
                var s = ex;
 
            }
            return fileSize;
        }
        private float GetLocalFileSizeByts(string filepath)
        {
            FileInfo fi = new FileInfo(filepath);
            double len = fi.Length/1024;
            return (float)len;
        }
        private void OnDownloadStarted()
        {
 
        }
 
        public void OnDownloadCancelled()
        {
            CanDownload = false;

            if (taskSources.Count > 0)
            {
                foreach (var s in taskSources)
                {
                    s.Cancel();
                }
            }
            if (MainTaskSource != null)
                MainTaskSource.Cancel();

            if (SubSource != null)
                SubSource.Cancel();

            if (client != null)
                client.CancelAsync();

            MainTaskSource = null;
            SubSource = null;
            client = null;
            taskSources = new List<CancellationTokenSource>();


        }


    }

    public class TerrainStreamingRequestedElement
    {
        public event OnCompleteHandler OnComplete;

        public delegate void OnCompleteHandler(ref byte[] data);

        public RequestedFileData request;
        public float avarageSize;
        public bool checkCRC;
        public readonly GISDataDownloaderDownloadType downloadType;
        public readonly string localFilename;
        public readonly string title;
        public readonly Uri url;
        public readonly string weburl;


        private string _errorFilename;
        private long _failedCRC;

        private string errorFilename
        {
            get
            {
                if (string.IsNullOrEmpty(_errorFilename)) _errorFilename = localFilename.Substring(0, localFilename.LastIndexOf(".") + 1) + "err";
                return _errorFilename;
            }
        }

        public bool exists
        {
            get { return File.Exists(localFilename) || File.Exists(errorFilename); }
        }

        public long failedCRC
        {
            get { return _failedCRC; }
            set
            {
                _failedCRC = value;
                checkCRC = true;
            }
        }

        public TerrainStreamingRequestedElement(RequestedFileData m_request)
        {
            this.url = new Uri(m_request.url);
            this.weburl = m_request.url;
            this.localFilename = m_request.localFilename;
            this.downloadType = m_request.downloadType;
            this.title = m_request.title;
            this.avarageSize = m_request.FileSize;
            this.request = m_request;
 
        }

        public TerrainStreamingRequestedElement(string url, string localFilename, GISDataDownloaderDownloadType downloadType, string title, float avarageSize)
        {
            this.url = new Uri(url);
            this.localFilename = localFilename;
            this.downloadType = downloadType;
            this.title = title;
            this.avarageSize = avarageSize;
        }


        public void DispatchCompete(ref byte[] data)
        {
            if (OnComplete != null) OnComplete(ref data);
        }

        public void CreateErrorFile()
        {
            File.WriteAllBytes(errorFilename, new byte[] { });
        }
    }
    public class RequestedFileData
    {
        public string url;
        public string localFilename;
        public GISDataDownloaderDownloadType downloadType;
        public string title;
        public float FileSize;
        public long failedCRC;

        public RequestedFileData(string m_url, string m_localFilename,
            GISDataDownloaderDownloadType m_downloadType, string m_title, int m_avarageSize, long m_failedCRC)
        {
            url = m_url;
            localFilename = m_localFilename;
            downloadType = m_downloadType;
            title = m_title;
            FileSize = m_avarageSize;
            failedCRC = m_failedCRC;
        }
        public RequestedFileData(string m_url, string m_localFilename,
            GISDataDownloaderDownloadType m_downloadType, string m_title, int m_avarageSize)
        {
            url = m_url;
            localFilename = m_localFilename;
            downloadType = m_downloadType;
            title = m_title;
            FileSize = m_avarageSize;
        }
    }
}