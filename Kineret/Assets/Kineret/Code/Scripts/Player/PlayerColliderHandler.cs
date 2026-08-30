using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerColliderHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovementHandler playerMovementHandler;
    [SerializeField] private SphereCollider playerCollider;

    //NOTE - these should be set as properties in the PlayerMovementHandler script, but for now they are serialized fields here for simplicity because I don't wanna touch anything there
    [SerializeField] private Transform pitchBody;
    [SerializeField] private Transform yawBody;
    [SerializeField] private Transform rollBody;

    [Header("Settings")]
    [SerializeField] private int maxCollisionsBeforeReset = 3;
    [SerializeField] private float resetDelay = 1f;
    [SerializeField] private float restartGameDelay = 2f;

    //private variables
    private const string TERRAIN_TAG = "Terrain";

    private Vector3 _startPosition;
    private Quaternion _startYawRotation;
    private bool _resetSettingsInitialized;

    private int _amountOfCollisions = 0;

    private void Start()
    {
        InitializeResetSettings();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(TERRAIN_TAG))
        {
            HandleTerrainCollision();
        }
    }

    private void HandleTerrainCollision()
    {
        _amountOfCollisions++;
        Debug.Log("Player collided with terrain. Total collisions: " + _amountOfCollisions);
        if (_amountOfCollisions >= maxCollisionsBeforeReset)
        {
            //first we probably need UI popup here so the player can know he's finished his attempts, so now he's being reset to the main menu or similar

            //after that UI, we can have a delay here, which will be set by a enumerator method
            StartCoroutine(LoseGameRoutine());
            _amountOfCollisions = 0;
        }
        else
        {
            ResetPlayerPosition();
        }
    }

    private void ResetPlayerPosition()
    {
        if (!_resetSettingsInitialized)
        {
            Debug.LogWarning("Reset settings were not initialized.");
            return;
        }

        StartCoroutine(DisableCollider());

        playerMovementHandler.transform.SetPositionAndRotation(_startPosition, Quaternion.identity);
        pitchBody.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        yawBody.transform.SetLocalPositionAndRotation(Vector3.zero, _startYawRotation);
        rollBody.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

        Debug.Log("Player position and rotation reset.");
    }

    private void InitializeResetSettings()
    {
        _startPosition = playerMovementHandler.transform.position;
        _startYawRotation = yawBody.transform.rotation;

        _amountOfCollisions = 0;
        _resetSettingsInitialized = true;
    }

    private IEnumerator DisableCollider()
    {
        EventsRelay.OnGamePause?.Invoke(true);
        playerCollider.enabled = false;
        //we can add some UI feedback here to let the player know he's been reset, and also how many attempts he has left maybe
        yield return new WaitForSeconds(resetDelay);
        EventsRelay.OnGamePause?.Invoke(false);
        playerCollider.enabled = true;
    }

    private IEnumerator LoseGameRoutine()
    {
        EventsRelay.OnGamePause?.Invoke(true);
        yield return new WaitForSeconds(restartGameDelay);
        EventsRelay.OnGamePause?.Invoke(false);
        yield return null; //extra frame for the scene to load properly, just in case
        SceneManager.LoadScene("Main Menu Scene");
    }
}
