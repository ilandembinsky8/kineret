using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class WaypointPathController : MonoBehaviour
{
    [Header("References")]
    public Transform player;                     // the racing car / character
    public List<Transform> waypoints = new List<Transform>();

    [Header("Curve")]
    public int samplesPerSegment = 20;

    [Header("Waypoint Options")]
    public bool loopPath = true;                  // if true, put reached waypoint at end (laps)
    public bool destroyOnReach = false;           // if true, Destroy(waypoint.gameObject) when reached
    public bool deactivateOnReach = true;         // if true, waypoint.gameObject.SetActive(false) when reached

    private LineRenderer lr;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.startWidth = 10;
        lr.endWidth = 10;
        if (player == null && waypoints.Count > 0)
        {
            Debug.LogWarning("WaypointPathController: no player assigned. Assign to detect waypoint reach.");
        }
    }

    private void Start()
    {
        DrawCurve();
    }

    //void LateUpdate()
    //{
    //    if (player != null && waypoints.Count > 0)
    //    {
    //        CheckAndConsumeWaypoint();
    //    }

    //    DrawCurve();
    //}

    void CheckAndConsumeWaypoint()
    {
        // Next waypoint is waypoints[0]
        Transform next = waypoints[0];
        if (next == null)
        {
            // clean nulls
            waypoints.RemoveAt(0);
            return;
        }
    }

    public void CycleWaypoint()
    {
        Transform reached = waypoints[0];
        waypoints.RemoveAt(0);

        if (destroyOnReach)
        {
            Destroy(reached.gameObject);
        }
        else if (deactivateOnReach)
        {
            reached.gameObject.SetActive(false);
        }

        if (loopPath)
        {
            // append to end to create a loop
            waypoints.Add(reached);
        }
    }

    void DrawCurve()
    {
        if (player == null || waypoints.Count == 0)
        {
            lr.positionCount = 0;
            return;
        }

        // Collect positions: first the player, then waypoints
        List<Vector3> pts = new List<Vector3>();
        pts.Add(player.position);
        foreach (var w in waypoints)
            if (w != null) pts.Add(w.position);

        if (pts.Count < 2)
        {
            lr.positionCount = 0;
            return;
        }

        // Compute automatic tangents for Bezier chaining
        List<(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)> segments = GenerateBezierSegments(pts);

        // Sample curve points
        List<Vector3> finalPoints = new List<Vector3>();
        foreach (var seg in segments)
        {
            for (int i = 0; i <= samplesPerSegment; i++)
            {
                float t = i / (float)samplesPerSegment;
                finalPoints.Add(Bezier(seg.p0, seg.p1, seg.p2, seg.p3, t));
            }
        }

        lr.positionCount = finalPoints.Count;
        lr.SetPositions(finalPoints.ToArray());
    }

    Vector3 Bezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1 - t;
        return
            u * u * u * p0 +
            3 * u * u * t * p1 +
            3 * u * t * t * p2 +
            t * t * t * p3;
    }

    List<(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)> GenerateBezierSegments(List<Vector3> pts)
    {
        var segments = new List<(Vector3, Vector3, Vector3, Vector3)>();

        for (int i = 0; i < pts.Count - 1; i++)
        {
            Vector3 p0 = pts[i];
            Vector3 p3 = pts[i + 1];

            // Auto tangent calculation
            Vector3 prev = (i == 0) ? p0 : pts[i - 1];
            Vector3 next = (i + 2 < pts.Count) ? pts[i + 2] : p3;

            // Tangent directions
            Vector3 m0 = (p3 - prev) * 0.25f;
            Vector3 m1 = (next - p0) * 0.25f;

            Vector3 p1 = p0 + m0;   // start tangent
            Vector3 p2 = p3 - m1;   // end tangent

            segments.Add((p0, p1, p2, p3));
        }

        return segments;
    }

}
