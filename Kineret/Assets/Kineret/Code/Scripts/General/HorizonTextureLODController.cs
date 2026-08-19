using System.Collections.Generic;
using System.IO;
using UnityEngine;
using GISTech.TerrainStreaming;

[DisallowMultipleComponent]
public class HorizonTextureLODController : MonoBehaviour
{
    [Header("Altitude thresholds")]
    public float switchToUniformAbove = 35000f;
    public float switchBackToTiledBelow = 30000f;

    [Header("Uniform high-altitude textures")]
    public string uniformFolderName = "HorizonRasterData_UniformHigh";

    private TerrainStreamingSystem system;
    private Transform currentHorizonRoot;
    private TerrainStreamingTerrainTile[] horizonTiles;
    private readonly Dictionary<Vector2Int, Texture2D> tiledTextures = new Dictionary<Vector2Int, Texture2D>();
    private readonly Dictionary<Vector2Int, Texture2D> uniformTextures = new Dictionary<Vector2Int, Texture2D>();
    private bool prepared;
    private bool uniformMode;
    private float retryTimer;

    private void Awake()
    {
        system = FindObjectOfType<TerrainStreamingSystem>(true);
    }

    private void Update()
    {
        if (system == null)
            system = FindObjectOfType<TerrainStreamingSystem>(true);

        if (prepared && currentHorizonRoot != null)
        {
            int currentCount = currentHorizonRoot.GetComponentsInChildren<TerrainStreamingTerrainTile>(true).Length;
            if (horizonTiles == null || currentCount != horizonTiles.Length)
                prepared = false;
        }

        
retryTimer -= Time.unscaledDeltaTime;
        if (!prepared || currentHorizonRoot == null)
        {
            if (retryTimer <= 0f)
            {
                retryTimer = 0.5f;
                TryPrepare();
            }
            return;
        }

        float altitude = GetViewerAltitude();
        if (!uniformMode && altitude >= switchToUniformAbove)
            ApplyUniformTextures();
        else if (uniformMode && altitude <= switchBackToTiledBelow)
            ApplyTiledTextures();
    }

    private float GetViewerAltitude()
    {
        if (system != null && system.prefs != null && system.prefs.player != null)
        {
            Camera cam = system.prefs.player.playerCam;
            if (cam != null)
                return Mathf.Max(0f, cam.transform.position.y);
            return Mathf.Max(0f, system.prefs.player.transform.position.y);
        }

        Camera main = Camera.main;
        return main != null ? Mathf.Max(0f, main.transform.position.y) : 0f;
    }

    private void TryPrepare()
    {
        GameObject rootObject = GameObject.Find("Hr_SectorsContainer");
        if (rootObject == null || system == null || string.IsNullOrEmpty(system.MainTerrainFolderPath))
            return;

        Transform root = rootObject.transform;
        TerrainStreamingTerrainTile[] tiles = root.GetComponentsInChildren<TerrainStreamingTerrainTile>(true);
        int expectedTiles = 1;
        if (system.prefs != null)
            expectedTiles = Mathf.Max(1, system.prefs.Hr_TilesCount.x * system.prefs.Hr_TilesCount.y);
        if (tiles.Length != expectedTiles)
            return;

        if (tiles == null || tiles.Length == 0)
            return;

        foreach (TerrainStreamingTerrainTile tile in tiles)
        {
            if (tile == null || tile.TextureState != LoadingState.Loaded || tile.terrainData == null)
                return;

            TerrainLayer[] layers = tile.terrainData.terrainLayers;
            if (layers == null || layers.Length == 0 || layers[layers.Length - 1] == null || layers[layers.Length - 1].diffuseTexture == null)
                return;
        }

        ClearUniformTextures();
        tiledTextures.Clear();
        uniformMode = false;
        currentHorizonRoot = root;
        horizonTiles = tiles;

        string folder = Path.Combine(system.MainTerrainFolderPath, uniformFolderName);
        foreach (TerrainStreamingTerrainTile tile in horizonTiles)
        {
            TerrainLayer[] layers = tile.terrainData.terrainLayers;
            TerrainLayer layer = layers[layers.Length - 1];
            tiledTextures[tile.Number] = layer.diffuseTexture as Texture2D;

            string path = Path.Combine(folder, "Tile__" + tile.Number.x + "__" + tile.Number.y + ".png");
            if (!File.Exists(path))
            {
                prepared = false;
                return;
            }

            byte[] bytes = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
            if (!tex.LoadImage(bytes, true))
            {
                Destroy(tex);
                prepared = false;
                return;
            }

            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.anisoLevel = 8;
            tex.name = "UniformHorizon_" + tile.Number.x + "_" + tile.Number.y;
            uniformTextures[tile.Number] = tex;
        }

        prepared = uniformTextures.Count == horizonTiles.Length;
        if (prepared && GetViewerAltitude() >= switchToUniformAbove)
            ApplyUniformTextures();
    }

    private void ApplyUniformTextures()
    {
        if (!prepared)
            return;

        ApplyTextureSet(uniformTextures);
        uniformMode = true;
    }

    private void ApplyTiledTextures()
    {
        if (!prepared)
            return;

        ApplyTextureSet(tiledTextures);
        uniformMode = false;
    }

    private void ApplyTextureSet(Dictionary<Vector2Int, Texture2D> set)
    {
        foreach (TerrainStreamingTerrainTile tile in horizonTiles)
        {
            if (tile == null || tile.terrainData == null)
                continue;

            Texture2D tex;
            if (!set.TryGetValue(tile.Number, out tex) || tex == null)
                continue;

            TerrainLayer[] layers = tile.terrainData.terrainLayers;
            if (layers == null || layers.Length == 0)
                continue;

            TerrainLayer layer = layers[layers.Length - 1];
            if (layer == null)
                continue;

            layer.diffuseTexture = tex;
            layer.metallic = 0f;
            layer.smoothness = 0f;
            layer.specular = Color.black;
            tile.terrainData.terrainLayers = layers;
            tile.terrain.Flush();
        }
    }

    private void ClearUniformTextures()
    {
        foreach (Texture2D texture in uniformTextures.Values)
        {
            if (texture != null)
                Destroy(texture);
        }
        uniformTextures.Clear();
    }

    private void OnDestroy()
    {
        ClearUniformTextures();
    }
}
