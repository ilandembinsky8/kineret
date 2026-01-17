using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class CollectableHandler : MonoBehaviour
{
    [SerializeField] private Vector3EventChannel playerMoved_EC;
    [SerializeField] private CollectableData data;

    private bool _hasNotified;
    private bool _wasCollected;
    private bool _isCollectable;

    private InputActions _actions;

    private void Awake()
    {
        _actions = new InputActions();
    }

    private void OnEnable()
    {
        _actions.Player.Enable();
        _actions.Player.Collect.performed += HandleCollectInput;
        playerMoved_EC.OnEventRaised += HandlePlayerMoved;
    }

    private void OnDisable()
    {
        _actions.Player.Disable();
        _actions.Player.Collect.performed -= HandleCollectInput;
        playerMoved_EC.OnEventRaised -= HandlePlayerMoved;
    }

    private void OnDrawGizmos()
    {
        if (data == null) return;

        Handles.color = Color.red;
        Handles.DrawWireDisc(transform.position,new Vector3(0f,1f,0f), data.notificationRange);

        Handles.color = Color.green;
        Handles.DrawWireDisc(transform.position, new Vector3(0f, 1f, 0f), data.collectionRange);
    }

    private void HandlePlayerMoved(Vector3 playerPosition)
    {
        Vector3 playerPositionXZ = new Vector3(playerPosition.x, 0f, playerPosition.z);
        Vector3 collectablePositionXZ = new Vector3(transform.position.x, 0f, transform.position.z);

        Vector3 delta = playerPositionXZ - collectablePositionXZ;

        if(delta.sqrMagnitude <= data.notificationRange * data.notificationRange)
        {
            Notify();
        }

        _isCollectable = false;
        if (delta.sqrMagnitude <= data.collectionRange * data.collectionRange)
        {
            _isCollectable = true;
        }       
    }

    private void HandleCollectInput(InputAction.CallbackContext context)
    {
        Debug.Log("Collect attempt");
        if(_isCollectable) Collect();
    }

    private void Notify()
    {
        if (_hasNotified) return;

        _hasNotified = true;
        Debug.Log("Notification");
    }

    private void Collect()
    {
        if (_wasCollected) return;
        _wasCollected = true;
        Debug.Log("Collected");
        OnDisable();
    }
}
