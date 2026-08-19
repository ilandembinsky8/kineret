using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class DynamicFarClipByAltitude : MonoBehaviour
{
    [Header("Far Clip")]
    public float minFarClip = 120000f;
    public float maxFarClip = 500000f;
    public float altitudeMultiplier = 3.2f;
    public float extraDistance = 120000f;

    [Header("Altitude View")]
    public bool controlAltitudeView = true;
    public float groundPitch = 16.16f;
    public float highAltitudePitch = 75f;
    public float viewChangeStartAltitude = 1500f;
    public float viewChangeEndAltitude = 60000f;
    public float groundFov = 63f;
    public float highAltitudeFov = 70f;
    public float groundNearClip = 0.3f;
    public float highAltitudeNearClip = 10f;

    [Header("Fog by Altitude")]
    public bool controlFog = false;
    public float groundFogDensity = 0.0006f;
    public float fogFadeStartAltitude = 1500f;
    public float fogFadeEndAltitude = 25000f;

    [Header("Terrain LOD by Altitude")]
    public bool controlHighDetailTerrain = true;
    public float hideHighDetailAbove = 20000f;
    public float showHighDetailBelow = 15000f;
    public float visibilityRefreshSeconds = 0.5f;

    private Camera cam;
    private bool highAltitudeMode;
    private float visibilityTimer;

private void Awake()
    {
        cam = GetComponent<Camera>();
        RenderSettings.fog = false;
        Apply(true);
    }

    private void LateUpdate()
    {
        Apply(false);
    }

    private void Apply(bool forceVisibility)
    {
        if (cam == null) return;

        float altitude = Mathf.Max(0f, transform.position.y);

        if (controlAltitudeView)
        {
            float viewT = Mathf.InverseLerp(viewChangeStartAltitude, viewChangeEndAltitude, altitude);
            Vector3 localEuler = transform.localEulerAngles;
            localEuler.x = Mathf.Lerp(groundPitch, highAltitudePitch, viewT);
            transform.localRotation = Quaternion.Euler(localEuler);
            cam.fieldOfView = Mathf.Lerp(groundFov, highAltitudeFov, viewT);
            cam.nearClipPlane = Mathf.Lerp(groundNearClip, highAltitudeNearClip, viewT);
        }

        cam.farClipPlane = Mathf.Clamp(extraDistance + altitude * altitudeMultiplier, minFarClip, maxFarClip);

        if (controlFog && cam.enabled && gameObject.activeInHierarchy)
        {
            float t = Mathf.InverseLerp(fogFadeStartAltitude, fogFadeEndAltitude, altitude);
            RenderSettings.fogDensity = Mathf.Lerp(groundFogDensity, 0f, t);
        }

        if (!controlHighDetailTerrain || !cam.enabled || !gameObject.activeInHierarchy) return;

        bool previousMode = highAltitudeMode;
        if (!highAltitudeMode && altitude >= hideHighDetailAbove) highAltitudeMode = true;
        else if (highAltitudeMode && altitude <= showHighDetailBelow) highAltitudeMode = false;

        visibilityTimer += Time.unscaledDeltaTime;
        if (forceVisibility || previousMode != highAltitudeMode || visibilityTimer >= visibilityRefreshSeconds)
        {
            visibilityTimer = 0f;
            SetHighDetailVisible(!highAltitudeMode);
        }
    }

private static void SetHighDetailVisible(bool visible)
    {
        GameObject horizonRoot = GameObject.Find("Hr_SectorsContainer");
        Terrain[] terrains = Object.FindObjectsOfType<Terrain>(true);
        foreach (Terrain terrain in terrains)
        {
            if (terrain == null) continue;
            if (horizonRoot != null && terrain.transform.IsChildOf(horizonRoot.transform)) continue;

            bool canShow = visible;
            if (canShow)
            {
                // Do not let the altitude controller re-enable a streaming tile while
                // its DEM or orthophoto is still loading. This used to defeat the
                // streaming-side visibility guard every visibility refresh interval.
                var streamingTile = terrain.GetComponent<GISTech.TerrainStreaming.TerrainStreamingTerrainTile>();
                if (streamingTile != null &&
                    (streamingTile.ElevationState != GISTech.TerrainStreaming.LoadingState.Loaded ||
                     streamingTile.TextureState != GISTech.TerrainStreaming.LoadingState.Loaded))
                {
                    canShow = false;
                }
            }

            terrain.drawHeightmap = canShow;
            terrain.drawTreesAndFoliage = canShow;
        }
    }
}
