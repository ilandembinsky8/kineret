using System;
using UnityEngine;

/// <summary>
/// Sits on the root of the bird flock challenge prefab, next to a kinematic Rigidbody.
/// Birds spawned by BirdFlockManager under this hierarchy attach their trigger colliders
/// to that Rigidbody, so player contact with any bird surfaces here as a single event.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class BirdFlockChallengeVisual : MonoBehaviour
{
    public event Action OnPlayerHit;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) { return; }

        OnPlayerHit?.Invoke();
    }
}
