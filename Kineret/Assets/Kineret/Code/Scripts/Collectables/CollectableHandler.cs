using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class CollectableHandler : MonoBehaviour
{
    [SerializeField] protected PopupDataEventChannel LoadPopup_EC;
    [SerializeField] private Vector3EventChannel playerMoved_EC;
    [SerializeField] protected CollectableData collectableData;

    [SerializeField] private PopupData notificationPopupData;
    [SerializeField] protected PopupData collectPopupData;

    [SerializeField] private GameObject visuals;

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
        if (collectableData == null) return;

        Handles.color = Color.red;
        Handles.DrawWireDisc(transform.position,new Vector3(0f,1f,0f), collectableData.notificationRange);

        Handles.color = Color.green;
        Handles.DrawWireDisc(transform.position, new Vector3(0f, 1f, 0f), collectableData.collectionRange);
    }

    private void HandlePlayerMoved(Vector3 playerPosition)
    {
        Vector3 playerPositionXZ = new Vector3(playerPosition.x, 0f, playerPosition.z);
        Vector3 collectablePositionXZ = new Vector3(transform.position.x, 0f, transform.position.z);

        Vector3 delta = playerPositionXZ - collectablePositionXZ;

        CheckNotifyRange(delta);
        CheckCollectRange(delta);          
    }
    protected virtual void CheckNotifyRange(Vector3 delta)
    {
        if (delta.sqrMagnitude <= collectableData.notificationRange * collectableData.notificationRange)
        {
            Notify();
        }
    }
    protected virtual void CheckCollectRange(Vector3 delta)
    {
        _isCollectable = false;
        if (delta.sqrMagnitude <= collectableData.collectionRange * collectableData.collectionRange)
        {
            _isCollectable = true;
        }
    }

    private void HandleCollectInput(InputAction.CallbackContext context)
    {
        if(_isCollectable) Collect();
    }

    private void Notify()
    {
        if (_hasNotified) return;

        LoadPopup_EC.RaiseEvent(notificationPopupData);
        _hasNotified = true;
    }

    protected virtual void Collect()
    {
        if (_wasCollected) return;
        visuals.gameObject.SetActive(false);
        LoadPopup_EC.RaiseEvent(collectPopupData);
        _wasCollected = true;
        OnDisable();
    }
}
