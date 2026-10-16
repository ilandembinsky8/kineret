using UnityEngine;
using System.Collections.Generic;
using System.IO;

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

    [Header("Waypoint Settings")]
    [SerializeField] private Waypoint waypointSpherePrefab; // prefab for the spheres to be drawn along the line
    [SerializeField, Min(0.1f)] private float waypointSphereDistance; // distance between spheres along the line
    [SerializeField] private int poolSize;
    [SerializeField, Min(0f)] private float waypointReachThreshold;
    [SerializeField, Min(1f)] private float waypointReAddMultiplier;
    [SerializeField] private float waypointSphereScale;

    private List<Vector3> _waypoints = new List<Vector3>();
    private readonly List<Vector3> _linePoints = new List<Vector3>();
    private Vector3[] _linePositions = new Vector3[0];
    private Transform _playerTransform;
    private int _currentLeg;

    private List<Waypoint> _spheres = new List<Waypoint>(); //the list of spheres that we will draw along the line renderer's path
    private int _firstWaypointIndex;
    private readonly List<Vector3> _points = new();
    private bool _waypointWindowInitialized;

    void Awake()
    {
        LoadWaypointPathSettings();
        lr.startWidth = 10;
        lr.endWidth = 10;
        InitializeSpheres();
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
        AlignAllWaypoints();
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
        _waypointWindowInitialized = false;

    }

    private void LateUpdate()
    {
        //Only the current leg's first point moves, so redraw only while it is anchored on the plane.
        if (startCurrentLegAtPlayer && _playerTransform != null)
        {
            DrawCurve();
            AlignAllWaypoints();
        }
    }

    /// <summary>Legs already flown drop off the line, so it only ever shows what is still ahead.</summary>
    private void HandleLegStart(int leg)
    {
        _currentLeg = leg;
        _waypointWindowInitialized = false;

        DrawCurve();
        AlignAllWaypoints();
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

    private void AlignAllWaypoints()
    {
        if (waypointSphereDistance <= 0f || _spheres.Count == 0 || lr.positionCount < 2)
            return;

        _points.Clear();
        float totalPathDistance = 0f;

        for (int i = 0; i < lr.positionCount; i++)
        {
            Vector3 point = lr.GetPosition(i);

            if (!lr.useWorldSpace)
                point = lr.transform.TransformPoint(point);

            if (i > 0)
            {
                totalPathDistance += Vector3.Distance(_points[i - 1], point);
            }

            _points.Add(point);
        }

        float distanceToCheckpoint = Vector3.Distance(_points[0], _points[1]);

        if (!_waypointWindowInitialized)
        {
            _firstWaypointIndex = Mathf.Max(0, Mathf.CeilToInt(distanceToCheckpoint / waypointSphereDistance) - 1);
            _waypointWindowInitialized = true;
        }

        float distanceToFirstWaypoint = distanceToCheckpoint - (_firstWaypointIndex * waypointSphereDistance);

        //if we are moving towards the checkpoint
        while (_firstWaypointIndex > 0 && distanceToFirstWaypoint <= waypointReachThreshold)
        {
            MoveFirstWaypointToLast();
            distanceToFirstWaypoint = distanceToCheckpoint - (_firstWaypointIndex * waypointSphereDistance);
        }

        float reAddDistance = waypointSphereDistance * waypointReAddMultiplier;

        //if we are moving away from the checkpoint
        while (distanceToFirstWaypoint >= reAddDistance)
        {
            MoveLastWaypointToFirst();
            distanceToFirstWaypoint = distanceToCheckpoint - (_firstWaypointIndex * waypointSphereDistance);
        }

        PositionAllWaypoints(distanceToFirstWaypoint, totalPathDistance);
    }

    private void PositionAllWaypoints(float firstDistanceFromAnchor, float totalPathDistance)
    {
        int segment = 0;
        float segmentStartDistance = 0f;

        for (int i = 0; i < _spheres.Count; i++)
        {
            float targetDistance = firstDistanceFromAnchor + i * waypointSphereDistance;
            bool visible = targetDistance <= totalPathDistance + 0.001f;

            _spheres[i].gameObject.SetActive(visible);

            if (!visible)
                continue;

            while (segment < _points.Count - 2)
            {
                float traversedSegmentLength = Vector3.Distance(_points[segment], _points[segment + 1]);

                if (targetDistance <= segmentStartDistance + traversedSegmentLength)
                    break;

                segmentStartDistance += traversedSegmentLength;
                segment++;
            }

            Vector3 segmentStart = _points[segment];
            Vector3 segmentEnd = _points[segment + 1];

            float selectedSegmentLength = Vector3.Distance(segmentStart, segmentEnd);
            float interpolation = selectedSegmentLength > 0f ? (targetDistance - segmentStartDistance) / selectedSegmentLength : 0f;
            _spheres[i].transform.position = Vector3.Lerp(segmentStart, segmentEnd, Mathf.Clamp01(interpolation));
        }
    }

    private void MoveFirstWaypointToLast()
    {
        Waypoint reached = _spheres[0];

        _spheres.RemoveAt(0);
        _spheres.Add(reached);

        _firstWaypointIndex--;
    }

    private void MoveLastWaypointToFirst()
    {
        int lastIndex = _spheres.Count - 1;
        Waypoint recycled = _spheres[lastIndex];

        _spheres.RemoveAt(lastIndex);
        _spheres.Insert(0, recycled);

        _firstWaypointIndex++;
    }

    private void InitializeSpheres()
    {
        for (int i = 0; i < poolSize; i++)
        {
            Waypoint sphere = Instantiate(waypointSpherePrefab, transform);
            sphere.gameObject.SetActive(false);
            _spheres.Add(sphere);
            sphere.Initialize(waypointSphereScale);
        }
    }


    private void LoadWaypointPathSettings()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, "WaypointsPathSettings.json");
        if (!File.Exists(filePath))
        {
            Debug.LogWarning("WaypointPathSettings.json file not found. Using default settings.");
            return;
        }

        string json = File.ReadAllText(filePath);
        WaypointPathSettings settings = JsonUtility.FromJson<WaypointPathSettings>(json);
        waypointReachThreshold = Mathf.Max(0f, settings.waypointReachThreshold); 
        poolSize = Mathf.Max(1, (int)settings.poolSize);
        waypointSphereDistance = Mathf.Max(0.1f, settings.waypointSphereDistance);
        waypointReachThreshold = Mathf.Max(0f, settings.waypointReachThreshold);
        waypointSphereScale = Mathf.Max(0f, settings.waypointSphereScale);
    }
}
