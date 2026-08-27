using System.Collections.Generic;
using UnityEngine;

public class BirdFlockManager : MonoBehaviour
{
    [Header("Bird Prefabs")]
    public List<GameObject> birdPrefabs = new List<GameObject>();

    [Min(0)]
    public int birdCount = 100;

    public bool spawnOnStart = true;

    [Tooltip("When enabled, changing Bird Count during Play Mode immediately adds/removes birds.")]
    public bool keepBirdCountSynced = true;

    [Tooltip("Optional parent for all spawned birds.")]
    public Transform birdsParent;


    [Header("Flock Center / Orbit")]
    [Tooltip("The point the flock flies around. If null, this GameObject is used.")]
    public Transform flockCenter;

    [Min(0.1f)]
    public float orbitRadius = 20f;

    [Tooltip("Each bird gets a slightly different preferred orbit radius.")]
    [Min(0f)]
    public float orbitRadiusVariation = 6f;

    [Tooltip("Average height above the flock center.")]
    public float averageHeight = 10f;

    [Tooltip("Random height variation between birds.")]
    [Min(0f)]
    public float heightVariation = 5f;

    public bool clockwise = true;

    [Tooltip("Small random spawn offset so birds don't start on a perfect ring.")]
    [Min(0f)]
    public float spawnPositionJitter = 2f;


    [Header("Movement")]
    [Min(0.01f)]
    public float minSpeed = 5f;

    [Min(0.01f)]
    public float maxSpeed = 9f;

    [Tooltip("How quickly a bird can change its velocity magnitude.")]
    [Min(0.01f)]
    public float maxAcceleration = 8f;

    [Tooltip("Maximum flight turn rate in degrees per second.")]
    [Min(1f)]
    public float maxTurnSpeed = 100f;

    [Tooltip("Maximum visual rotation speed.")]
    [Min(1f)]
    public float visualRotationSpeed = 360f;

    [Tooltip("Adds some direction variation at spawn.")]
    [Range(0f, 1f)]
    public float initialDirectionJitter = 0.15f;


    [Header("Boids - Neighbours")]
    [Tooltip("Birds farther away than this are ignored by alignment/cohesion.")]
    [Min(0.1f)]
    public float neighbourRadius = 7f;

    [Tooltip("Birds inside this distance strongly repel each other.")]
    [Min(0.1f)]
    public float separationRadius = 2.5f;


    [Header("Boids - Weights")]
    [Min(0f)]
    public float separationWeight = 1.8f;

    [Min(0f)]
    public float alignmentWeight = 0.7f;

    [Min(0f)]
    public float cohesionWeight = 0.6f;


    [Header("Orbit Behaviour")]
    [Tooltip("Strength of the desire to fly tangentially around the center.")]
    [Min(0f)]
    public float orbitWeight = 2f;

    [Tooltip("Strength of correction toward the bird's preferred orbit radius.")]
    [Min(0f)]
    public float radialCorrectionWeight = 1.3f;

    [Tooltip("Distance error over which radial correction reaches full strength.")]
    [Min(0.1f)]
    public float radialCorrectionRange = 8f;

    [Tooltip("Strength of correction toward the bird's preferred altitude.")]
    [Min(0f)]
    public float altitudeWeight = 0.8f;

    [Tooltip("Height error over which altitude correction reaches full strength.")]
    [Min(0.1f)]
    public float altitudeCorrectionRange = 5f;


    [Header("Wander / Organic Motion")]
    [Tooltip("Adds slow random variation to flight direction.")]
    [Range(0f, 2f)]
    public float wanderWeight = 0.25f;

    [Tooltip("How quickly the random flight variation changes.")]
    [Min(0.001f)]
    public float wanderFrequency = 0.15f;

    [Tooltip("How much of the wander is allowed vertically.")]
    [Range(0f, 1f)]
    public float verticalWander = 0.3f;


    [Header("Emergency Containment")]
    [Tooltip("Birds beyond this distance receive an additional force back toward the center. 0 disables it.")]
    [Min(0f)]
    public float maxDistanceFromCenter = 50f;

    [Min(0f)]
    public float returnToCenterWeight = 4f;


    [Header("Banking")]
    [Tooltip("How strongly birds roll when changing direction.")]
    public float bankMultiplier = 0.7f;

    [Range(0f, 90f)]
    public float maxBankAngle = 35f;

    [Tooltip("How fast banking can change.")]
    [Min(1f)]
    public float bankSpeed = 90f;


    [Header("Animation")]
    [Tooltip("Random Animator.speed range assigned to each bird.")]
    public Vector2 animationSpeedRange = new Vector2(0.9f, 1.15f);

    [Tooltip("Disable root motion so animation doesn't move the bird independently.")]
    public bool disableRootMotion = true;


    [Header("Visual Variation")]
    public Vector2 randomScaleRange = new Vector2(0.9f, 1.1f);

    [Tooltip("Use this if the bird model doesn't face Unity's +Z direction.")]
    public Vector3 modelRotationOffsetEuler;


    [Header("Performance")]
    [Tooltip("Boid decisions per second. Movement still updates every frame.")]
    [Range(5f, 60f)]
    public float steeringUpdateRate = 20f;


    private class BirdState
    {
        public GameObject gameObject;
        public Transform transform;

        public Vector3 velocity;
        public Vector3 desiredVelocity;

        // -1..1 individual variations.
        public float radiusVariation;
        public float heightVariation;

        // 0..1 individual speed.
        public float speedVariation;

        public float wanderSeed;
        public float bankAngle;

        public Animator[] animators;
    }


    private readonly List<BirdState> birds = new List<BirdState>();

    private float steeringTimer;


    private void Start()
    {
        if (spawnOnStart)
            RespawnFlock();
    }


    private void Update()
    {
        CleanupMissingBirds();

        if (keepBirdCountSynced)
            SyncBirdCount();

        if (birds.Count == 0)
            return;

        float dt = Time.deltaTime;

        if (dt <= 0f)
            return;

        steeringTimer -= dt;

        if (steeringTimer <= 0f)
        {
            CalculateDesiredVelocities();

            steeringTimer = 1f / Mathf.Max(1f, steeringUpdateRate);
        }

        UpdateMovement(dt);
    }


    // ------------------------------------------------------------
    // SPAWNING
    // ------------------------------------------------------------

    [ContextMenu("Respawn Flock")]
    public void RespawnFlock()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Respawn Flock is intended to be used during Play Mode.");
            return;
        }

        ClearFlock();

        for (int i = 0; i < birdCount; i++)
        {
            if (!SpawnBird())
                break;
        }
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
        GameObject prefab = GetRandomPrefab();

        if (prefab == null)
            return false;

        BirdState bird = new BirdState();

        bird.radiusVariation = Random.Range(-1f, 1f);
        bird.heightVariation = Random.Range(-1f, 1f);
        bird.speedVariation = Random.value;
        bird.wanderSeed = Random.Range(0f, 10000f);

        Vector3 center = GetCenter();

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

        Vector3 flightDirection =
            (tangent + randomDirection).normalized;

        float speed = GetPreferredSpeed(bird);

        bird.velocity = flightDirection * speed;
        bird.desiredVelocity = bird.velocity;

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

        // Random visual scale.
        float minScale = Mathf.Min(randomScaleRange.x, randomScaleRange.y);
        float maxScale = Mathf.Max(randomScaleRange.x, randomScaleRange.y);

        float scale = Random.Range(minScale, maxScale);

        bird.transform.localScale *= scale;

        // Configure Animator(s).
        bird.animators = instance.GetComponentsInChildren<Animator>(true);

        float minAnimSpeed = Mathf.Min(
            animationSpeedRange.x,
            animationSpeedRange.y
        );

        float maxAnimSpeed = Mathf.Max(
            animationSpeedRange.x,
            animationSpeedRange.y
        );

        float animationSpeed = Random.Range(
            minAnimSpeed,
            maxAnimSpeed
        );

        foreach (Animator animator in bird.animators)
        {
            if (animator == null)
                continue;

            animator.speed = animationSpeed;

            if (disableRootMotion)
                animator.applyRootMotion = false;
        }

        birds.Add(bird);

        return true;
    }


    private GameObject GetRandomPrefab()
    {
        if (birdPrefabs == null || birdPrefabs.Count == 0)
            return null;

        // Try random selection first.
        for (int attempt = 0; attempt < 10; attempt++)
        {
            GameObject prefab =
                birdPrefabs[Random.Range(0, birdPrefabs.Count)];

            if (prefab != null)
                return prefab;
        }

        // Fallback in case the list contains many null entries.
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


    // ------------------------------------------------------------
    // BOIDS
    // ------------------------------------------------------------

    private void CalculateDesiredVelocities()
    {
        Vector3 center = GetCenter();

        float neighbourRadiusSqr =
            neighbourRadius * neighbourRadius;

        float separationRadiusSqr =
            separationRadius * separationRadius;


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


            // ----------------------------------------------------
            // Find neighbouring birds
            // ----------------------------------------------------

            for (int j = 0; j < birds.Count; j++)
            {
                if (i == j)
                    continue;

                BirdState other = birds[j];

                if (other.transform == null)
                    continue;

                Vector3 offset =
                    other.transform.position - position;

                float sqrDistance = offset.sqrMagnitude;

                if (sqrDistance > neighbourRadiusSqr)
                    continue;

                neighbours++;

                alignment += other.velocity;
                cohesionPosition += other.transform.position;


                // Separation
                if (sqrDistance < separationRadiusSqr)
                {
                    float distance =
                        Mathf.Sqrt(Mathf.Max(sqrDistance, 0.0001f));

                    Vector3 away = -offset / distance;

                    float strength =
                        1f - Mathf.Clamp01(
                            distance / separationRadius
                        );

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


                Vector3 averagePosition =
                    cohesionPosition / neighbours;

                Vector3 toAverage =
                    averagePosition - position;

                if (toAverage.sqrMagnitude > 0.001f)
                    cohesionDirection = toAverage.normalized;
            }


            if (separation.sqrMagnitude > 0.001f)
                separationDirection = separation.normalized;


            // ----------------------------------------------------
            // Orbit
            // ----------------------------------------------------

            Vector3 radial = position - center;
            radial.y = 0f;

            float radialDistance = radial.magnitude;

            Vector3 radialDirection;

            if (radialDistance > 0.001f)
            {
                radialDirection = radial / radialDistance;
            }
            else
            {
                radialDirection = Vector3.forward;
                radialDistance = 0f;
            }


            Vector3 orbitDirection =
                GetOrbitTangent(radialDirection);


            // ----------------------------------------------------
            // Radial correction
            // ----------------------------------------------------

            float preferredRadius =
                GetPreferredRadius(bird);

            float radiusError =
                radialDistance - preferredRadius;

            float radialCorrectionAmount =
                Mathf.Clamp(
                    radiusError / radialCorrectionRange,
                    -1f,
                    1f
                );

            // Positive error = too far => move inward.
            Vector3 radialCorrection =
                -radialDirection * radialCorrectionAmount;


            // ----------------------------------------------------
            // Altitude correction
            // ----------------------------------------------------

            float desiredY =
                center.y + GetPreferredHeight(bird);

            float altitudeError =
                desiredY - position.y;

            float altitudeCorrectionAmount =
                Mathf.Clamp(
                    altitudeError / altitudeCorrectionRange,
                    -1f,
                    1f
                );

            Vector3 altitudeCorrection =
                Vector3.up * altitudeCorrectionAmount;


            // ----------------------------------------------------
            // Wander
            // ----------------------------------------------------

            Vector3 wander =
                CalculateWander(bird);


            // ----------------------------------------------------
            // Combine behaviours
            // ----------------------------------------------------

            Vector3 desiredDirection = Vector3.zero;

            desiredDirection +=
                orbitDirection * orbitWeight;

            desiredDirection +=
                radialCorrection * radialCorrectionWeight;

            desiredDirection +=
                altitudeCorrection * altitudeWeight;

            desiredDirection +=
                separationDirection * separationWeight;

            desiredDirection +=
                alignmentDirection * alignmentWeight;

            desiredDirection +=
                cohesionDirection * cohesionWeight;

            desiredDirection +=
                wander * wanderWeight;


            // ----------------------------------------------------
            // Emergency containment
            // ----------------------------------------------------

            if (maxDistanceFromCenter > 0f)
            {
                Vector3 fromCenter =
                    position - center;

                float distance =
                    fromCenter.magnitude;

                if (distance > maxDistanceFromCenter)
                {
                    Vector3 towardCenter =
                        (-fromCenter).normalized;

                    float excess =
                        (distance - maxDistanceFromCenter) /
                        Mathf.Max(maxDistanceFromCenter, 0.01f);

                    float strength =
                        1f + excess;

                    desiredDirection +=
                        towardCenter *
                        returnToCenterWeight *
                        strength;
                }
            }


            if (desiredDirection.sqrMagnitude < 0.001f)
                desiredDirection = bird.velocity.normalized;


            float preferredSpeed =
                GetPreferredSpeed(bird);

            bird.desiredVelocity =
                desiredDirection.normalized *
                preferredSpeed;
        }
    }


    // ------------------------------------------------------------
    // MOVEMENT
    // ------------------------------------------------------------

    private void UpdateMovement(float dt)
    {
        for (int i = 0; i < birds.Count; i++)
        {
            BirdState bird = birds[i];

            if (bird.transform == null)
                continue;

            Vector3 oldVelocity = bird.velocity;


            if (oldVelocity.sqrMagnitude < 0.001f)
                oldVelocity = bird.desiredVelocity;


            // Turn and accelerate smoothly toward desired velocity.
            bird.velocity = Vector3.RotateTowards(
                oldVelocity,
                bird.desiredVelocity,
                maxTurnSpeed * Mathf.Deg2Rad * dt,
                maxAcceleration * dt
            );


            float speed = bird.velocity.magnitude;

            speed = Mathf.Clamp(
                speed,
                minSpeed,
                maxSpeed
            );

            if (bird.velocity.sqrMagnitude > 0.001f)
                bird.velocity =
                    bird.velocity.normalized * speed;


            // Move.
            bird.transform.position +=
                bird.velocity * dt;


            // ----------------------------------------------------
            // Banking
            // ----------------------------------------------------

            Vector3 currentFlat =
                new Vector3(
                    oldVelocity.x,
                    0f,
                    oldVelocity.z
                );

            Vector3 desiredFlat =
                new Vector3(
                    bird.desiredVelocity.x,
                    0f,
                    bird.desiredVelocity.z
                );


            float turnAngle = 0f;

            if (
                currentFlat.sqrMagnitude > 0.001f &&
                desiredFlat.sqrMagnitude > 0.001f
            )
            {
                turnAngle = Vector3.SignedAngle(
                    currentFlat,
                    desiredFlat,
                    Vector3.up
                );
            }


            float targetBank =
                Mathf.Clamp(
                    -turnAngle * bankMultiplier,
                    -maxBankAngle,
                    maxBankAngle
                );


            bird.bankAngle = Mathf.MoveTowards(
                bird.bankAngle,
                targetBank,
                bankSpeed * dt
            );


            // ----------------------------------------------------
            // Visual rotation
            // ----------------------------------------------------

            if (bird.velocity.sqrMagnitude > 0.001f)
            {
                Quaternion flightRotation =
                    Quaternion.LookRotation(
                        bird.velocity.normalized,
                        Vector3.up
                    );


                Quaternion bankRotation =
                    Quaternion.AngleAxis(
                        bird.bankAngle,
                        Vector3.forward
                    );


                Quaternion modelOffset =
                    Quaternion.Euler(
                        modelRotationOffsetEuler
                    );


                Quaternion targetRotation =
                    flightRotation *
                    bankRotation *
                    modelOffset;


                bird.transform.rotation =
                    Quaternion.RotateTowards(
                        bird.transform.rotation,
                        targetRotation,
                        visualRotationSpeed * dt
                    );
            }
        }
    }


    // ------------------------------------------------------------
    // HELPERS
    // ------------------------------------------------------------

    private Vector3 CalculateWander(BirdState bird)
    {
        float time =
            Time.time * wanderFrequency;

        float x =
            Mathf.PerlinNoise(
                bird.wanderSeed,
                time
            ) * 2f - 1f;

        float y =
            Mathf.PerlinNoise(
                bird.wanderSeed + 100f,
                time
            ) * 2f - 1f;

        float z =
            Mathf.PerlinNoise(
                bird.wanderSeed + 200f,
                time
            ) * 2f - 1f;


        Vector3 wander =
            new Vector3(
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
            bird.radiusVariation *
            orbitRadiusVariation
        );
    }


    private float GetPreferredHeight(BirdState bird)
    {
        return
            averageHeight +
            bird.heightVariation *
            heightVariation;
    }


    private float GetPreferredSpeed(BirdState bird)
    {
        float min = Mathf.Min(minSpeed, maxSpeed);
        float max = Mathf.Max(minSpeed, maxSpeed);

        return Mathf.Lerp(
            min,
            max,
            bird.speedVariation
        );
    }


    private Vector3 GetOrbitTangent(Vector3 radialDirection)
    {
        if (clockwise)
        {
            return Vector3.Cross(
                Vector3.up,
                radialDirection
            ).normalized;
        }

        return Vector3.Cross(
            radialDirection,
            Vector3.up
        ).normalized;
    }


    private Vector3 GetCenter()
    {
        return flockCenter != null
            ? flockCenter.position
            : transform.position;
    }


    // ------------------------------------------------------------
    // GIZMOS
    // ------------------------------------------------------------

    private void OnDrawGizmosSelected()
    {
        Vector3 center = GetCenter();

        Vector3 middle =
            center + Vector3.up * averageHeight;

        DrawCircle(
            middle,
            orbitRadius,
            64
        );

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

        Gizmos.DrawWireSphere(
            center,
            0.5f
        );
    }


    private void DrawCircle(
        Vector3 center,
        float radius,
        int segments
    )
    {
        if (radius <= 0f)
            return;

        Vector3 previous =
            center +
            new Vector3(radius, 0f, 0f);

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

            Gizmos.DrawLine(
                previous,
                next
            );

            previous = next;
        }
    }
}