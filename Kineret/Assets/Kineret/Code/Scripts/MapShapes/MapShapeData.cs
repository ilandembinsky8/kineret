using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mirrors StreamingAssets/MapShapes.json. Kept flat because JsonUtility has no dictionary support.
/// </summary>
[Serializable]
public class MapShapesData
{
    public List<MapShapeEntry> Shapes = new List<MapShapeEntry>();
}

[Serializable]
public class MapShapeEntry
{
    public string Id;
    public string HebName;
    public string EngName;

    /// <summary>Polygons close back to the first point; a route line stays open.</summary>
    public bool IsClosed = true;

    public string ColorHex = "#39DD20";

    /// <summary>Thickness held constant on screen, so distance does not change how thick it looks.</summary>
    public float ScreenWidthPixels = 4f;

    /// <summary>Lift off the ground, enough to avoid z-fighting with the terrain.</summary>
    public float HeightAboveGround = 15f;

    /// <summary>
    /// Draw through the terrain instead of being blocked by it. The outline hugs the ground, so
    /// at the game's low flying height ridges hide most of it; this keeps the whole shape readable.
    /// </summary>
    public bool AlwaysOnTop = true;

    public float DrawDuration = 2.5f;
    public float EraseDuration = 1.5f;

    public List<GeoPoint> Points = new List<GeoPoint>();

    public Color GetColor()
    {
        Color parsed;
        if (!string.IsNullOrEmpty(ColorHex) && ColorUtility.TryParseHtmlString(ColorHex, out parsed))
        {
            return parsed;
        }

        Debug.LogWarning($"MapShape '{Id}': could not parse ColorHex '{ColorHex}', falling back to green.");
        return new Color(0.224f, 0.867f, 0.125f);
    }
}

[Serializable]
public struct GeoPoint
{
    public double lat;
    public double lon;
}
