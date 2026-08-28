using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using GISTech.TerrainStreaming;

[DefaultExecutionOrder(100)]
public class KineretTerrainBootstrap : MonoBehaviour
{
    public static event Action OnTerrainReady;

    [Header("Terrain Streaming")]
    [SerializeField] private TerrainStreamingSystem system;
    [SerializeField] private TerrainStreamingSystemPrefs prefs;
    [SerializeField] private TerrainStreamingPlayer streamingProxy;
    [SerializeField] private Camera gameplayCamera;

    [Header("Legacy Terrain")]
    [SerializeField] private Terrain legacyTerrain;
    [SerializeField] private bool disableLegacyTerrainWhenReady = true;
    [SerializeField] private bool autoCalibrateVerticalOffset = true;

    [Header("Terrain Data")]
    [SerializeField] private string editorTerrainDataPath = @"D:\Ido\Work\Ilan\Kineret\map\TSS_North_38x41_2048\TerrainData.dat";
    [SerializeField] private string deployedTerrainFolder = "KineretTerrain";

    [Header("Optional Camera LOD")]
    [SerializeField] private DynamicFarClipByAltitude altitudeController;

    public static bool IsReady { get; private set; }
    public static TerrainStreamingContainer ActiveContainer { get; private set; }

    private bool finalizationStarted;

    private void Awake()
    {
        IsReady = false;
        ActiveContainer = null;

        if (gameplayCamera == null)
            gameplayCamera = Camera.main;

        if (system == null)
            system = GetComponent<TerrainStreamingSystem>();

        if (prefs == null)
            prefs = GetComponent<TerrainStreamingSystemPrefs>();

        if (streamingProxy != null && gameplayCamera != null)
            streamingProxy.playerCam = gameplayCamera;

        if (prefs != null && streamingProxy != null)
            prefs.player = streamingProxy;

        if (system != null && prefs != null)
            system.prefs = prefs;

        if (system != null)
            system.suppressInitialTerrainVisibility = true;

        if (altitudeController != null)
            altitudeController.enabled = false;
    }

    private IEnumerator Start()
    {
        if (system == null || prefs == null || streamingProxy == null || gameplayCamera == null)
        {
            Debug.LogError("[KineretTerrain] Missing TSS integration references; automatic terrain startup aborted.");
            yield break;
        }

        string terrainDataPath = ResolveTerrainDataPath();
        if (string.IsNullOrEmpty(terrainDataPath))
        {
            Debug.LogError("[KineretTerrain] TerrainData.dat was not found. Checked StreamingAssets, deployment folder and editor override.");
            yield break;
        }

        if (!TryConfigureWorldAlignment(terrainDataPath))
        {
            Debug.LogError("[KineretTerrain] Could not calculate world alignment from TerrainData.dat and game Texture settings.");
            yield break;
        }

        TerrainStreamingSystem.OnFinish += HandleTerrainGenerated;

        system.LoadTerrainDataFile(terrainDataPath);
        Debug.Log("[KineretTerrain] Automatic terrain generation started from: " + terrainDataPath);

        // Host the enumerator on this bootstrap rather than on TerrainStreamingSystem.
        // GenerateTerrains() calls StopAllCoroutines() on the TSS component at startup.
        yield return StartCoroutine(system.GenerateTerrains());
    }

    private void OnDestroy()
    {
        TerrainStreamingSystem.OnFinish -= HandleTerrainGenerated;
    }

    private void HandleTerrainGenerated(TerrainStreamingContainer container)
    {
        if (finalizationStarted)
            return;

        finalizationStarted = true;
        ActiveContainer = container;
        StartCoroutine(FinalizeTerrain(container));
    }

private IEnumerator FinalizeTerrain(TerrainStreamingContainer container)
    {
        // Let neighbor connections/material uploads settle for one rendered frame while
        // the legacy terrain is still the only visible ground.
        yield return null;

        if (autoCalibrateVerticalOffset && legacyTerrain != null)
            CalibrateVerticalOffsetAgainstLegacy();

        if (system != null)
            system.suppressInitialTerrainVisibility = false;

        // Swap old -> new in the same frame, after the streamed terrain is aligned.
        if (disableLegacyTerrainWhenReady && legacyTerrain != null)
            legacyTerrain.gameObject.SetActive(false);

        foreach (TerrainStreamingTerrainTile tile in FindObjectsOfType<TerrainStreamingTerrainTile>(true))
        {
            if (tile != null && tile.terrain != null && tile.ElevationState == LoadingState.Loaded)
                tile.terrain.drawHeightmap = true;
        }

        if (altitudeController != null)
            altitudeController.enabled = true;

        IsReady = true;
        OnTerrainReady?.Invoke();
        Debug.Log("[KineretTerrain] READY. Legacy terrain disabled=" +
                  (disableLegacyTerrainWhenReady && legacyTerrain != null) +
                  ", world bounds XZ=" + system.customWorldMinXZ + " -> " + system.customWorldMaxXZ +
                  ", Y offset=" + system.customWorldYOffset.ToString("F2", CultureInfo.InvariantCulture));
    }

    private string ResolveTerrainDataPath()
    {
        var candidates = new List<string>();

        if (!string.IsNullOrEmpty(Application.streamingAssetsPath))
            candidates.Add(Path.Combine(Application.streamingAssetsPath, deployedTerrainFolder, "TerrainData.dat"));

        string playerRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        candidates.Add(Path.Combine(playerRoot, deployedTerrainFolder, "TerrainData.dat"));
        candidates.Add(Path.Combine(playerRoot, "TerrainData", "TerrainData.dat"));

#if UNITY_EDITOR
        if (!string.IsNullOrWhiteSpace(editorTerrainDataPath))
            candidates.Add(editorTerrainDataPath);
#endif

        foreach (string candidate in candidates)
        {
            if (!string.IsNullOrEmpty(candidate) && File.Exists(candidate))
                return Path.GetFullPath(candidate);
        }

        return null;
    }

    private bool TryConfigureWorldAlignment(string terrainDataPath)
    {
        Dictionary<string, double> metadata = ParseTerrainMetadata(terrainDataPath);

        double west, east, north, south;
        if (!metadata.TryGetValue("UpperLeftCoordinate_x", out west) ||
            !metadata.TryGetValue("UpperLeftCoordinate_y", out north) ||
            !metadata.TryGetValue("BottomRightCoordiante_x", out east) ||
            !metadata.TryGetValue("BottomRightCoordiante_y", out south))
            return false;

        float gameMinLon = GameSettingsManager.GetFloat("Texture", "min_lon");
        float gameMaxLon = GameSettingsManager.GetFloat("Texture", "max_lon");
        float gameMinLat = GameSettingsManager.GetFloat("Texture", "min_lat");
        float gameMaxLat = GameSettingsManager.GetFloat("Texture", "max_lat");
        float gameWidth = GameSettingsManager.GetFloat("Texture", "width");
        float gameHeight = GameSettingsManager.GetFloat("Texture", "height");

        if (Mathf.Approximately(gameMaxLon, gameMinLon) || Mathf.Approximately(gameMaxLat, gameMinLat) || gameWidth <= 0f || gameHeight <= 0f)
            return false;

        float minX = (float)((west - gameMinLon) / (gameMaxLon - gameMinLon) * gameWidth);
        float maxX = (float)((east - gameMinLon) / (gameMaxLon - gameMinLon) * gameWidth);
        float minZ = (float)((south - gameMinLat) / (gameMaxLat - gameMinLat) * gameHeight);
        float maxZ = (float)((north - gameMinLat) / (gameMaxLat - gameMinLat) * gameHeight);

        system.useCustomWorldBounds = true;
        system.customWorldMinXZ = new Vector2(minX, minZ);
        system.customWorldMaxXZ = new Vector2(maxX, maxZ);
        system.customWorldYOffset = 0f;

        Debug.Log(string.Format(CultureInfo.InvariantCulture,
            "[KineretTerrain] Alignment calculated: X {0:F2}..{1:F2}, Z {2:F2}..{3:F2}",
            minX, maxX, minZ, maxZ));

        return true;
    }

    private static Dictionary<string, double> ParseTerrainMetadata(string path)
    {
        var result = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (string rawLine in File.ReadAllLines(path))
        {
            int equals = rawLine.IndexOf('=');
            if (equals < 0)
                continue;

            string key = rawLine.Substring(0, equals).Trim();
            string value = rawLine.Substring(equals + 1).Trim();
            double parsed;
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
                result[key] = parsed;
        }
        return result;
    }

    private void CalibrateVerticalOffsetAgainstLegacy()
    {
        if (legacyTerrain == null || system == null)
            return;

        TerrainStreamingTerrainTile[] tiles = FindObjectsOfType<TerrainStreamingTerrainTile>(true);
        var deltas = new List<float>();

        Vector3 legacyPosition = legacyTerrain.transform.position;
        Vector3 legacySize = legacyTerrain.terrainData.size;
        float legacyMinX = legacyPosition.x;
        float legacyMaxX = legacyPosition.x + legacySize.x;
        float legacyMinZ = legacyPosition.z;
        float legacyMaxZ = legacyPosition.z + legacySize.z;

        foreach (TerrainStreamingTerrainTile tile in tiles)
        {
            if (tile == null || tile.terrain == null || tile.ElevationState != LoadingState.Loaded)
                continue;

            if (tile.transform.IsChildOf(GameObject.Find("Hr_SectorsContainer") != null
                    ? GameObject.Find("Hr_SectorsContainer").transform
                    : null))
                continue;

            Terrain terrain = tile.terrain;
            Vector3 p = terrain.transform.position + new Vector3(terrain.terrainData.size.x * 0.5f, 0f, terrain.terrainData.size.z * 0.5f);

            if (p.x < legacyMinX || p.x > legacyMaxX || p.z < legacyMinZ || p.z > legacyMaxZ)
                continue;

            float legacyWorldY = legacyTerrain.SampleHeight(p) + legacyTerrain.transform.position.y;
            float streamedWorldY = terrain.SampleHeight(p) + terrain.transform.position.y;
            deltas.Add(legacyWorldY - streamedWorldY);
        }

        if (deltas.Count == 0)
        {
            Debug.LogWarning("[KineretTerrain] No common detailed samples were available for vertical calibration; Y offset left unchanged.");
            return;
        }

        deltas.Sort();
        float median = deltas[deltas.Count / 2];
        float min = deltas.First();
        float max = deltas.Last();

        system.customWorldYOffset += median;

        GameObject detailedRoot = GameObject.Find("Terrains");
        if (detailedRoot != null)
            detailedRoot.transform.position += Vector3.up * median;

        GameObject sectorsRoot = GameObject.Find("SectorsContainer");
        if (sectorsRoot != null)
            sectorsRoot.transform.position += Vector3.up * median;

        GameObject horizonRoot = GameObject.Find("Hr_SectorsContainer");
        if (horizonRoot != null)
            horizonRoot.transform.position += Vector3.up * median;

        Debug.Log(string.Format(CultureInfo.InvariantCulture,
            "[KineretTerrain] Vertical calibration: samples={0}, median={1:F2}m, spread={2:F2}..{3:F2}m",
            deltas.Count, median, min, max));
    }
}
