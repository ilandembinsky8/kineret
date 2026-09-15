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
    }


    private readonly List<BirdState> birds = new List<BirdState>();

    private float steeringTimer;
    private FlockMode lastMode;

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
            wanderSeed = Random.Range(0f, 10000f)
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
        for (int i = 0; i < birds.Count; i++)
        {
            BirdState bird = birds[i];

            if (bird.transform == null)
                continue;

            bird.transform.position += bird.velocity * dt;
        }
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
