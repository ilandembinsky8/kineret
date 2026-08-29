using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class WaypointPathController : MonoBehaviour
{
    public LineRenderer lr;

    [Header("Current Leg")]
    [Tooltip("Draw the leg being flown from the aircraft itself rather than from the waypoint it started at. Legs further ahead are always drawn waypoint to waypoint.")]
    public bool startCurrentLegAtPlayer = true;

    [Tooltip("Offset for the line's first point. The camera sits exactly on the aircraft root, so anchoring the line there would put a view-facing quad on the near plane. Matches Route Settings/LineStartingYBonus by default.")]
    public Vector3 playerAnchorOffset = new Vector3(0f, -150f, 0f);

    [Header("Waypoint Options")]
    public bool loopPath = true;                  // if true, put reached waypoint at end (laps)
    public bool destroyOnReach = false;           // if true, Destroy(waypoint.gameObject) when reached
    public bool deactivateOnReach = true;         // if true, waypoint.gameObject.SetActive(false) when reached


    private List<Vector3> _waypoints = new List<Vector3>();
    private readonly List<Vector3> _linePoints = new List<Vector3>();
    private Vector3[] _linePositions = new Vector3[0];
    private Transform _playerTransform;
    private int _currentLeg;

    void Awake()
    {
        lr.startWidth = 10;
        lr.endWidth = 10;
    }

    private void OnEnable()
    {
        EventsRelay.OnLegStart += HandleLegStart;
    }

    private void OnDisable()
    {
        EventsRelay.OnLegStart -= HandleLegStart;
    }

    private void Start()
    {
        DrawCurve();
    }

    /// <summary>
    /// The player transform is passed in rather than wired in the scene: GameDestinationLoader
    /// already holds both references, and the Game Scene carries a stale prefab override that
    /// Unity silently prunes whenever the scene is re-saved.
    /// </summary>
    public void Init(List<Vector3> waypoints, Transform playerTransform = null)
    {
        _waypoints = waypoints;
        _playerTransform = playerTransform;
    }

    private void LateUpdate()
    {
        //Only the current leg's first point moves, so redraw only while it is anchored on the plane.
        if (startCurrentLegAtPlayer && _playerTransform != null)
        {
            DrawCurve();
        }
    }

    /// <summary>Legs already flown drop off the line, so it only ever shows what is still ahead.</summary>
    private void HandleLegStart(int leg)
    {
        _currentLeg = leg;

        DrawCurve();
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
    /// Draws the route as straight segments. _waypoints is [start, destination0, destination1, ...],
    /// so leg N runs from _waypoints[N] to _waypoints[N + 1]. Everything before the current leg is
    /// dropped, the current leg is anchored on the aircraft, and the legs after it stay waypoint to
    /// waypoint.
    /// </summary>
    void DrawCurve()
    {
        if (_waypoints.Count < 2)
        {
            lr.positionCount = 0;
            return;
        }

        int nextDestination = Mathf.Clamp(_currentLeg + 1, 1, _waypoints.Count - 1);

        _linePoints.Clear();

        if (startCurrentLegAtPlayer && _playerTransform != null)
        {
            _linePoints.Add(_playerTransform.position + playerAnchorOffset);
        }
        else
        {
            _linePoints.Add(_waypoints[nextDestination - 1]);
        }

        for (int i = nextDestination; i < _waypoints.Count; i++)
        {
            _linePoints.Add(_waypoints[i]);
        }

        //Reused so redrawing every time the player moves does not allocate.
        if (_linePositions.Length != _linePoints.Count)
        {
            _linePositions = new Vector3[_linePoints.Count];
        }
        _linePoints.CopyTo(_linePositions);

        lr.positionCount = _linePositions.Length;
        lr.SetPositions(_linePositions);
    }

}
