using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Draws one shape as a terrain-hugging outline, drawn on like a pencil stroke and erased by
/// retracting back along the same path.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class MapShapeRenderer : MonoBehaviour
{
    private enum State { Hidden, Drawing, Shown, Erasing }

    /// <summary>
    /// Width is held constant on screen, and it varies smoothly along the line, so a handful of
    /// curve keys spread over the path is indistinguishable from one key per vertex and far cheaper.
    /// </summary>
    private const int WidthCurveKeys = 32;

    private LineRenderer _line;
    private MapShapeEntry _shape;
    private Camera _camera;

    private Vector3[] _path = new Vector3[0];
    private float[] _cumulativeLength = new float[0];
    private float _totalLength;
    private bool _pathIsComplete;

    private State _state = State.Hidden;
    private float _progress;

    private readonly List<Vector3> _visiblePoints = new List<Vector3>();
    private Vector3[] _positionBuffer = new Vector3[0];
    private readonly AnimationCurve _widthCurve = new AnimationCurve();
    private Material _material;

    public string Id { get { return _shape != null ? _shape.Id : name; } }
    public bool IsVisible { get { return _state != State.Hidden; } }

    public void Initialize(MapShapeEntry shape, Material sharedMaterial, float maxSegmentLength)
    {
        _shape = shape;
        _line = GetComponent<LineRenderer>();

        _line.useWorldSpace = true;
        _line.alignment = LineAlignment.View;
        _line.numCornerVertices = 2;
        _line.numCapVertices = 2;
        _line.textureMode = LineTextureMode.Stretch;
        _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _line.receiveShadows = false;
        _line.widthMultiplier = 1f;
        _line.positionCount = 0;

        // Depth state is baked into a material, not something a property block can override, and
        // AlwaysOnTop differs per shape - so each shape gets its own instance of the source material.
        _material = new Material(sharedMaterial);
        _material.name = sharedMaterial.name + " (" + shape.Id + ")";
        _material.SetColor("_BaseColor", shape.GetColor());

        if (_material.HasProperty("_ZTest"))
        {
            _material.SetFloat("_ZTest", shape.AlwaysOnTop
                ? (float)UnityEngine.Rendering.CompareFunction.Always
                : (float)UnityEngine.Rendering.CompareFunction.LessEqual);
            _material.SetFloat("_ZWrite", shape.AlwaysOnTop ? 0f : 1f);
        }

        //Drawn after everything else, otherwise opaque geometry rendered later would cover it anyway.
        _material.renderQueue = shape.AlwaysOnTop ? 3100 : 2000;
        _line.sharedMaterial = _material;

        BuildPath(maxSegmentLength);
    }

    private void BuildPath(float maxSegmentLength)
    {
        _path = MapShapePath.Build(_shape, maxSegmentLength, out _pathIsComplete);

        _cumulativeLength = new float[_path.Length];
        _totalLength = 0f;
        for (int i = 1; i < _path.Length; i++)
        {
            _totalLength += Vector3.Distance(_path[i - 1], _path[i]);
            _cumulativeLength[i] = _totalLength;
        }

        if (_positionBuffer.Length < _path.Length + 1)
        {
            _positionBuffer = new Vector3[_path.Length + 1];
        }
    }

    /// <summary>Rebuilds the path if parts of it were drawn over terrain that had not streamed in.</summary>
    public void RebuildIfIncomplete(float maxSegmentLength)
    {
        if (_pathIsComplete) { return; }

        BuildPath(maxSegmentLength);
    }

    public void Show()
    {
        if (_state == State.Drawing || _state == State.Shown) { return; }

        _state = State.Drawing;
        enabled = true;
    }

    public void Hide()
    {
        if (_state == State.Hidden || _state == State.Erasing) { return; }

        _state = State.Erasing;
        enabled = true;
    }

    private void OnDestroy()
    {
        if (_material != null) { Destroy(_material); }
    }

    /// <summary>Clears the line with no animation.</summary>
    public void HideImmediate()
    {
        _state = State.Hidden;
        _progress = 0f;
        _line.positionCount = 0;
    }

    private void LateUpdate()
    {
        if (_path.Length < 2) { return; }

        AdvanceProgress();

        if (_progress <= 0f && _state == State.Hidden)
        {
            _line.positionCount = 0;
            return;
        }

        UpdatePositions();
        UpdateScreenConstantWidth();
    }

    private void AdvanceProgress()
    {
        if (_state == State.Drawing)
        {
            //Fixed duration, so every shape finishes together regardless of how long its outline is.
            float duration = Mathf.Max(0.01f, _shape.DrawDuration);
            _progress += Time.deltaTime / duration;

            if (_progress >= 1f)
            {
                _progress = 1f;
                _state = State.Shown;
            }
        }
        else if (_state == State.Erasing)
        {
            float duration = Mathf.Max(0.01f, _shape.EraseDuration);
            _progress -= Time.deltaTime / duration;

            if (_progress <= 0f)
            {
                _progress = 0f;
                _state = State.Hidden;
            }
        }
    }

    /// <summary>
    /// Reveals the path up to the current progress. The leading end lands between two vertices
    /// rather than snapping from one to the next, so the stroke moves smoothly. Erasing runs the
    /// same reveal backwards, which retracts the end back along the path in reverse point order.
    /// </summary>
    private void UpdatePositions()
    {
        float revealed = _progress * _totalLength;

        _visiblePoints.Clear();
        _visiblePoints.Add(_path[0]);

        for (int i = 1; i < _path.Length; i++)
        {
            if (_cumulativeLength[i] <= revealed)
            {
                _visiblePoints.Add(_path[i]);
                continue;
            }

            float segment = _cumulativeLength[i] - _cumulativeLength[i - 1];
            if (segment > 0.0001f)
            {
                float t = (revealed - _cumulativeLength[i - 1]) / segment;
                _visiblePoints.Add(Vector3.Lerp(_path[i - 1], _path[i], t));
            }

            break;
        }

        if (_visiblePoints.Count < 2)
        {
            _line.positionCount = 0;
            return;
        }

        _visiblePoints.CopyTo(_positionBuffer);
        _line.positionCount = _visiblePoints.Count;
        _line.SetPositions(_positionBuffer);
    }

    /// <summary>
    /// LineRenderer has no screen-space width, and a single widthMultiplier is wrong for an
    /// outline kilometres across seen in perspective: the near edge would be fat and the far edge
    /// hairline. Instead the width curve is rebuilt from each sampled point's camera distance.
    /// </summary>
    private void UpdateScreenConstantWidth()
    {
        if (_camera == null)
        {
            _camera = Camera.main;
            if (_camera == null) { return; }
        }

        int count = _line.positionCount;
        if (count < 2) { return; }

        float pixels = Mathf.Max(0.1f, _shape.ScreenWidthPixels);
        float screenHeight = Mathf.Max(1, Screen.height);
        float tanHalfFov = Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float worldPerPixelAtOneUnit = 2f * tanHalfFov / screenHeight;

        Vector3 eye = _camera.transform.position;
        int keys = Mathf.Min(WidthCurveKeys, count);

        _widthCurve.keys = null;
        var frames = new Keyframe[keys];

        for (int k = 0; k < keys; k++)
        {
            float t = keys == 1 ? 0f : k / (float)(keys - 1);
            int index = Mathf.Clamp(Mathf.RoundToInt(t * (count - 1)), 0, count - 1);

            float distance = Vector3.Distance(eye, _visiblePoints[index]);
            frames[k] = new Keyframe(t, Mathf.Max(0.01f, distance * worldPerPixelAtOneUnit * pixels));
        }

        _widthCurve.keys = frames;
        _line.widthCurve = _widthCurve;
    }
}
