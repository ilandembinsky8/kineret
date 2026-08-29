/*     Unity GIS Tech 2020-2021      */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace GISTech.TerrainStreaming
{
    public delegate void ReaderEvents();
    public delegate void TerrainProgression(string phasename, float value);
    public delegate void DEMEvent(List<TerrainStreamingTileData> tiles);
    public delegate void TerrainStreamingSystemEvent(TerrainStreamingContainer container);
    public class TerrainStreamingSystem : MonoSingleton<TerrainStreamingSystem>
    {
        public static event TerrainStreamingSystemEvent OnFinish;
 
        private TerrainStreamingTileSector[,] Hr_AllSectors;
        private TerrainStreamingTileSector[,] AllSectors;

        private TerrainStreamingTerrainTile[,] GeneratedTerrains;
        private TerrainStreamingTerrainTile[,] Hr_GeneratedTerrains;

        private List<TerrainStreamingTileSector> m_SectorToLoad = new List<TerrainStreamingTileSector>();
        private List<TerrainStreamingTileSector> m_SectorToLoadC = new List<TerrainStreamingTileSector>();
        private List<TerrainStreamingTileSector> m_SectorToUnLoad = new List<TerrainStreamingTileSector>();

        private readonly HashSet<Vector2Int> m_HighResTextureTargets = new HashSet<Vector2Int>();
        private readonly Queue<TerrainStreamingTerrainTile> m_TextureUpgradeQueue = new Queue<TerrainStreamingTerrainTile>();
        private readonly HashSet<TerrainStreamingTerrainTile> m_QueuedTextureUpgrades = new HashSet<TerrainStreamingTerrainTile>();
        private bool m_TextureUpgradeWorkerRunning = false;


        private List<TerrainStreamingTileSector> m_EnvironmentToLoad = new List<TerrainStreamingTileSector>();
        private List<TerrainStreamingTileSector> m_EnvironmentToLoadC = new List<TerrainStreamingTileSector>();
        private List<TerrainStreamingTileSector> m_EnvironmentToUnLoad = new List<TerrainStreamingTileSector>();
        [HideInInspector]
        public TerrainStreamingContainer SectorContainer;
        private TerrainStreamingContainer Hr_SectorContainer;
        private TerrainStreamingTerrainContainer TerrainContainer;
        private TerrainStreamingTerrainContainer Hr_TerrainContainer;

        private TerrainStreamingContainerDataReader TerrainFiledata;
        private TerrainStreamingContainerDataReader Hr_TerrainFileddata;


        private bool AbleToPlay;
        private bool AbleToUpdate;
        private float TimeCount = 0;

        [HideInInspector]
        public string MainTerrainFolderPath;

        [Header("Optional World Alignment")]
        public bool useCustomWorldBounds = false;
        public Vector2 customWorldMinXZ = Vector2.zero;
        public Vector2 customWorldMaxXZ = Vector2.zero;
        public float customWorldYOffset = 0f;
        [HideInInspector] public bool suppressInitialTerrainVisibility = false;
        [HideInInspector]
        public TerrainStreamingSystemPrefs prefs;


        void Start()
        {
            prefs = TerrainStreamingSystemPrefs.Get;

            if (prefs.terrainMaterialMode == TerrainMaterialMode.Standard || prefs.terrainMaterial == null)
            {
                prefs.terrainMaterial = (Material)Resources.Load("Materials/Default-Terrain-Standard", typeof(Material));

                if (prefs.terrainMaterial == null)
                    Debug.LogError("Custom terrain material null or standard terrain material not found in 'Resources/Materials/Default-Terrain-Standard' ");
            }

            if(prefs.player)
                prefs.player.gameObject.SetActive(false);

            if (prefs.GenerateRoads == OptionEnabDisab.Enable)
                prefs.GetRoadsPrefab(RoadGenerator.SimpleUnityLine);
        }
        /// <summary>
        /// Spends a slice of each frame on terrain loading and reports when that slice is used up.
        ///
        /// Tile loading is entirely synchronous main-thread work (a raw file read plus SetHeights,
        /// or a file read plus Texture2D.LoadImage), so it cannot be overlapped with threads - the
        /// Unity calls are main-thread only and TerrainStreamingRawLoader keeps its depth/byte-order
        /// in static fields that concurrent readers would race on. What it can do is stop paying a
        /// frame per tile. The slice re-sizes itself from what each frame actually costs, so a
        /// weaker machine does fewer tiles per frame and a stronger one more, and neither drops the
        /// loading animation below the target rate.
        /// </summary>
        private class TerrainLoadBudget
        {
            private const double TargetFrameMs = 1000.0 / 30.0;
            private const double MinBudgetMs = 4.0;
            private const double MaxBudgetMs = 24.0;

            private readonly System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            private double budgetMs = 12.0;

            public TerrainLoadBudget()
            {
                timer.Start();
            }

            public bool FrameIsFull
            {
                get { return timer.Elapsed.TotalMilliseconds >= budgetMs; }
            }

            public void BeginFrame()
            {
                double frameMs = Time.unscaledDeltaTime * 1000.0;
                double otherWorkMs = System.Math.Max(0.0, frameMs - budgetMs);

                budgetMs = System.Math.Min(MaxBudgetMs, System.Math.Max(MinBudgetMs, TargetFrameMs - otherWorkMs));

                timer.Restart();
            }
        }

        public IEnumerator GenerateTerrains()
        {
            if (prefs.player)
            {
                Clear();
                m_SectorToLoadC.Clear();
                m_EnvironmentToLoadC.Clear();
                m_HighResTextureTargets.Clear();
                m_TextureUpgradeQueue.Clear();
                m_QueuedTextureUpgrades.Clear();
                m_TextureUpgradeWorkerRunning = false;

                AllSectors = null;
                GeneratedTerrains = null;
                StopAllCoroutines();
                AbleToPlay = false;

                GeneratedTerrains = new TerrainStreamingTerrainTile[prefs.TilesCount.x, prefs.TilesCount.y];

                GenerateTerrainSectors();


                if (prefs.GenerateHorizon == OptionEnabDisab.Enable)
                {
                    Hr_GeneratedTerrains = new TerrainStreamingTerrainTile[prefs.Hr_TilesCount.x, prefs.Hr_TilesCount.y];

                    GenerateHorizonTerrainSectors();

                    var horizonBudget = new TerrainLoadBudget();

                    foreach (var sector in Hr_AllSectors)
                    {
                        var terrainTile = CreateTerrain(Hr_SectorContainer.transform, Hr_SectorContainer, Hr_AllSectors, sector, sector.Number.x, sector.Number.y, SectorContainer.SubTerrainSize, SectorContainer.Scale);
                        sector.TerrainTile = terrainTile;
                        terrainTile.container = Hr_SectorContainer;

                        Hr_GeneratedTerrains[sector.Number.x, sector.Number.y] = terrainTile;
                        terrainTile.size = Hr_SectorContainer.SubTerrainSize;

                        // Horizon terrains stay on their full-resolution texture even from high altitude.
                        terrainTile.terrain.basemapDistance = 500000f;
                        terrainTile.terrain.heightmapPixelError = 1f;

                        terrainTile.transform.position = new Vector3(terrainTile.transform.position.x, 0, terrainTile.transform.position.z);
                        
                        terrainTile.LoadElevationImmediate(true);

                        if (prefs.GenerateTextures == OptionEnabDisab.Enable)
                        {
                            terrainTile.LoadTextureImmediate(terrainTile, true);
                        }

                        if (horizonBudget.FrameIsFull)
                        {
                            yield return null;
                            horizonBudget.BeginFrame();
                        }
                    }

                    var Hr_SectorsContainer_Ob = GameObject.Find("Hr_SectorsContainer");

                    if (Hr_SectorsContainer_Ob)
                    {
                        float horizonBaseY = useCustomWorldBounds ? customWorldYOffset : 0f;
                        Hr_SectorsContainer_Ob.transform.position = new Vector3(
                            Hr_SectorsContainer_Ob.transform.position.x,
                            horizonBaseY + prefs.HorizonYOffest,
                            Hr_SectorsContainer_Ob.transform.position.z);

                    }
                }

                prefs.player.SetBodyActive(false);

                prefs.player.SetStartPosition(SectorContainer);

                TerrainStreamingIntersection.SetParamerters(prefs.player, SectorContainer, prefs, AllSectors);

                CheckForIntersectTiles();

                m_SectorToLoadC.AddRange(m_SectorToLoad);

                var terrainBudget = new TerrainLoadBudget();

                foreach (var sector in m_SectorToLoad)
                {

                    var terrainTile = CreateTerrain(TerrainContainer.transform, SectorContainer, AllSectors, sector, sector.Number.x, sector.Number.y, SectorContainer.SubTerrainSize, SectorContainer.Scale);
                    sector.TerrainTile = terrainTile;
                    terrainTile.container = SectorContainer;

                    GeneratedTerrains[sector.Number.x, sector.Number.y] = terrainTile;
                    terrainTile.size = SectorContainer.SubTerrainSize;

                    terrainTile.LoadElevationImmediate();

                    if (prefs.GenerateTextures == OptionEnabDisab.Enable)
                    {
                        terrainTile.LoadLowResolutionTextureImmediate(terrainTile);
                    }

                    if (terrainBudget.FrameIsFull)
                    {
                        yield return null;
                        terrainBudget.BeginFrame();
                    }
                }

                    foreach (var sector in m_EnvironmentToLoad)
                    {
                        if (sector.TerrainTile)
                        {
                            if (prefs.GenerateTrees == OptionEnabDisab.Enable && prefs.TreePrefabs.Count > 0)
                            {
                                StartCoroutine(sector.TerrainTile.GenerateTrees());
                                yield return new WaitUntil(() => sector.TerrainTile.TreeState != LoadingState.Loading);
                            }
                            if (prefs.GenerateGrass == OptionEnabDisab.Enable && prefs.GrassPrefabs.Count > 0)
                            {
                                StartCoroutine(sector.TerrainTile.GenerateGrass());
                                yield return new WaitUntil(() => sector.TerrainTile.GrassState != LoadingState.Loading);
                            }
                            if (prefs.GenerateRoads == OptionEnabDisab.Enable && prefs.RoadsPrefab.Count > 0)
                            {
                                yield return StartCoroutine(sector.TerrainTile.GenerateRoads());
                                yield return new WaitUntil(() => sector.TerrainTile.RoadState != LoadingState.Loading);
                            }
                        }

                    }
               


                if (OnFinish != null)
                {
                    OnFinish(SectorContainer);
                }

                StartCoroutine(GenerateNeighbors());

                yield return new WaitForSeconds(1);

                prefs.player.SetBodyActive(true);

                prefs.player.GetPositionOnTerrain();

                StartCoroutine(UpdateTerrainsWhen());

                prefs.player.CheckFall(SectorContainer, 2000);
            }
            else
            {
                Debug.Log("No player attached to the streaming system ... !");
                
                yield return null;
            }
     


        }
        private IEnumerator GenerateHorizonTerrains()
        {
            prefs.GenerateHorizonGenerated = true;
            yield return null;
        }
        void Update()
        {
            if (AbleToPlay)
            {
                if (prefs.player.gameObject.activeSelf)
                {

                    TimeCount += Time.deltaTime;

                    if (TimeCount >= prefs.UpdateTime)
                    {
                        Clear();

                        CheckForIntersectTiles();
                        QueueHighResolutionTextureUpgrades();
                        CheckForSimularContent();

                        TimeCount = 0;
                    }

                }

            }
            if (Input.GetKeyDown(KeyCode.R))
            {
                if(prefs.player)
                {
                    //Set Player Position
                    prefs.player.gameObject.SetActive(false);
                    prefs.player.GetPositionOnTerrain();
                    prefs.player.gameObject.SetActive(true);
                }
            }

        }
private void CheckForIntersectTiles()
        {
            var worldPosOffset = useCustomWorldBounds
                ? customWorldMinXZ
                : new Vector2(-SectorContainer.SubTerrainSize.x / 2, -(SectorContainer.ContainerSize.z / 2 - SectorContainer.SubTerrainSize.z / 2));
            List<TerrainStreamingTileSector> TerrainSectors = new List<TerrainStreamingTileSector>();
            List<TerrainStreamingTileSector> EnvironmentSectors = new List<TerrainStreamingTileSector>();

            switch (prefs.player.intersectionMode)
            {
                case IntersectionMode.FieldOfView:
                    if (prefs.player.playerCam)
                    {
                        TerrainSectors = TerrainStreamingIntersection.GetAllTilesInFOV(prefs.player, worldPosOffset, 100);
                        EnvironmentSectors = TerrainStreamingIntersection.GetAllTilesInFOV(prefs.player, worldPosOffset, prefs.player.EnvironmentFOVDistancePercent);
                    }
                    break;
                case IntersectionMode.Area:
                    TerrainSectors = TerrainStreamingIntersection.GetTilesWithinRectangle(prefs.player, worldPosOffset);
                    EnvironmentSectors = TerrainStreamingIntersection.GetTilesWithinRectangle(prefs.player, worldPosOffset);
                    break;
                case IntersectionMode.InCircular:
                    TerrainSectors = TerrainStreamingIntersection.GetTilesInRadius(prefs.player.transform.position, worldPosOffset, prefs.player.m_InCircularRadius);
                    EnvironmentSectors = TerrainStreamingIntersection.GetTilesInRadius(prefs.player.transform.position, worldPosOffset, prefs.player.m_EnvironmentInCircularRadius);
                    break;
            }

            // Keep one full tile outside the visible set preloaded.  This gives a fast
            // camera/player time to cross a tile boundary without exposing the horizon.
            if (TerrainSectors != null && TerrainSectors.Count > 0)
            {
                TerrainSectors = ExpandTerrainSet(TerrainSectors, 1);
                SetHighResolutionTextureTargets(TerrainSectors);
                TerrainSectors = AddForwardPreload(TerrainSectors, worldPosOffset, 6, 1);
            }
            else if (m_SectorToLoadC.Count > 0)
                TerrainSectors = new List<TerrainStreamingTileSector>(m_SectorToLoadC);
            else
                TerrainSectors = new List<TerrainStreamingTileSector>();

            // A transient empty intersection result must not blank the world for one frame.
            if ((EnvironmentSectors == null || EnvironmentSectors.Count == 0) && m_EnvironmentToLoadC.Count > 0)
                EnvironmentSectors = new List<TerrainStreamingTileSector>(m_EnvironmentToLoadC);
            else if (EnvironmentSectors == null)
                EnvironmentSectors = new List<TerrainStreamingTileSector>();

            UpdateSectorsToLoad(TerrainSectors, m_SectorToLoad, true);
            UpdateSectorsToLoad(EnvironmentSectors, m_EnvironmentToLoad);

            foreach (var sector in AllSectors)
            {
                UpdateSectorsToUnLoad(TerrainSectors, m_SectorToUnLoad, sector, true);
                UpdateSectorsToUnLoad(EnvironmentSectors, m_EnvironmentToUnLoad, sector);
            }
        }
        private void UpdateSectorsToLoad(List<TerrainStreamingTileSector> Sectors, List<TerrainStreamingTileSector> SectorsToLoad,bool GreenColor=false)
        {
            if (Sectors != null && Sectors.Count > 0)
            {
                foreach (var sector in Sectors)
                {
                    if (!SectorsToLoad.Contains(sector))
                    {
                        SectorsToLoad.Add(sector);

                        if(GreenColor)
                        sector.SelectedColor = Color.green;
                    }
                }
            }
        }
        private void UpdateSectorsToUnLoad(List<TerrainStreamingTileSector> Sectors, List<TerrainStreamingTileSector> SectorsToUnLoad, TerrainStreamingTileSector sector, bool RedColor = false)
        {
            if (!Sectors.Contains(sector))
            {
                SectorsToUnLoad.Add(sector);

                if(RedColor)
                sector.SelectedColor = Color.red;
            }
        }

private List<TerrainStreamingTileSector> ExpandTerrainSet(List<TerrainStreamingTileSector> source, int ring)
        {
            var result = new List<TerrainStreamingTileSector>();
            if (source == null || source.Count == 0 || AllSectors == null)
                return result;

            var seen = new HashSet<TerrainStreamingTileSector>();
            foreach (var sector in source)
            {
                if (sector == null)
                    continue;

                for (int dx = -ring; dx <= ring; dx++)
                {
                    for (int dy = -ring; dy <= ring; dy++)
                    {
                        int x = sector.Number.x + dx;
                        int y = sector.Number.y + dy;
                        if (x < 0 || y < 0 || x >= AllSectors.GetLength(0) || y >= AllSectors.GetLength(1))
                            continue;

                        var candidate = AllSectors[x, y];
                        if (candidate != null && seen.Add(candidate))
                            result.Add(candidate);
                    }
                }
            }
            return result;
        }

private List<TerrainStreamingTileSector> AddForwardPreload(List<TerrainStreamingTileSector> source, Vector2 worldPosOffset, int tilesAhead, int halfWidth)
        {
            var result = source != null ? new List<TerrainStreamingTileSector>(source) : new List<TerrainStreamingTileSector>();
            if (AllSectors == null || SectorContainer == null || prefs == null || prefs.player == null || prefs.player.playerCam == null)
                return result;

            Camera cam = prefs.player.playerCam;
            Vector3 forward = cam.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
                return result;
            forward.Normalize();

            float tileX = SectorContainer.SubTerrainSize.x;
            float tileZ = SectorContainer.SubTerrainSize.z;
            if (tileX <= 0f || tileZ <= 0f)
                return result;

            Vector3 localCam = new Vector3(
                cam.transform.position.x - worldPosOffset.x,
                0f,
                cam.transform.position.z - worldPosOffset.y);

            float step = Mathf.Min(tileX, tileZ) * 0.9f;
            var seen = new HashSet<TerrainStreamingTileSector>(result);

            for (int i = 1; i <= tilesAhead; i++)
            {
                Vector3 p = localCam + forward * (step * i);
                int col = Mathf.FloorToInt(p.x / tileX);
                int row = Mathf.FloorToInt(p.z / tileZ);
                int centerY = prefs.TilesCount.y - row - 1;

                for (int dx = -halfWidth; dx <= halfWidth; dx++)
                {
                    for (int dy = -halfWidth; dy <= halfWidth; dy++)
                    {
                        int x = col + dx;
                        int y = centerY + dy;
                        if (x < 0 || y < 0 || x >= AllSectors.GetLength(0) || y >= AllSectors.GetLength(1))
                            continue;

                        TerrainStreamingTileSector candidate = AllSectors[x, y];
                        if (candidate != null && seen.Add(candidate))
                            result.Add(candidate);
                    }
                }
            }

            return result;
        }

private void SetHighResolutionTextureTargets(List<TerrainStreamingTileSector> sectors)
        {
            m_HighResTextureTargets.Clear();
            if (sectors == null)
                return;

            foreach (var sector in sectors)
            {
                if (sector != null)
                    m_HighResTextureTargets.Add(sector.Number);
            }
        }

        private void QueueHighResolutionTextureUpgrades()
        {
            if (GeneratedTerrains == null || m_HighResTextureTargets.Count == 0)
                return;

            foreach (Vector2Int number in m_HighResTextureTargets)
            {
                if (number.x < 0 || number.y < 0 || number.x >= GeneratedTerrains.GetLength(0) || number.y >= GeneratedTerrains.GetLength(1))
                    continue;

                TerrainStreamingTerrainTile tile = GeneratedTerrains[number.x, number.y];
                if (tile == null || tile.TextureState != LoadingState.Loaded || tile.IsHighResolutionTexture || tile.HighResolutionTextureLoading)
                    continue;

                if (m_QueuedTextureUpgrades.Add(tile))
                    m_TextureUpgradeQueue.Enqueue(tile);
            }

            if (!m_TextureUpgradeWorkerRunning && m_TextureUpgradeQueue.Count > 0)
                StartCoroutine(ProcessHighResolutionTextureQueue());
        }

        private IEnumerator ProcessHighResolutionTextureQueue()
        {
            m_TextureUpgradeWorkerRunning = true;

            while (m_TextureUpgradeQueue.Count > 0)
            {
                TerrainStreamingTerrainTile tile = m_TextureUpgradeQueue.Dequeue();
                m_QueuedTextureUpgrades.Remove(tile);

                if (tile == null || tile.terrain == null || !m_HighResTextureTargets.Contains(tile.Number))
                {
                    yield return null;
                    continue;
                }

                if (tile.TextureState == LoadingState.Loaded && !tile.IsHighResolutionTexture && !tile.HighResolutionTextureLoading)
                {
                    yield return StartCoroutine(tile.UpgradeToHighResolutionTextureFile(tile));
                    if (tile != null && tile.IsHighResolutionTexture)
                        StitchHighResolutionTileToNeighbors(tile);
                }

                // At most one 2048 texture is promoted at a time, and always yield a
                // rendered frame between promotions so fast travel cannot create a batch spike.
                yield return null;
            }

            m_TextureUpgradeWorkerRunning = false;
        }

        private void StitchHighResolutionTileToNeighbors(TerrainStreamingTerrainTile tile)
        {
            if (tile == null || GeneratedTerrains == null || !tile.IsHighResolutionTexture)
                return;

            Texture2D current = GetTerrainDiffuse(tile);
            if (current == null || !current.isReadable)
                return;

            int x = tile.Number.x;
            int y = tile.Number.y;
            bool changed = false;

            TerrainStreamingTerrainTile neighbor;
            Texture2D other;

            if (x > 0)
            {
                neighbor = GeneratedTerrains[x - 1, y];
                other = neighbor != null && neighbor.IsHighResolutionTexture ? GetTerrainDiffuse(neighbor) : null;
                if (other != null && other.isReadable && other.height == current.height)
                {
                    current.SetPixels(0, 0, 1, current.height, other.GetPixels(other.width - 1, 0, 1, other.height));
                    changed = true;
                }
            }

            if (x + 1 < GeneratedTerrains.GetLength(0))
            {
                neighbor = GeneratedTerrains[x + 1, y];
                other = neighbor != null && neighbor.IsHighResolutionTexture ? GetTerrainDiffuse(neighbor) : null;
                if (other != null && other.isReadable && other.height == current.height)
                {
                    current.SetPixels(current.width - 1, 0, 1, current.height, other.GetPixels(0, 0, 1, other.height));
                    changed = true;
                }
            }

            if (y > 0)
            {
                neighbor = GeneratedTerrains[x, y - 1];
                other = neighbor != null && neighbor.IsHighResolutionTexture ? GetTerrainDiffuse(neighbor) : null;
                if (other != null && other.isReadable && other.width == current.width)
                {
                    current.SetPixels(0, current.height - 1, current.width, 1, other.GetPixels(0, 0, other.width, 1));
                    changed = true;
                }
            }

            if (y + 1 < GeneratedTerrains.GetLength(1))
            {
                neighbor = GeneratedTerrains[x, y + 1];
                other = neighbor != null && neighbor.IsHighResolutionTexture ? GetTerrainDiffuse(neighbor) : null;
                if (other != null && other.isReadable && other.width == current.width)
                {
                    current.SetPixels(0, 0, current.width, 1, other.GetPixels(0, other.height - 1, other.width, 1));
                    changed = true;
                }
            }

            if (changed)
                current.Apply(false, false);
        }



private void ApplyTerrainNeighbors(TerrainStreamingTerrainTile[,] grid)
        {
            if (grid == null)
                return;

            int width = grid.GetLength(0);
            int height = grid.GetLength(1);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var tile = grid[x, y];
                    if (tile == null || tile.terrain == null)
                        continue;

                    Terrain left = x > 0 && grid[x - 1, y] != null ? grid[x - 1, y].terrain : null;
                    Terrain top = y > 0 && grid[x, y - 1] != null ? grid[x, y - 1].terrain : null;
                    Terrain right = x + 1 < width && grid[x + 1, y] != null ? grid[x + 1, y].terrain : null;
                    Terrain bottom = y + 1 < height && grid[x, y + 1] != null ? grid[x, y + 1].terrain : null;

                    tile.terrain.allowAutoConnect = false;
                    tile.terrain.SetNeighbors(left, top, right, bottom);
                }
            }
        }

private void RefreshTerrainNeighbors()
        {
            ApplyTerrainNeighbors(GeneratedTerrains);
            ApplyTerrainNeighbors(Hr_GeneratedTerrains);
        }

private Texture2D GetTerrainDiffuse(TerrainStreamingTerrainTile tile)
        {
            if (tile == null || tile.terrainData == null)
                return null;

            var layers = tile.terrainData.terrainLayers;
            if (layers == null || layers.Length == 0 || layers[layers.Length - 1] == null)
                return null;

            return layers[layers.Length - 1].diffuseTexture as Texture2D;
        }

private void StitchTexturePair(Texture2D a, Texture2D b, bool verticalSeam)
        {
            if (a == null || b == null || !a.isReadable || !b.isReadable)
                return;

            if (verticalSeam)
            {
                int count = Mathf.Min(a.height, b.height);
                for (int y = 0; y < count; y++)
                {
                    Color shared = (a.GetPixel(a.width - 1, y) + b.GetPixel(0, y)) * 0.5f;
                    a.SetPixel(a.width - 1, y, shared);
                    b.SetPixel(0, y, shared);
                }
            }
            else
            {
                int count = Mathf.Min(a.width, b.width);
                for (int x = 0; x < count; x++)
                {
                    // Tile y+1 is geographically south, so its top edge touches
                    // the bottom edge of tile y.
                    Color shared = (a.GetPixel(x, 0) + b.GetPixel(x, b.height - 1)) * 0.5f;
                    a.SetPixel(x, 0, shared);
                    b.SetPixel(x, b.height - 1, shared);
                }
            }

            a.Apply(false, false);
            b.Apply(false, false);
        }

private void StitchLoadedTextureSeams()
        {
            if (GeneratedTerrains == null)
                return;

            int width = GeneratedTerrains.GetLength(0);
            int height = GeneratedTerrains.GetLength(1);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var tile = GeneratedTerrains[x, y];
                    if (tile == null || tile.TextureState != LoadingState.Loaded)
                        continue;

                    Texture2D current = GetTerrainDiffuse(tile);
                    if (current == null)
                        continue;

                    if (x + 1 < width && GeneratedTerrains[x + 1, y] != null && GeneratedTerrains[x + 1, y].TextureState == LoadingState.Loaded)
                        StitchTexturePair(current, GetTerrainDiffuse(GeneratedTerrains[x + 1, y]), true);

                    if (y + 1 < height && GeneratedTerrains[x, y + 1] != null && GeneratedTerrains[x, y + 1].TextureState == LoadingState.Loaded)
                        StitchTexturePair(current, GetTerrainDiffuse(GeneratedTerrains[x, y + 1]), false);
                }
            }
        }

private void DestroyRuntimeTerrain(TerrainStreamingTerrainTile tile)
        {
            if (tile == null)
                return;

            Terrain terrain = tile.terrain;
            TerrainData data = terrain != null ? terrain.terrainData : null;
            if (data != null)
            {
                var layers = data.terrainLayers;
                data.terrainLayers = new TerrainLayer[0];
                if (layers != null)
                {
                    foreach (var layer in layers)
                    {
                        if (layer == null)
                            continue;

                        Texture2D runtimeTexture = layer.diffuseTexture as Texture2D;
                        layer.diffuseTexture = null;
                        UnityEngine.Object.Destroy(layer);
                        if (runtimeTexture != null && tile.TextureState == LoadingState.Loaded)
                            UnityEngine.Object.Destroy(runtimeTexture);
                    }
                }

                terrain.terrainData = null;
                UnityEngine.Object.Destroy(data);
            }

            UnityEngine.Object.Destroy(tile.gameObject);
        }







        private void CheckForSimularContent()
        {
            var content = m_SectorToLoad.All(m_SectorToLoadC.Contains) && m_SectorToLoad.Count == m_SectorToLoadC.Count;

            if (!content)
            {
                if (AbleToUpdate)
                {
                    StartCoroutine(UpdateTerrains());
                }

            }

            var Environmentcontent = m_EnvironmentToLoad.All(m_EnvironmentToLoadC.Contains) && m_EnvironmentToLoad.Count == m_EnvironmentToLoadC.Count;

            if (!Environmentcontent)
            {
                m_EnvironmentToLoadC.Clear();
                m_EnvironmentToLoadC.AddRange(m_EnvironmentToLoad);
                StartCoroutine(UpdateEnvironement());
            }
        }
private IEnumerator UpdateTerrains()
        {
            AbleToUpdate = false;
            var newlyLoadedTerrains = new List<TerrainStreamingTerrainTile>();

            // Load the replacement set FIRST. Old terrains remain visible and every new
            // terrain remains hidden until the whole replacement batch is fully ready.
            for (int i = 0; i < m_SectorToLoad.Count; i++)
            {
                var sector = m_SectorToLoad[i];
                if (GeneratedTerrains[sector.Number.x, sector.Number.y])
                    continue;

                var terrainTile = CreateTerrain(TerrainContainer.transform, SectorContainer, AllSectors, sector,
                    sector.Number.x, sector.Number.y, SectorContainer.SubTerrainSize, SectorContainer.Scale);

                GeneratedTerrains[sector.Number.x, sector.Number.y] = terrainTile;
                sector.TerrainTile = terrainTile;
                terrainTile.container = SectorContainer;

                // Never expose the temporary flat/grey Terrain created by Unity while its
                // DEM/orthophoto are still loading.
                terrainTile.terrain.drawHeightmap = false;
                terrainTile.terrain.drawTreesAndFoliage = false;
                newlyLoadedTerrains.Add(terrainTile);

                StartCoroutine(terrainTile.LoadElevationFile());
                yield return new WaitUntil(() => terrainTile.ElevationState != LoadingState.Loading);

                if (prefs.GenerateTextures == OptionEnabDisab.Enable)
                {
                    StartCoroutine(terrainTile.LoadLowResolutionTextureFile(terrainTile));
                    yield return new WaitUntil(() => terrainTile.TextureState != LoadingState.Loading);
                }
            }

            // Finish all cross-tile work while the new batch is still invisible.
            RefreshTerrainNeighbors();

            // Reveal the whole batch in one frame instead of letting individual tiles pop
            // in one by one during fast movement.
            foreach (var terrainTile in newlyLoadedTerrains)
            {
                if (terrainTile != null && terrainTile.ElevationState == LoadingState.Loaded &&
                    (prefs.GenerateTextures != OptionEnabDisab.Enable || terrainTile.TextureState == LoadingState.Loaded))
                {
                    terrainTile.terrain.drawHeightmap = true;
                }
            }

            // Give the fully prepared replacement set one rendered frame before old tiles go away.
            yield return null;

            foreach (var sectorToUnload in m_SectorToUnLoad)
            {
                if (m_SectorToLoad.Contains(sectorToUnload))
                    continue;

                int unloadX = sectorToUnload.Number.x;
                int unloadY = sectorToUnload.Number.y;
                var tile = GeneratedTerrains[unloadX, unloadY];
                if (tile != null && tile.HighResolutionTextureLoading)
                    continue;

                if (tile != null)
                {
                    GeneratedTerrains[unloadX, unloadY] = null;
                    sectorToUnload.TerrainTile = null;
                    DestroyRuntimeTerrain(tile);
                }
            }

            RefreshTerrainNeighbors();
            StartCoroutine(GenerateNeighbors());
        }
        private IEnumerator UpdateEnvironement()
        {
            for(int s=0;s< m_EnvironmentToLoad.Count;s++)
            {
                var sector = m_EnvironmentToLoad[s];

                if (sector.TerrainTile)
                {
                    if (prefs.GenerateTrees == OptionEnabDisab.Enable)
                    {
                        if (sector.TerrainTile.TreeState != LoadingState.Loaded)
                        {
                            StartCoroutine(sector.TerrainTile.GenerateTrees());
                            yield return new WaitUntil(() => sector.TerrainTile.TreeState != LoadingState.Loading);
                        }
                    }
                    if (prefs.GenerateGrass == OptionEnabDisab.Enable)
                    {
                        if (sector.TerrainTile.GrassState != LoadingState.Loaded)
                        {
                            StartCoroutine(sector.TerrainTile.GenerateGrass());
                            yield return new WaitUntil(() => sector.TerrainTile.GrassState != LoadingState.Loading);
                        }
                    }
                    if (prefs.GenerateRoads == OptionEnabDisab.Enable)
                    {
                        if (sector.TerrainTile.RoadState != LoadingState.Loaded)
                        {
                            yield return StartCoroutine(sector.TerrainTile.GenerateRoads());
                            yield return new WaitUntil(() => sector.TerrainTile.RoadState != LoadingState.Loading);
                        }
                    }
                }
            }
 
            yield return null;
        }      
private IEnumerator GenerateNeighbors()
        {
            RefreshTerrainNeighbors();

            AbleToUpdate = true;
            AbleToPlay = true;
            m_SectorToUnLoad.Clear();

            m_SectorToLoadC.Clear();
            m_SectorToLoadC.AddRange(m_SectorToLoad);

            QueueHighResolutionTextureUpgrades();
            yield return null;
        }
        private IEnumerator UpdateTerrainsWhen()
        {
            yield return new WaitUntil(() => AbleToPlay == true);

            prefs.player.gameObject.SetActive(true);
        }
private void GenerateTerrainSectors()
        {
            AllSectors = new TerrainStreamingTileSector[prefs.TilesCount.x, prefs.TilesCount.y];

            if (GameObject.Find("SectorsContainer"))
                Destroy(GameObject.Find("SectorsContainer"));

            GameObject SectorsContainer_GO = new GameObject("SectorsContainer");
            GetLoadedData(SectorsContainer_GO, TerrainFiledata);

            if (useCustomWorldBounds)
            {
                SectorsContainer_GO.transform.position = new Vector3(customWorldMinXZ.x, customWorldYOffset, customWorldMinXZ.y);
            }
            else
            {
                SectorsContainer_GO.transform.position = new Vector3(
                    SectorsContainer_GO.transform.position.x - SectorContainer.SubTerrainSize.x / 2,
                    0,
                    SectorsContainer_GO.transform.position.z - (SectorContainer.ContainerSize.z / 2) + (SectorContainer.SubTerrainSize.z / 2));
            }

            var LonStep = (SectorContainer.BottomRightCoordiante.x - SectorContainer.UpperLeftCoordinate.x) / SectorContainer.TilesCount.x;
            var LatStep = (SectorContainer.UpperLeftCoordinate.y - SectorContainer.BottomRightCoordiante.y) / SectorContainer.TilesCount.y;

            for (int x = 0; x < prefs.TilesCount.x; x++)
            {
                for (int y = 0; y < prefs.TilesCount.y; y++)
                {
                    var TerrainTileSectorData = new TerrainStreamingTileData(string.Format("Tile_{0}__{1}", x, y));
                    TerrainTileSectorData.Number = new Vector2Int(x, y);

                    if (useCustomWorldBounds)
                    {
                        TerrainTileSectorData.Position = new Vector3(
                            customWorldMinXZ.x + (x + 0.5f) * SectorContainer.SubTerrainSize.x,
                            customWorldYOffset,
                            customWorldMaxXZ.y - (y + 0.5f) * SectorContainer.SubTerrainSize.z);
                    }
                    else
                    {
                        TerrainTileSectorData.Position = new Vector3(
                            SectorContainer.SubTerrainSize.x * x,
                            0,
                            SectorContainer.ContainerSize.z / 2 - SectorContainer.SubTerrainSize.z * y);
                    }

                    TerrainTileSectorData.TileBounds = new Bounds(TerrainTileSectorData.Position, new Vector3(SectorContainer.SubTerrainSize.x, 0, SectorContainer.SubTerrainSize.z));
                    TerrainTileSectorData.UpperLeftCoordinate = new DVector2(SectorContainer.UpperLeftCoordinate.x + x * LonStep, SectorContainer.UpperLeftCoordinate.y - y * LatStep);
                    TerrainTileSectorData.BottomRightCoordiante = new DVector2(TerrainTileSectorData.UpperLeftCoordinate.x + LonStep, TerrainTileSectorData.UpperLeftCoordinate.y - LatStep);
                    TerrainTileSectorData.UpperLeftPointMercator = TerrainStreamingGeoConversion.LatLongToMercat(TerrainTileSectorData.UpperLeftCoordinate.x, TerrainTileSectorData.UpperLeftCoordinate.y);
                    TerrainTileSectorData.BottomRightPointMercator = TerrainStreamingGeoConversion.LatLongToMercat(TerrainTileSectorData.BottomRightCoordiante.x, TerrainTileSectorData.BottomRightCoordiante.y);

                    GameObject TileSector = new GameObject(TerrainTileSectorData.Name);
                    var Tile = TileSector.AddComponent<TerrainStreamingTileSector>();
                    Tile.TileData = TerrainTileSectorData;
                    Tile.tileBounds = TerrainTileSectorData.TileBounds;
                    Tile.size = SectorContainer.SubTerrainSize;
                    Tile.Number = TerrainTileSectorData.Number;
                    TileSector.transform.position = TerrainTileSectorData.Position;
                    TileSector.transform.parent = SectorsContainer_GO.transform;
                    AllSectors[x, y] = Tile;
                }
            }

            const string containerName = "Terrains";
            Destroy(GameObject.Find(containerName));
            var container_obj = new GameObject(containerName);
            TerrainContainer = container_obj.AddComponent<TerrainStreamingTerrainContainer>();
            container_obj.transform.position = SectorsContainer_GO.transform.position;
        }
private void GenerateHorizonTerrainSectors()
        {
            Hr_AllSectors = new TerrainStreamingTileSector[prefs.Hr_TilesCount.x, prefs.Hr_TilesCount.y];

            if (GameObject.Find("Hr_SectorsContainer"))
                Destroy(GameObject.Find("Hr_SectorsContainer"));

            GameObject Hr_SectorsContainer_GO = new GameObject("Hr_SectorsContainer");
            GetLoadedData_Hr(Hr_SectorsContainer_GO, Hr_TerrainFileddata);

            if (useCustomWorldBounds)
            {
                Hr_SectorsContainer_GO.transform.position = new Vector3(customWorldMinXZ.x, customWorldYOffset, customWorldMinXZ.y);
            }
            else
            {
                Hr_SectorsContainer_GO.transform.position = new Vector3(
                    Hr_SectorsContainer_GO.transform.position.x - SectorContainer.SubTerrainSize.x / 2,
                    0,
                    Hr_SectorsContainer_GO.transform.position.z - (SectorContainer.ContainerSize.z / 2) + (SectorContainer.SubTerrainSize.z / 2));
            }

            var LonStep = (Hr_SectorContainer.BottomRightCoordiante.x - Hr_SectorContainer.UpperLeftCoordinate.x) / Hr_SectorContainer.TilesCount.x;
            var LatStep = (Hr_SectorContainer.UpperLeftCoordinate.y - Hr_SectorContainer.BottomRightCoordiante.y) / Hr_SectorContainer.TilesCount.y;

            for (int x = 0; x < prefs.Hr_TilesCount.x; x++)
            {
                for (int y = 0; y < prefs.Hr_TilesCount.y; y++)
                {
                    var TerrainTileSectorData = new TerrainStreamingTileData(string.Format("Tile_{0}__{1}", x, y));
                    TerrainTileSectorData.Number = new Vector2Int(x, y);

                    if (useCustomWorldBounds)
                    {
                        TerrainTileSectorData.Position = new Vector3(
                            customWorldMinXZ.x + (x + 0.5f) * Hr_SectorContainer.SubTerrainSize.x,
                            customWorldYOffset,
                            customWorldMaxXZ.y - (y + 0.5f) * Hr_SectorContainer.SubTerrainSize.z);
                    }
                    else
                    {
                        TerrainTileSectorData.Position = new Vector3(
                            Hr_SectorsContainer_GO.transform.position.x + Hr_SectorContainer.SubTerrainSize.x * x + Hr_SectorContainer.SubTerrainSize.x / 2,
                            0,
                            Hr_SectorsContainer_GO.transform.position.z + Hr_SectorContainer.SubTerrainSize.z * (Hr_SectorContainer.TilesCount.y - y - 1) + Hr_SectorContainer.SubTerrainSize.z / 2);
                    }

                    TerrainTileSectorData.TileBounds = new Bounds(TerrainTileSectorData.Position, new Vector3(Hr_SectorContainer.SubTerrainSize.x, 0, Hr_SectorContainer.SubTerrainSize.z));
                    TerrainTileSectorData.UpperLeftCoordinate = new DVector2(Hr_SectorContainer.UpperLeftCoordinate.x + x * LonStep, Hr_SectorContainer.UpperLeftCoordinate.y - y * LatStep);
                    TerrainTileSectorData.BottomRightCoordiante = new DVector2(TerrainTileSectorData.UpperLeftCoordinate.x + LonStep, TerrainTileSectorData.UpperLeftCoordinate.y - LatStep);
                    TerrainTileSectorData.UpperLeftPointMercator = TerrainStreamingGeoConversion.LatLongToMercat(TerrainTileSectorData.UpperLeftCoordinate.x, TerrainTileSectorData.UpperLeftCoordinate.y);
                    TerrainTileSectorData.BottomRightPointMercator = TerrainStreamingGeoConversion.LatLongToMercat(TerrainTileSectorData.BottomRightCoordiante.x, TerrainTileSectorData.BottomRightCoordiante.y);

                    GameObject TileSector = new GameObject(TerrainTileSectorData.Name);
                    var Tile = TileSector.AddComponent<TerrainStreamingTileSector>();
                    Tile.TileData = TerrainTileSectorData;
                    Tile.tileBounds = TerrainTileSectorData.TileBounds;
                    Tile.size = Hr_SectorContainer.SubTerrainSize;
                    Tile.Number = TerrainTileSectorData.Number;
                    TileSector.transform.parent = Hr_SectorsContainer_GO.transform;
                    TileSector.transform.position = TerrainTileSectorData.Position;
                    Hr_AllSectors[x, y] = Tile;
                }
            }

            Hr_TerrainContainer = Hr_SectorContainer.gameObject.AddComponent<TerrainStreamingTerrainContainer>();
            Hr_SectorContainer.transform.position = Hr_SectorsContainer_GO.transform.position;
        }
        private TerrainStreamingTerrainTile CreateTerrain(Transform parent, TerrainStreamingContainer SectorContainer, TerrainStreamingTileSector[,] allSectors, TerrainStreamingTileSector sector, int x, int y, Vector3 size, Vector3 scale)
        {

            TerrainData tdata = new TerrainData();

            int terrainHeightmapResolution = prefs.heightmapResolution;
            if (SectorContainer == Hr_SectorContainer && Hr_TerrainFileddata != null)
                terrainHeightmapResolution = Hr_TerrainFileddata.Container.heightmapResolution;
            else if (TerrainFiledata != null)
                terrainHeightmapResolution = TerrainFiledata.Container.heightmapResolution;

            tdata.heightmapResolution = terrainHeightmapResolution;
            tdata.SetDetailResolution(prefs.detailResolution, prefs.resolutionPerPatch);

            tdata.size = SectorContainer.SubTerrainSize;


            GameObject tile = Terrain.CreateTerrainGameObject(tdata);
            tile.gameObject.SetActive(true);
            tile.name = string.Format("TerrainTile_{0}__{1}", x, y);
            tile.transform.parent = parent;
            float terrainWorldY = useCustomWorldBounds ? customWorldYOffset : 0f;
            tile.transform.position = new Vector3(
                sector.transform.position.x - sector.size.x / 2,
                terrainWorldY,
                sector.transform.position.z - sector.size.z / 2);



            var terrain = tile.GetComponent<Terrain>();

            // Do not let Unity auto-connect the coarse Horizon (513) to the
            // high-detail grid (129).  Each grid receives explicit neighbors below.
            terrain.allowAutoConnect = false;
            terrain.groupingID = SectorContainer == Hr_SectorContainer ? 2 : 1;


            terrain.heightmapPixelError = 1f;
            terrain.basemapDistance = prefs.BaseMapDistance;
            terrain.detailObjectDistance = prefs.DetailDistance;
            terrain.detailObjectDensity = prefs.DetailDensity;
            terrain.treeDistance = prefs.TreeDistance;
            terrain.treeBillboardDistance = prefs.BillBoardStartDistance;  
            terrain.treeCrossFadeLength = prefs.FadeLength;
            terrain.terrainData.SetDetailResolution(prefs.detailResolution, prefs.resolutionPerPatch);
            terrain.terrainData.baseMapResolution = prefs.baseMapResolution;

        
            terrain.materialTemplate = prefs.terrainMaterial;

            if (suppressInitialTerrainVisibility)
                terrain.drawHeightmap = false;

            TerrainStreamingTerrainTile item = tile.AddComponent<TerrainStreamingTerrainTile>();
            item.Number = new Vector2Int(x,y);
            item.size = size;
            item.MainDataFolder = MainTerrainFolderPath;
            item.prefs = prefs;
            item.TileSector = allSectors[x, y];


            return item;
        }
        public void LoadTerrainDataFile(string terrainDataPath)
        {
            var MainDir = Path.GetDirectoryName(terrainDataPath);

            if (Directory.Exists(MainDir))
            {
                TerrainFiledata = new TerrainStreamingContainerDataReader(terrainDataPath);

                prefs.TilesCount = TerrainFiledata.Container.TilesCount;
                prefs.Dimensions = TerrainFiledata.Container.Dimensions;
                prefs.TerrainHeight = TerrainFiledata.Container.SubTerrainSize.y;

                TerrainStreamingIntersection.SetParamerters(prefs.player, SectorContainer, prefs, AllSectors);

                if (prefs.GenerateHorizon == OptionEnabDisab.Enable)
                {
                    var Hr_Path = terrainDataPath.Replace(".dat", "_Hr.hor");

                    if (File.Exists(Hr_Path))
                    {
                        Hr_TerrainFileddata = new TerrainStreamingContainerDataReader(Hr_Path);
                        prefs.Hr_TilesCount = Hr_TerrainFileddata.Container.TilesCount;

                    }else
                    {
                        prefs.GenerateHorizon = OptionEnabDisab.Disable;
                        Debug.LogError("Horizon Terrain MetaData not found");
                    }
                }
                MainTerrainFolderPath = MainDir;
            }
            else
                Debug.LogError("Terrain MetaData File or Directory not found");
        }
        public void GetLoadedData(GameObject sectorsContainer_go, TerrainStreamingContainerDataReader m_TerrainFiledata)
        {
            SectorContainer = sectorsContainer_go.AddComponent<TerrainStreamingContainer>();
            SectorContainer.ResetData();

            SectorContainer.ZoneName = m_TerrainFiledata.Container.ZoneName;
            SectorContainer.UpperLeftCoordinate = m_TerrainFiledata.Container.UpperLeftCoordinate;
            SectorContainer.BottomRightCoordiante = m_TerrainFiledata.Container.BottomRightCoordiante;
            SectorContainer.TilesCount = m_TerrainFiledata.Container.TilesCount;

            SectorContainer.Dimensions = m_TerrainFiledata.Container.Dimensions;
            SectorContainer.MinMaxElevation = m_TerrainFiledata.Container.MinMaxElevation;
            SectorContainer.GeneratedTerrainfolder = m_TerrainFiledata.Container.GeneratedTerrainfolder;
            SectorContainer.AllSectorsData = m_TerrainFiledata.Container.AllSectorsData;
            SectorContainer.Sectors = m_TerrainFiledata.Container.Sectors;
            SectorContainer.TerrainContainer = TerrainContainer;

            float ElevationRange = SectorContainer.MinMaxElevation.y - SectorContainer.MinMaxElevation.x;

            var sizeX = Mathf.Floor((float)SectorContainer.Dimensions.x * prefs.terrainScale.x * prefs.ScaleFactor) / SectorContainer.TilesCount.x;
            var sizeZ = Mathf.Floor((float)SectorContainer.Dimensions.y * prefs.terrainScale.z * prefs.ScaleFactor) / SectorContainer.TilesCount.y;

            if (useCustomWorldBounds)
            {
                sizeX = (customWorldMaxXZ.x - customWorldMinXZ.x) / SectorContainer.TilesCount.x;
                sizeZ = (customWorldMaxXZ.y - customWorldMinXZ.y) / SectorContainer.TilesCount.y;
            }
            var sizeY = (ElevationRange / prefs.ElevationScaleValue) * prefs.terrainScale.y * prefs.ScaleFactor;

            SectorContainer.SubTerrainSize = new Vector3(sizeX, sizeY, sizeZ);
            SectorContainer.ContainerSize = new Vector3(SectorContainer.SubTerrainSize.x * SectorContainer.TilesCount.x, sizeY, SectorContainer.SubTerrainSize.z * SectorContainer.TilesCount.y);
            SectorContainer.Scale = prefs.terrainScale;

            SectorContainer.DRPointMercator = TerrainStreamingGeoConversion.LatLongToMercat(SectorContainer.BottomRightCoordiante.x, SectorContainer.BottomRightCoordiante.y);
            SectorContainer.TLPointMercator = TerrainStreamingGeoConversion.LatLongToMercat(SectorContainer.UpperLeftCoordinate.x, SectorContainer.UpperLeftCoordinate.y);

            prefs.heightmapResolution = m_TerrainFiledata.Container.heightmapResolution;

        }
        public void GetLoadedData_Hr(GameObject sectorsContainer_go, TerrainStreamingContainerDataReader TerrainFiledata)
        {
            Hr_SectorContainer = sectorsContainer_go.AddComponent<TerrainStreamingContainer>();
            Hr_SectorContainer.ResetData();

            Hr_SectorContainer.ZoneName = TerrainFiledata.Container.ZoneName;
            Hr_SectorContainer.UpperLeftCoordinate = TerrainFiledata.Container.UpperLeftCoordinate;
            Hr_SectorContainer.BottomRightCoordiante = TerrainFiledata.Container.BottomRightCoordiante;
            Hr_SectorContainer.TilesCount = TerrainFiledata.Container.TilesCount;

            Hr_SectorContainer.Dimensions = TerrainFiledata.Container.Dimensions;
            Hr_SectorContainer.MinMaxElevation = TerrainFiledata.Container.MinMaxElevation;
            Hr_SectorContainer.GeneratedTerrainfolder = TerrainFiledata.Container.GeneratedTerrainfolder;
            Hr_SectorContainer.AllSectorsData = TerrainFiledata.Container.AllSectorsData;
            Hr_SectorContainer.Sectors = TerrainFiledata.Container.Sectors;

            Hr_SectorContainer.TerrainContainer = Hr_TerrainContainer;

            float ElevationRange = Hr_SectorContainer.MinMaxElevation.y - Hr_SectorContainer.MinMaxElevation.x;

            var sizeX = Mathf.Floor((float)Hr_SectorContainer.Dimensions.x * prefs.terrainScale.x * prefs.ScaleFactor) / Hr_SectorContainer.TilesCount.x;
            var sizeZ = Mathf.Floor((float)Hr_SectorContainer.Dimensions.y * prefs.terrainScale.z * prefs.ScaleFactor) / Hr_SectorContainer.TilesCount.y;

            if (useCustomWorldBounds)
            {
                sizeX = (customWorldMaxXZ.x - customWorldMinXZ.x) / Hr_SectorContainer.TilesCount.x;
                sizeZ = (customWorldMaxXZ.y - customWorldMinXZ.y) / Hr_SectorContainer.TilesCount.y;
            }
            var sizeY = (ElevationRange / prefs.ElevationScaleValue) * prefs.terrainScale.y * prefs.ScaleFactor;

            Hr_SectorContainer.SubTerrainSize = new Vector3(sizeX, sizeY, sizeZ);
            Hr_SectorContainer.ContainerSize = new Vector3(Hr_SectorContainer.SubTerrainSize.x * Hr_SectorContainer.TilesCount.x, sizeY, Hr_SectorContainer.SubTerrainSize.z * Hr_SectorContainer.TilesCount.y);
            Hr_SectorContainer.Scale = prefs.terrainScale;

            Hr_SectorContainer.DRPointMercator = TerrainStreamingGeoConversion.LatLongToMercat(Hr_SectorContainer.BottomRightCoordiante.x, Hr_SectorContainer.BottomRightCoordiante.y);
            Hr_SectorContainer.TLPointMercator = TerrainStreamingGeoConversion.LatLongToMercat(Hr_SectorContainer.UpperLeftCoordinate.x, Hr_SectorContainer.UpperLeftCoordinate.y);

        }
private void Clear()
        {
            // Clear only the newly calculated target sets.  The *C lists are the
            // previous stable frame and must survive between streaming updates.
            m_SectorToLoad.Clear();
            m_SectorToUnLoad.Clear();
            m_EnvironmentToLoad.Clear();
            m_EnvironmentToUnLoad.Clear();
        }
        void OnDisable()
        {
            GeneratedTerrains = null;
            if (prefs.player) prefs.player.gameObject.SetActive(false);
        }

    }
}