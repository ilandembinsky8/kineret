using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Turns a shape's lat/lon list into a dense polyline that hugs the terrain.
/// </summary>
public static class MapShapePath
{
    /// <summary>
    /// KMZ vertices sit kilometres apart, so a straight run between two of them cuts through
    /// hills. Segments are split to roughly this length before the ground is sampled.
    /// </summary>
    public const float DefaultMaxSegmentLength = 60f;

    /// <summary>
    /// Builds the draped path. <paramref name="sampledEveryPoint"/> is false when some point sat
    /// over terrain that was not streamed in yet, so the caller can rebuild later.
    /// </summary>
    public static Vector3[] Build(MapShapeEntry shape, float maxSegmentLength, out bool sampledEveryPoint)
    {
        sampledEveryPoint = true;

        if (shape == null || shape.Points == null || shape.Points.Count < 2)
        {
            return new Vector3[0];
        }

        List<Vector3> flat = BuildFlatPath(shape, maxSegmentLength);
        var heights = new float[flat.Count];
        var sampled = new bool[flat.Count];

        for (int i = 0; i < flat.Count; i++)
        {
            float y;
            if (TrySampleGround(flat[i], out y))
            {
                heights[i] = y;
                sampled[i] = true;
            }
            else
            {
                sampledEveryPoint = false;
            }
        }

        FillGapsFromNeighbours(heights, sampled);

        var path = new Vector3[flat.Count];
        for (int i = 0; i < flat.Count; i++)
        {
            path[i] = new Vector3(flat[i].x, heights[i] + shape.HeightAboveGround, flat[i].z);
        }

        return path;
    }

    private static List<Vector3> BuildFlatPath(MapShapeEntry shape, float maxSegmentLength)
    {
        var corners = new List<Vector3>(shape.Points.Count + 1);
        foreach (GeoPoint p in shape.Points)
        {
            corners.Add(MapShapeGeo.GeoToWorldXZ(p.lat, p.lon));
        }

        //A polygon closes back onto its first corner; a route line stops where it stops.
        if (shape.IsClosed)
        {
            corners.Add(corners[0]);
        }

        float step = Mathf.Max(1f, maxSegmentLength);
        var dense = new List<Vector3>(corners.Count * 8) { corners[0] };

        for (int i = 1; i < corners.Count; i++)
        {
            Vector3 from = corners[i - 1];
            Vector3 to = corners[i];

            int subdivisions = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(from, to) / step));
            for (int s = 1; s <= subdivisions; s++)
            {
                dense.Add(Vector3.Lerp(from, to, s / (float)subdivisions));
            }
        }

        return dense;
    }

    /// <summary>
    /// Ground height at an XZ position, from whichever terrain is actually loaded there.
    ///
    /// The live terrain is sampled rather than the DEM files in StreamingAssets because
    /// KineretTerrainBootstrap.CalibrateVerticalOffsetAgainstLegacy shifts the streamed terrain
    /// vertically at runtime; a height derived straight from the DEM would miss that offset.
    /// </summary>
    public static bool TrySampleGround(Vector3 worldXZ, out float height)
    {
        height = 0f;

        Terrain best = null;
        float bestArea = float.MaxValue;

        foreach (Terrain terrain in Terrain.activeTerrains)
        {
            if (terrain == null || terrain.terrainData == null) { continue; }

            Vector3 origin = terrain.transform.position;
            Vector3 size = terrain.terrainData.size;

            if (worldXZ.x < origin.x || worldXZ.x > origin.x + size.x) { continue; }
            if (worldXZ.z < origin.z || worldXZ.z > origin.z + size.z) { continue; }

            //Several terrains can cover one point (a streamed tile sitting on the legacy
            //full-map terrain). The smallest one is the most detailed.
            float area = size.x * size.z;
            if (area < bestArea)
            {
                bestArea = area;
                best = terrain;
            }
        }

        if (best == null) { return false; }

        height = best.transform.position.y + best.SampleHeight(worldXZ);
        return true;
    }

    /// <summary>Points over terrain that has not streamed in borrow the nearest sampled height.</summary>
    private static void FillGapsFromNeighbours(float[] heights, bool[] sampled)
    {
        int anchor = -1;
        for (int i = 0; i < sampled.Length; i++)
        {
            if (sampled[i]) { anchor = i; break; }
        }

        if (anchor < 0)
        {
            return;
        }

        for (int i = 0; i < heights.Length; i++)
        {
            if (sampled[i]) { anchor = i; continue; }

            int next = -1;
            for (int j = i + 1; j < sampled.Length; j++)
            {
                if (sampled[j]) { next = j; break; }
            }

            if (next < 0)
            {
                heights[i] = heights[anchor];
                continue;
            }

            float t = (i - anchor) / (float)(next - anchor);
            heights[i] = Mathf.Lerp(heights[anchor], heights[next], t);
        }
    }
}
