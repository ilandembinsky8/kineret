using System.Collections.Generic;
using UnityEngine;

public class BirdFlockManager : MonoBehaviour
{
    public enum FlockMode
    {
        OrbitBoids,
        AircraftEncounter
    }

    [Header("Mode")]
    public FlockMode mode = FlockMode.OrbitBoids;

    [Tooltip("If the mode is changed during Play Mode, the flock is rebuilt using the new mode.")]
    public bool respawnWhenModeChanges = true;


    [Header("Bird Prefabs")]
    public List<GameObject> birdPrefabs = new List<GameObject>();

    [Min(0)]
    public int birdCount = 100;

    public bool spawnOnStart = true;

    [Tooltip("Changing Bird Count during Play Mode immediately adds/removes birds.")]
    public bool keepBirdCountSynced = true;

    [Tooltip("Optional parent for spawned birds.")]
    public Transform birdsParent;


    // =====================================================================
    // ORBIT / BOIDS MODE
    // =====================================================================

    [Header("ORBIT MODE - Center / Orbit")]
    [Tooltip("The point the flock flies around. If null, this GameObject is used.")]
    public Transform flockCenter;

    [Min(0.1f)]
    public float orbitRadius = 20f;

    [Min(0f)]
    public float orbitRadiusVariation = 6f;

    public float averageHeight = 10f;

    [Min(0f)]
    public float heightVariation = 5f;

    public bool clockwise = true;

    [Min(0f)]
    public float spawnPositionJitter = 2f;


    [Header("ORBIT MODE - Movement")]
    [Min(0.01f)]
    public float minSpeed = 5f;

    [Min(0.01f)]
    public float maxSpeed = 9f;

    [Min(0.01f)]
    public float maxAcceleration = 8f;

    [Min(1f)]
    public float maxTurnSpeed = 100f;

    [Min(1f)]
    public float visualRotationSpeed = 360f;

    [Range(0f, 1f)]
    public float initialDirectionJitter = 0.15f;


    [Header("ORBIT MODE - Boids Neighbours")]
    [Min(0.1f)]
    public float neighbourRadius = 7f;

    [Min(0.1f)]
    public float separationRadius = 2.5f;


    [Header("ORBIT MODE - Boids Weights")]
    [Min(0f)]
    public float separationWeight = 1.8f;

    [Min(0f)]
    public float alignmentWeight = 0.7f;

    [Min(0f)]
    public float cohesionWeight = 0.6f;


    [Header("ORBIT MODE - Orbit Behaviour")]
    [Min(0f)]
    public float orbitWeight = 2f;

    [Min(0f)]
    public float radialCorrectionWeight = 1.3f;

    [Min(0.1f)]
    public float radialCorrectionRange = 8f;

    [Min(0f)]
    public float altitudeWeight = 0.8f;

    [Min(0.1f)]
    public float altitudeCorrectionRange = 5f;


    [Header("ORBIT MODE - Wander")]
    [Range(0f, 2f)]
    public float wanderWeight = 0.25f;

    [Min(0.001f)]
    public float wanderFrequency = 0.15f;

    [Range(0f, 1f)]
    public float verticalWander = 0.3f;


    [Header("ORBIT MODE - Emergency Containment")]
    [Min(0f)]
    public float maxDistanceFromCenter = 50f;

    [Min(0f)]
    public float returnToCenterWeight = 4f;


    [Header("ORBIT MODE - Banking")]
    public float bankMultiplier = 0.7f;

    [Range(0f, 90f)]
    public float maxBankAngle = 35f;

    [Min(1f)]
    public float bankSpeed = 90f;


    // =====================================================================
    // AIRCRAFT ENCOUNTER MODE
    // =====================================================================

    [Header("ENCOUNTER MODE - Aircraft")]
    [Tooltip("The aircraft/player Transform the flock should intercept.")]
    public Transform aircraft;

    [Tooltip("Optional fixed spawn point. If null, the flock is spawned ahead of the aircraft.")]
    public Transform encounterSpawnPoint;

    [Tooltip("When no Encounter Spawn Point is assigned, place the flock this far ahead of the aircraft.")]
    [Min(0f)]
    public float encounterSpawnDistanceAhead = 120f;

    [Tooltip("If enabled, the center of the flock is forced to the aircraft's current altitude.")]
    public bool matchAircraftAltitude = true;

    [Tooltip("Added to the aircraft altitude when Match Aircraft Altitude is enabled.")]
    public float encounterAltitudeOffset = 0f;

    [Tooltip("Used when aircraft movement cannot yet be measured, for example on the first frame.")]
    [Min(0f)]
    public float aircraftSpeedFallback = 40f;

    [Tooltip("Smoothing applied to velocity estimated from the aircraft Transform.")]
    [Min(0f)]
    public float aircraftVelocitySmoothing = 8f;

    [Tooltip("If measured speed is below this, Aircraft Speed Fallback is used.")]
    [Min(0f)]
    public float minimumMeasuredAircraftSpeed = 0.5f;


    [Header("ENCOUNTER MODE - Flock Spawn Volume")]
    [Tooltip("Total left/right width of the spawn volume.")]
    [Min(0f)]
    public float encounterSpreadWidth = 30f;

    [Tooltip("Total vertical height of the spawn volume.")]
    [Min(0f)]
    public float encounterSpreadHeight = 12f;

    [Tooltip("Total front/back depth of the spawn volume.")]
    [Min(0f)]
    public float encounterSpreadDepth = 12f;

    [Tooltip("Use an ellipsoid volume instead of a rectangular box.")]
    public bool encounterUseEllipsoidDistribution = true;


    [Header("ENCOUNTER MODE - Bird Flight")]
    [Min(0.01f)]
    public float encounterMinBirdSpeed = 12f;

    [Min(0.01f)]
    public float encounterMaxBirdSpeed = 16f;

    [Tooltip("Calculate a true intercept course based on the aircraft velocity at spawn time.")]
    public bool useInterceptCourse = true;

    [Tooltip("Maximum prediction time used when calculating the intercept point.")]
    [Min(0.1f)]
    public float maxInterceptLeadTime = 15f;

    [Tooltip("Keep the flock roughly as one moving volume by giving all birds nearly the same heading. Recommended.")]
    public bool keepEncounterFormationShape = true;

    [Tooltip("Random angular deviation from the calculated encounter direction. 0 gives the most deterministic collision course.")]
    [Range(0f, 15f)]
    public float encounterDirectionScatterDegrees = 1f;

    [Tooltip("For individual intercept mode, spreads the birds' target points around the calculated intercept point.")]
    [Min(0f)]
    public float encounterAimPointSpread = 2f;


    [Header("ENCOUNTER MODE - Gradual Reveal")]
    [Tooltip("Let the flock build up instead of arriving all at once. Every bird is still spawned together in the right place; only how many are shown changes over time.")]
    public bool encounterGradualReveal = false;

    [Tooltip("Share of the flock visible the moment it spawns. These are the birds nearest the middle of the volume.")]
    [Range(0f, 1f)]
    public float encounterInitialVisibleFraction = 1f / 3f;

    [Tooltip("Seconds taken to go from the initial share up to the full Bird Count.")]
    [Min(0.01f)]
    public float encounterRevealDuration = 5f;

    [Tooltip("Seconds to hold the initial share before the flock starts filling out. Useful because a ramp that finishes while the flock is still distant is not visible; delaying it lets it play out when the flock is close enough to read.")]
    [Min(0f)]
    public float encounterRevealDelay = 0f;

    [Tooltip("Also grow the volume over the ramp. Birds start bunched near the middle and drift out to their final spread, so the flock opens up instead of only gaining members.")]
    public bool encounterExpandFromCentre = false;

    [Tooltip("Width, height and depth of the spawn volume at the start of the ramp, as a share of the final size.")]
    [Range(0.01f, 1f)]
    public float encounterInitialSpreadFraction = 1f / 3f;

    [Tooltip("Seconds each bird takes to fade up from fully transparent as its turn to be revealed comes round. Zero makes birds pop in. Only the birds currently mid-fade are drawn transparent; once a bird is fully faded in it goes back to its original opaque materials.")]
    [Min(0f)]
    public float encounterFadeInDuration = 1f;


    [Header("Shared Animation")]
    [Tooltip("Random Animator.speed range assigned to each bird.")]
    public Vector2 animationSpeedRange = new Vector2(0.9f, 1.15f);

    public bool disableRootMotion = true;


    [Header("Shared Visual Variation")]
    public Vector2 randomScaleRange = new Vector2(0.9f, 1.1f);

    [Tooltip("Use this if the model doesn't face Unity's +Z direction.")]
    public Vector3 modelRotationOffsetEuler;


    [Header("Performance")]
    [Tooltip("Boid decisions per second. Used only by OrbitBoids mode.")]
    [Range(5f, 60f)]
    public float steeringUpdateRate = 20f;


    private class BirdState
    {
        public GameObject gameObject;
        public Transform transform;

        public Vector3 velocity;
        public Vector3 desiredVelocity;

        public float radiusVariation;
        public float heightVariation;
        public float speedVariation;
        public float wanderSeed;
        public float bankAngle;

        public Animator[] animators;

        //Gradual reveal: a hidden bird keeps flying, it just is not drawn yet.
        public bool revealed = true;
        public float revealOrder;
        public Renderer[] renderers;
        public Collider[] colliders;
        public bool collidersOn = true;

        //Fade-in: while a bird is fading its renderers run on transparent copies of their
        //materials, and go back to the shared opaque originals the moment it reaches full alpha.
        public bool fading;
        public float fadeTimer;
        public Material[][] opaqueMaterials;
        public Material[][] fadeMaterials;
        public Color[][] baseColours;

        //Expansion: the bird flies its normal course from flightPosition, and is then drawn pulled
        //in toward the middle by a share of spawnOffsetWorld that shrinks to nothing as the ramp runs.
        public Vector3 flightPosition;
        public Vector3 spawnOffsetWorld;
    }


    private readonly List<BirdState> birds = new List<BirdState>();

    private float steeringTimer;
    private FlockMode lastMode;
    private float encounterRevealTimer;

    private static readonly int BaseColourId = Shader.PropertyToID("_BaseColor");

    //One transparent copy per source material, shared by every bird that is mid-fade.
    private readonly Dictionary<Material, Material> fadeVariants = new Dictionary<Material, Material>();
    private MaterialPropertyBlock fadeBlock;

    // Aircraft velocity estimation.
    private Vector3 estimatedAircraftVelocity;
    private Vector3 lastAircraftPosition;
    private bool hasAircraftPositionSample;

    // Encounter snapshot. Once a flock is spawned, this snapshot does not move.
    private bool encounterContextPrepared;
    private Vector3 encounterCenterSnapshot;
    private Quaternion encounterOrientationSnapshot;
    private Vector3 aircraftPositionSnapshot;
    private Vector3 aircraftVelocitySnapshot;
    private Vector3 commonEncounterDirection;
    private Vector3 commonEncounterTargetPoint;


    private void Start()
    {
        lastMode = mode;

        if (aircraft != null)
        {
            lastAircraftPosition = aircraft.position;
            hasAircraftPositionSample = true;
            estimatedAircraftVelocity = aircraft.forward * aircraftSpeedFallback;
        }

        if (spawnOnStart)
            RespawnFlock();
    }


    private void Update()
    {
        UpdateAircraftVelocityEstimate();

        if (mode != lastMode)
        {
            lastMode = mode;
            encounterContextPrepared = false;

            if (respawnWhenModeChanges)
                RespawnFlock();
        }

        CleanupMissingBirds();

        if (keepBirdCountSynced)
            SyncBirdCount();

        if (birds.Count == 0)
            return;

        float dt = Time.deltaTime;

        if (dt <= 0f)
            return;

        UpdateGradualReveal(dt);

        if (mode == FlockMode.OrbitBoids)
        {
            steeringTimer -= dt;

            if (steeringTimer <= 0f)
            {
                CalculateDesiredVelocities();
                steeringTimer = 1f / Mathf.Max(1f, steeringUpdateRate);
            }

            UpdateOrbitMovement(dt);
        }
        else
        {
            UpdateEncounterMovement(dt);
        }
    }


    // =====================================================================
    // PUBLIC / SPAWNING
    // =====================================================================

    [ContextMenu("Respawn Flock")]
    public void RespawnFlock()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Respawn Flock is intended for Play Mode.", this);
            return;
        }

        ClearFlock();
        encounterContextPrepared = false;
        encounterRevealTimer = 0f;

        if (mode == FlockMode.AircraftEncounter && !PrepareEncounterContext())
            return;

        for (int i = 0; i < birdCount; i++)
        {
            if (!SpawnBird())
                break;
        }
    }


    [ContextMenu("Spawn Aircraft Encounter")]
    public void SpawnAircraftEncounter()
    {
        mode = FlockMode.AircraftEncounter;
        lastMode = mode;
        RespawnFlock();
    }


    [ContextMenu("Clear Flock")]
    public void ClearFlock()
    {
        for (int i = birds.Count - 1; i >= 0; i--)
        {
            if (birds[i].gameObject != null)
                Destroy(birds[i].gameObject);
        }

        birds.Clear();
    }


    private void SyncBirdCount()
    {
        birdCount = Mathf.Max(0, birdCount);

        if (birds.Count < birdCount)
        {
            if (mode == FlockMode.AircraftEncounter && !encounterContextPrepared)
            {
                if (!PrepareEncounterContext())
                    return;
            }

            int amountToAdd = birdCount - birds.Count;

            for (int i = 0; i < amountToAdd; i++)
            {
                if (!SpawnBird())
                    break;
            }
        }
        else if (birds.Count > birdCount)
        {
            while (birds.Count > birdCount)
            {
                int index = birds.Count - 1;

                if (birds[index].gameObject != null)
                    Destroy(birds[index].gameObject);

                birds.RemoveAt(index);
            }
        }
    }


    private bool SpawnBird()
    {
        return mode == FlockMode.OrbitBoids
            ? SpawnOrbitBird()
            : SpawnEncounterBird();
    }


    private bool SpawnOrbitBird()
    {
        GameObject prefab = GetRandomPrefab();

        if (prefab == null)
            return false;

        BirdState bird = CreateBaseBirdState();

        Vector3 center = GetOrbitCenter();
        float angle = Random.Range(0f, Mathf.PI * 2f);

        Vector3 radialDirection = new Vector3(
            Mathf.Cos(angle),
            0f,
            Mathf.Sin(angle)
        );

        float preferredRadius = GetPreferredRadius(bird);
        float preferredHeight = GetPreferredHeight(bird);

        Vector3 position =
            center +
            radialDirection * preferredRadius +
            Vector3.up * preferredHeight;

        position += Random.insideUnitSphere * spawnPositionJitter;

        Vector3 tangent = GetOrbitTangent(radialDirection);
        Vector3 randomDirection = Random.insideUnitSphere * initialDirectionJitter;
        Vector3 flightDirection = (tangent + randomDirection).normalized;

        float speed = GetPreferredOrbitSpeed(bird);

        bird.velocity = flightDirection * speed;
        bird.desiredVelocity = bird.velocity;

        InstantiateBirdVisual(prefab, bird, position, flightDirection);
        birds.Add(bird);

        return true;
    }


    private bool SpawnEncounterBird()
    {
        if (!encounterContextPrepared && !PrepareEncounterContext())
            return false;

        GameObject prefab = GetRandomPrefab();

        if (prefab == null)
            return false;

        BirdState bird = CreateBaseBirdState();

        Vector3 localOffset = GetEncounterSpawnOffset();
        Vector3 position =
            encounterCenterSnapshot +
            encounterOrientationSnapshot * localOffset;

        //Reveal runs from the middle of the volume outwards, fixed here at spawn rather than
        //recomputed as the formation drifts, so birds cannot flicker back out.
        //
        //The offset is scaled by the half-extents so an ellipsoid spread still reads 0 at the
        //centre and 1 at the shell. It is then cubed: Random.insideUnitSphere is uniform by
        //VOLUME, so the share of birds within radius r is r^3. Cubing makes revealOrder uniform
        //over 0..1, which is what the visible-fraction threshold expects - without it, asking for
        //a third of the flock would show only about 4% of it.
        float normalisedRadius = new Vector3(
            localOffset.x / Mathf.Max(0.01f, encounterSpreadWidth * 0.5f),
            localOffset.y / Mathf.Max(0.01f, encounterSpreadHeight * 0.5f),
            localOffset.z / Mathf.Max(0.01f, encounterSpreadDepth * 0.5f)
        ).magnitude;

        bird.revealOrder = Mathf.Clamp01(normalisedRadius * normalisedRadius * normalisedRadius);

        //Kept so the expansion can pull the bird in toward the middle and release it over the ramp.
        bird.spawnOffsetWorld = encounterOrientationSnapshot * localOffset;
        bird.flightPosition = position;

        float speed = Random.Range(
            Mathf.Min(encounterMinBirdSpeed, encounterMaxBirdSpeed),
            Mathf.Max(encounterMinBirdSpeed, encounterMaxBirdSpeed)
        );

        Vector3 flightDirection;

        if (keepEncounterFormationShape)
        {
            flightDirection = commonEncounterDirection;
        }
        else
        {
            Vector3 targetPoint;

            if (useInterceptCourse)
            {
                TryCalculateInterceptDirection(
                    position,
                    aircraftPositionSnapshot,
                    aircraftVelocitySnapshot,
                    speed,
                    out flightDirection,
                    out targetPoint
                );
            }
            else
            {
                targetPoint = aircraftPositionSnapshot;
                flightDirection = (targetPoint - position).normalized;
            }

            if (encounterAimPointSpread > 0f)
            {
                Vector3 targetOffset = Random.insideUnitSphere * encounterAimPointSpread;
                flightDirection = (targetPoint + targetOffset - position).normalized;
            }
        }

        flightDirection = ApplyDirectionScatter(
            flightDirection,
            encounterDirectionScatterDegrees
        );

        bird.velocity = flightDirection * speed;
        bird.desiredVelocity = bird.velocity;

        InstantiateBirdVisual(prefab, bird, position, flightDirection);
        birds.Add(bird);

        return true;
    }


    private BirdState CreateBaseBirdState()
    {
        return new BirdState
        {
            radiusVariation = Random.Range(-1f, 1f),
            heightVariation = Random.Range(-1f, 1f),
            speedVariation = Random.value,
            wanderSeed = Random.Range(0f, 10000f),
            revealOrder = Random.value
        };
    }


    private void InstantiateBirdVisual(
        GameObject prefab,
        BirdState bird,
        Vector3 position,
        Vector3 flightDirection)
    {
        Quaternion rotation =
            Quaternion.LookRotation(flightDirection, Vector3.up) *
            Quaternion.Euler(modelRotationOffsetEuler);

        GameObject instance = Instantiate(
            prefab,
            position,
            rotation,
            birdsParent
        );

        bird.gameObject = instance;
        bird.transform = instance.transform;

        float minScale = Mathf.Min(randomScaleRange.x, randomScaleRange.y);
        float maxScale = Mathf.Max(randomScaleRange.x, randomScaleRange.y);
        float scale = Random.Range(minScale, maxScale);

        bird.transform.localScale *= scale;

        bird.animators = instance.GetComponentsInChildren<Animator>(true);

        float minAnimSpeed = Mathf.Min(animationSpeedRange.x, animationSpeedRange.y);
        float maxAnimSpeed = Mathf.Max(animationSpeedRange.x, animationSpeedRange.y);
        float animationSpeed = Random.Range(minAnimSpeed, maxAnimSpeed);

        foreach (Animator animator in bird.animators)
        {
            if (animator == null)
                continue;

            animator.speed = animationSpeed;

            if (disableRootMotion)
                animator.applyRootMotion = false;
        }

        bird.renderers = instance.GetComponentsInChildren<Renderer>(true);
        bird.colliders = instance.GetComponentsInChildren<Collider>(true);
        bird.revealed = true;
        bird.collidersOn = true;

        if (UsesFadeIn())
            CacheFadeMaterials(bird);

        //revealOrder is already set by the caller: encounter mode orders it from the middle of
        //the volume outwards, everything else leaves the random value from CreateBaseBirdState.
        bool visible = bird.revealOrder <= GetVisibleFraction();

        ApplyBirdVisibility(bird, visible);

        //ApplyBirdVisibility only acts on a change, and the bird is born visible, so the first
        //batch - the birds already on show at spawn - needs its fade kicked off here.
        if (visible && UsesFadeIn())
            BeginFade(bird);

        ApplyBirdColliders(bird, IsRampComplete());

        //Place it contracted straight away, so the first frame already shows the tight formation.
        if (encounterGradualReveal && encounterExpandFromCentre && mode == FlockMode.AircraftEncounter)
        {
            bird.transform.position = bird.flightPosition - bird.spawnOffsetWorld * (1f - GetSpreadScale());
        }
    }


    // ------------------------------------------------------------
    // GRADUAL REVEAL (encounter mode)
    // ------------------------------------------------------------

    /// <summary>
    /// How much of the flock should be on show right now. Climbs from the initial share to all
    /// of it over Encounter Reveal Duration.
    /// </summary>
    private float GetVisibleFraction()
    {
        if (!encounterGradualReveal || mode != FlockMode.AircraftEncounter)
            return 1f;

        float elapsed = encounterRevealTimer - encounterRevealDelay;

        if (elapsed <= 0f)
            return Mathf.Clamp01(encounterInitialVisibleFraction);

        float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, encounterRevealDuration));

        return Mathf.Lerp(Mathf.Clamp01(encounterInitialVisibleFraction), 1f, t);
    }


    /// <summary>
    /// True once the flock is whole and at its final spread. Until then it is still assembling,
    /// so nothing in it can be hit.
    /// </summary>
    private bool IsRampComplete()
    {
        return GetVisibleFraction() >= 1f && GetSpreadScale() >= 1f;
    }


    private bool UsesFadeIn()
    {
        return encounterGradualReveal
            && mode == FlockMode.AircraftEncounter
            && encounterFadeInDuration > 0f;
    }


    private void ApplyBirdVisibility(BirdState bird, bool visible)
    {
        if (bird.revealed == visible)
            return;

        bird.revealed = visible;

        if (bird.renderers != null)
        {
            foreach (Renderer renderer in bird.renderers)
                if (renderer != null)
                    renderer.enabled = visible;
        }

        if (visible && UsesFadeIn())
            BeginFade(bird);
        else if (!visible && bird.fading)
            EndFade(bird);
    }


    /// <summary>
    /// Colliders follow the flock as a whole rather than each bird's own reveal: they come on
    /// only once the ramp has finished. That leaves the whole assembly - birds appearing and the
    /// volume opening out - as something the player watches rather than something that can hit
    /// them, and the ramp finishes well before the aircraft is anywhere near the flock.
    /// </summary>
    private void ApplyBirdColliders(BirdState bird, bool on)
    {
        if (bird.collidersOn == on)
            return;

        bird.collidersOn = on;

        if (bird.colliders == null)
            return;

        foreach (Collider collider in bird.colliders)
            if (collider != null)
                collider.enabled = on;
    }


    private void UpdateGradualReveal(float dt)
    {
        if (!encounterGradualReveal || mode != FlockMode.AircraftEncounter)
            return;

        encounterRevealTimer += dt;

        float fraction = GetVisibleFraction();
        bool armed = IsRampComplete();

        for (int i = 0; i < birds.Count; i++)
        {
            BirdState bird = birds[i];

            ApplyBirdVisibility(bird, bird.revealOrder <= fraction);
            ApplyBirdColliders(bird, armed);

            if (!bird.fading)
                continue;

            bird.fadeTimer += dt;

            float alpha = bird.fadeTimer / Mathf.Max(0.0001f, encounterFadeInDuration);

            if (alpha >= 1f)
                EndFade(bird);
            else
                ApplyFadeAlpha(bird, alpha);
        }
    }


    // ------------------------------------------------------------
    // FADE IN (encounter mode)
    // ------------------------------------------------------------

    /// <summary>
    /// A transparent copy of an opaque material. URP's Lit shader ignores alpha entirely while
    /// its surface type is Opaque, so there is no way to fade the birds without a second material.
    /// One copy per source material is enough: the per-bird alpha rides on a property block.
    /// </summary>
    private Material GetFadeVariant(Material source)
    {
        if (source == null)
            return null;

        Material variant;
        if (fadeVariants.TryGetValue(source, out variant))
            return variant;

        variant = new Material(source);
        variant.name = source.name + " (Fade)";

        variant.SetOverrideTag("RenderType", "Transparent");
        variant.SetFloat("_Surface", 1f);
        variant.SetFloat("_Blend", 0f);
        variant.SetFloat("_BlendModePreserveSpecular", 0f);
        variant.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        variant.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        variant.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
        variant.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        variant.SetFloat("_ZWrite", 0f);

        variant.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        variant.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        variant.DisableKeyword("_ALPHAMODULATE_ON");

        //Depth and shadows are written by opaque geometry. A half-faded bird doing either would
        //punch a hole in the sky behind it and drop a full-strength shadow it has not earned.
        variant.SetShaderPassEnabled("DepthOnly", false);
        variant.SetShaderPassEnabled("DepthNormals", false);
        variant.SetShaderPassEnabled("ShadowCaster", false);

        variant.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        fadeVariants.Add(source, variant);

        return variant;
    }


    private void CacheFadeMaterials(BirdState bird)
    {
        if (bird.renderers == null || bird.renderers.Length == 0)
            return;

        bird.opaqueMaterials = new Material[bird.renderers.Length][];
        bird.fadeMaterials = new Material[bird.renderers.Length][];
        bird.baseColours = new Color[bird.renderers.Length][];

        for (int i = 0; i < bird.renderers.Length; i++)
        {
            Renderer renderer = bird.renderers[i];

            if (renderer == null)
                continue;

            Material[] source = renderer.sharedMaterials;
            var fade = new Material[source.Length];
            var colours = new Color[source.Length];

            for (int s = 0; s < source.Length; s++)
            {
                fade[s] = GetFadeVariant(source[s]);
                colours[s] = source[s] != null && source[s].HasProperty(BaseColourId)
                    ? source[s].GetColor(BaseColourId)
                    : Color.white;
            }

            bird.opaqueMaterials[i] = source;
            bird.fadeMaterials[i] = fade;
            bird.baseColours[i] = colours;
        }
    }


    private void BeginFade(BirdState bird)
    {
        if (bird.fadeMaterials == null)
            return;

        bird.fading = true;
        bird.fadeTimer = 0f;

        for (int i = 0; i < bird.renderers.Length; i++)
        {
            if (bird.renderers[i] != null && bird.fadeMaterials[i] != null)
                bird.renderers[i].sharedMaterials = bird.fadeMaterials[i];
        }

        ApplyFadeAlpha(bird, 0f);
    }


    private void ApplyFadeAlpha(BirdState bird, float alpha)
    {
        if (bird.baseColours == null)
            return;

        if (fadeBlock == null)
            fadeBlock = new MaterialPropertyBlock();

        for (int i = 0; i < bird.renderers.Length; i++)
        {
            Renderer renderer = bird.renderers[i];

            if (renderer == null || bird.baseColours[i] == null)
                continue;

            for (int s = 0; s < bird.baseColours[i].Length; s++)
            {
                Color colour = bird.baseColours[i][s];
                colour.a *= alpha;

                fadeBlock.Clear();
                fadeBlock.SetColor(BaseColourId, colour);
                renderer.SetPropertyBlock(fadeBlock, s);
            }
        }
    }


    private void EndFade(BirdState bird)
    {
        bird.fading = false;

        if (bird.opaqueMaterials == null)
            return;

        for (int i = 0; i < bird.renderers.Length; i++)
        {
            Renderer renderer = bird.renderers[i];

            if (renderer == null || bird.opaqueMaterials[i] == null)
                continue;

            for (int s = 0; s < bird.opaqueMaterials[i].Length; s++)
                renderer.SetPropertyBlock(null, s);

            renderer.sharedMaterials = bird.opaqueMaterials[i];
        }
    }


    private void OnDestroy()
    {
        foreach (Material variant in fadeVariants.Values)
            if (variant != null)
                Destroy(variant);

        fadeVariants.Clear();
    }


    private GameObject GetRandomPrefab()
    {
        if (birdPrefabs == null || birdPrefabs.Count == 0)
            return null;

        for (int attempt = 0; attempt < 10; attempt++)
        {
            GameObject prefab = birdPrefabs[Random.Range(0, birdPrefabs.Count)];

            if (prefab != null)
                return prefab;
        }

        foreach (GameObject prefab in birdPrefabs)
        {
            if (prefab != null)
                return prefab;
        }

        return null;
    }


    private void CleanupMissingBirds()
    {
        for (int i = birds.Count - 1; i >= 0; i--)
        {
            if (birds[i].gameObject == null)
                birds.RemoveAt(i);
        }
    }


    // =====================================================================
    // AIRCRAFT ENCOUNTER
    // =====================================================================

    private void UpdateAircraftVelocityEstimate()
    {
        if (aircraft == null)
        {
            hasAircraftPositionSample = false;
            return;
        }

        float dt = Time.deltaTime;

        if (!hasAircraftPositionSample || dt <= 0f)
        {
            lastAircraftPosition = aircraft.position;
            hasAircraftPositionSample = true;
            return;
        }

        Vector3 rawVelocity = (aircraft.position - lastAircraftPosition) / dt;
        lastAircraftPosition = aircraft.position;

        if (aircraftVelocitySmoothing <= 0f)
        {
            estimatedAircraftVelocity = rawVelocity;
        }
        else
        {
            float t = 1f - Mathf.Exp(-aircraftVelocitySmoothing * dt);
            estimatedAircraftVelocity = Vector3.Lerp(
                estimatedAircraftVelocity,
                rawVelocity,
                t
            );
        }
    }


    private Vector3 GetAircraftVelocityForEncounter()
    {
        if (aircraft == null)
            return Vector3.zero;

        if (estimatedAircraftVelocity.magnitude >= minimumMeasuredAircraftSpeed)
            return estimatedAircraftVelocity;

        return aircraft.forward * aircraftSpeedFallback;
    }


    private bool PrepareEncounterContext()
    {
        if (aircraft == null)
        {
            Debug.LogWarning(
                "AircraftEncounter mode requires an Aircraft Transform.",
                this
            );
            aircraft = GameObject.Find("Player").transform;
            //return false;
        }

        aircraftPositionSnapshot = aircraft.position;
        aircraftVelocitySnapshot = GetAircraftVelocityForEncounter();

        if (encounterSpawnPoint != null)
        {
            encounterCenterSnapshot = encounterSpawnPoint.position;
            encounterOrientationSnapshot = encounterSpawnPoint.rotation;
        }
        else
        {
            encounterCenterSnapshot =
                aircraft.position +
                aircraft.forward * encounterSpawnDistanceAhead;

            encounterOrientationSnapshot = aircraft.rotation;
        }

        if (matchAircraftAltitude)
        {
            encounterCenterSnapshot.y =
                aircraft.position.y + encounterAltitudeOffset;
        }

        float averageBirdSpeed =
            (Mathf.Min(encounterMinBirdSpeed, encounterMaxBirdSpeed) +
             Mathf.Max(encounterMinBirdSpeed, encounterMaxBirdSpeed)) * 0.5f;

        if (useInterceptCourse)
        {
            TryCalculateInterceptDirection(
                encounterCenterSnapshot,
                aircraftPositionSnapshot,
                aircraftVelocitySnapshot,
                averageBirdSpeed,
                out commonEncounterDirection,
                out commonEncounterTargetPoint
            );
        }
        else
        {
            commonEncounterTargetPoint = aircraftPositionSnapshot;
            commonEncounterDirection =
                (commonEncounterTargetPoint - encounterCenterSnapshot).normalized;
        }

        if (commonEncounterDirection.sqrMagnitude < 0.001f)
            commonEncounterDirection = -aircraft.forward;

        encounterContextPrepared = true;
        return true;
    }


    private bool TryCalculateInterceptDirection(
        Vector3 birdStart,
        Vector3 targetPosition,
        Vector3 targetVelocity,
        float birdSpeed,
        out Vector3 direction,
        out Vector3 interceptPoint)
    {
        birdSpeed = Mathf.Max(0.01f, birdSpeed);

        Vector3 relativePosition = targetPosition - birdStart;

        float a = targetVelocity.sqrMagnitude - birdSpeed * birdSpeed;
        float b = 2f * Vector3.Dot(relativePosition, targetVelocity);
        float c = relativePosition.sqrMagnitude;

        float t = -1f;

        if (Mathf.Abs(a) < 0.0001f)
        {
            if (Mathf.Abs(b) > 0.0001f)
                t = -c / b;
        }
        else
        {
            float discriminant = b * b - 4f * a * c;

            if (discriminant >= 0f)
            {
                float sqrt = Mathf.Sqrt(discriminant);
                float t1 = (-b - sqrt) / (2f * a);
                float t2 = (-b + sqrt) / (2f * a);

                bool t1Valid = t1 > 0f;
                bool t2Valid = t2 > 0f;

                if (t1Valid && t2Valid)
                    t = Mathf.Min(t1, t2);
                else if (t1Valid)
                    t = t1;
                else if (t2Valid)
                    t = t2;
            }
        }

        bool exactIntercept = t > 0f;

        if (!exactIntercept)
        {
            // Approximate lead if an exact mathematical intercept is impossible.
            t = relativePosition.magnitude / birdSpeed;
        }

        t = Mathf.Clamp(t, 0f, maxInterceptLeadTime);

        interceptPoint = targetPosition + targetVelocity * t;
        direction = (interceptPoint - birdStart).normalized;

        if (direction.sqrMagnitude < 0.001f)
            direction = -targetVelocity.normalized;

        return exactIntercept;
    }


    private Vector3 GetEncounterSpawnOffset()
    {
        Vector3 halfSize = new Vector3(
            encounterSpreadWidth * 0.5f,
            encounterSpreadHeight * 0.5f,
            encounterSpreadDepth * 0.5f
        );

        if (encounterUseEllipsoidDistribution)
        {
            Vector3 random = Random.insideUnitSphere;

            return new Vector3(
                random.x * halfSize.x,
                random.y * halfSize.y,
                random.z * halfSize.z
            );
        }

        return new Vector3(
            Random.Range(-halfSize.x, halfSize.x),
            Random.Range(-halfSize.y, halfSize.y),
            Random.Range(-halfSize.z, halfSize.z)
        );
    }


    private Vector3 ApplyDirectionScatter(Vector3 direction, float scatterDegrees)
    {
        if (scatterDegrees <= 0f || direction.sqrMagnitude < 0.001f)
            return direction.normalized;

        Quaternion scatter = Quaternion.Euler(
            Random.Range(-scatterDegrees, scatterDegrees),
            Random.Range(-scatterDegrees, scatterDegrees),
            0f
        );

        return (scatter * direction.normalized).normalized;
    }


    private void UpdateEncounterMovement(float dt)
    {
        // IMPORTANT: No steering here.
        // Each bird keeps exactly the velocity assigned at spawn time.
        bool expanding = encounterGradualReveal && encounterExpandFromCentre;
        float pullIn = expanding ? 1f - GetSpreadScale() : 0f;

        for (int i = 0; i < birds.Count; i++)
        {
            BirdState bird = birds[i];

            if (bird.transform == null)
                continue;

            if (!expanding)
            {
                bird.transform.position += bird.velocity * dt;
                continue;
            }

            //The course is flown at full spread; the contraction is applied on top, so the flight
            //path stays exactly what the intercept solution produced.
            bird.flightPosition += bird.velocity * dt;
            bird.transform.position = bird.flightPosition - bird.spawnOffsetWorld * pullIn;
        }
    }


    /// <summary>
    /// Size of the volume right now as a share of its final size, on the same ramp as the reveal.
    /// </summary>
    private float GetSpreadScale()
    {
        if (!encounterGradualReveal || !encounterExpandFromCentre || mode != FlockMode.AircraftEncounter)
            return 1f;

        float elapsed = encounterRevealTimer - encounterRevealDelay;

        if (elapsed <= 0f)
            return Mathf.Clamp01(encounterInitialSpreadFraction);

        float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, encounterRevealDuration));

        return Mathf.Lerp(Mathf.Clamp01(encounterInitialSpreadFraction), 1f, t);
    }


    // =====================================================================
    // ORBIT / BOIDS
    // =====================================================================

    private void CalculateDesiredVelocities()
    {
        Vector3 center = GetOrbitCenter();

        float neighbourRadiusSqr = neighbourRadius * neighbourRadius;
        float separationRadiusSqr = separationRadius * separationRadius;

        for (int i = 0; i < birds.Count; i++)
        {
            BirdState bird = birds[i];

            if (bird.transform == null)
                continue;

            Vector3 position = bird.transform.position;

            Vector3 separation = Vector3.zero;
            Vector3 alignment = Vector3.zero;
            Vector3 cohesionPosition = Vector3.zero;

            int neighbours = 0;

            for (int j = 0; j < birds.Count; j++)
            {
                if (i == j)
                    continue;

                BirdState other = birds[j];

                if (other.transform == null)
                    continue;

                Vector3 offset = other.transform.position - position;
                float sqrDistance = offset.sqrMagnitude;

                if (sqrDistance > neighbourRadiusSqr)
                    continue;

                neighbours++;
                alignment += other.velocity;
                cohesionPosition += other.transform.position;

                if (sqrDistance < separationRadiusSqr)
                {
                    float distance = Mathf.Sqrt(Mathf.Max(sqrDistance, 0.0001f));
                    Vector3 away = -offset / distance;

                    float strength =
                        1f - Mathf.Clamp01(distance / separationRadius);

                    separation +=
                        away * strength / Mathf.Max(distance, 0.1f);
                }
            }

            Vector3 alignmentDirection = Vector3.zero;
            Vector3 cohesionDirection = Vector3.zero;
            Vector3 separationDirection = Vector3.zero;

            if (neighbours > 0)
            {
                alignment /= neighbours;

                if (alignment.sqrMagnitude > 0.001f)
                    alignmentDirection = alignment.normalized;

                Vector3 averagePosition = cohesionPosition / neighbours;
                Vector3 toAverage = averagePosition - position;

                if (toAverage.sqrMagnitude > 0.001f)
                    cohesionDirection = toAverage.normalized;
            }

            if (separation.sqrMagnitude > 0.001f)
                separationDirection = separation.normalized;

            Vector3 radial = position - center;
            radial.y = 0f;

            float radialDistance = radial.magnitude;

            Vector3 radialDirection =
                radialDistance > 0.001f
                    ? radial / radialDistance
                    : Vector3.forward;

            Vector3 orbitDirection = GetOrbitTangent(radialDirection);

            float preferredRadius = GetPreferredRadius(bird);
            float radiusError = radialDistance - preferredRadius;

            float radialCorrectionAmount = Mathf.Clamp(
                radiusError / radialCorrectionRange,
                -1f,
                1f
            );

            Vector3 radialCorrection =
                -radialDirection * radialCorrectionAmount;

            float desiredY = center.y + GetPreferredHeight(bird);
            float altitudeError = desiredY - position.y;

            float altitudeCorrectionAmount = Mathf.Clamp(
                altitudeError / altitudeCorrectionRange,
                -1f,
                1f
            );

            Vector3 altitudeCorrection =
                Vector3.up * altitudeCorrectionAmount;

            Vector3 wander = CalculateWander(bird);

            Vector3 desiredDirection = Vector3.zero;

            desiredDirection += orbitDirection * orbitWeight;
            desiredDirection += radialCorrection * radialCorrectionWeight;
            desiredDirection += altitudeCorrection * altitudeWeight;
            desiredDirection += separationDirection * separationWeight;
            desiredDirection += alignmentDirection * alignmentWeight;
            desiredDirection += cohesionDirection * cohesionWeight;
            desiredDirection += wander * wanderWeight;

            if (maxDistanceFromCenter > 0f)
            {
                Vector3 fromCenter = position - center;
                float distance = fromCenter.magnitude;

                if (distance > maxDistanceFromCenter)
                {
                    Vector3 towardCenter = (-fromCenter).normalized;

                    float excess =
                        (distance - maxDistanceFromCenter) /
                        Mathf.Max(maxDistanceFromCenter, 0.01f);

                    desiredDirection +=
                        towardCenter *
                        returnToCenterWeight *
                        (1f + excess);
                }
            }

            if (desiredDirection.sqrMagnitude < 0.001f)
                desiredDirection = bird.velocity.normalized;

            bird.desiredVelocity =
                desiredDirection.normalized *
                GetPreferredOrbitSpeed(bird);
        }
    }


    private void UpdateOrbitMovement(float dt)
    {
        for (int i = 0; i < birds.Count; i++)
        {
            BirdState bird = birds[i];

            if (bird.transform == null)
                continue;

            Vector3 oldVelocity = bird.velocity;

            if (oldVelocity.sqrMagnitude < 0.001f)
                oldVelocity = bird.desiredVelocity;

            bird.velocity = Vector3.RotateTowards(
                oldVelocity,
                bird.desiredVelocity,
                maxTurnSpeed * Mathf.Deg2Rad * dt,
                maxAcceleration * dt
            );

            float speed = Mathf.Clamp(
                bird.velocity.magnitude,
                Mathf.Min(minSpeed, maxSpeed),
                Mathf.Max(minSpeed, maxSpeed)
            );

            if (bird.velocity.sqrMagnitude > 0.001f)
                bird.velocity = bird.velocity.normalized * speed;

            bird.transform.position += bird.velocity * dt;

            Vector3 currentFlat = new Vector3(
                oldVelocity.x,
                0f,
                oldVelocity.z
            );

            Vector3 desiredFlat = new Vector3(
                bird.desiredVelocity.x,
                0f,
                bird.desiredVelocity.z
            );

            float turnAngle = 0f;

            if (
                currentFlat.sqrMagnitude > 0.001f &&
                desiredFlat.sqrMagnitude > 0.001f)
            {
                turnAngle = Vector3.SignedAngle(
                    currentFlat,
                    desiredFlat,
                    Vector3.up
                );
            }

            float targetBank = Mathf.Clamp(
                -turnAngle * bankMultiplier,
                -maxBankAngle,
                maxBankAngle
            );

            bird.bankAngle = Mathf.MoveTowards(
                bird.bankAngle,
                targetBank,
                bankSpeed * dt
            );

            if (bird.velocity.sqrMagnitude > 0.001f)
            {
                Quaternion flightRotation = Quaternion.LookRotation(
                    bird.velocity.normalized,
                    Vector3.up
                );

                Quaternion bankRotation = Quaternion.AngleAxis(
                    bird.bankAngle,
                    Vector3.forward
                );

                Quaternion modelOffset = Quaternion.Euler(
                    modelRotationOffsetEuler
                );

                Quaternion targetRotation =
                    flightRotation *
                    bankRotation *
                    modelOffset;

                bird.transform.rotation = Quaternion.RotateTowards(
                    bird.transform.rotation,
                    targetRotation,
                    visualRotationSpeed * dt
                );
            }
        }
    }


    // =====================================================================
    // SHARED HELPERS
    // =====================================================================

    private Vector3 CalculateWander(BirdState bird)
    {
        float time = Time.time * wanderFrequency;

        float x = Mathf.PerlinNoise(bird.wanderSeed, time) * 2f - 1f;
        float y = Mathf.PerlinNoise(bird.wanderSeed + 100f, time) * 2f - 1f;
        float z = Mathf.PerlinNoise(bird.wanderSeed + 200f, time) * 2f - 1f;

        Vector3 wander = new Vector3(
            x,
            y * verticalWander,
            z
        );

        if (wander.sqrMagnitude > 0.001f)
            wander.Normalize();

        return wander;
    }


    private float GetPreferredRadius(BirdState bird)
    {
        return Mathf.Max(
            0.1f,
            orbitRadius +
            bird.radiusVariation * orbitRadiusVariation
        );
    }


    private float GetPreferredHeight(BirdState bird)
    {
        return
            averageHeight +
            bird.heightVariation * heightVariation;
    }


    private float GetPreferredOrbitSpeed(BirdState bird)
    {
        float min = Mathf.Min(minSpeed, maxSpeed);
        float max = Mathf.Max(minSpeed, maxSpeed);

        return Mathf.Lerp(min, max, bird.speedVariation);
    }


    private Vector3 GetOrbitTangent(Vector3 radialDirection)
    {
        return clockwise
            ? Vector3.Cross(Vector3.up, radialDirection).normalized
            : Vector3.Cross(radialDirection, Vector3.up).normalized;
    }


    private Vector3 GetOrbitCenter()
    {
        return flockCenter != null
            ? flockCenter.position
            : transform.position;
    }


    // =====================================================================
    // GIZMOS
    // =====================================================================

    private void OnDrawGizmosSelected()
    {
        if (mode == FlockMode.OrbitBoids)
        {
            DrawOrbitGizmos();
        }
        else
        {
            DrawEncounterGizmos();
        }
    }


    private void DrawOrbitGizmos()
    {
        Vector3 center = GetOrbitCenter();
        Vector3 middle = center + Vector3.up * averageHeight;

        DrawCircle(middle, orbitRadius, 64);

        if (heightVariation > 0f)
        {
            DrawCircle(
                middle + Vector3.up * heightVariation,
                orbitRadius,
                48
            );

            DrawCircle(
                middle - Vector3.up * heightVariation,
                orbitRadius,
                48
            );
        }

        Gizmos.DrawWireSphere(center, 0.5f);
    }


    private void DrawEncounterGizmos()
    {
        if (aircraft == null && encounterSpawnPoint == null)
            return;

        Vector3 center;
        Quaternion orientation;

        if (Application.isPlaying && encounterContextPrepared)
        {
            center = encounterCenterSnapshot;
            orientation = encounterOrientationSnapshot;
        }
        else if (encounterSpawnPoint != null)
        {
            center = encounterSpawnPoint.position;
            orientation = encounterSpawnPoint.rotation;

            if (matchAircraftAltitude && aircraft != null)
                center.y = aircraft.position.y + encounterAltitudeOffset;
        }
        else
        {
            center =
                aircraft.position +
                aircraft.forward * encounterSpawnDistanceAhead;

            if (matchAircraftAltitude)
                center.y = aircraft.position.y + encounterAltitudeOffset;

            orientation = aircraft.rotation;
        }

        Matrix4x4 oldMatrix = Gizmos.matrix;

        Gizmos.matrix = Matrix4x4.TRS(
            center,
            orientation,
            Vector3.one
        );

        Gizmos.DrawWireCube(
            Vector3.zero,
            new Vector3(
                encounterSpreadWidth,
                encounterSpreadHeight,
                encounterSpreadDepth
            )
        );

        Gizmos.matrix = oldMatrix;

        if (aircraft != null)
        {
            Vector3 previewVelocity = Application.isPlaying
                ? GetAircraftVelocityForEncounter()
                : aircraft.forward * aircraftSpeedFallback;

            float previewBirdSpeed =
                (Mathf.Min(encounterMinBirdSpeed, encounterMaxBirdSpeed) +
                 Mathf.Max(encounterMinBirdSpeed, encounterMaxBirdSpeed)) * 0.5f;

            Vector3 direction;
            Vector3 target;

            if (useInterceptCourse)
            {
                TryCalculateInterceptDirection(
                    center,
                    aircraft.position,
                    previewVelocity,
                    previewBirdSpeed,
                    out direction,
                    out target
                );
            }
            else
            {
                target = aircraft.position;
                direction = (target - center).normalized;
            }

            Gizmos.DrawLine(center, target);
            Gizmos.DrawWireSphere(target, 1f);
        }
    }


    private void DrawCircle(Vector3 center, float radius, int segments)
    {
        if (radius <= 0f)
            return;

        Vector3 previous =
            center + new Vector3(radius, 0f, 0f);

        for (int i = 1; i <= segments; i++)
        {
            float angle =
                i / (float)segments *
                Mathf.PI * 2f;

            Vector3 next =
                center +
                new Vector3(
                    Mathf.Cos(angle) * radius,
                    0f,
                    Mathf.Sin(angle) * radius
                );

            Gizmos.DrawLine(previous, next);
            previous = next;
        }
    }
}
