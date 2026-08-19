/*     Unity GIS Tech 2020-2022      */

using System.Collections.Generic;
using UnityEngine;

namespace GISTech.TerrainStreaming
{
    public class TerrainStreamingSystemPrefs : MonoSingleton<TerrainStreamingSystemPrefs>
    {
 
#if UNITY_EDITOR

        public int lastTab = 0;
#endif
        private TerrainStreamingSystem RuntimeGenerator;
        public TerrainStreamingPlayer player;

        public Vector3 terrainScale = Vector3.one;
        public TerrainMaterialMode terrainMaterialMode = TerrainMaterialMode.Standard;
        public Material terrainMaterial = null;

        public StartMode PlayerStartMode = StartMode.Centre;
        public DVector2 startPosition = new DVector2(0, 0);

        public Vector2 Dimensions = new Vector2(10, 10);
        public Vector2Int TilesCount = new Vector2Int(10, 10);
        public float TerrainHeight = 1112f;

        public int textureResolution = 1024;

        public float ScaleFactor = 100;
        public float ElevationScaleValue = 1112.0f;


        public OptionEnabDisab GenerateTextures = OptionEnabDisab.Enable;
        public OptionEnabDisab GenerateTrees = OptionEnabDisab.Disable;
        public GenerationMode  TreesGenerationMode = GenerationMode.Random;

        public OptionEnabDisab GenerateRoads = OptionEnabDisab.Disable;
        public RoadGenerator TerrainRoadGenerator = RoadGenerator.SimpleUnityLine;

        public OptionEnabDisab GenerateGrass = OptionEnabDisab.Disable;
        public GenerationMode GrassGenerationMode = GenerationMode.Random;

        public Vector2Int Hr_TilesCount = new Vector2Int(0, 0);
        public OptionEnabDisab GenerateHorizon = OptionEnabDisab.Disable;
        public bool GenerateHorizonGenerated;
        public float HorizonYOffest = -10;

        public Projections Projection = Projections.Geographic_LatLon_Decimale;

        public float UpdateTime = 0.1f;

        [Header("Environment Parameters")]
        [Space(4)]
        [Tooltip("Detail Counter Per Detail Pixel")]
        public int resolutionPerPatch = 8;
        [Range(0, 250)]
        public int detailCountPerPixel = 1;

        public float basemapDistance = 1000;
 
        [Space(6)]
        [Range(0, 1)]
        public float treeDensity=0.5f;
        [Range(0, 20)]
        public float GrassScaleFactor = 5;



        #region TerrainPrefs
        

        private TerrainStreamingTerrainContainer TerrainContaier;
        public int heightmapResolution;

        public int[] availableHeights = { 32, 64, 129, 256, 512, 1024, 2048, 4096 };
        public string[] availableHeightSrt = new string[] { "32", "64", "128", "256", "512", "1024", "2048", "4097" };

        public int[] availableHeightsResolutionPrePec = { 8, 16, 32};
        public string[] availableHeightsResolutionPrePectSrt = new string[] {"8", "16", "32" };


        void Start()
        {
            ScaleFactor = 100;
        }

        public int detailResolution = 128;
        public int m_detailResolution_index = 2;
        public int detailResolution_index
        {
            get { return m_detailResolution_index; }
            set
            {
                if (m_detailResolution_index != value)
                {
                    m_detailResolution_index = value;

                    if (Application.isPlaying)
                    {
                        if (TerrainContaier == null)
                            TerrainContaier = TerrainStreamingTerrainContainer.Get;

                        if (TerrainContaier != null)
                            OnDetailResolutionChanged(value);
                    }else

                        detailResolution = availableHeights[detailResolution_index];

                }
            }
        }
        public void OnDetailResolutionChanged(float value)
        {
            detailResolution = availableHeights[detailResolution_index];

            var terrains = TerrainContaier.GetTerrainTiles();
            for (int t = 0; t < terrains.Length; t++)
            {
                var terrain = terrains[t];
                if (terrain != null)
                {
                    var resolutionPerPatch = terrain.terrainData.detailResolutionPerPatch;
                    terrain.terrainData.SetDetailResolution(detailResolution, resolutionPerPatch);

                }
            }
        }



        public int detailResolutionPerPatch = 8;

        public int m_resolutionPerPatch_index = 2;
        public int resolutionPerPatch_index
        {
            get { return m_resolutionPerPatch_index; }
            set
            {
                if (m_resolutionPerPatch_index != value)
                {
                    m_resolutionPerPatch_index = value;

                    if (Application.isPlaying)
                    {
                        if (TerrainContaier == null)
                            TerrainContaier = TerrainStreamingTerrainContainer.Get;

                        if (TerrainContaier != null)
                            OnDetailResolutionPerPatchChanged(value);
                    }
                    else
                        detailResolutionPerPatch = availableHeightsResolutionPrePec[resolutionPerPatch_index];

                }
            }
        }
        public void OnDetailResolutionPerPatchChanged(float value)
        {
            detailResolutionPerPatch = availableHeightsResolutionPrePec[resolutionPerPatch_index];

            var terrains = TerrainContaier.GetTerrainTiles();
            for (int t = 0; t < terrains.Length; t++)
            {
                var terrain = terrains[t];
                if (terrain != null)
                {
                    terrain.terrainData.SetDetailResolution(detailResolution, detailResolutionPerPatch);
                }
            }
        }



        public int baseMapResolution = 512;

        public int m_baseMapResolution_index = 2;
        public int baseMapResolution_index
        {
            get { return m_baseMapResolution_index; }
            set
            {
                if (m_baseMapResolution_index != value)
                {
                    m_baseMapResolution_index = value;

                    if (Application.isPlaying)
                    {
                        if (TerrainContaier == null)
                            TerrainContaier = TerrainStreamingTerrainContainer.Get;

                        if (TerrainContaier != null)
                            OnBaseMapResolutionChanged(value);
                    }
                    else
                        baseMapResolution = availableHeights[m_baseMapResolution_index];

                }
            }
        }
        public void OnBaseMapResolutionChanged(float value)
        {
            baseMapResolution = availableHeights[m_baseMapResolution_index];

            var terrains = TerrainContaier.GetTerrainTiles();
            for (int t = 0; t < terrains.Length; t++)
            {
                var terrain = terrains[t];
                if (terrain != null)
                {
                    terrain.terrainData.baseMapResolution = baseMapResolution;
                }
            }
        }





        public float m_PixelErro;
        public float PixelErro
        {
            get { return m_PixelErro; }
            set
            {
                if (m_PixelErro != value)
                {
                    m_PixelErro = value;
                    if (Application.isPlaying)
                    {
                        if (TerrainContaier == null)
                            TerrainContaier = TerrainStreamingTerrainContainer.Get;

                        if (TerrainContaier != null)
                            OnPixelErroValueChanged(value);
                    }


                }
            }
        }
        public void OnPixelErroValueChanged(float value)
        {
            var terrains = TerrainContaier.GetTerrainTiles();
            for (int t = 0; t < terrains.Length; t++)
            {
                var terrain = terrains[t];
                if (terrain != null)
                    terrain.terrain.heightmapPixelError = value;
            }
        }



       public float m_BaseMapDistance = 1000;
        public float BaseMapDistance
        {
            get { return m_BaseMapDistance; }
            set
            {
                if (m_BaseMapDistance != value)
                {
                    m_BaseMapDistance = value;

                    if (Application.isPlaying)
                    {
                        if (TerrainContaier == null)
                            TerrainContaier = TerrainStreamingTerrainContainer.Get;

                        if (TerrainContaier != null)
                            OnBaseMapDistanceValueChanged(value);
                    }
                 }
            }
        }
        public void OnBaseMapDistanceValueChanged(float value)
        {
            var terrains = TerrainContaier.GetTerrainTiles();
            for (int t = 0; t < terrains.Length; t++)
            {
                var terrain = terrains[t];
                if (terrain != null)
                    terrain.terrain.basemapDistance = value;
            }
         }






        public float m_DetailDistance = 100;
        public float DetailDistance
        {
            get { return m_DetailDistance; }
            set
            {
                if (m_DetailDistance != value)
                {
                    m_DetailDistance = value;

                    if (Application.isPlaying)
                    {
                        if (TerrainContaier == null)
                            TerrainContaier = TerrainStreamingTerrainContainer.Get;

                        if (TerrainContaier != null)
                            OnDetailDistanceValueChanged(value);
                    }

               

                }
            }
        }
        public void OnDetailDistanceValueChanged(float value)
        {
            var terrains = TerrainContaier.GetTerrainTiles();
            for (int t = 0; t < terrains.Length; t++)
            {
                var terrain = terrains[t];
                if (terrain != null)
                    terrain.terrain.detailObjectDistance = value;
            }
        }



        public float m_DetailDensity = 100;
        public float DetailDensity
        {
            get { return m_DetailDensity; }
            set
            {
                if (m_DetailDensity != value)
                {
                    m_DetailDensity = value;

                    if (Application.isPlaying)
                    {
                        if (TerrainContaier == null)
                            TerrainContaier = TerrainStreamingTerrainContainer.Get;

                        if (TerrainContaier != null)
                            OnDetailDensityValueChanged(value);
                    }

                

                }
            }
        }
        public void OnDetailDensityValueChanged(float value)
        {
            var terrains = TerrainContaier.GetTerrainTiles();
            for (int t = 0; t < terrains.Length; t++)
            {
                var terrain = terrains[t];
                if (terrain != null)
                    terrain.terrain.detailObjectDensity = value;
            }
        }


        public float m_TreeDistance = 4000;
        public float TreeDistance
        {
            get { return m_TreeDistance; }
            set
            {
                if (m_TreeDistance != value)
                {
                    m_TreeDistance = value;

                    if (Application.isPlaying)
                    {
                        if (TerrainContaier == null)
                            TerrainContaier = TerrainStreamingTerrainContainer.Get;

                        if (TerrainContaier != null)
                            OnTreeDistanceValueChanged(value);
                    }
                    

                }
            }
        }
        public void OnTreeDistanceValueChanged(float value)
        {
            var terrains = TerrainContaier.GetTerrainTiles();
            for (int t = 0; t < terrains.Length; t++)
            {
                var terrain = terrains[t];
                if (terrain != null)
                    terrain.terrain.treeDistance = value;
            }
        }




        public float m_BillBoardStartDistance = 500;
        public float BillBoardStartDistance
        {
            get { return m_BillBoardStartDistance; }
            set
            {
                if (m_BillBoardStartDistance != value)
                {
                    m_BillBoardStartDistance = value;
                    if (Application.isPlaying)
                    {
                        if (TerrainContaier == null)
                            TerrainContaier = TerrainStreamingTerrainContainer.Get;

                        if (TerrainContaier != null)
                            OnBillBoardStartDistanceValueChanged(value);
                    }
                 }
            }
        }
        public void OnBillBoardStartDistanceValueChanged(float value)
        {
            var terrains = TerrainContaier.GetTerrainTiles();
            for (int t = 0; t < terrains.Length; t++)
            {
                var terrain = terrains[t];
                if (terrain != null)
                    terrain.terrain.treeBillboardDistance = value;
            }
        }



        private float m_FadeLength = 10;
        public float FadeLength
        {
            get { return m_FadeLength; }
            set
            {
                if (m_FadeLength != value)
                {
                    m_FadeLength = value;
                    if (Application.isPlaying)
                    {
                        if (TerrainContaier == null)
                            TerrainContaier = TerrainStreamingTerrainContainer.Get;

                        if (TerrainContaier != null)
                            OnFadeLengthValueChanged(value);
                    }
                 }
            }
        }
        public void OnFadeLengthValueChanged(float value)
        {
            var terrains = TerrainContaier.GetTerrainTiles();
            for (int t = 0; t < terrains.Length; t++)
            {
                var terrain = terrains[t];
                if (terrain != null)
                    terrain.terrain.treeCrossFadeLength = value;
            }
        }

        #endregion

        #region Environement
        [SerializeField]
        public List<TerrainStreamingSO_Tree> TreePrefabs = new List<TerrainStreamingSO_Tree>();
        [SerializeField]
        public List<TerrainStreamingSO_GrassObject> GrassPrefabs = new List<TerrainStreamingSO_GrassObject>();
        public List<TerrainStreamingSO_Road> RoadsPrefab = new List<TerrainStreamingSO_Road>();
        public void LoadAllTreePrefabs()
        {
            var prefabs = Resources.LoadAll("Prefabs/Environment/Trees", typeof(TerrainStreamingSO_Tree));

            if (prefabs.Length > 0)
            {
                TreePrefabs.Clear();

                foreach (var prefab in prefabs)
                {
                    if (prefab != null)
                        TreePrefabs.Add(prefab as TerrainStreamingSO_Tree);
                }

            }
            else
                Debug.Log("Not tree prefabs detected in '/Resources/Prefabs/Environment/Trees'");
        }
        public void LoadAllGrassPrefabs()
        {
            var prefabs = Resources.LoadAll("Prefabs/Environment/Grass", typeof(TerrainStreamingSO_GrassObject));

            if (prefabs.Length > 0)
            {
                GrassPrefabs.Clear();

                foreach (var prefab in prefabs)
                {
                    if (prefab != null)
                        GrassPrefabs.Add(prefab as TerrainStreamingSO_GrassObject);
                }

            }
            else
                Debug.Log("Not Grass prefabs detected in 'Resources/Prefabs/Environment/Grass'");
        }
        public void GetRoadsPrefab(RoadGenerator roadType)
        {
            var roadsPrefab = Resources.LoadAll("Prefabs/Environment/Roads/", typeof(TerrainStreamingSO_Road));

            RoadsPrefab = new List<TerrainStreamingSO_Road>();

            foreach (var road in roadsPrefab)
            {
                var r = (TerrainStreamingSO_Road)(road as TerrainStreamingSO_Road);
                Material mat = null;

                if (r.MaterialType == MaterialSet.Auto)
                {
                    if (roadType == RoadGenerator.SimpleUnityLine)
                    {
                        mat = Resources.Load("Environment/Roads/Materials/StandardLineRender/" + road.name, typeof(Material)) as Material;
                        if (mat) mat.SetColor("_Color", r.RoadColor);
                    }
                }

                if (r.Roadmaterial == null)
                    mat = Resources.Load("Environment/Roads/Materials/Standard", typeof(Material)) as Material;
                else
                    mat = r.Roadmaterial;


                r.Roadmaterial = mat;
                RoadsPrefab.Add(r);

            }

        }

        //[SerializeField]
        //public List<GISTerrainLoaderSO_Building> BuildingsPrefab = new List<GISTerrainLoaderSO_Building>();

        #endregion








    }
}
