using System;
using UnityEngine;
using UnityEngine.UI;

public class MinimapController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private TextAsset minimapSpecJson;

    [Header("World References")]
    [SerializeField] private PlayerMovementHandler player;
    [SerializeField] private GameDestinationLoader destinationLoader;

    [Header("Main UI")]
    [SerializeField] private RectTransform minimapRoot;
    [SerializeField] private Image bezel;
    [SerializeField] private Image glider;

    [Header("Base Map")]
    [SerializeField] private TextAsset mapPlacementJson;
    [SerializeField] private RawImage mapRaw;
    [SerializeField] private float coverageMeters = 42900f;

    [Header("North UI")]
    [SerializeField] private RectTransform northArrowRect;
    [SerializeField] private Image northArrow;
    [SerializeField] private RectTransform northLetterRect;
    [SerializeField] private Image northLetter;

    [Header("Marker UI")]
    [SerializeField] private RectTransform markersRoot;
    [SerializeField] private Image markerTemplate;

    [Tooltip("Maximum distance from the minimap center, as a fraction of its diameter.")]
    [SerializeField, Range(0.1f, 0.49f)] private float markerRadius = 0.385f;

    [Header("Route UI")]
    [SerializeField] private Image flownLine;
    [SerializeField] private Image currentLine;
    [SerializeField] private Image futureLine;
    [SerializeField] private Image extraLine;

    [SerializeField] private Sprite flownLineSprite;
    [SerializeField] private Sprite currentLineSprite;
    [SerializeField] private Sprite futureLineSprite;

    private MinimapSpec _settings;
    private MapPlacementSettings _mapPlacement;
    private Vector3 _routeStartWorldPosition;

    private DestinationHandler[] _destinations;
    private Image[] _destinationMarkers;
    private Image[] _routeLineSlots;

    private int _currentLeg;
    private bool _initialized;

    private void Awake()
    {
        _routeLineSlots = new Image[]
        {
            flownLine,
            currentLine,
            futureLine,
            extraLine
        };

        //sprite fallbacks
        if (currentLineSprite == null && currentLine != null)
            currentLineSprite = currentLine.sprite;

        if (futureLineSprite == null && futureLine != null)
            futureLineSprite = futureLine.sprite;

        if (flownLineSprite == null && flownLine != null)
            flownLineSprite = flownLine.sprite;

        LoadJsonFiles();
        ApplyVisualSettings();
        HideRouteLines();
    }

    private void OnEnable()
    {
        EventsRelay.OnLegStart += HandleLegStart;
        EventsRelay.OnDestinationReached += HandleDestinationReached;
    }

    private void OnDisable()
    {
        EventsRelay.OnLegStart -= HandleLegStart;
        EventsRelay.OnDestinationReached -= HandleDestinationReached;
    }

    private void Start()
    {
        if (_settings == null || _mapPlacement == null || destinationLoader == null || player == null || mapRaw == null)
        {
            Debug.LogWarning("MinimapController is missing required references.", this);
            return;
        }

        _destinations = destinationLoader.GetCurrentDestinations;
        _routeStartWorldPosition = player.transform.position; //here we store the starting position

        if (_destinations == null || _destinations.Length == 0)
        {
            Debug.LogWarning("No minimap destinations were found.", this);
            return;
        }

        if (_routeLineSlots.Length < _destinations.Length)
            Debug.LogWarning("There are fewer route line slots than destinations.", this);

        CreateDestinationMarkers();
        _initialized = true;
    }

    private void LateUpdate()
    {
        if (!_initialized)
            return;

        UpdateBaseMap();
        UpdateNorthIndicator();
        UpdateDestinationMarkers();
        UpdateRouteLines();
    }

    private void LoadJsonFiles()
    {
        if (minimapSpecJson == null)
        {
            Debug.LogWarning("MM_Spec.json has not been assigned.", this);
            return;
        }
        _settings = JsonUtility.FromJson<MinimapSpec>(minimapSpecJson.text);

        if (mapPlacementJson != null)
            _mapPlacement = JsonUtility.FromJson<MapPlacementSettings>(mapPlacementJson.text);

        else
            Debug.LogWarning("KineretMinimapBase.json has not been assigned.", this);
    }

    private void HandleLegStart(int leg)
    {
        _currentLeg = leg;
    }

    private void HandleDestinationReached(int destinationID)
    {
        if (_destinations == null)
            return;

        //find the destination in the actual route order, and set the current leg to the next one
        for (int i = 0; i < _destinations.Length; i++)
        {
            if (_destinations[i].Destination != destinationID)
                continue;

            _currentLeg = Mathf.Clamp(i + 1, 0, _destinations.Length);
            return;
        }
    }

    private void ApplyVisualSettings()
    {
        float diameter = GetMinimapDiameter();

        //these are the only two sprites that are not tinted by the minimap settings, so that they can be replaced with custom sprites if desired
        bezel.color = Color.white;
        glider.color = Color.white;
        glider.rectTransform.sizeDelta = Vector2.one * (_settings.geometryAsFractionOfDiameter.gliderBox * diameter);

        ApplyNorthVisuals(diameter);
        ApplyMarkerVisuals(diameter);
        DisableRaycasts();
    }

    private void ApplyNorthVisuals(float diameter)
    {
        Color northColor = ParseColor(_settings.colours.north);

        northArrow.color = northColor;
        northLetter.color = northColor;

        northArrowRect.sizeDelta = new Vector2(_settings.geometryAsFractionOfDiameter.arrowSpriteW * diameter,
            _settings.geometryAsFractionOfDiameter.arrowSpriteH * diameter);

        northLetterRect.sizeDelta = new Vector2(_settings.geometryAsFractionOfDiameter.letterSpriteW * diameter,
            _settings.geometryAsFractionOfDiameter.letterSpriteH * diameter);
    }

    private void ApplyMarkerVisuals(float diameter)
    {
        float markerDiameter = (_settings.geometryAsFractionOfDiameter.markerCore + _settings.geometryAsFractionOfDiameter.markerRing) * 2f * diameter;
        markerTemplate.rectTransform.sizeDelta = Vector2.one * markerDiameter;
        markerTemplate.color = ParseColor(_settings.colours.markerPending);
        markerTemplate.raycastTarget = false;
    }

    private void CreateDestinationMarkers()
    {
        _destinationMarkers = new Image[_destinations.Length];
        markerTemplate.gameObject.SetActive(false);

        for (int i = 0; i < _destinations.Length; i++)
        {
            Image marker = Instantiate(markerTemplate, markersRoot);
            marker.name = $"DestinationMarker_{i + 1}";

            marker.raycastTarget = false;
            marker.gameObject.SetActive(true);

            _destinationMarkers[i] = marker;
        }
    }

    private void UpdateDestinationMarkers()
    {
        Color pendingColor = ParseColor(_settings.colours.markerPending);
        Color collectedColor = ParseColor(_settings.colours.markerCollected);

        for (int i = 0; i < _destinationMarkers.Length; i++)
        {
            RectTransform markerRect = _destinationMarkers[i].rectTransform;
            markerRect.anchoredPosition = GetDestinationUIPosition(i);
            _destinationMarkers[i].color = i < _currentLeg ? collectedColor : pendingColor;
        }
    }

    private void UpdateNorthIndicator()
    {
        if (player.YawBody == null)
            return;

        float diameter = GetMinimapDiameter();
        float heading = player.YawBody.eulerAngles.y;

        float northAngle = -heading * Mathf.Deg2Rad;
        Vector2 northDirection = new Vector2(Mathf.Sin(northAngle), Mathf.Cos(northAngle));
        northArrowRect.anchoredPosition = northDirection * (_settings.geometryAsFractionOfDiameter.arrowSpriteCentreR * diameter);

        northArrowRect.localRotation = Quaternion.Euler(0f, 0f, heading);
        northLetterRect.anchoredPosition = northDirection * (_settings.geometryAsFractionOfDiameter.letterR * diameter);
        northLetterRect.localRotation = Quaternion.identity;
    }

    private void UpdateRouteLines()
    {
        if (_destinations == null || _routeLineSlots == null)
            return;

        int slot = 0;
        int destinationCount = _destinations.Length;

        Color flownColor = ParseColor(_settings.colours.legFlown, _settings.colours.legFlownAlpha);
        Color currentColor = ParseColor(_settings.colours.legCurrent);
        Color futureColor = ParseColor(_settings.colours.legFuture, _settings.colours.legFutureAlpha);

        for (int leg = 0; leg < _currentLeg && slot < _routeLineSlots.Length; leg++)
        {
            Vector2 start = GetRoutePointUIPosition(leg);
            Vector2 end = GetRoutePointUIPosition(leg + 1);
            SetRouteLine(_routeLineSlots[slot], start, end, flownLineSprite, flownColor, _settings.geometryAsFractionOfDiameter.legFlownW, 0f, false);
            slot++;
        }

        if (_currentLeg < destinationCount)
        {
            if (slot < _routeLineSlots.Length)
            {
                Vector2 activeLegStart = GetRoutePointUIPosition(_currentLeg);
                SetRouteLine(_routeLineSlots[slot], activeLegStart, Vector2.zero, flownLineSprite, flownColor, _settings.geometryAsFractionOfDiameter.legFlownW, 0f, false);
                slot++;
            }

            //from the player to the next destination
            if (slot < _routeLineSlots.Length)
            {
                Vector2 nextDestination = GetRoutePointUIPosition(_currentLeg + 1);

                SetRouteLine(_routeLineSlots[slot], Vector2.zero, nextDestination, currentLineSprite, currentColor, _settings.geometryAsFractionOfDiameter.legCurrentW,
                    _settings.geometryAsFractionOfDiameter.MM_DashCurrentPeriodFracD, true);

                slot++;
            }

            //then just the legs after the current leg, if any
            for (int leg = _currentLeg + 1; leg < destinationCount && slot < _routeLineSlots.Length; leg++)
            {
                Vector2 start = GetRoutePointUIPosition(leg);
                Vector2 end = GetRoutePointUIPosition(leg + 1);

                SetRouteLine(_routeLineSlots[slot], start, end, futureLineSprite, futureColor, _settings.geometryAsFractionOfDiameter.legFutureW,
                    _settings.geometryAsFractionOfDiameter.MM_DashFuturePeriodFracD, true);

                slot++;
            }
        }

        //any remaining slots are hidden, so that if the route shrinks, we don't leave old lines visible
        while (slot < _routeLineSlots.Length)
        {
            if (_routeLineSlots[slot] != null)
                _routeLineSlots[slot].gameObject.SetActive(false);

            slot++;
        }
    }

    private void SetRouteLine(Image line, Vector2 start, Vector2 end, Sprite sprite, Color color, float widthFraction, float periodFraction, bool tiled)
    {
        if (line == null)
            return;

        if (sprite == null)
        {
            line.gameObject.SetActive(false);
            return;
        }

        Vector2 difference = end - start;
        float distance = difference.magnitude;

        //small lines can cause visual artifacts, especially when using tiled sprites
        if (distance <= 0.01f)
        {
            line.gameObject.SetActive(false);
            return;
        }

        float diameter = GetMinimapDiameter();

        line.gameObject.SetActive(true);
        line.sprite = sprite;
        line.color = color;
        line.raycastTarget = false;

        line.type = tiled ? Image.Type.Tiled : Image.Type.Simple;
        RectTransform lineRect = line.rectTransform;

        lineRect.anchorMin = new Vector2(0.5f, 0.5f);
        lineRect.anchorMax = new Vector2(0.5f, 0.5f);

        lineRect.pivot = new Vector2(0f, 0.5f);
        lineRect.anchoredPosition = start;
        lineRect.sizeDelta = new Vector2(distance, diameter * widthFraction);

        float angle = Mathf.Atan2(difference.y, difference.x) * Mathf.Rad2Deg;
        lineRect.localRotation = Quaternion.Euler(0f, 0f, angle);

        if (tiled)
            ApplyDashPeriod(line, sprite, periodFraction * diameter);

        else
            line.pixelsPerUnitMultiplier = 1f;
    }

    private void ApplyDashPeriod(Image line, Sprite sprite, float desiredPeriod)
    {
        if (desiredPeriod <= 0f)
            return;

        Canvas canvas = line.canvas;
        float referencePixelsPerUnit = canvas != null ? canvas.referencePixelsPerUnit : 100f;
        float nativeTileWidth = sprite.rect.width * referencePixelsPerUnit / sprite.pixelsPerUnit;
        line.pixelsPerUnitMultiplier = Mathf.Max(0.01f, nativeTileWidth / desiredPeriod);
    }

    private Vector2 GetDestinationUIPosition(int index)
    {
        Vector2 position = WorldToMinimapPosition(_destinations[index].transform.position);
        float maximumRadius = GetMinimapDiameter() * markerRadius;

        return Vector2.ClampMagnitude(position, maximumRadius);
    }

    private Vector2 WorldToMinimapPosition(Vector3 worldPosition)
    {
        Vector3 playerPosition = player.transform.position;

        float deltaX = worldPosition.x - playerPosition.x;
        float deltaZ = worldPosition.z - playerPosition.z;
        float distance = Mathf.Sqrt(deltaX * deltaX + deltaZ * deltaZ);
        float heading = player.YawBody.eulerAngles.y * Mathf.Deg2Rad;

        float relativeAngle = Mathf.Atan2(deltaX, deltaZ) - heading;
        float radius = distance / coverageMeters * GetMinimapDiameter();

        return new Vector2(radius * Mathf.Sin(relativeAngle), radius * Mathf.Cos(relativeAngle));
    }

    private Vector2 GetRoutePointUIPosition(int routePointIndex)
    {
        if (routePointIndex == 0)
            return WorldToMinimapPosition(_routeStartWorldPosition);

        return WorldToMinimapPosition(_destinations[routePointIndex - 1].transform.position);
    }

    private float GetMinimapDiameter()
    {
        return Mathf.Min(minimapRoot.rect.width, minimapRoot.rect.height);
    }

    private void HideRouteLines()
    {
        foreach (Image line in _routeLineSlots)
        {
            if (line != null)
                line.gameObject.SetActive(false);
        }
    }

    private void DisableRaycasts()
    {
        bezel.raycastTarget = false;
        glider.raycastTarget = false;
        northArrow.raycastTarget = false;
        northLetter.raycastTarget = false;
        markerTemplate.raycastTarget = false;

        foreach (Image line in _routeLineSlots)
        {
            if (line != null)
                line.raycastTarget = false;
        }
    }
    private void UpdateBaseMap()
    {
        if (_mapPlacement == null || mapRaw == null)
            return;

        float diameter = GetMinimapDiameter();
        float diagonalMultiplier = 1.41422f;

        float mapWidth = _mapPlacement.worldXMax - _mapPlacement.worldXMin;
        float mapHeight = _mapPlacement.worldZMax - _mapPlacement.worldZMin;
        float spanMeters = coverageMeters * diagonalMultiplier;
        float uvWidth = spanMeters / mapWidth;
        float uvHeight = spanMeters / mapHeight;

        Vector3 playerPosition = player.transform.position;
        float playerU = (playerPosition.x - _mapPlacement.worldXMin) / mapWidth;
        float playerV = (playerPosition.z - _mapPlacement.worldZMin) / mapHeight;
        mapRaw.uvRect = new Rect(playerU - uvWidth * 0.5f, playerV - uvHeight * 0.5f, uvWidth, uvHeight);
        mapRaw.rectTransform.sizeDelta = Vector2.one * (diameter * diagonalMultiplier);

        float heading = player.YawBody.eulerAngles.y;
        mapRaw.rectTransform.localRotation = Quaternion.Euler(0f, 0f, heading);
    }

    private static Color ParseColor(string hex, int alpha = 255)
    {
        if (!ColorUtility.TryParseHtmlString(hex, out Color color))
            color = Color.white;

        color.a = Mathf.Clamp01(alpha / 255f);
        return color;
    }
}