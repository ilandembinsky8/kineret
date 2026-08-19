
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace GISTech.TerrainStreaming
{
    public class TerrainStreamingTerrainTile : MonoBehaviour
    {
        [HideInInspector]
        public TerrainStreamingSystemPrefs prefs;
        [HideInInspector]
        public TerrainStreamingContainer container;
        [HideInInspector]
        public TerrainStreamingTileSector TileSector;

        [HideInInspector]
        public LoadingState ElevationState;
        [HideInInspector]
        public LoadingState TextureState;

        [HideInInspector]
        public bool IsHighResolutionTexture = false;
        [HideInInspector]
        public bool HighResolutionTextureLoading = false;

        public LoadingState TreeState;

        public LoadingState GrassState;

        public LoadingState RoadState;

        [HideInInspector]
        public Vector3 size;

        public Vector2Int Number;
 
        private bool TreePrefabsAdded = false;
        private bool GrassPrefabsAdded = false;
        
        private string m_MainDataFolder;
        public string MainDataFolder
        {
            get { return m_MainDataFolder; }
            set
            {
                if (m_MainDataFolder != value)
                {
                    m_MainDataFolder = value;
                    OnMainDataFolderChanged(MainDataFolder);
                }
            }
        }
        static Texture2D terrainTexture = null;
        private static void GenerateTexture(TerrainStreamingFastTexture2D t)
        {
            terrainTexture = new Texture2D(2, 2);
            terrainTexture = t.NativeTexture;

        }
        private string DEMFolder;
        private string RasterFolder;
        private string VectorFolder;
        private string HorizonDEMFolder;
        private string HorizonRasterFolder;

        private void OnMainDataFolderChanged(string mainDataFolder)
        {
            DEMFolder = mainDataFolder + "/DEMData";
            RasterFolder = mainDataFolder + "/RasterData";
            VectorFolder = mainDataFolder + "/VectorData";

            HorizonDEMFolder = mainDataFolder + "/HorizonDEMData";
            HorizonRasterFolder = mainDataFolder + "/HorizonRasterData";
        }
 


        private Terrain _terrain;
        public Terrain terrain
        {
            get { return _terrain ?? (_terrain = GetComponent<Terrain>()); }
        }

        public TerrainData terrainData
        {
            get
            {
                return terrain.terrainData;
            }
        }
        void Start()
        {
        }

        #region Elevation
        public IEnumerator LoadElevationFile(bool Horizon=false)
        {
            var time = UnityEngine.Random.Range(0f, 0.01f);

            yield return new WaitForSeconds(time);

            bool EleExist;
            var demFolder = DEMFolder;

            if (Horizon)
                demFolder = HorizonDEMFolder;

    var ElePath = CheckForElevationFile(demFolder, out EleExist);

            if (!EleExist)
            {
                Debug.LogError("No Elevation File found On : " + ElePath);
                ElevationState = LoadingState.Error;
                yield return null;
            }
            else
            {
                var RawReader = new TerrainStreamingRawLoader();

                RawReader.heightmapResolution = terrainData.heightmapResolution;

                RawReader.LoadRawGrid(ElePath);

                yield return new WaitUntil(() => RawReader.LoadComplet == true);

                terrain.terrainData.SetHeights(0, 0, RawReader.data.floatheightData);

                terrainData.SetHeights(0, 0, RawReader.data.floatheightData);

                ElevationState = LoadingState.Loaded;
            }


            yield return null;
        }
        private string CheckForElevationFile(string ElevationDirectory, out bool exist)
        {
            string ElevationFile = "";
            exist = false;
            ElevationFile = ElevationDirectory + "/" + "Tile__" + Number.x.ToString() + "__" + Number.y.ToString() + ".raw";

            if (File.Exists(ElevationFile))
            {
                exist = true;
            }
            return ElevationFile;
        }

        #endregion

        #region Textures
        public IEnumerator LoadTextureFile(TerrainStreamingTerrainTile terrainItem,bool Horizon = false)
        {
            var rasterFolder = RasterFolder;

            if (Horizon)
                rasterFolder = HorizonRasterFolder;

            bool texExist;
            var texPath = TerrainStreamingTextureLoader.CheckForTexture(rasterFolder, terrainItem, out texExist);

            if (texExist)
            {
                TerrainStreamingFastTexture2D.CreateFastTexture2D(texPath, false, prefs.textureResolution, prefs.textureResolution, GenerateTexture);
                TextureState = LoadingState.Loaded;
            }
            else
            {
                terrainTexture = (Texture2D)Resources.Load("Textures/NullTexture");
                TextureState = LoadingState.Error;
            }

#if UNITY_2018_1_OR_NEWER
            TerrainLayer NewterrainLayer = new TerrainLayer();

            TerrainLayer[] ExistingTerrainLayers = terrainItem.terrainData.terrainLayers;

            List<TerrainLayer> NewLayers = new List<TerrainLayer>();

            foreach (var l in ExistingTerrainLayers)
            {
                NewLayers.Add(l);
            }


            
            NewterrainLayer.metallic = 0f;
            NewterrainLayer.smoothness = 0f;
            NewterrainLayer.specular = Color.black;
NewterrainLayer.diffuseTexture = terrainTexture;

            NewterrainLayer.tileSize = new Vector2(terrainItem.terrainData.size.x, terrainItem.terrainData.size.z);
            NewterrainLayer.tileOffset = Vector2.zero;

            NewLayers.Add(NewterrainLayer);
            terrainItem.terrainData.terrainLayers = NewLayers.ToArray();


#else
            SplatPrototype sp = new SplatPrototype
            {
                texture = tex,
                tileSize = new Vector2(terrainItem.size.x, terrainItem.size.z),
                tileOffset = Vector2.zero
            };
            terrain.terrainData.splatPrototypes = new[] { sp };
#endif

            yield return null;
        }

public IEnumerator LoadLowResolutionTextureFile(TerrainStreamingTerrainTile terrainItem)
        {
            string lowFolder = MainDataFolder + "/RasterData_Low256";
            bool texExist;
            string texPath = TerrainStreamingTextureLoader.CheckForTexture(lowFolder, terrainItem, out texExist);

            if (!texExist)
            {
                // Safe fallback if the proxy cache is missing for any reason.
                yield return StartCoroutine(LoadTextureFile(terrainItem, false));
                IsHighResolutionTexture = true;
                yield break;
            }

            byte[] imageBytes = File.ReadAllBytes(texPath);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
            if (!tex.LoadImage(imageBytes, true))
            {
                Destroy(tex);
                TextureState = LoadingState.Error;
                yield break;
            }

            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.anisoLevel = 2;
            tex.name = "TerrainProxy256_" + Number.x + "_" + Number.y;

            ReplaceTerrainTexture(terrainItem, tex);
            TextureState = LoadingState.Loaded;
            IsHighResolutionTexture = false;
            yield return null;
        }

        public IEnumerator UpgradeToHighResolutionTextureFile(TerrainStreamingTerrainTile terrainItem)
        {
            if (IsHighResolutionTexture || HighResolutionTextureLoading)
                yield break;

            bool texExist;
            string texPath = TerrainStreamingTextureLoader.CheckForTexture(RasterFolder, terrainItem, out texExist);
            if (!texExist)
                yield break;

            HighResolutionTextureLoading = true;
            string uri = new Uri(texPath).AbsoluteUri;

            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(uri, false))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning("Failed to asynchronously load high-resolution terrain texture: " + texPath + " :: " + request.error);
                    HighResolutionTextureLoading = false;
                    yield break;
                }

                Texture2D tex = DownloadHandlerTexture.GetContent(request);
                if (tex == null)
                {
                    HighResolutionTextureLoading = false;
                    yield break;
                }

                tex.wrapMode = TextureWrapMode.Clamp;
                tex.filterMode = FilterMode.Bilinear;
                tex.anisoLevel = 8;
                tex.name = "TerrainHigh2048_" + Number.x + "_" + Number.y;

                ReplaceTerrainTexture(terrainItem, tex);
                IsHighResolutionTexture = true;
            }

            HighResolutionTextureLoading = false;
            yield return null;
        }

        private void ReplaceTerrainTexture(TerrainStreamingTerrainTile terrainItem, Texture2D newTexture)
        {
            TerrainLayer[] layers = terrainItem.terrainData.terrainLayers;
            TerrainLayer layer = null;

            if (layers != null && layers.Length > 0)
                layer = layers[layers.Length - 1];

            if (layer == null)
            {
                layer = new TerrainLayer();
                var newLayers = new List<TerrainLayer>();
                if (layers != null)
                    newLayers.AddRange(layers);
                newLayers.Add(layer);
                layers = newLayers.ToArray();
            }

            Texture2D oldTexture = layer.diffuseTexture as Texture2D;
            layer.diffuseTexture = newTexture;
            layer.tileSize = new Vector2(terrainItem.terrainData.size.x, terrainItem.terrainData.size.z);
            layer.tileOffset = Vector2.zero;
            layer.metallic = 0f;
            layer.smoothness = 0f;
            layer.specular = Color.black;
            terrainItem.terrainData.terrainLayers = layers;
            terrainItem.terrain.Flush();

            if (oldTexture != null && oldTexture != newTexture)
                Destroy(oldTexture);
        }


        #endregion

        #region Tree
        public IEnumerator GenerateTrees()
        {
            AddTreePrefabs();

            if (prefs.TreesGenerationMode == GenerationMode.Random)
            {
                if(prefs.TreePrefabs[0]!=null)
                {
                   yield return StartCoroutine(SetTreeToTerrain(prefs.TreePrefabs[0]));
                   TreeState = LoadingState.Loaded;
                }
            }
            else
            {
                var TreePath = VectorFolder + "/Trees/Tile__" + Number.x + "__" + Number.y + "__Tree.tsv";
               
                if (File.Exists(TreePath))
                {
                    int offest = 0;
                    yield return ReadFile(SetData, TreePath); ;

                    var Geodata = TerrainStreamingVectorSerializer.DeserializePolygonGeoDataArray(data, ref offest).ToList();

                    TerrainStreamingTreeGenerator.GenerateTrees(this, Geodata);

                    TreeState = LoadingState.Loaded;

                }
                else
                    TreeState = LoadingState.Error;
            }

        }

        public void AddTreePrefabs()
        {
            if (!TreePrefabsAdded)
                TerrainStreamingTreeGenerator.AddTreePrefabsToTerrains(this, prefs.TreesGenerationMode, prefs.TreePrefabs, prefs.TreeDistance, prefs.BillBoardStartDistance);

            TreePrefabsAdded = true;
        }
        IEnumerator SetTreeToTerrain(TerrainStreamingSO_Tree tree_SO)
        {

            var totalPositions = UnityEngine.Random.Range(0, tree_SO.TreeDensity);
            int vegDensity = (int)(totalPositions * tree_SO.TreeDensity);

            float TreeScaleFactor = tree_SO.TreeScaleFactor * container.Scale.x;
            float RandomScaleFactor = tree_SO.TreeRandomScaleFactor * container.Scale.x;

            for (int i = 0; i < vegDensity; i++)
            {

                var Posx = UnityEngine.Random.Range(0, terrainData.size.x);

                var Posz = UnityEngine.Random.Range(0, terrainData.size.z);

                Vector3 LocalPosition = new Vector3(Posx, 0, Posz);

                TerrainData tData = terrain.terrainData;

                float heightmapWidth = (tData.heightmapResolution - 1) * tData.heightmapScale.x;
                float heightmapHeight = (tData.heightmapResolution - 1) * tData.heightmapScale.z;

                TreeInstance tree = new TreeInstance();
                tree.color = Color.white;
                tree.heightScale = TreeScaleFactor + UnityEngine.Random.Range(-RandomScaleFactor, RandomScaleFactor);


                tree.lightmapColor = Color.white;
                Vector3 position = LocalPosition - transform.position; ;
                tree.position = new Vector3(LocalPosition.x / heightmapWidth, 0, LocalPosition.z / heightmapHeight);
                tree.prototypeIndex = UnityEngine.Random.Range(0, tData.treePrototypes.Length);
                tree.widthScale = TreeScaleFactor + UnityEngine.Random.Range(-RandomScaleFactor, RandomScaleFactor);


                terrain.AddTreeInstance(tree);
            }
            yield return null;
        }
        #endregion
        #region Grass
        public IEnumerator GenerateGrass()
        {
            AddGrassPrefabs();

            if (prefs.GrassGenerationMode == GenerationMode.Random)
            {
                if (prefs.GrassPrefabs[0] != null)
                {
                    //yield return StartCoroutine(SetGrassToTerrain(prefs.GrassPrefabs[0]));
                    GrassState = LoadingState.Loaded;
                }
            }
            else
            {
                var GrassPath = VectorFolder + "/Grass/Tile__" + Number.x + "__" + Number.y + "__Grass.tsv";

                if (File.Exists(GrassPath))
                {
                    int offest = 0;
                    yield return ReadFile(SetData, GrassPath); ;

                    var Geodata = TerrainStreamingVectorSerializer.DeserializePolygonGeoDataArray(data, ref offest).ToList();

                    TerrainStreamingGrassGenerator.GenerateGrass(this, Geodata);

                    GrassState = LoadingState.Loaded;

                }
                else
                    GrassState = LoadingState.Error;
            }

        }
        public void AddGrassPrefabs()
        {
            if (!GrassPrefabsAdded)
                TerrainStreamingGrassGenerator.AddGrassPrefabsToTerrains(this,prefs.GrassGenerationMode, prefs.GrassPrefabs, prefs.DetailDistance, prefs.GrassScaleFactor);

            GrassPrefabsAdded = true;
        }
        #endregion
        #region Roads
        public IEnumerator GenerateRoads()
        {
            if (prefs.GenerateRoads == OptionEnabDisab.Enable)
            {
                var RoadPath = VectorFolder + "/Roads/Tile__" + Number.x + "__" + Number.y + "__Road.tsv";

                if (File.Exists(RoadPath))
                {
                    int offest = 0;

                    yield return ReadFile(SetData, RoadPath); ;

                    var Geodata = TerrainStreamingVectorSerializer.TerrainStreamingLinesGeoData(data, ref offest).ToList();

                    TerrainStreamingSystemRoadGenerator.GenerateRoades(this, Geodata);

                    RoadState = LoadingState.Loaded;

                }
                else
                    RoadState = LoadingState.Error;
            }

        }
        #endregion

        #region FileLoader
        IEnumerator ReadFile(Action<byte[]> Callback, string Path)
        {
            using (var www = new UnityWebRequest(Path))
            {
                www.downloadHandler = new DownloadHandlerBuffer();
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.ProtocolError || www.result == UnityWebRequest.Result.ConnectionError)
                {
                    Debug.Log("Error while downloading data: " + www.error);
                }
                else
                {
                    var data = www.downloadHandler.data;
                    Callback(data);
                }
            }

        }
        private byte[] data;
        public void SetData(byte[] m_data)
        {
            data = m_data;
        }
        #endregion

    }
 }