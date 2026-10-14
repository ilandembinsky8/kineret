using UnityEngine;

/// <summary>
/// Latitude/longitude to world position, using the same mapping the rest of the game uses for
/// destinations and interest points (see GameDestinationLoader.GetDestinationWorldPosition).
/// The bounds come from the [Texture] section of config.ini.
/// </summary>
public static class MapShapeGeo
{
    private static bool _loaded;
    private static float _minLon, _maxLon, _minLat, _maxLat, _width, _height;

    private static void EnsureLoaded()
    {
        if (_loaded) { return; }

        _minLon = GameSettingsManager.GetFloat("Texture", "min_lon");
        _maxLon = GameSettingsManager.GetFloat("Texture", "max_lon");
        _minLat = GameSettingsManager.GetFloat("Texture", "min_lat");
        _maxLat = GameSettingsManager.GetFloat("Texture", "max_lat");
        _width = GameSettingsManager.GetFloat("Texture", "width");
        _height = GameSettingsManager.GetFloat("Texture", "height");

        _loaded = true;
    }

    /// <summary>XZ only; Y is resolved separately by sampling the terrain.</summary>
    public static Vector3 GeoToWorldXZ(double lat, double lon)
    {
        EnsureLoaded();

        float x = (float)((lon - _minLon) / (_maxLon - _minLon)) * _width;
        float z = (float)((lat - _minLat) / (_maxLat - _minLat)) * _height;

        return new Vector3(x, 0f, z);
    }

    public static bool IsInsideTerrain(double lat, double lon)
    {
        EnsureLoaded();

        return lon >= _minLon && lon <= _maxLon && lat >= _minLat && lat <= _maxLat;
    }
}
