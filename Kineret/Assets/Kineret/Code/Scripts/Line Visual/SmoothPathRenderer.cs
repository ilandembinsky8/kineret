using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class WaypointPathController : MonoBehaviour
{
    public LineRenderer lr;

    [Header("Waypoint Options")]
    public bool loopPath = true;                  // if true, put reached waypoint at end (laps)
    public bool destroyOnReach = false;           // if true, Destroy(waypoint.gameObject) when reached
    public bool deactivateOnReach = true;         // if true, waypoint.gameObject.SetActive(false) when reached

   
    private List<Vector3> _waypoints = new List<Vector3>();

    void Awake()
    {
        lr.startWidth = 10;
        lr.endWidth = 10;
    }

    private void Start()
    {
        DrawCurve();
    }

    public void Init(List<Vector3> waypoints)
    {
        _waypoints = waypoints;
    }

    /*void LateUpdate()
    {
        if (player != null && waypoints.Count > 0)
        {
            CheckAndConsumeWaypoint();
        }

        DrawCurve();
    }*/

   /* void CheckAndConsumeWaypoint()
    {
        // Next waypoint is waypoints[0]
        Transform next = waypoints[0];
        if (next == null)
        {
            // clean nulls
            waypoints.RemoveAt(0);
            return;
        }
    }*/

 /*   public void CycleWaypoint()
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
    }*/

    /// <summary>
    /// Draws the route as straight segments: start point -> first destination -> ... -> last.
    /// The waypoints are the only line positions, so the LineRenderer connects them directly.
    /// </summary>
    void DrawCurve()
    {
        if (_waypoints.Count < 2)
        {
            lr.positionCount = 0;
            return;
        }

        lr.positionCount = _waypoints.Count;
        lr.SetPositions(_waypoints.ToArray());
    }

}
