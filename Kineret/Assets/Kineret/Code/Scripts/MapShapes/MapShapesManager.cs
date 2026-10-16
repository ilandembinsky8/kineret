using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Loads the shape definitions and owns one renderer per shape.
///
/// Lives on a child of GameScene-Managers.prefab, so it reaches the game scene through the prefab
/// rather than through an edit to the scene file. It loads its own JSON instead of going through
/// GameDataManager, which keeps it independent of the loading scene.
/// </summary>
public class MapShapesManager : MonoBehaviour
{
    public const string DataFileName = "MapShapes.json";

    [Header("Data")]
    [Tooltip("File in StreamingAssets holding the shape definitions.")]
    [SerializeField] private string dataFileName = DataFileName;

    [Header("Rendering")]
    [Tooltip("Shared material for every outline. Colour comes from each shape's ColorHex.")]
    [SerializeField] private Material lineMaterial;

    [Tooltip("Outline segments are split to about this length before the ground is sampled, so the line follows hills instead of cutting through them.")]
    [SerializeField] private float maxSegmentLength = MapShapePath.DefaultMaxSegmentLength;

    [Header("Startup")]
    [SerializeField] private bool showAllOnStart = false;

    private readonly List<MapShapeRenderer> _renderers = new List<MapShapeRenderer>();
    private readonly Dictionary<string, MapShapeRenderer> _byId = new Dictionary<string, MapShapeRenderer>();

    private MapShapesData _data;
    private bool _loaded;

    public IReadOnlyList<MapShapeRenderer> Shapes { get { return _renderers; } }

    private void Awake()
    {
        Load();
    }

    private void Start()
    {
        if (showAllOnStart)
        {
            ShowAll();
        }
    }

    private void Load()
    {
        if (_loaded) { return; }
        _loaded = true;

        var json = new JsonManager();
        json.TryReadFromJson(OnJsonRead, dataFileName);
    }

    private void OnJsonRead(string text)
    {
        _data = JsonUtility.FromJson<MapShapesData>(text);

        if (_data == null || _data.Shapes == null || _data.Shapes.Count == 0)
        {
            Debug.LogError($"MapShapesManager: no shapes parsed from {dataFileName}.");
            return;
        }

        foreach (MapShapeEntry shape in _data.Shapes)
        {
            CreateRenderer(shape);
        }
    }

    private void CreateRenderer(MapShapeEntry shape)
    {
        if (string.IsNullOrEmpty(shape.Id))
        {
            Debug.LogWarning("MapShapesManager: skipping a shape with no Id.");
            return;
        }
        if (_byId.ContainsKey(shape.Id))
        {
            Debug.LogWarning($"MapShapesManager: duplicate shape Id '{shape.Id}', skipping the second one.");
            return;
        }

        var go = new GameObject("Shape - " + shape.Id);
        go.transform.SetParent(transform, false);

        var renderer = go.AddComponent<MapShapeRenderer>();
        renderer.Initialize(shape, lineMaterial, maxSegmentLength);
        renderer.HideImmediate();

        _renderers.Add(renderer);
        _byId.Add(shape.Id, renderer);
    }

    public void Show(string id)
    {
        MapShapeRenderer renderer;
        if (!_byId.TryGetValue(id, out renderer))
        {
            Debug.LogWarning($"MapShapesManager: no shape with Id '{id}'.");
            return;
        }

        //Terrain streams in, so a shape built before its tiles arrived gets another go here.
        renderer.RebuildIfIncomplete(maxSegmentLength);
        renderer.Show();
    }

    public void Hide(string id)
    {
        MapShapeRenderer renderer;
        if (!_byId.TryGetValue(id, out renderer))
        {
            Debug.LogWarning($"MapShapesManager: no shape with Id '{id}'.");
            return;
        }

        renderer.Hide();
    }

    public void ShowAll()
    {
        foreach (MapShapeRenderer renderer in _renderers)
        {
            renderer.RebuildIfIncomplete(maxSegmentLength);
            renderer.Show();
        }
    }

    public void HideAll()
    {
        foreach (MapShapeRenderer renderer in _renderers)
        {
            renderer.Hide();
        }
    }

    public void ToggleAll()
    {
        bool anyVisible = false;
        foreach (MapShapeRenderer renderer in _renderers)
        {
            if (renderer.IsVisible) { anyVisible = true; break; }
        }

        if (anyVisible) { HideAll(); } else { ShowAll(); }
    }
}
