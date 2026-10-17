using System.IO;
using UnityEngine;

/// <summary>
/// Draws the Kineret as a water surface instead of leaving it to the satellite imagery.
///
/// The lake reads differently at different distances because the raster it is painted with is a
/// mosaic stitched from several captures, and because the high-altitude horizon texture comes from
/// a different source again. A surface of our own is the same colour from every distance by
/// construction.
///
/// It is one flat quad over the lake's bounding box, a metre above the water level in the terrain.
/// The shoreline is not in the mesh: the terrain writes depth, so every part of the quad that sits
/// below ground is hidden, and the waterline ends up being the terrain's own silhouette - exact at
/// any distance and at any streaming level of detail. Inside the bounding box only 0.6% of the
/// ground below the waterline is not lake, so there is nothing else for the quad to flood.
///
/// Lives on a child of GameScene-Managers.prefab, so it reaches the game scene through the prefab
/// rather than through an edit to the scene file.
/// </summary>
public class KineretLakeWater : MonoBehaviour
{
    public const string DataFileName = "KineretWater.json";
    public const string ShoreMapFileName = "KineretWaterShore.png";

    [Header("Data")]
    [SerializeField] private string dataFileName = DataFileName;
    [SerializeField] private string shoreMapFileName = ShoreMapFileName;

    [Header("Rendering")]
    [SerializeField] private Shader waterShader;

    public static KineretLakeWater Instance;

    private KineretWaterSettings _settings;
    private GameObject _surface;
    private Material _material;
    private Texture2D _shoreMap;

    public bool IsVisible { get { return _surface != null && _surface.activeSelf; } }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Build();
    }

    private void Build()
    {
        if (!LoadSettings())
            return;

        if (!LoadShoreMap())
            return;

        if (waterShader == null)
            waterShader = Shader.Find("Kineret/LakeWater");

        if (waterShader == null)
        {
            Debug.LogError("KineretLakeWater: shader 'Kineret/LakeWater' not found.");
            return;
        }

        _material = new Material(waterShader) { name = "KineretLakeWater (runtime)" };
        ApplySettings();

        _surface = new GameObject("Kineret Water Surface");
        _surface.transform.SetParent(transform, false);

        //The mesh is built in absolute world coordinates, and the Managers object this hangs off
        //is parked away from the origin, so the surface has to be pinned back to it.
        _surface.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        _surface.transform.localScale = Vector3.one;

        _surface.layer = gameObject.layer;

        MeshFilter filter = _surface.AddComponent<MeshFilter>();
        filter.sharedMesh = BuildQuad();

        MeshRenderer renderer = _surface.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = _material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        _surface.SetActive(_settings.StartVisible);
    }

    private bool LoadSettings()
    {
        string path = Path.Combine(Application.streamingAssetsPath, dataFileName);

        if (!File.Exists(path))
        {
            Debug.LogError($"KineretLakeWater: {dataFileName} not found at {path}.");
            return false;
        }

        _settings = JsonUtility.FromJson<KineretWaterSettings>(File.ReadAllText(path));

        if (_settings == null)
        {
            Debug.LogError($"KineretLakeWater: could not parse {dataFileName}.");
            return false;
        }

        _settings.ResolvePreset();
        return true;
    }

    private bool LoadShoreMap()
    {
        string path = Path.Combine(Application.streamingAssetsPath, shoreMapFileName);

        if (!File.Exists(path))
        {
            Debug.LogError($"KineretLakeWater: {shoreMapFileName} not found at {path}.");
            return false;
        }

        _shoreMap = new Texture2D(2, 2, TextureFormat.RGB24, false);

        if (!_shoreMap.LoadImage(File.ReadAllBytes(path), true))
        {
            Debug.LogError($"KineretLakeWater: could not decode {shoreMapFileName}.");
            Destroy(_shoreMap);
            _shoreMap = null;
            return false;
        }

        //Clamped, so the bilinear filter does not wrap the far shore round to the near one.
        _shoreMap.wrapMode = TextureWrapMode.Clamp;
        _shoreMap.filterMode = FilterMode.Bilinear;
        _shoreMap.anisoLevel = 4;
        _shoreMap.name = "KineretShoreMap";
        return true;
    }

    /// <summary>Two triangles over the lake's bounding box. The shoreline comes from the terrain.</summary>
    private Mesh BuildQuad()
    {
        KineretWaterSettings s = _settings;
        float y = s.SurfaceY + s.HeightAboveSurface;

        var mesh = new Mesh { name = "KineretWaterQuad" };
        mesh.vertices = new[]
        {
            new Vector3(s.XMin, y, s.ZMin),
            new Vector3(s.XMin, y, s.ZMax),
            new Vector3(s.XMax, y, s.ZMax),
            new Vector3(s.XMax, y, s.ZMin)
        };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
        mesh.RecalculateBounds();
        return mesh;
    }

    public void ApplySettings()
    {
        if (_material == null || _settings == null)
            return;

        KineretWaterSettings s = _settings;

        _material.SetTexture("_ShoreMap", _shoreMap);
        _material.SetVector("_WorldRect", new Vector4(s.XMin, s.ZMin, s.XMax, s.ZMax));
        _material.SetFloat("_MaxShoreDistance", s.MaxShoreDistance);

        _material.SetColor("_DeepColor", s.GetDeepColour());
        _material.SetColor("_ShallowColor", s.GetShallowColour());
        _material.SetFloat("_ShallowDistance", s.ShallowDistance);
        _material.SetFloat("_EdgeFade", s.EdgeFade);
        _material.SetFloat("_Opacity", s.Opacity);
        _material.SetFloat("_Smoothness", s.Smoothness);
        _material.SetFloat("_SpecularStrength", s.SunGlint);
        _material.SetFloat("_SkyReflection", s.SkyReflection);
        _material.SetFloat("_WaveStrength", s.WaveStrength);
        _material.SetFloat("_WaveScale", s.WaveScale);
        _material.SetFloat("_WaveSpeed", s.WaveSpeed);
        _material.SetFloat("_WaveFadeDistance", s.WaveFadeDistance);
        _material.SetFloat("_WaveContrast", s.WaveContrast);
    }

    /// <summary>
    /// How far the surface floats above the water level in the terrain. The terrain's shoreline is
    /// sampled 25 m apart while the imagery under it is 12.6 m, so right at the waterline there is
    /// a fringe of ground that stands above the lake but is painted lake-dark. Lifting the surface
    /// a few metres covers that fringe, at the cost of the water creeping as far up the shore.
    /// </summary>
    public void SetHeightAboveSurface(float metres)
    {
        if (_settings == null || _surface == null)
            return;

        _settings.HeightAboveSurface = metres;
        _surface.GetComponent<MeshFilter>().sharedMesh = BuildQuad();
    }

    public void Show() { if (_surface != null) _surface.SetActive(true); }
    public void Hide() { if (_surface != null) _surface.SetActive(false); }
    public void Toggle() { if (_surface != null) _surface.SetActive(!_surface.activeSelf); }

    /// <summary>Switches to one of the named presets in the JSON and rebuilds the material.</summary>
    public void ApplyPreset(string presetName)
    {
        if (_settings == null)
            return;

        _settings.ResolvePreset(presetName);
        ApplySettings();

        if (_surface != null)
            _surface.GetComponent<MeshFilter>().sharedMesh = BuildQuad();
    }

    private void OnDestroy()
    {
        if (_material != null) { Destroy(_material); }
        if (_shoreMap != null) { Destroy(_shoreMap); }
        if (Instance == this) { Instance = null; }
    }
}
